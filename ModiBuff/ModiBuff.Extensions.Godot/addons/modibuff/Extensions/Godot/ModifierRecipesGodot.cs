using System;
using System.Collections.Generic;
using Godot;
using ModiBuff.Core;
using ModiBuff.Core.Units;

namespace ModiBuff.Extensions.Godot
{
	public abstract class ModifierRecipesGodot : ModifierRecipes
	{
		private const string DefaultPath = "res://resources/modibuff_recipes/";
		private const string RemapSuffix = ".remap";

		private readonly string _path;
		private readonly ModifierIdManager _idManager;
		private readonly Dictionary<int, ModifierRecipeResource> _recipeResources;

		public ModifierRecipesGodot(ModifierIdManager idManager, EffectTypeIdManager effectTypeIdManager,
			string path = DefaultPath) : base(idManager, effectTypeIdManager)
		{
			_path = path.EndsWith("/") ? path : path + "/";
			_idManager = idManager;
			_recipeResources = new Dictionary<int, ModifierRecipeResource>();
		}

		/// <summary>
		///		Creates the apply checks that were set up on the recipe resource of the modifier, null if it has none.
		///		Apply checks are owned by appliers, feed them to the unit when adding the applier,
		///		ex. <see cref="IModifierApplierOwner.AddApplierModifierNew"/>.
		/// </summary>
		/// <remarks> Returns new check instances on every call, checks like cooldown have state per applier. </remarks>
		public ICheck[]? CreateApplyChecks(int modifierId)
		{
			return _recipeResources.TryGetValue(modifierId, out var recipeResource)
				? recipeResource.CreateApplyChecks(_idManager)
				: null;
		}

		/// <inheritdoc cref="CreateApplyChecks(int)"/>
		public ICheck[]? CreateApplyChecks(string modifierName)
		{
			int? modifierId = _idManager.GetId(modifierName);
			return modifierId != null ? CreateApplyChecks(modifierId.Value) : null;
		}

		protected override void SetupRecipes()
		{
			SetupResourceRecipes();
		}

		private void SetupResourceRecipes()
		{
			if (!DirAccess.DirExistsAbsolute(_path))
			{
				GD.PushError($"ModiBuff recipe resource directory {_path} doesn't exist");
				return;
			}

			var resources = new List<IModifierRecipeResource>();

			foreach (string listedFile in DirAccess.GetFilesAt(_path))
			{
				//Exported projects list converted resources as "name.tres.remap", they're loaded by their original name
				string file = listedFile.EndsWith(RemapSuffix)
					? listedFile.Substring(0, listedFile.Length - RemapSuffix.Length)
					: listedFile;

				if (!file.EndsWith(".tres") && !file.EndsWith(".res"))
					continue;

				var resource = ResourceLoader.Load(_path + file);
				if (resource == null)
					continue;

				if (!(resource is IModifierRecipeResource modifierRecipeResource))
				{
					GD.PushWarning($"Resource {_path + file} is not a modifier recipe resource, skipping...");
					continue;
				}

				if (string.IsNullOrEmpty(modifierRecipeResource.Name))
					modifierRecipeResource.SetName(file.Substring(0, file.LastIndexOf('.')));

				resources.Add(modifierRecipeResource);
			}

			//All recipes are registered before any of them is set up, so effects and conditions
			//can refer to other modifiers by name, no matter in which order the files get loaded
			var recipes = new List<(ModifierRecipeResource Resource, ModifierRecipe Recipe)>();
			var auraRecipes = new List<(AuraModifierRecipeResource Resource, ModifierRecipe Recipe,
				List<(AuraEffectModifierRecipeResource Resource, ModifierRecipe Recipe)> AuraEffects)>();

			foreach (var recipeResource in resources)
			{
				switch (recipeResource)
				{
					case ModifierRecipeResource modifierRecipeResource:
						if (!modifierRecipeResource.Validate())
						{
							GD.PushError($"Recipe {modifierRecipeResource.Name} is invalid, skipping...");
							break;
						}

						recipes.Add((modifierRecipeResource, Add(modifierRecipeResource.Name)));
						break;
					case AuraModifierRecipeResource auraModifierRecipeResource:
						if (!auraModifierRecipeResource.Validate())
						{
							GD.PushError($"Aura Recipe {auraModifierRecipeResource.Name} is invalid, skipping...");
							break;
						}

						var auraRecipe = Add(auraModifierRecipeResource.Name);
						var auraEffects = new List<(AuraEffectModifierRecipeResource, ModifierRecipe)>();
						if (auraModifierRecipeResource.AuraEffectResources != null)
							foreach (var auraEffectResource in auraModifierRecipeResource.AuraEffectResources)
								auraEffects.Add((auraEffectResource, Add(auraEffectResource.Name)));

						auraRecipes.Add((auraModifierRecipeResource, auraRecipe, auraEffects));
						break;
					default:
						GD.PushError($"Resource {recipeResource} is not a valid recipe resource");
						break;
				}
			}

			foreach ((var recipeResource, var recipe) in recipes)
				SetupResource(recipeResource, recipe);

			foreach ((var recipeResource, var recipe, var auraEffects) in auraRecipes)
				SetupAuraResource(recipeResource, recipe, auraEffects);
		}

		private void SetupResource(ModifierRecipeResource recipeResource, ModifierRecipe recipe)
		{
			Save(recipe, recipeResource);

			//---ApplyConditions---

			//Apply checks are owned by appliers, not by recipes. See CreateApplyChecks
			_recipeResources[recipe.Id] = recipeResource;

			//---EffectConditions---

			if (recipeResource.LegalActionEffectCondition != LegalAction.None)
				recipe.EffectCondition(recipeResource.LegalActionEffectCondition);

			var effectCondition = recipeResource.EffectCondition.GetModiBuffConditionType();
			if (effectCondition != null)
				recipe.EffectCondition(effectCondition.Value);

			if (recipeResource.StatEffectCondition != null)
				recipe.EffectCondition(recipeResource.StatEffectCondition.StatType,
					recipeResource.StatEffectCondition.Value,
					recipeResource.StatEffectCondition.ComparisonType);

			if (recipeResource.StatusEffectEffectCondition != StatusEffectType.None)
				recipe.EffectCondition(recipeResource.StatusEffectEffectCondition);

			if (!string.IsNullOrEmpty(recipeResource.ModifierNameEffectCondition))
				recipe.EffectCondition(recipeResource.ModifierNameEffectCondition);

			if (recipeResource.EffectCooldown > 0)
				recipe.EffectCooldown(recipeResource.EffectCooldown);

			if (recipeResource.EffectCost != null)
				recipe.EffectCost(recipeResource.EffectCost.CostType, recipeResource.EffectCost.Amount);

			if (recipeResource.EffectChance > 0)
				recipe.EffectChance(recipeResource.EffectChance);

			//---Actions---

			if (recipeResource.Interval > 0)
				recipe.Interval(recipeResource.Interval);

			if (recipeResource.Duration > 0)
				recipe.Duration(recipeResource.Duration);

			if (recipeResource.RemoveDuration > 0)
				recipe.Remove(recipeResource.RemoveDuration);

			if (recipeResource.RefreshType != RefreshType.Invalid)
				recipe.Refresh(recipeResource.RefreshType.GetModiBuffRefreshType());

			if (recipeResource.StackResource != null)
				recipe.Stack(recipeResource.StackResource.WhenStackEffect, recipeResource.StackResource.MaxStacks,
					recipeResource.StackResource.EveryXStacks);

			//---Effects---

			if (recipeResource.EffectResources != null)
				foreach (var effectResource in recipeResource.EffectResources)
					recipe.Effect(effectResource.GetEffect(), effectResource.EffectOn);

			recipeResource.Reset();

			//GD.Print($"Loaded recipe {recipeResource.Name}");
		}

		private void SetupAuraResource(AuraModifierRecipeResource recipeResource, ModifierRecipe recipe,
			List<(AuraEffectModifierRecipeResource Resource, ModifierRecipe Recipe)> auraEffects)
		{
			//---Actions---

			if (recipeResource.Interval > 0)
				recipe.Interval(recipeResource.Interval);

			//---Effects---

			foreach ((var auraEffectResource, var auraEffectRecipe) in auraEffects)
			{
				auraEffectResource.SetId(auraEffectRecipe.Id);
				if (auraEffectResource.NeedsSaving)
					recipeResource.SetChanged();

				//---Actions---

				if (auraEffectResource.RemoveDuration > 0)
					auraEffectRecipe.Remove(auraEffectResource.RemoveDuration).Refresh();

				//---Effects---

				if (auraEffectResource.EffectResources != null)
					foreach (var effectResource in auraEffectResource.EffectResources)
						auraEffectRecipe.Effect(effectResource.GetEffect(), effectResource.EffectOn);
			}

			if (recipeResource.EffectResources != null)
				foreach (var effectResource in recipeResource.EffectResources)
					recipe.Effect(effectResource.GetEffect(), EffectOn.Interval);

			Save(recipe, recipeResource);
		}

		private static void Save<T>(ModifierRecipe recipe, T recipeResource) where T : BaseModifierRecipeResource
		{
			recipeResource.SetId(recipe.Id);
			if (!recipeResource.NeedsSaving)
				return;

			//The generated name and id are written back so they show up in the inspector.
			//Only possible when running from the editor, res:// is read-only in exported projects
			if (OS.HasFeature("editor"))
				ResourceSaver.Save(recipeResource);
		}
	}
}

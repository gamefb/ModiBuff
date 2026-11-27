using System;
using System.Collections.Generic;
using ModiBuff.Core;
using ModiBuff.Core.Units;
using CoreTagType = ModiBuff.Core.TagType;
using UnitsTagType = ModiBuff.Core.Units.TagType;

namespace ModiBuff.Extensions.Godot.Samples
{
	/// <summary>
	///		All ModiBuff logic of the test level: recipes loaded from resources, two units, and the player abilities.
	///		Doesn't touch any nodes, so the level scene and the self test drive it in the same way.
	/// </summary>
	public sealed class TestLevelGame
	{
		public const string RecipesPath = "res://test_level/recipes/";

		/// <summary>
		///		Seconds between enemy attacks.
		/// </summary>
		public const float EnemyAttackInterval = 2f;

		/// <summary>
		///		Player abilities, every name is a recipe resource in <see cref="RecipesPath"/>.
		/// </summary>
		public static readonly string[] Abilities = { "Fireball", "Poison", "Blight", "Stun", "Heal", "Focus" };

		public Unit Player { get; }
		public Unit Enemy { get; }

		/// <summary>
		///		Game messages, ex. for a log.
		/// </summary>
		public event Action<string>? Logged;

		private readonly ModifierIdManager _idManager;
		private readonly EffectTypeIdManager _effectTypeIdManager;
		private readonly Recipes _recipes;
		private readonly ModifierPool _pool;
		private readonly ModifierControllerPool _controllerPool;

		private float _enemyAttackTimer;

		public TestLevelGame()
		{
			//Units need the default tag to be the Units one, otherwise no target is a legal cast target
			Config.DefaultTag = (ulong)UnitsTagType.Default;

			_effectTypeIdManager = new EffectTypeIdManager();
			_effectTypeIdManager.RegisterAllEffectTypesInAssemblies();
			_idManager = new ModifierIdManager();

			//Loads every recipe resource in the path, when the generators get created
			_recipes = new Recipes(_idManager, _effectTypeIdManager, RecipesPath);
			_recipes.CreateGenerators();
			_pool = new ModifierPool(_recipes);
			_controllerPool = new ModifierControllerPool();

			Player = new Unit(health: 300, damage: 10, healValue: 5, mana: 300, unitType: UnitType.Good);
			Enemy = new Unit(health: 600, damage: 20, healValue: 5, mana: 0, unitType: UnitType.Bad);

			//Apply checks (cooldown, cost, conditions) are set up on the recipe resources,
			//but they're owned by the applier, so they get created here and fed to the unit
			foreach (string ability in Abilities)
			{
				int id = GetId(ability);
				Player.AddApplierModifierNew(id, ApplierType.Cast, _recipes.CreateApplyChecks(id));
			}
		}

		public bool HasRecipe(string name) => _idManager.GetId(name) != null;

		public int GetId(string name)
		{
			return _idManager.GetId(name) ??
			       throw new InvalidOperationException($"No modifier recipe named {name} in {RecipesPath}");
		}

		public static bool IsSelfTargeted(string ability) => ability == "Heal" || ability == "Focus";

		/// <summary>
		///		Player tries to cast the ability, fails if any of the apply checks of the recipe resource fail.
		/// </summary>
		public bool Cast(string ability)
		{
			if (Player.IsDead)
				return false;

			var target = IsSelfTargeted(ability) ? Player : Enemy;
			bool success = Player.TryCast(GetId(ability), target);
			Log(success
				? $"Player casts {ability}"
				: $"Player can't cast {ability} (cooldown, cost or condition)");
			return success;
		}

		public float PlayerAttack()
		{
			if (Player.IsDead || Enemy.IsDead)
				return 0;

			float damage = Player.Attack(Enemy);
			Log($"Player attacks for {damage:0.#}");
			return damage;
		}

		public void Update(float delta)
		{
			Player.Update(delta);
			Enemy.Update(delta);

			if (Player.IsDead || Enemy.IsDead)
				return;

			_enemyAttackTimer += delta;
			while (_enemyAttackTimer >= EnemyAttackInterval)
			{
				_enemyAttackTimer -= EnemyAttackInterval;

				//Attack does nothing if the unit can't act, ex. when stunned
				if (Enemy.StatusEffectController.HasLegalAction(LegalAction.Act))
					Log($"Enemy attacks for {Enemy.Attack(Player):0.#}");
				else
					Log("Enemy can't attack");
			}
		}

		/// <summary>
		///		One line for every modifier on the unit, with the time left and stacks if it has those.
		/// </summary>
		public List<string> DescribeModifiers(Unit unit)
		{
			var lines = new List<string>();

			foreach (var reference in unit.ModifierController.GetModifierReferences())
			{
				string line = _recipes.GetModifierInfo(reference.Id)?.InternalName ?? $"Modifier {reference.Id}";

				var data = reference.GenId != null
					? unit.ModifierController.GetModifierDataReference(reference.Id, reference.GenId.Value)
					: null;
				if (data != null)
				{
					var timers = data.GetTimers();
					for (int i = 0; i < timers?.Length; i++)
						if (timers[i] is DurationComponent duration)
							line += $" {Math.Max(0f, duration.Time - duration.Timer):0.0}s";

					//Asking a modifier without stacks for its stack reference is logged as an error
					if (ModifierRecipes.GetTag(reference.Id).HasTag(CoreTagType.IsStack))
						line += $" x{data.GetStackReference()?.Stacks}";
				}

				lines.Add(line);
			}

			return lines;
		}

		/// <summary>
		///		ModiBuff keeps its pools and id managers in static instances, they have to be reset
		///		before a new game can be created (ex. when the scene gets reloaded).
		/// </summary>
		public void Shutdown()
		{
			_pool.Reset();
			_controllerPool.Reset();
			_idManager.Reset();
			_effectTypeIdManager.Reset();
		}

		private void Log(string message) => Logged?.Invoke(message);

		private sealed class Recipes : ModifierRecipesGodot
		{
			public Recipes(ModifierIdManager idManager, EffectTypeIdManager effectTypeIdManager, string path)
				: base(idManager, effectTypeIdManager, path)
			{
			}
		}
	}
}

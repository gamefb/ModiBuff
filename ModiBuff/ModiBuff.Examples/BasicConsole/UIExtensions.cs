using System.Linq;
using ModiBuff.Core;
using ModiBuff.Core.Units;
using ModiBuff.Core.Units.Interfaces.NonGeneric;
using IDamagable = ModiBuff.Core.Units.Interfaces.NonGeneric.IDamagable;

namespace ModiBuff.Examples.BasicConsole
{
	public static class UIExtensions
	{
		public static void PrintStateAndModifiers(this IModifierApplierOwner owner, IModifierRecipes modifierRecipes)
		{
			var modifierController = ((IModifierOwner)owner).ModifierController;
			//Stats, ApplyModifiers, Normal modifiers.
			var damagable = (IDamagable)owner;
			var attacker = (IAttacker)owner;
			Console.GameMessage($"Player stats: {damagable.Health}/{damagable.MaxHealth} HP, " +
			                    $"{attacker.Damage} Damage");
			//Appliers
			//Name, description, checks, (states, like cooldown)
			var unit = (Unit)owner;
			PrintAppliers(unit, ApplierType.Attack, "attack", modifierRecipes);
			PrintAppliers(unit, ApplierType.Cast, "cast", modifierRecipes);

			//Normal modifiers
			var references = modifierController.GetModifierReferences();
			if (references != null && references.Length > 0)
			{
				Console.GameMessage("Player modifiers:");
				for (int i = 0; i < references.Length; i++)
				{
					var reference = references[i];
					var modifierInfo = modifierRecipes.GetModifierInfo(reference.Id);
					Console.GameMessage($"{i + 1} - {modifierInfo.DisplayName} - {modifierInfo.Description}");
					var modifierDataReference =
						modifierController.GetModifierDataReference(reference.Id, reference.GenId.Value);
					var timers = modifierDataReference.GetTimers();
					for (int j = 0; j < timers?.Length; j++)
					{
						switch (timers[j])
						{
							case IntervalComponent intervalComponent:
								Console.GameMessage(
									$"Interval timer: {intervalComponent.Timer}/{intervalComponent.Time}");
								break;
							case DurationComponent durationComponent:
								Console.GameMessage(
									$"Duration timer: {durationComponent.Timer}/{durationComponent.Time}");
								break;
						}
					}

					var stackReference = modifierDataReference.GetStackReference();
					if (stackReference != null)
						Console.GameMessage($"Stacks: {stackReference.Stacks}/{stackReference.MaxStacks}");
				}
			}
		}

		private static void PrintAppliers(Unit unit, ApplierType applierType, string applierName,
			IModifierRecipes modifierRecipes)
		{
			var appliers = unit.GetAppliers(applierType).ToArray();
			if (appliers.Length == 0)
				return;

			Console.GameMessage($"Player {applierName} appliers:");
			for (int i = 0; i < appliers.Length; i++)
			{
				var modifierInfo = modifierRecipes.GetModifierInfo(appliers[i].Id);
				Console.GameMessage($"{i + 1} - {modifierInfo.DisplayName} - {modifierInfo.Description}");

				//Checks are owned by the applier, most checks are without state
				var checks = appliers[i].Checks;
				for (int j = 0; j < checks?.Length; j++)
				{
					switch (checks[j])
					{
						case CostCheck costCheck:
							var data = costCheck.GetData();
							Console.GameMessage($"Cost: {data.Cost} {data.CostType}");
							break;
						case CooldownCheck cooldownCheck:
							var stateData = cooldownCheck.GetData();
							Console.GameMessage($"Cooldown: {stateData.Timer}/{stateData.Cooldown}");
							break;
						case ChanceCheck chanceCheck:
							Console.GameMessage($"Chance: {chanceCheck.GetData() * 100f}%");
							break;
					}
				}
			}
		}
	}
}
using System.Collections.Generic;
using Godot;
using ModiBuff.Core.Units;

namespace ModiBuff.Extensions.Godot.Samples
{
	/// <summary>
	///		Shows the state of one unit: health, mana, damage, status effects and modifiers.
	/// </summary>
	public partial class UnitPanel : PanelContainer
	{
		private Label _name = null!;
		private ColorRect _avatar = null!;
		private ProgressBar _health = null!;
		private Label _healthText = null!;
		private ProgressBar _mana = null!;
		private Label _manaText = null!;
		private Label _stats = null!;
		private Label _modifiers = null!;

		public override void _Ready()
		{
			_name = GetNode<Label>("%Name");
			_avatar = GetNode<ColorRect>("%Avatar");
			_health = GetNode<ProgressBar>("%Health");
			_healthText = GetNode<Label>("%HealthText");
			_mana = GetNode<ProgressBar>("%Mana");
			_manaText = GetNode<Label>("%ManaText");
			_stats = GetNode<Label>("%Stats");
			_modifiers = GetNode<Label>("%Modifiers");
		}

		public void Setup(string unitName, Color color)
		{
			_name.Text = unitName;
			_avatar.Color = color;
		}

		public void Refresh(Unit unit, List<string> modifiers)
		{
			_health.MaxValue = unit.MaxHealth;
			_health.Value = Mathf.Max(unit.Health, 0f);
			_healthText.Text = $"Health {Mathf.Max(unit.Health, 0f):0}/{unit.MaxHealth:0}";

			bool hasMana = unit.MaxMana > 0;
			_mana.Visible = hasMana;
			_manaText.Visible = hasMana;
			if (hasMana)
			{
				_mana.MaxValue = unit.MaxMana;
				_mana.Value = unit.Mana;
				_manaText.Text = $"Mana {unit.Mana:0}/{unit.MaxMana:0}";
			}

			string status = "";
			if (unit.IsDead)
				status = "  -  Defeated";
			else if (unit.StatusEffectController.HasStatusEffect(StatusEffectType.Stun))
				status = "  -  Stunned";
			_stats.Text = $"Damage {unit.Damage:0.#}{status}";

			_modifiers.Text = modifiers.Count > 0 ? string.Join("\n", modifiers) : "No modifiers";
		}
	}
}

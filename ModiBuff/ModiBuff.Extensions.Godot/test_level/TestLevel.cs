using System;
using Godot;

namespace ModiBuff.Extensions.Godot.Samples
{
	/// <summary>
	///		Test level for the Godot extension. A player and an enemy, the player abilities are modifier recipes
	///		that are set up as resources in res://test_level/recipes/.
	/// </summary>
	public partial class TestLevel : Control
	{
		private TestLevelGame _game = null!;

		private UnitPanel _playerPanel = null!;
		private UnitPanel _enemyPanel = null!;
		private Label _result = null!;
		private RichTextLabel _log = null!;

		public override void _Ready()
		{
			ModiBuff.Core.Logger.SetLogger(new TestLevelLogger());

			_playerPanel = GetNode<UnitPanel>("%PlayerPanel");
			_enemyPanel = GetNode<UnitPanel>("%EnemyPanel");
			_result = GetNode<Label>("%Result");
			_log = GetNode<RichTextLabel>("%Log");

			_playerPanel.Setup("Player", new Color(0.35f, 0.65f, 0.95f));
			_enemyPanel.Setup("Enemy", new Color(0.9f, 0.4f, 0.35f));

			_game = new TestLevelGame();
			_game.Logged += OnLogged;

			var buttons = GetNode<HBoxContainer>("%Buttons");
			for (int i = 0; i < TestLevelGame.Abilities.Length; i++)
			{
				string ability = TestLevelGame.Abilities[i];
				AddButton(buttons, $"{i + 1}  {ability}", () => _game.Cast(ability));
			}

			AddButton(buttons, "Space  Attack", () => _game.PlayerAttack());
			AddButton(buttons, "R  Restart", Restart);

			OnLogged($"Loaded {TestLevelGame.Abilities.Length} recipes from {TestLevelGame.RecipesPath}");
		}

		public override void _Process(double delta)
		{
			_game.Update((float)delta);

			_playerPanel.Refresh(_game.Player, _game.DescribeModifiers(_game.Player));
			_enemyPanel.Refresh(_game.Enemy, _game.DescribeModifiers(_game.Enemy));

			if (_game.Enemy.IsDead)
				_result.Text = "Enemy defeated. Press R to restart.";
			else if (_game.Player.IsDead)
				_result.Text = "Player defeated. Press R to restart.";
			else
				_result.Text = "";
		}

		public override void _UnhandledKeyInput(InputEvent @event)
		{
			if (!(@event is InputEventKey { Pressed: true, Echo: false } key))
				return;

			int abilityIndex = (int)(key.Keycode - Key.Key1);
			if (abilityIndex >= 0 && abilityIndex < TestLevelGame.Abilities.Length)
				_game.Cast(TestLevelGame.Abilities[abilityIndex]);
			else if (key.Keycode == Key.Space)
				_game.PlayerAttack();
			else if (key.Keycode == Key.R)
				Restart();
		}

		public override void _ExitTree()
		{
			_game.Logged -= OnLogged;
			_game.Shutdown();
		}

		private void Restart() => GetTree().ReloadCurrentScene();

		private void OnLogged(string message) => _log.AppendText(message + "\n");

		private static void AddButton(Container parent, string text, Action pressed)
		{
			var button = new Button
			{
				Text = text,
				//Keeps Space (attack) from pressing the last clicked button
				FocusMode = FocusModeEnum.None,
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
			};
			button.Pressed += pressed;
			parent.AddChild(button);
		}
	}
}

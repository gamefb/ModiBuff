using System;
using Godot;
using ModiBuff.Core.Units;

namespace ModiBuff.Extensions.Godot.Samples
{
	/// <summary>
	///		Runs the test level without input and checks the results, then quits with exit code 0 (all passed) or 1.
	///		<code>godot --headless --path . res://test_level/self_test.tscn</code>
	/// </summary>
	public partial class SelfTest : Node
	{
		//Exactly representable as a float, so timers hit their intervals and durations without rounding drift
		private const float Step = 0.25f;

		private int _passed;
		private int _failed;
		private bool _currentFailed;

		public override void _Ready()
		{
			var logger = new TestLevelLogger();
			ModiBuff.Core.Logger.SetLogger(logger);

			Test("Recipes load from resources", game =>
			{
				foreach (string ability in TestLevelGame.Abilities)
					Check(game.HasRecipe(ability), $"recipe {ability} exists");
			});

			Test("Fireball: init damage, mana cost and cooldown from apply checks", game =>
			{
				Check(game.Cast("Fireball"), "first cast");
				Expect(540, game.Enemy.Health, "enemy health after first cast");
				Expect(200, game.Player.Mana, "mana after first cast");

				Check(!game.Cast("Fireball"), "cast on cooldown is refused");
				Expect(540, game.Enemy.Health, "enemy health after refused cast");
				Expect(200, game.Player.Mana, "mana after refused cast");

				Advance(game, 1.75f);
				Check(game.Cast("Fireball"), "cast after cooldown");
				Advance(game, 1.75f);
				Check(game.Cast("Fireball"), "third cast");
				Expect(420, game.Enemy.Health, "enemy health after three casts");
				Expect(0, game.Player.Mana, "mana after three casts");

				Advance(game, 1.75f);
				Check(!game.Cast("Fireball"), "cast without mana is refused");
				Expect(420, game.Enemy.Health, "enemy health after cast without mana");
			});

			Test("Poison: interval damage, removed after its duration", game =>
			{
				Check(game.Cast("Poison"), "cast");
				Expect(600, game.Enemy.Health, "no damage on cast");
				Check(game.Enemy.ModifierController.Contains(game.GetId("Poison")), "enemy has the modifier");

				Advance(game, 4.5f);
				Expect(600 - 4 * 8, game.Enemy.Health, "four ticks in four seconds");
				Check(!game.Enemy.ModifierController.Contains(game.GetId("Poison")), "modifier got removed");

				Advance(game, 2f);
				Expect(600 - 4 * 8, game.Enemy.Health, "no damage after removal");
			});

			Test("Poison: casting again refreshes the duration", game =>
			{
				Check(game.Cast("Poison"), "cast");
				Advance(game, 2f);
				Check(game.Cast("Poison"), "second cast");
				Advance(game, 4.5f);
				Expect(600 - 6 * 8, game.Enemy.Health, "six ticks, duration restarted at two seconds");
				Check(!game.Enemy.ModifierController.Contains(game.GetId("Poison")), "modifier got removed");
			});

			Test("Blight: init damage, and applies Poison by name", game =>
			{
				Check(game.Cast("Blight"), "cast");
				Expect(590, game.Enemy.Health, "init damage");
				Check(game.Enemy.ModifierController.Contains(game.GetId("Poison")), "enemy got Poison");

				Advance(game, 4.5f);
				Expect(590 - 4 * 8, game.Enemy.Health, "poison ran its course");
			});

			Test("Stun: status effect stops the enemy from attacking", game =>
			{
				Check(game.Cast("Stun"), "cast");
				Check(game.Enemy.StatusEffectController.HasStatusEffect(StatusEffectType.Stun), "enemy is stunned");

				Advance(game, 2.25f);
				Expect(300, game.Player.Health, "enemy attack at two seconds is blocked");

				Advance(game, 2f);
				Check(!game.Enemy.StatusEffectController.HasStatusEffect(StatusEffectType.Stun), "stun ended");
				Expect(280, game.Player.Health, "enemy attack at four seconds lands");
			});

			Test("Heal: heals the caster, capped at max health", game =>
			{
				Advance(game, 6.25f);
				Expect(240, game.Player.Health, "three enemy attacks");

				Check(game.Cast("Heal"), "cast");
				Expect(280, game.Player.Health, "healed for 40");
				Expect(250, game.Player.Mana, "mana cost");

				Check(game.Cast("Heal"), "second cast");
				Expect(300, game.Player.Health, "capped at max health");
			});

			Test("Focus: needs full health, adds damage until removed", game =>
			{
				Expect(10, game.Player.Damage, "base damage");
				Check(game.Cast("Focus"), "cast at full health");
				Expect(25, game.Player.Damage, "damage while focused");
				Expect(25, game.PlayerAttack(), "attack damage while focused");

				Advance(game, 6.25f);
				Expect(10, game.Player.Damage, "damage reverted after six seconds");

				Check(game.Player.Health < game.Player.MaxHealth, "player is damaged");
				Check(!game.Cast("Focus"), "cast is refused when not at full health");
				Expect(10, game.Player.Damage, "damage after refused cast");
			});

			Test("Modifier list that the unit panels show", game =>
			{
				Check(game.DescribeModifiers(game.Enemy).Count == 0, "enemy starts without modifiers");

				Check(game.Cast("Fireball"), "cast Fireball");
				Check(game.Cast("Poison"), "cast Poison");
				Check(game.Cast("Focus"), "cast Focus");
				Advance(game, 1f);

				var enemyModifiers = game.DescribeModifiers(game.Enemy);
				Check(enemyModifiers.Contains("Fireball"), "enemy list has Fireball");
				Check(enemyModifiers.Contains("Poison 3.0s"), "enemy list has Poison with its time left");
				Check(game.DescribeModifiers(game.Player).Contains("Focus 5.0s"),
					"player list has Focus with its time left");
			});

			Test("ModiBuff logged no errors or warnings", game =>
			{
				Check(logger.ErrorCount == 0, $"{logger.ErrorCount} errors logged");
				Check(logger.WarningCount == 0, $"{logger.WarningCount} warnings logged");
			});

			GD.Print($"[SelfTest] {_passed} passed, {_failed} failed");
			GetTree().Quit(_failed == 0 ? 0 : 1);
		}

		private static void Advance(TestLevelGame game, float seconds)
		{
			for (float time = 0; time < seconds; time += Step)
				game.Update(Step);
		}

		private void Test(string name, Action<TestLevelGame> test)
		{
			_currentFailed = false;
			TestLevelGame? game = null;
			try
			{
				//A new game for every test, this also covers the static reset that a level restart needs
				game = new TestLevelGame();
				test(game);
			}
			catch (Exception e)
			{
				Fail($"exception: {e}");
			}
			finally
			{
				game?.Shutdown();
			}

			if (_currentFailed)
				_failed++;
			else
				_passed++;
			GD.Print($"[SelfTest] {(_currentFailed ? "FAIL" : "PASS")} {name}");
		}

		private void Check(bool condition, string what)
		{
			if (!condition)
				Fail(what);
		}

		private void Expect(float expected, float actual, string what)
		{
			if (Mathf.Abs(expected - actual) > 0.001f)
				Fail($"{what}: expected {expected}, got {actual}");
		}

		private void Fail(string message)
		{
			_currentFailed = true;
			GD.PrintErr($"[SelfTest]   {message}");
		}
	}
}

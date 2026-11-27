using Godot;

namespace ModiBuff.Extensions.Godot.Samples
{
	/// <summary>
	///		Sends ModiBuff's log messages to the Godot output.
	/// </summary>
	/// <remarks>
	///		ModiBuff.Core has its own GodotLogger, but only when the Core source is compiled inside the Godot project.
	///		This project references ModiBuff as a library, so it brings its own.
	/// </remarks>
	public sealed class TestLevelLogger : ModiBuff.Core.ILogger
	{
		public int WarningCount { get; private set; }
		public int ErrorCount { get; private set; }

		public void Log(string message) => GD.Print(message);

		public void LogWarning(string message)
		{
			WarningCount++;
			GD.PushWarning(message);
		}

		public void LogError(string message)
		{
			ErrorCount++;
			GD.PushError(message);
		}
	}
}

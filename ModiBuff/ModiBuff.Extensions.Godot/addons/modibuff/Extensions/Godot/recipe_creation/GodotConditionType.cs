using ModiBuff.Core.Units;

namespace ModiBuff.Extensions.Godot
{
	/// <summary>
	///		Inspector version of <see cref="ConditionType"/>, with a None value.
	/// </summary>
	/// <remarks>
	///		Godot can't export nullable enums, and <see cref="ConditionType"/> doesn't have a None value.
	///		The values match the ones <see cref="ConditionType"/> had when it still had None,
	///		so recipe resources saved back then keep their meaning.
	/// </remarks>
	public enum GodotConditionType
	{
		None = 0,
		HealthIsFull = 1,
		ManaIsFull = 2,
		ManaIsEmpty = 3,
	}

	public static class GodotConditionTypeExtensions
	{
		public static ConditionType? GetModiBuffConditionType(this GodotConditionType conditionType)
		{
			switch (conditionType)
			{
				case GodotConditionType.HealthIsFull:
					return ConditionType.HealthIsFull;
				case GodotConditionType.ManaIsFull:
					return ConditionType.ManaIsFull;
				case GodotConditionType.ManaIsEmpty:
					return ConditionType.ManaIsEmpty;
				default:
					return null;
			}
		}
	}
}

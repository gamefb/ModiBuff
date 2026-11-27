using System;

namespace ModiBuff.Core
{
	public interface IData
	{
	}

	public abstract record Data : IData;

	public abstract record ModifierData : Data;

	public sealed record ModifierIntervalData(float Interval) : ModifierData;

	public sealed record ModifierIntervalTickData(int IntervalTick) : ModifierData;

	public sealed record ModifierDurationData(float Duration) : ModifierData;

	public sealed record ModifierDurationTickData(int DurationTick) : ModifierData;

	public sealed record ModifierStartingStacksData(int Stacks) : ModifierData;

	public abstract record EffectData((Type EffectType, int EffectNumber)? Data) : Data;

	public sealed record EffectData<TValue>(TValue Value, (Type EffectType, int EffectNumber)? Data = null)
		: EffectData(Data);
}

//Only needed below .NET 5. The guard also avoids a duplicate definition when Core and Units
//are compiled into one assembly (ex. source dropped into a Godot project)
#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
	internal static class IsExternalInit
	{
	}
}
#endif
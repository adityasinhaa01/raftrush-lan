using System;
using FishNet.Managing.Timing;

namespace PuffPirates.Net
{
	[Serializable]
	public struct TickTimer : IEquatable<TickTimer>
	{
		public uint TargetTick;

		public static TickTimer None => default;

		public bool IsRunning => TargetTick != 0;

		public static TickTimer CreateFromTicks(uint nowTick, uint ticks)
		{
			return new TickTimer
			{
				TargetTick = Math.Max(1u, nowTick + ticks)
			};
		}

		public static TickTimer CreateFromSeconds(uint nowTick, double tickDelta, float seconds)
		{
			return CreateFromTicks(nowTick, SecondsToTicks(seconds, tickDelta));
		}

		public static uint SecondsToTicks(float seconds, double tickDelta)
		{
			if (!(seconds <= 0f) && !(tickDelta <= 0.0))
			{
				return (uint)Math.Ceiling((double)seconds / tickDelta - 0.0001);
			}
			return 0u;
		}

		public bool Expired(uint nowTick)
		{
			if (IsRunning)
			{
				return nowTick >= TargetTick;
			}
			return false;
		}

		public bool ExpiredOrNotRunning(uint nowTick)
		{
			if (IsRunning)
			{
				return nowTick >= TargetTick;
			}
			return true;
		}

		public uint? RemainingTicks(uint nowTick)
		{
			if (!IsRunning)
			{
				return null;
			}
			return (nowTick < TargetTick) ? (TargetTick - nowTick) : 0u;
		}

		public float? RemainingTime(uint nowTick, double tickDelta)
		{
			uint? num = RemainingTicks(nowTick);
			if (!num.HasValue)
			{
				return null;
			}
			return (float)((double)num.Value * tickDelta);
		}

		public static TickTimer CreateFromSeconds(TimeManager tm, float seconds)
		{
			return CreateFromSeconds(tm.Tick, tm.TickDelta, seconds);
		}

		public static TickTimer CreateFromTicks(TimeManager tm, uint ticks)
		{
			return CreateFromTicks(tm.Tick, ticks);
		}

		public bool Expired(TimeManager tm)
		{
			if (tm != null)
			{
				return Expired(tm.Tick);
			}
			return false;
		}

		public bool ExpiredOrNotRunning(TimeManager tm)
		{
			if (!(tm == null))
			{
				return ExpiredOrNotRunning(tm.Tick);
			}
			return true;
		}

		public uint? RemainingTicks(TimeManager tm)
		{
			if (!(tm == null))
			{
				return RemainingTicks(tm.Tick);
			}
			return null;
		}

		public float? RemainingTime(TimeManager tm)
		{
			if (!(tm == null))
			{
				return RemainingTime(tm.Tick, tm.TickDelta);
			}
			return null;
		}

		public bool Equals(TickTimer other)
		{
			return TargetTick == other.TargetTick;
		}

		public override bool Equals(object obj)
		{
			if (obj is TickTimer other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (int)TargetTick;
		}

		public static bool operator ==(TickTimer a, TickTimer b)
		{
			return a.TargetTick == b.TargetTick;
		}

		public static bool operator !=(TickTimer a, TickTimer b)
		{
			return a.TargetTick != b.TargetTick;
		}
	}
}

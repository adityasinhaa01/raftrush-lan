using GamesMrkt.Localization;

namespace PuffPirates.Utils
{
	public static class MatchTimeFormat
	{
		public static string Format(int seconds)
		{
			int num = seconds / 60;
			int num2 = seconds % 60;
			if (num2 != 0)
			{
				return Loc.Get("net.duration.minutes_seconds", ("minutes", num), ("seconds", num2));
			}
			return Loc.Get("net.duration.minutes", ("minutes", num));
		}
	}
}

using GamesMrkt.Localization;

namespace PuffPirates.Modes
{
	public static class GameModes
	{
		public const int Count = 3;

		public static string Name(int id)
		{
			return id switch
			{
				1 => Loc.Get("lobby.mode.yaris"), 
				2 => Loc.Get("lobby.mode.fartball"), 
				_ => Loc.Get("lobby.mode.parkur"), 
			};
		}

		public static string Description(int id)
		{
			return id switch
			{
				1 => Loc.Get("mode.desc.yaris"), 
				2 => Loc.Get("mode.desc.fartball"), 
				_ => Loc.Get("mode.desc.parkur"), 
			};
		}

		public static bool IsLooping(int id)
		{
			return id == 1;
		}

		public static bool BoatsAreTeamOwned(int id)
		{
			return id != 2;
		}
	}
}

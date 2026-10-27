using System;

namespace PuffPirates.Save
{
	[Serializable]
	public sealed class HostRunData
	{
		public string id = "";

		public long createdAt;

		public long updatedAt;

		public float elapsedSeconds;

		public int[] checkpoints = new int[0];

		public int[] deaths = new int[0];

		public int mapId;
	}
}

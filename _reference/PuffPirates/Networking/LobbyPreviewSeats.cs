using UnityEngine;

namespace PuffPirates.Networking
{
	public static class LobbyPreviewSeats
	{
		public const int SeatCount = 4;

		public const float StageY = -500f;

		public const float SeatSpacing = 2.4f;

		public static Vector3 WorldPosition(int seat)
		{
			return new Vector3((float)seat * 2.4f, -500f, 0f);
		}
	}
}

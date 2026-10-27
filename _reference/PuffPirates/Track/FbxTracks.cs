using UnityEngine;

namespace PuffPirates.Track
{
	public static class FbxTracks
	{
		public const float ArenaScale = 4f;

		public const float ArenaGoalX = 19.3f;

		public const float ArenaGoalHalfX = 0.8f;

		public const float ArenaGoalHalfZ = 4f;

		public const float ArenaFloorHalfX = 19.5f;

		public const float ArenaFloorHalfZ = 11.5f;

		public const float ArenaWallHeight = 6f;

		public const float ArenaWallThick = 2f;

		public const float RaceSceneScale = 2.5f;

		public const float RaceSceneRoadMaxY = 0.3f;

		public const float RaceSceneRoadY = 0.12f;

		public static readonly Vector2[] RaceSceneCheckpoints = new Vector2[14]
		{
			new Vector2(-29.5f, -7f),
			new Vector2(-29.5f, 16f),
			new Vector2(-29.5f, 40f),
			new Vector2(-14f, 48f),
			new Vector2(16f, 48f),
			new Vector2(44f, 42f),
			new Vector2(45f, 26f),
			new Vector2(30f, 19f),
			new Vector2(8f, 16f),
			new Vector2(3f, 0f),
			new Vector2(14f, -13f),
			new Vector2(20.5f, -30f),
			new Vector2(6f, -36f),
			new Vector2(-18f, -36f)
		};

		public static readonly Vector2[] RaceSceneItemBoxes = new Vector2[12]
		{
			new Vector2(-32f, 28f),
			new Vector2(-29.5f, 28f),
			new Vector2(-27f, 28f),
			new Vector2(0f, 45f),
			new Vector2(0f, 48f),
			new Vector2(0f, 51f),
			new Vector2(42f, 34f),
			new Vector2(45f, 34f),
			new Vector2(48f, 34f),
			new Vector2(-6f, -39f),
			new Vector2(-6f, -36f),
			new Vector2(-6f, -33f)
		};

		public const float Level1Scale = 2f;

		public const float Level1RoadMaxY = 0.1f;

		public const float Level1RoadY = -0.16f;

		public static readonly Vector2[] Level1Checkpoints = new Vector2[12]
		{
			new Vector2(0.2f, -3f),
			new Vector2(0.2f, -15f),
			new Vector2(1.7f, -27f),
			new Vector2(4.7f, -44f),
			new Vector2(0f, -52f),
			new Vector2(-15f, -52f),
			new Vector2(-29f, -57f),
			new Vector2(-29f, -68f),
			new Vector2(-25f, -74f),
			new Vector2(-14f, -81f),
			new Vector2(-14f, -96f),
			new Vector2(-14f, -110f)
		};

		public static readonly Vector2[] Level1ItemBoxes = new Vector2[10]
		{
			new Vector2(-3f, -10f),
			new Vector2(0.2f, -10f),
			new Vector2(3.5f, -10f),
			new Vector2(1.7f, -33f),
			new Vector2(-10f, -49f),
			new Vector2(-10f, -52f),
			new Vector2(-29f, -63f),
			new Vector2(-17f, -90f),
			new Vector2(-14f, -90f),
			new Vector2(-11f, -90f)
		};

		public const float ParkurScale = 2f;

		public const float ParkurRoadMaxY = 0.5f;

		public const float ParkurRoadY = 0f;

		public static readonly Vector2[] ParkurCheckpoints = new Vector2[16]
		{
			new Vector2(-8f, 11f),
			new Vector2(-2f, -35.5f),
			new Vector2(4f, -64f),
			new Vector2(-12.5f, -79f),
			new Vector2(17.5f, -77.5f),
			new Vector2(32.5f, -112f),
			new Vector2(46f, -73f),
			new Vector2(37f, -35.5f),
			new Vector2(89.5f, -35.5f),
			new Vector2(110.5f, -43f),
			new Vector2(131.5f, -44.5f),
			new Vector2(131.5f, -16f),
			new Vector2(157f, -1f),
			new Vector2(115f, 9.5f),
			new Vector2(88f, 11f),
			new Vector2(85f, 20f)
		};

		public static Vector3 ArenaGoalSize => new Vector3(6.4f, 8f, 32f);

		public static Vector3 ArenaBoatSpawn(int boatIndex, int boatCount)
		{
			Vector3 vector = new Vector3(0f, 2.5f, 0f);
			if (boatCount <= 1)
			{
				return vector;
			}
			float num = 28f;
			float num2 = (float)boatIndex / (float)(boatCount - 1);
			return vector + new Vector3(0f, 0f, (num2 - 0.5f) * num);
		}

		public static Vector3 ArenaGoalCenter(int ownerTeam)
		{
			return new Vector3(((ownerTeam == 0) ? (-19.3f) : 19.3f) * 4f, 2f, 0f);
		}

		public static Vector3 World(Vector2 p, float scale, float y)
		{
			return new Vector3(p.x * scale, y, p.y * scale);
		}

		public static Quaternion Heading(Vector2[] pts, int i, bool looping)
		{
			int num = pts.Length;
			if (num < 2)
			{
				return Quaternion.identity;
			}
			Vector2 vector;
			Vector2 vector2;
			if (i + 1 < num)
			{
				vector = pts[i];
				vector2 = pts[i + 1];
			}
			else if (looping)
			{
				vector = pts[i];
				vector2 = pts[0];
			}
			else
			{
				vector = pts[num - 2];
				vector2 = pts[num - 1];
			}
			Vector2 vector3 = vector2 - vector;
			if (vector3.sqrMagnitude < 0.0001f)
			{
				return Quaternion.identity;
			}
			return Quaternion.LookRotation(new Vector3(vector3.x, 0f, vector3.y).normalized, Vector3.up);
		}
	}
}

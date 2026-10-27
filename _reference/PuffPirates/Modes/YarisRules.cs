using FishNet.Utility.Template;
using PuffPirates.Boats;
using PuffPirates.Net;
using PuffPirates.Networking;
using PuffPirates.Track;
using UnityEngine;

namespace PuffPirates.Modes
{
	[DisallowMultipleComponent]
	public class YarisRules : TickNetworkBehaviour
	{
		[Tooltip("Ilk bot bitince kalanlara taninan sure (sn). Dolunca yaris zorla kapanir.")]
		[SerializeField]
		private float _graceSeconds = 20f;

		private const int MyMode = 1;

		private TickTimer GraceTimer;

		private int FinishedCount;

		private bool NetworkInitialize___EarlyPuffPirates_002EModes_002EYarisRulesAssembly_002DCSharp_002Edll_Excuted;

		private bool NetworkInitialize___LatePuffPirates_002EModes_002EYarisRulesAssembly_002DCSharp_002Edll_Excuted;

		public static YarisRules Instance { get; private set; }

		private bool Active
		{
			get
			{
				if (base.IsServerInitialized && RaceState.Instance != null)
				{
					return RaceState.Instance.ModeId == 1;
				}
				return false;
			}
		}

		public static bool IsLapRace
		{
			get
			{
				if (RaceState.Instance != null)
				{
					return LevelBuilder.IsLoopingMap(RaceState.Instance.MapId);
				}
				return false;
			}
		}

		public override void Awake()
		{
			NetworkInitialize___Early();
			Awake_UserLogic_PuffPirates_002EModes_002EYarisRules_Assembly_002DCSharp_002Edll();
			NetworkInitialize___Late();
		}

		public override void OnStartNetwork()
		{
			base.OnStartNetwork();
			Instance = this;
		}

		public override void OnStartServer()
		{
			base.OnStartServer();
			GraceTimer = TickTimer.None;
			FinishedCount = 0;
		}

		public override void OnStopNetwork()
		{
			base.OnStopNetwork();
			if (Instance == this)
			{
				Instance = null;
			}
		}

		public void ResetForRematch()
		{
			if (base.IsServerInitialized)
			{
				GraceTimer = TickTimer.None;
				FinishedCount = 0;
			}
		}

		protected override void TimeManager_OnTick()
		{
			RaceState instance = RaceState.Instance;
			if (!Active || !instance.RaceRunning || instance.HasWinner)
			{
				return;
			}
			if (IsLapRace)
			{
				foreach (NetworkBoat item in NetworkBoat.All)
				{
					if (!(item == null) && item.FinishRank == 0 && item.Lap >= instance.LapCount)
					{
						FinishedCount++;
						item.SetFinished(FinishedCount);
						if (FinishedCount == 1)
						{
							GraceTimer = TickTimer.CreateFromSeconds(base.TimeManager, _graceSeconds);
						}
					}
				}
			}
			if ((FinishedCount >= instance.BoatCount && FinishedCount > 0) || GraceTimer.Expired(base.TimeManager))
			{
				DeclareWinner(instance);
			}
		}

		public void OnBoatFinished(NetworkBoat boat, int rank)
		{
			RaceState instance = RaceState.Instance;
			if (Active && !(boat == null) && !(instance == null) && !instance.HasWinner)
			{
				FinishedCount = Mathf.Max(FinishedCount, rank);
				if (rank == 1)
				{
					GraceTimer = TickTimer.CreateFromSeconds(base.TimeManager, _graceSeconds);
				}
				if (FinishedCount >= instance.BoatCount)
				{
					DeclareWinner(instance);
				}
			}
		}

		private void DeclareWinner(RaceState race)
		{
			foreach (NetworkBoat item in NetworkBoat.All)
			{
				if (!(item == null) && item.FinishRank == 1)
				{
					race.ReportWinner(item.OwnerTeam);
					return;
				}
			}
			NetworkBoat networkBoat = Leader();
			if (networkBoat != null)
			{
				race.ReportWinner(networkBoat.OwnerTeam);
			}
		}

		public int BoatRank(NetworkBoat boat)
		{
			if (boat == null)
			{
				return 1;
			}
			int num = Progress(boat);
			int num2 = 1;
			foreach (NetworkBoat item in NetworkBoat.All)
			{
				if (!(item == null) && !(item == boat) && Progress(item) > num)
				{
					num2++;
				}
			}
			return num2;
		}

		public NetworkBoat Leader()
		{
			NetworkBoat result = null;
			int num = int.MinValue;
			foreach (NetworkBoat item in NetworkBoat.All)
			{
				if (!(item == null))
				{
					int num2 = Progress(item);
					if (num2 > num)
					{
						num = num2;
						result = item;
					}
				}
			}
			return result;
		}

		public NetworkBoat BoatAhead(NetworkBoat boat)
		{
			if (boat == null)
			{
				return null;
			}
			int num = Progress(boat);
			NetworkBoat result = null;
			int num2 = int.MaxValue;
			foreach (NetworkBoat item in NetworkBoat.All)
			{
				if (!(item == null) && !(item == boat))
				{
					int num3 = Progress(item);
					if (num3 > num && num3 < num2)
					{
						num2 = num3;
						result = item;
					}
				}
			}
			return result;
		}

		private static int Progress(NetworkBoat b)
		{
			int num = ((!(RaceTrack.Instance != null)) ? 1 : RaceTrack.Instance.CheckpointCount);
			if (num <= 0)
			{
				num = 1;
			}
			if (num <= 1 && b != null)
			{
				RaceState instance = RaceState.Instance;
				if (instance != null && instance.MapId == 6)
				{
					return Mathf.RoundToInt(DistanceAlongCenterline(FbxTracks.Level1Checkpoints, 2f, b.transform.position) * 10f);
				}
			}
			return b.Lap * num + b.CurrentCheckpointId;
		}

		private static float DistanceAlongCenterline(Vector2[] pts, float scale, Vector3 world)
		{
			if (pts == null || pts.Length < 2)
			{
				return 0f;
			}
			Vector2 vector = new Vector2(world.x, world.z);
			float num = float.MaxValue;
			float result = 0f;
			float num2 = 0f;
			for (int i = 0; i < pts.Length - 1; i++)
			{
				Vector2 vector2 = new Vector2(pts[i].x * scale, pts[i].y * scale);
				Vector2 vector3 = new Vector2(pts[i + 1].x * scale, pts[i + 1].y * scale) - vector2;
				float sqrMagnitude = vector3.sqrMagnitude;
				float num3 = ((sqrMagnitude > 1E-06f) ? Mathf.Clamp01(Vector2.Dot(vector - vector2, vector3) / sqrMagnitude) : 0f);
				Vector2 vector4 = vector2 + vector3 * num3;
				float sqrMagnitude2 = (vector - vector4).sqrMagnitude;
				float num4 = Mathf.Sqrt(sqrMagnitude);
				if (sqrMagnitude2 < num)
				{
					num = sqrMagnitude2;
					result = num2 + num3 * num4;
				}
				num2 += num4;
			}
			return result;
		}

		public override void NetworkInitialize___Early()
		{
			if (!NetworkInitialize___EarlyPuffPirates_002EModes_002EYarisRulesAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___EarlyPuffPirates_002EModes_002EYarisRulesAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Early();
			}
		}

		public override void NetworkInitialize___Late()
		{
			if (!NetworkInitialize___LatePuffPirates_002EModes_002EYarisRulesAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___LatePuffPirates_002EModes_002EYarisRulesAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Late();
			}
		}

		public override void NetworkInitializeIfDisabled()
		{
			NetworkInitialize___Early();
			NetworkInitialize___Late();
		}

		private void Awake_UserLogic_PuffPirates_002EModes_002EYarisRules_Assembly_002DCSharp_002Edll()
		{
			SetTickCallbacks(TickCallback.Tick);
		}
	}
}

using System;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Serializing;
using FishNet.Transporting;
using FishNet.Utility.Template;
using PuffPirates.Boats;
using PuffPirates.Football;
using PuffPirates.Net;
using PuffPirates.Networking;
using PuffPirates.Track;
using UnityEngine;

namespace PuffPirates.Modes
{
	[DisallowMultipleComponent]
	public class FutbolRules : TickNetworkBehaviour
	{
		[Tooltip("Gol sonrası botun dokunulmazlık süresi (sn). Aynı golün iki kez sayılmasını da bu engeller.")]
		[SerializeField]
		private float _goalResetInvuln = 2f;

		[Tooltip("Gol kilidi (sn): bir gol sayıldıktan sonra AYNI takıma bu kısa pencerede ikinci gol yazılmaz. Top + eşlik eden bot kaleyi BİRLİKTE geçince ÇİFT SAYIMI engeller.")]
		[SerializeField]
		private float _goalScoreCooldown = 0.75f;

		private readonly TickTimer[] _goalLock = new TickTimer[2];

		private const int MyMode = 2;

		private bool NetworkInitialize___EarlyPuffPirates_002EModes_002EFutbolRulesAssembly_002DCSharp_002Edll_Excuted;

		private bool NetworkInitialize___LatePuffPirates_002EModes_002EFutbolRulesAssembly_002DCSharp_002Edll_Excuted;

		public static FutbolRules Instance { get; private set; }

		private bool Active
		{
			get
			{
				if (base.IsServerInitialized && RaceState.Instance != null)
				{
					return RaceState.Instance.ModeId == 2;
				}
				return false;
			}
		}

		public static Vector3 BallSpawnPoint => new Vector3(0f, 4f, 0f);

		public static event Action<int, Vector3> GoalScored;

		public override void Awake()
		{
			NetworkInitialize___Early();
			Awake_UserLogic_PuffPirates_002EModes_002EFutbolRules_Assembly_002DCSharp_002Edll();
			NetworkInitialize___Late();
		}

		public override void OnStartNetwork()
		{
			base.OnStartNetwork();
			Instance = this;
		}

		public override void OnStopNetwork()
		{
			base.OnStopNetwork();
			if (Instance == this)
			{
				Instance = null;
			}
		}

		public void BeginMatch(float seconds)
		{
			if (Active)
			{
				RaceState.Instance.StartMatchClock(seconds);
			}
		}

		protected override void TimeManager_OnTick()
		{
			RaceState instance = RaceState.Instance;
			if (Active && instance.RaceRunning && !instance.HasWinner && instance.MatchClock.Expired(base.TimeManager))
			{
				EndMatch(instance);
			}
		}

		private void EndMatch(RaceState race)
		{
			int num = race.TeamScore[0];
			int num2 = race.TeamScore[1];
			if (num > num2)
			{
				race.ReportWinner(0);
			}
			else if (num2 > num)
			{
				race.ReportWinner(1);
			}
			else
			{
				race.ReportDraw();
			}
		}

		public void ScoreGoal(int scoringTeam, NetworkBoat boat)
		{
			RaceState instance = RaceState.Instance;
			if (Active && !(boat == null) && !(instance == null) && instance.RaceRunning && !instance.HasWinner && !boat.IsInvulnerable && GoalOpen(scoringTeam))
			{
				LockGoal(scoringTeam);
				Vector3 position = boat.transform.position;
				instance.AddScore(scoringTeam);
				Vector3 position2 = FbxTracks.ArenaBoatSpawn(boat.BoatIndex, 4);
				boat.RespawnAt(position2, Quaternion.identity, _goalResetInvuln, DeathCause.None);
				RpcGoalFx(scoringTeam, position);
			}
		}

		public void ScoreGoalByBall(int scoringTeam, FartBall ball)
		{
			RaceState instance = RaceState.Instance;
			if (Active && !(ball == null) && !(instance == null) && instance.RaceRunning && !instance.HasWinner && !ball.IsLocked && GoalOpen(scoringTeam))
			{
				LockGoal(scoringTeam);
				Vector3 position = ball.transform.position;
				instance.AddScore(scoringTeam);
				ball.ResetToCenter(BallSpawnPoint);
				RpcGoalFx(scoringTeam, position);
			}
		}

		private bool GoalOpen(int team)
		{
			if ((uint)team < (uint)_goalLock.Length)
			{
				return _goalLock[team].ExpiredOrNotRunning(base.TimeManager);
			}
			return false;
		}

		private void LockGoal(int team)
		{
			if ((uint)team < (uint)_goalLock.Length)
			{
				_goalLock[team] = TickTimer.CreateFromSeconds(base.TimeManager, _goalScoreCooldown);
			}
		}

		[ObserversRpc]
		private void RpcGoalFx(int scoringTeam, Vector3 where, Channel channel = Channel.Unreliable)
		{
			RpcWriter___Observers_RpcGoalFx___3175676366(scoringTeam, where, channel);
		}

		public override void NetworkInitialize___Early()
		{
			if (!NetworkInitialize___EarlyPuffPirates_002EModes_002EFutbolRulesAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___EarlyPuffPirates_002EModes_002EFutbolRulesAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Early();
				RegisterObserversRpc(0u, RpcReader___Observers_RpcGoalFx___3175676366);
			}
		}

		public override void NetworkInitialize___Late()
		{
			if (!NetworkInitialize___LatePuffPirates_002EModes_002EFutbolRulesAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___LatePuffPirates_002EModes_002EFutbolRulesAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Late();
			}
		}

		public override void NetworkInitializeIfDisabled()
		{
			NetworkInitialize___Early();
			NetworkInitialize___Late();
		}

		private void RpcWriter___Observers_RpcGoalFx___3175676366(int scoringTeam, Vector3 where, Channel channel = Channel.Unreliable)
		{
			if (!base.IsServerInitialized)
			{
				NetworkManager networkManager = base.NetworkManager;
				networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
				return;
			}
			Channel channel2 = channel;
			PooledWriter pooledWriter = WriterPool.Retrieve();
			pooledWriter.WriteInt32(scoringTeam);
			pooledWriter.WriteVector3(where);
			SendObserversRpc(0u, pooledWriter, channel2, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			pooledWriter.Store();
		}

		private void RpcLogic___RpcGoalFx___3175676366(int P_0, Vector3 P_1, Channel P_2)
		{
			GoalScored?.Invoke(P_0, P_1);
		}

		private void RpcReader___Observers_RpcGoalFx___3175676366(PooledReader PooledReader0, Channel channel)
		{
			int num = PooledReader0.ReadInt32();
			Vector3 vector = PooledReader0.ReadVector3();
			if (base.IsClientInitialized)
			{
				RpcLogic___RpcGoalFx___3175676366(num, vector, channel);
			}
		}

		private void Awake_UserLogic_PuffPirates_002EModes_002EFutbolRules_Assembly_002DCSharp_002Edll()
		{
			SetTickCallbacks(TickCallback.Tick);
		}
	}
}

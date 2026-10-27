using System.Collections.Generic;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Serializing;
using FishNet.Transporting;
using FishNet.Utility.Template;
using GamesMrkt.Core;
using GamesMrkt.PuffPirates;
using PuffPirates.Biomes;
using PuffPirates.Net;
using PuffPirates.Obstacles;
using PuffPirates.Systems;
using UnityEngine;

namespace PuffPirates.Networking
{
	[DisallowMultipleComponent]
	public class RaceState : TickNetworkBehaviour
	{
		public const int MinTeams = 1;

		public const int MaxTeams = 4;

		public const int MinTeamSize = 1;

		public const int MaxTeamSize = 4;

		public readonly SyncVar<int> _teamCount = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<int> _teamSize = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<int> _mapId = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<int> _themeId = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<int> _waterId = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<float> _gasXf1Start = new SyncVar<float>(new SyncTypeSettings(0f));

		public readonly SyncVar<float> _gasXf2Start = new SyncVar<float>(new SyncTypeSettings(0f));

		public readonly SyncVar<float> _gasCrossfadeDuration = new SyncVar<float>(new SyncTypeSettings(0f));

		public readonly SyncVar<float> _gasReleaseFadeDuration = new SyncVar<float>(new SyncTypeSettings(0f));

		public readonly SyncVar<float> _gasReleasePitchTarget = new SyncVar<float>(new SyncTypeSettings(0f));

		public readonly SyncVar<int> _modeId = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<int> _boatCount = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<int> _lapCount = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<TickTimer> _matchClock = new SyncVar<TickTimer>(new SyncTypeSettings(0f));

		public readonly SyncList<int> _teamScore = new SyncList<int>(new SyncTypeSettings(0f));

		public const int MaxItemBoxes = 16;

		public readonly SyncList<TickTimer> _itemBoxRespawnTicks = new SyncList<TickTimer>(new SyncTypeSettings(0f));

		public const int MaxPickups = 32;

		public readonly SyncList<TickTimer> _pickupRespawnTicks = new SyncList<TickTimer>(new SyncTypeSettings(0f));

		public readonly SyncList<int> _pickupEffectId = new SyncList<int>(new SyncTypeSettings(0f));

		public const int MaxDoors = 8;

		public readonly SyncList<int> _doorPressTick = new SyncList<int>(new SyncTypeSettings(0f));

		public const int MinBoats = 1;

		public const int MaxBoats = 4;

		public const int MinLaps = 1;

		public const int MaxLaps = 5;

		public static readonly int[] MatchDurations = new int[3] { 60, 180, 300 };

		public const int DrawWinner = -1;

		public const float TurnSpeedMin = 30f;

		public const float TurnSpeedMax = 500f;

		public const float TurnSpeedDefault = 140f;

		public const float GasForceMin = 100f;

		public const float GasForceMax = 6000f;

		public const float NormalGasDefault = 235f;

		public const float SwirlMin = 0f;

		public const float SwirlMax = 1f;

		public const float SwirlDefault = 0.01f;

		public const float GasFalloffFraction = 0.3f;

		public readonly SyncVar<float> _turnSpeedDeg = new SyncVar<float>(new SyncTypeSettings(0f));

		public readonly SyncVar<float> _normalGasForce = new SyncVar<float>(new SyncTypeSettings(0f));

		public readonly SyncVar<float> _swirlBias = new SyncVar<float>(new SyncTypeSettings(0f));

		public readonly SyncVar<bool> _crewBlocking = new SyncVar<bool>(new SyncTypeSettings(0f));

		public readonly SyncVar<bool> _sectorLock = new SyncVar<bool>(new SyncTypeSettings(0f));

		public readonly SyncVar<bool> _lockRotX = new SyncVar<bool>(new SyncTypeSettings(0f));

		public readonly SyncVar<bool> _lockRotY = new SyncVar<bool>(new SyncTypeSettings(0f));

		public readonly SyncVar<bool> _boundaryLeanEnabled = new SyncVar<bool>(new SyncTypeSettings(0f));

		public readonly SyncVar<bool> _raceRunning = new SyncVar<bool>(new SyncTypeSettings(0f));

		public readonly SyncVar<TickTimer> _startCountdown = new SyncVar<TickTimer>(new SyncTypeSettings(0f));

		public readonly SyncVar<int> _winnerTeam = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<float> _runBaseSeconds = new SyncVar<float>(new SyncTypeSettings(0f));

		public readonly SyncVar<uint> _runStartTick = new SyncVar<uint>(new SyncTypeSettings(0f));

		public readonly SyncVar<uint> _runEndTick = new SyncVar<uint>(new SyncTypeSettings(0f));

		private int _lastThemeId;

		private bool _themeApplied;

		private float _pendingMatchSeconds;

		private bool NetworkInitialize___EarlyPuffPirates_002ENetworking_002ERaceStateAssembly_002DCSharp_002Edll_Excuted;

		private bool NetworkInitialize___LatePuffPirates_002ENetworking_002ERaceStateAssembly_002DCSharp_002Edll_Excuted;

		public static RaceState Instance { get; private set; }

		public int TeamCount
		{
			get
			{
				return _teamCount.Value;
			}
			private set
			{
				_teamCount.Value = value;
			}
		}

		public int TeamSize
		{
			get
			{
				return _teamSize.Value;
			}
			private set
			{
				_teamSize.Value = value;
			}
		}

		public int MapId
		{
			get
			{
				return _mapId.Value;
			}
			private set
			{
				_mapId.Value = value;
			}
		}

		public int ThemeId
		{
			get
			{
				return _themeId.Value;
			}
			private set
			{
				_themeId.Value = value;
			}
		}

		public int WaterId
		{
			get
			{
				return _waterId.Value;
			}
			private set
			{
				_waterId.Value = value;
			}
		}

		public float GasXf1Start
		{
			get
			{
				return _gasXf1Start.Value;
			}
			private set
			{
				_gasXf1Start.Value = value;
			}
		}

		public float GasXf2Start
		{
			get
			{
				return _gasXf2Start.Value;
			}
			private set
			{
				_gasXf2Start.Value = value;
			}
		}

		public float GasCrossfadeDuration
		{
			get
			{
				return _gasCrossfadeDuration.Value;
			}
			private set
			{
				_gasCrossfadeDuration.Value = value;
			}
		}

		public float GasReleaseFadeDuration
		{
			get
			{
				return _gasReleaseFadeDuration.Value;
			}
			private set
			{
				_gasReleaseFadeDuration.Value = value;
			}
		}

		public float GasReleasePitchTarget
		{
			get
			{
				return _gasReleasePitchTarget.Value;
			}
			private set
			{
				_gasReleasePitchTarget.Value = value;
			}
		}

		public int ModeId
		{
			get
			{
				return _modeId.Value;
			}
			private set
			{
				_modeId.Value = value;
			}
		}

		public int BoatCount
		{
			get
			{
				return _boatCount.Value;
			}
			private set
			{
				_boatCount.Value = value;
			}
		}

		public int LapCount
		{
			get
			{
				return _lapCount.Value;
			}
			private set
			{
				_lapCount.Value = value;
			}
		}

		public TickTimer MatchClock
		{
			get
			{
				return _matchClock.Value;
			}
			private set
			{
				_matchClock.Value = value;
			}
		}

		public IReadOnlyList<int> TeamScore => _teamScore;

		public IReadOnlyList<TickTimer> ItemBoxRespawnTicks => _itemBoxRespawnTicks;

		public IReadOnlyList<TickTimer> PickupRespawnTicks => _pickupRespawnTicks;

		public IReadOnlyList<int> PickupEffectId => _pickupEffectId;

		public IReadOnlyList<int> DoorPressTick => _doorPressTick;

		public bool IsFootball => ModeId == 2;

		public bool IsRace => ModeId == 1;

		public bool IsParkour => ModeId == 0;

		public float MatchRemaining => MatchClock.RemainingTime(base.TimeManager).GetValueOrDefault();

		public float TurnSpeedDeg
		{
			get
			{
				return _turnSpeedDeg.Value;
			}
			private set
			{
				_turnSpeedDeg.Value = value;
			}
		}

		public float NormalGasForce
		{
			get
			{
				return _normalGasForce.Value;
			}
			private set
			{
				_normalGasForce.Value = value;
			}
		}

		public float SwirlBias
		{
			get
			{
				return _swirlBias.Value;
			}
			private set
			{
				_swirlBias.Value = value;
			}
		}

		public bool CrewBlocking
		{
			get
			{
				return _crewBlocking.Value;
			}
			private set
			{
				_crewBlocking.Value = value;
			}
		}

		public bool SectorLock
		{
			get
			{
				return _sectorLock.Value;
			}
			private set
			{
				_sectorLock.Value = value;
			}
		}

		public bool LockRotX
		{
			get
			{
				return _lockRotX.Value;
			}
			private set
			{
				_lockRotX.Value = value;
			}
		}

		public bool LockRotY
		{
			get
			{
				return _lockRotY.Value;
			}
			private set
			{
				_lockRotY.Value = value;
			}
		}

		public bool BoundaryLeanEnabled
		{
			get
			{
				return _boundaryLeanEnabled.Value;
			}
			private set
			{
				_boundaryLeanEnabled.Value = value;
			}
		}

		public bool RaceRunning
		{
			get
			{
				return _raceRunning.Value;
			}
			private set
			{
				_raceRunning.Value = value;
			}
		}

		public TickTimer StartCountdown
		{
			get
			{
				return _startCountdown.Value;
			}
			private set
			{
				_startCountdown.Value = value;
			}
		}

		public int WinnerTeam
		{
			get
			{
				return _winnerTeam.Value;
			}
			private set
			{
				_winnerTeam.Value = value;
			}
		}

		public bool CanMove => RaceRunning;

		public bool HasWinner => WinnerTeam != 0;

		public bool InLobby
		{
			get
			{
				if (!RaceRunning)
				{
					return !HasWinner;
				}
				return false;
			}
		}

		public float CountdownRemaining => StartCountdown.RemainingTime(base.TimeManager).GetValueOrDefault();

		public float RunBaseSeconds
		{
			get
			{
				return _runBaseSeconds.Value;
			}
			private set
			{
				_runBaseSeconds.Value = value;
			}
		}

		private uint RunStartTick
		{
			get
			{
				return _runStartTick.Value;
			}
			set
			{
				_runStartTick.Value = value;
			}
		}

		private uint RunEndTick
		{
			get
			{
				return _runEndTick.Value;
			}
			set
			{
				_runEndTick.Value = value;
			}
		}

		public float RunElapsedSeconds
		{
			get
			{
				if (RunStartTick == 0 || base.TimeManager == null)
				{
					return RunBaseSeconds;
				}
				uint num = ((RunEndTick != 0) ? RunEndTick : base.TimeManager.Tick);
				double num2 = ((num > RunStartTick) ? ((double)(num - RunStartTick) * base.TimeManager.TickDelta) : 0.0);
				return RunBaseSeconds + (float)num2;
			}
		}

		public void PressDoor(int id, int tick)
		{
			if (base.IsServerInitialized && id >= 0 && id < DoorPressTick.Count && DoorPressTick[id] == 0)
			{
				SetAt(_doorPressTick, id, Mathf.Max(1, tick));
			}
		}

		public void HostSetRunBaseSeconds(float seconds)
		{
			if (base.IsServerInitialized)
			{
				RunBaseSeconds = Mathf.Max(0f, seconds);
			}
		}

		private void FreezeRunClock()
		{
			if (RunStartTick != 0 && RunEndTick == 0)
			{
				RunEndTick = base.TimeManager.Tick;
			}
		}

		private void ApplyTheme()
		{
			BiomeController.Instance?.Apply(ThemeId);
		}

		public void HostSetWaterId(int id)
		{
			if (base.IsServerInitialized)
			{
				WaterId = id;
			}
		}

		public void HostSetGasAudio(float xf1, float xf2, float crossfadeDur, float releaseDur, float releasePitch)
		{
			if (base.IsServerInitialized)
			{
				GasXf1Start = xf1;
				GasXf2Start = xf2;
				GasCrossfadeDuration = crossfadeDur;
				GasReleaseFadeDuration = releaseDur;
				GasReleasePitchTarget = releasePitch;
			}
		}

		public override void Awake()
		{
			NetworkInitialize___Early();
			Awake_UserLogic_PuffPirates_002ENetworking_002ERaceState_Assembly_002DCSharp_002Edll();
			NetworkInitialize___Late();
		}

		public override void OnStartNetwork()
		{
			base.OnStartNetwork();
			Instance = this;
			_lastThemeId = ThemeId;
			ApplyTheme();
			_themeApplied = BiomeController.Instance != null;
		}

		public override void OnStartServer()
		{
			base.OnStartServer();
			TeamCount = 2;
			TeamSize = 2;
			MapId = 7;
			ThemeId = 0;
			WaterId = -1;
			GasXf1Start = -1f;
			RaceRunning = false;
			WinnerTeam = 0;
			ModeId = 0;
			BoatCount = 2;
			LapCount = 3;
			MatchClock = TickTimer.None;
			Fill(_teamScore, 4, 0);
			Fill(_itemBoxRespawnTicks, 16, TickTimer.None);
			Fill(_pickupRespawnTicks, 32, TickTimer.None);
			Fill(_pickupEffectId, 32, 0);
			Fill(_doorPressTick, 8, 0);
			TurnSpeedDeg = 140f;
			NormalGasForce = 235f;
			SwirlBias = 0.01f;
			CrewBlocking = false;
			SectorLock = true;
			LockRotX = false;
			LockRotY = false;
			BoundaryLeanEnabled = false;
		}

		public override void OnStopNetwork()
		{
			base.OnStopNetwork();
			if (Instance == this)
			{
				Instance = null;
			}
		}

		private static void Fill<T>(SyncList<T> list, int capacity, T value)
		{
			for (int i = list.Count; i < capacity; i++)
			{
				list.Add(value);
			}
			for (int j = 0; j < list.Count; j++)
			{
				SetAt(list, j, value);
			}
		}

		private static void SetAt<T>(SyncList<T> list, int index, T value)
		{
			if (index >= 0 && index < list.Count && !EqualityComparer<T>.Default.Equals(list[index], value))
			{
				list[index] = value;
			}
		}

		protected override void TimeManager_OnTick()
		{
			if (!base.IsServerInitialized)
			{
				return;
			}
			if (!RaceRunning && !HasWinner && StartCountdown.Expired(base.TimeManager))
			{
				StartCountdown = TickTimer.None;
				RaceRunning = true;
				RunStartTick = base.TimeManager.Tick;
				RunEndTick = 0u;
				if (_pendingMatchSeconds > 0f)
				{
					StartMatchClock(_pendingMatchSeconds);
					_pendingMatchSeconds = 0f;
				}
			}
			int tick = (int)base.TimeManager.Tick;
			IReadOnlyList<RandomPickup> active = RandomPickup.Active;
			for (int i = 0; i < active.Count; i++)
			{
				RandomPickup randomPickup = active[i];
				if (!(randomPickup == null))
				{
					int spawnId = randomPickup.SpawnId;
					if (spawnId >= 0)
					{
						SelectPickup(spawnId, randomPickup.Allowed, tick);
					}
				}
			}
		}

		protected override void TimeManager_OnUpdate()
		{
			if (ThemeId != _lastThemeId || !_themeApplied)
			{
				if (BiomeController.Instance != null)
				{
					ApplyTheme();
					_themeApplied = true;
				}
				_lastThemeId = ThemeId;
			}
		}

		public void SetTeamCount(int count)
		{
			if (base.IsServerInitialized && InLobby && !IsFootball)
			{
				TeamCount = Mathf.Clamp(count, 1, 4);
				BoatCount = TeamCount;
			}
		}

		public void SetTeamSize(int size)
		{
			if (base.IsServerInitialized && InLobby && !IsFootball)
			{
				TeamSize = Mathf.Clamp(size, 1, 4);
			}
		}

		public void SetMode(int modeId)
		{
			if (base.IsServerInitialized && InLobby)
			{
				ModeId = Mathf.Clamp(modeId, 0, 2);
				if (IsFootball)
				{
					TeamCount = 2;
					TeamSize = 2;
				}
				else
				{
					BoatCount = TeamCount;
				}
				MapId = 7;
			}
		}

		public void SetBoatCount(int n)
		{
			if (base.IsServerInitialized && InLobby)
			{
				BoatCount = Mathf.Clamp(n, 1, 4);
				if (!IsFootball)
				{
					TeamCount = BoatCount;
				}
			}
		}

		public void SetLapCount(int n)
		{
			if (base.IsServerInitialized && InLobby)
			{
				LapCount = Mathf.Clamp(n, 1, 5);
			}
		}

		public void SetThemeId(int id)
		{
			if (base.IsServerInitialized)
			{
				int count = ((!(BiomeController.Instance != null)) ? 1 : BiomeController.Instance.ThemeCount);
				ThemeId = BiomeController.ClampThemeId(id, count);
			}
		}

		public void SetTurnSpeed(float degPerSec)
		{
			if (base.IsServerInitialized)
			{
				TurnSpeedDeg = Mathf.Clamp(degPerSec, 30f, 500f);
			}
		}

		public void SetGasForce(float normal)
		{
			if (base.IsServerInitialized)
			{
				NormalGasForce = Mathf.Clamp(normal, 100f, 6000f);
			}
		}

		public void SetSwirlBias(float bias)
		{
			if (base.IsServerInitialized)
			{
				SwirlBias = Mathf.Clamp(bias, 0f, 1f);
			}
		}

		public void SetBoundaryLean(bool on)
		{
			if (base.IsServerInitialized)
			{
				BoundaryLeanEnabled = on;
			}
		}

		public void SetSectorLock(bool on)
		{
			if (base.IsServerInitialized)
			{
				SectorLock = on;
			}
		}

		public void SetCrewBlocking(bool on)
		{
			if (base.IsServerInitialized)
			{
				CrewBlocking = on;
			}
		}

		public void SetRotationLock(bool lockX, bool lockY)
		{
			if (base.IsServerInitialized)
			{
				LockRotX = lockX;
				LockRotY = lockY;
			}
		}

		public void BeginCountdown(float seconds, float matchSeconds = 0f)
		{
			if (base.IsServerInitialized && !RaceRunning && !HasWinner)
			{
				StartCountdown = TickTimer.CreateFromSeconds(base.TimeManager, seconds);
				_pendingMatchSeconds = matchSeconds;
			}
		}

		public void ReportWinner(int teamId)
		{
			if (base.IsServerInitialized && !HasWinner)
			{
				WinnerTeam = teamId + 1;
				RaceRunning = false;
				FreezeRunClock();
			}
		}

		public void StartMatchClock(float seconds)
		{
			if (base.IsServerInitialized)
			{
				MatchClock = TickTimer.CreateFromSeconds(base.TimeManager, seconds);
			}
		}

		public void AddScore(int teamId)
		{
			if (base.IsServerInitialized && teamId >= 0 && teamId < TeamScore.Count)
			{
				SetAt(_teamScore, teamId, TeamScore[teamId] + 1);
			}
		}

		public bool IsItemBoxAvailable(int boxId)
		{
			if (boxId < 0 || boxId >= ItemBoxRespawnTicks.Count)
			{
				return true;
			}
			TickTimer tickTimer = ItemBoxRespawnTicks[boxId];
			if (tickTimer.IsRunning)
			{
				return tickTimer.Expired(base.TimeManager);
			}
			return true;
		}

		public void PickUpItemBox(int boxId, float respawnSeconds)
		{
			if (base.IsServerInitialized && boxId >= 0 && boxId < ItemBoxRespawnTicks.Count)
			{
				SetAt(_itemBoxRespawnTicks, boxId, TickTimer.CreateFromSeconds(base.TimeManager, respawnSeconds));
			}
		}

		public static PickupEffectType PickPickupEffect(PickupEffectType[] allowed, int tick, int spawnId)
		{
			if (allowed == null || allowed.Length == 0)
			{
				return PickupEffectType.GasBoost;
			}
			uint num = (uint)((tick * 73856093) ^ (spawnId * 19349663));
			num ^= num >> 13;
			num *= 1540483477;
			num ^= num >> 15;
			return allowed[num % (uint)allowed.Length];
		}

		public bool IsPickupAvailable(int id)
		{
			if (id < 0 || id >= PickupRespawnTicks.Count)
			{
				return true;
			}
			TickTimer tickTimer = PickupRespawnTicks[id];
			if (tickTimer.IsRunning)
			{
				return tickTimer.Expired(base.TimeManager);
			}
			return true;
		}

		public PickupEffectType CurrentEffect(int id)
		{
			if (id < 0 || id >= PickupEffectId.Count || PickupEffectId[id] == 0)
			{
				return PickupEffectType.GasBoost;
			}
			return (PickupEffectType)PickupEffectId[id];
		}

		public void SelectPickup(int id, PickupEffectType[] allowed, int tick)
		{
			if (base.IsServerInitialized && id >= 0 && id < PickupEffectId.Count && IsPickupAvailable(id) && PickupEffectId[id] == 0)
			{
				SetAt(_pickupEffectId, id, (int)PickPickupEffect(allowed, tick, id));
			}
		}

		public void PickUpPickup(int id, float respawnSeconds)
		{
			if (base.IsServerInitialized && id >= 0 && id < PickupRespawnTicks.Count)
			{
				SetAt(_pickupRespawnTicks, id, TickTimer.CreateFromSeconds(base.TimeManager, respawnSeconds));
				SetAt(_pickupEffectId, id, 0);
			}
		}

		public void ReportDraw()
		{
			if (base.IsServerInitialized && !HasWinner)
			{
				WinnerTeam = -1;
				RaceRunning = false;
				FreezeRunClock();
			}
		}

		public void ResetToLobby()
		{
			if (base.IsServerInitialized)
			{
				WinnerTeam = 0;
				RaceRunning = false;
				StartCountdown = TickTimer.None;
				RunBaseSeconds = 0f;
				RunStartTick = 0u;
				RunEndTick = 0u;
				MatchClock = TickTimer.None;
				for (int i = 0; i < TeamScore.Count; i++)
				{
					SetAt(_teamScore, i, 0);
				}
				for (int j = 0; j < ItemBoxRespawnTicks.Count; j++)
				{
					SetAt(_itemBoxRespawnTicks, j, TickTimer.None);
				}
				for (int k = 0; k < PickupRespawnTicks.Count; k++)
				{
					SetAt(_pickupRespawnTicks, k, TickTimer.None);
				}
				for (int l = 0; l < PickupEffectId.Count; l++)
				{
					SetAt(_pickupEffectId, l, 0);
				}
				for (int m = 0; m < DoorPressTick.Count; m++)
				{
					SetAt(_doorPressTick, m, 0);
				}
			}
		}

		[ObserversRpc]
		public void RpcPlayerLeftNotice(string playerName, int reason)
		{
			RpcWriter___Observers_RpcPlayerLeftNotice___3643459082(playerName, reason);
		}

		[ObserversRpc]
		public void RpcVersionNotice(string theirVersion)
		{
			RpcWriter___Observers_RpcVersionNotice___3615296227(theirVersion);
		}

		[ObserversRpc(ExcludeServer = true)]
		public void RpcHostClosing()
		{
			RpcWriter___Observers_RpcHostClosing___2166136261();
		}

		private static bool TryGetConnection(out ConnectionSystem connection)
		{
			connection = null;
			CoreSystemHandler instance = Singleton<CoreSystemHandler>.Instance;
			if (instance != null)
			{
				return instance.TryGetSystem<ConnectionSystem>(out connection);
			}
			return false;
		}

		public override void NetworkInitialize___Early()
		{
			if (!NetworkInitialize___EarlyPuffPirates_002ENetworking_002ERaceStateAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___EarlyPuffPirates_002ENetworking_002ERaceStateAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Early();
				_runEndTick.InitializeEarly(this, 32u, isSyncObject: false);
				_runStartTick.InitializeEarly(this, 31u, isSyncObject: false);
				_runBaseSeconds.InitializeEarly(this, 30u, isSyncObject: false);
				_winnerTeam.InitializeEarly(this, 29u, isSyncObject: false);
				_startCountdown.InitializeEarly(this, 28u, isSyncObject: false);
				_raceRunning.InitializeEarly(this, 27u, isSyncObject: false);
				_boundaryLeanEnabled.InitializeEarly(this, 26u, isSyncObject: false);
				_lockRotY.InitializeEarly(this, 25u, isSyncObject: false);
				_lockRotX.InitializeEarly(this, 24u, isSyncObject: false);
				_sectorLock.InitializeEarly(this, 23u, isSyncObject: false);
				_crewBlocking.InitializeEarly(this, 22u, isSyncObject: false);
				_swirlBias.InitializeEarly(this, 21u, isSyncObject: false);
				_normalGasForce.InitializeEarly(this, 20u, isSyncObject: false);
				_turnSpeedDeg.InitializeEarly(this, 19u, isSyncObject: false);
				_doorPressTick.InitializeEarly(this, 18u, isSyncObject: true);
				_pickupEffectId.InitializeEarly(this, 17u, isSyncObject: true);
				_pickupRespawnTicks.InitializeEarly(this, 16u, isSyncObject: true);
				_itemBoxRespawnTicks.InitializeEarly(this, 15u, isSyncObject: true);
				_teamScore.InitializeEarly(this, 14u, isSyncObject: true);
				_matchClock.InitializeEarly(this, 13u, isSyncObject: false);
				_lapCount.InitializeEarly(this, 12u, isSyncObject: false);
				_boatCount.InitializeEarly(this, 11u, isSyncObject: false);
				_modeId.InitializeEarly(this, 10u, isSyncObject: false);
				_gasReleasePitchTarget.InitializeEarly(this, 9u, isSyncObject: false);
				_gasReleaseFadeDuration.InitializeEarly(this, 8u, isSyncObject: false);
				_gasCrossfadeDuration.InitializeEarly(this, 7u, isSyncObject: false);
				_gasXf2Start.InitializeEarly(this, 6u, isSyncObject: false);
				_gasXf1Start.InitializeEarly(this, 5u, isSyncObject: false);
				_waterId.InitializeEarly(this, 4u, isSyncObject: false);
				_themeId.InitializeEarly(this, 3u, isSyncObject: false);
				_mapId.InitializeEarly(this, 2u, isSyncObject: false);
				_teamSize.InitializeEarly(this, 1u, isSyncObject: false);
				_teamCount.InitializeEarly(this, 0u, isSyncObject: false);
				RegisterObserversRpc(0u, RpcReader___Observers_RpcPlayerLeftNotice___3643459082);
				RegisterObserversRpc(1u, RpcReader___Observers_RpcVersionNotice___3615296227);
				RegisterObserversRpc(2u, RpcReader___Observers_RpcHostClosing___2166136261);
			}
		}

		public override void NetworkInitialize___Late()
		{
			if (!NetworkInitialize___LatePuffPirates_002ENetworking_002ERaceStateAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___LatePuffPirates_002ENetworking_002ERaceStateAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Late();
				_runEndTick.InitializeLate();
				_runStartTick.InitializeLate();
				_runBaseSeconds.InitializeLate();
				_winnerTeam.InitializeLate();
				_startCountdown.InitializeLate();
				_raceRunning.InitializeLate();
				_boundaryLeanEnabled.InitializeLate();
				_lockRotY.InitializeLate();
				_lockRotX.InitializeLate();
				_sectorLock.InitializeLate();
				_crewBlocking.InitializeLate();
				_swirlBias.InitializeLate();
				_normalGasForce.InitializeLate();
				_turnSpeedDeg.InitializeLate();
				_doorPressTick.InitializeLate();
				_pickupEffectId.InitializeLate();
				_pickupRespawnTicks.InitializeLate();
				_itemBoxRespawnTicks.InitializeLate();
				_teamScore.InitializeLate();
				_matchClock.InitializeLate();
				_lapCount.InitializeLate();
				_boatCount.InitializeLate();
				_modeId.InitializeLate();
				_gasReleasePitchTarget.InitializeLate();
				_gasReleaseFadeDuration.InitializeLate();
				_gasCrossfadeDuration.InitializeLate();
				_gasXf2Start.InitializeLate();
				_gasXf1Start.InitializeLate();
				_waterId.InitializeLate();
				_themeId.InitializeLate();
				_mapId.InitializeLate();
				_teamSize.InitializeLate();
				_teamCount.InitializeLate();
			}
		}

		public override void NetworkInitializeIfDisabled()
		{
			NetworkInitialize___Early();
			NetworkInitialize___Late();
		}

		private void RpcWriter___Observers_RpcPlayerLeftNotice___3643459082(string playerName, int reason)
		{
			if (!base.IsServerInitialized)
			{
				NetworkManager networkManager = base.NetworkManager;
				networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
				return;
			}
			Channel channel = Channel.Reliable;
			PooledWriter pooledWriter = WriterPool.Retrieve();
			pooledWriter.WriteString(playerName);
			pooledWriter.WriteInt32(reason);
			SendObserversRpc(0u, pooledWriter, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			pooledWriter.Store();
		}

		public void RpcLogic___RpcPlayerLeftNotice___3643459082(string P_0, int P_1)
		{
			if (TryGetConnection(out var connection))
			{
				connection.ShowPlayerLeftNotice(P_0, (ConnectionSystem.PlayerLeaveReason)P_1);
			}
		}

		private void RpcReader___Observers_RpcPlayerLeftNotice___3643459082(PooledReader PooledReader0, Channel channel)
		{
			string text = PooledReader0.ReadStringAllocated();
			int num = PooledReader0.ReadInt32();
			if (base.IsClientInitialized)
			{
				RpcLogic___RpcPlayerLeftNotice___3643459082(text, num);
			}
		}

		private void RpcWriter___Observers_RpcVersionNotice___3615296227(string theirVersion)
		{
			if (!base.IsServerInitialized)
			{
				NetworkManager networkManager = base.NetworkManager;
				networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
				return;
			}
			Channel channel = Channel.Reliable;
			PooledWriter pooledWriter = WriterPool.Retrieve();
			pooledWriter.WriteString(theirVersion);
			SendObserversRpc(1u, pooledWriter, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			pooledWriter.Store();
		}

		public void RpcLogic___RpcVersionNotice___3615296227(string P_0)
		{
			if (TryGetConnection(out var connection))
			{
				connection.ShowVersionNotice(P_0);
			}
		}

		private void RpcReader___Observers_RpcVersionNotice___3615296227(PooledReader PooledReader0, Channel channel)
		{
			string text = PooledReader0.ReadStringAllocated();
			if (base.IsClientInitialized)
			{
				RpcLogic___RpcVersionNotice___3615296227(text);
			}
		}

		private void RpcWriter___Observers_RpcHostClosing___2166136261()
		{
			if (!base.IsServerInitialized)
			{
				NetworkManager networkManager = base.NetworkManager;
				networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
				return;
			}
			Channel channel = Channel.Reliable;
			PooledWriter pooledWriter = WriterPool.Retrieve();
			SendObserversRpc(2u, pooledWriter, channel, DataOrderType.Default, bufferLast: false, excludeServer: true, excludeOwner: false);
			pooledWriter.Store();
		}

		public void RpcLogic___RpcHostClosing___2166136261()
		{
			if (TryGetConnection(out var connection))
			{
				connection.NoteHostClosing();
			}
		}

		private void RpcReader___Observers_RpcHostClosing___2166136261(PooledReader PooledReader0, Channel channel)
		{
			if (base.IsClientInitialized)
			{
				RpcLogic___RpcHostClosing___2166136261();
			}
		}

		private void Awake_UserLogic_PuffPirates_002ENetworking_002ERaceState_Assembly_002DCSharp_002Edll()
		{
			SetTickCallbacks(TickCallback.Tick | TickCallback.Update);
		}
	}
}

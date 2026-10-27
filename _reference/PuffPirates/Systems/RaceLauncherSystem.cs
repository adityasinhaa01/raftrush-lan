using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Object;
using GamesMrkt.Core;
using GamesMrkt.Inputs;
using GamesMrkt.Localization;
using GamesMrkt.PuffPirates;
using PuffPirates.Backend;
using PuffPirates.Boats;
using PuffPirates.Modes;
using PuffPirates.Networking;
using PuffPirates.Obstacles;
using PuffPirates.Players;
using PuffPirates.Save;
using PuffPirates.Track;
using PuffPirates.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PuffPirates.Systems
{
	[RequireSystem(typeof(ConnectionSystem))]
	[RequireSystem(typeof(LobbySystem))]
	[RequireSystem(typeof(HostRunSystem))]
	public class RaceLauncherSystem : MonoBehaviour, IGameSystem, IPostInitializeSystem, IUpdateableSystem
	{
		[SerializeField]
		private NetworkObject _boatPrefab;

		[Tooltip("Used when a team has 3 players (TeamSize==3). Falls back to _boatPrefab if unset.")]
		[SerializeField]
		private NetworkObject _boat3Prefab;

		[Tooltip("Used when a team has 4 players (TeamSize==4). Falls back to _boatPrefab if unset.")]
		[SerializeField]
		private NetworkObject _boat4Prefab;

		[Tooltip("FARTBALL's actual ball. Sits by the boats on the field, can be pushed around.")]
		[SerializeField]
		private NetworkObject _fartBallPrefab;

		[Tooltip("Shark spawned at every SharkSpawnPoint in the level when the race starts.")]
		[SerializeField]
		private NetworkObject _sharkPrefab;

		[SerializeField]
		private float _countdownSec = 3f;

		private ConnectionSystem _connection;

		private LobbySystem _lobby;

		private HostRunSystem _runs;

		private readonly Dictionary<int, NetworkObject> _boats = new Dictionary<int, NetworkObject>();

		private NetworkObject _fartBall;

		private readonly List<NetworkObject> _sharks = new List<NetworkObject>();

		private bool _pendingBeginRace;

		private static readonly System.Random _crewRng = new System.Random();

		private int _lastTc = -1;

		private int _lastTs = -1;

		private int _lastMode = -1;

		private int _lastBoats = -1;

		private int _lastLaps = -1;

		private int _lastTheme = -1;

		private PlayerInputSystem _input;

		private IDisposable _unstuckSubscription;

		private const float BoatSpawnLift = 1.5f;

		private readonly List<PiratePlayer> _crew = new List<PiratePlayer>();

		public string SystemId => "RaceLauncherSystem";

		public NetworkBoat LocalBoat => _connection.LocalBoat;

		public event Action Changed;

		public event Action<string> Toast;

		public UniTask Initialize()
		{
			_connection = this.GetSystem<ConnectionSystem, CoreSystemHandler>();
			_lobby = this.GetSystem<LobbySystem, CoreSystemHandler>();
			_runs = this.GetSystem<HostRunSystem, CoreSystemHandler>();
			_connection.PlayerObjectSpawned += HandlePlayerObjectSpawned;
			_connection.PlayerObjectDespawning += HandlePlayerLeft;
			_connection.RunnerCleanedUp += HandleRunnerCleanedUp;
			_connection.SceneLoadDone += HandleSceneLoadDone;
			return UniTask.CompletedTask;
		}

		public UniTask OnSystemsInitialized()
		{
			if (this.TryGetSystem<PlayerInputSystem, CoreSystemHandler>(out _input))
			{
				_unstuckSubscription = _input.Player?.Subscribe("Pirate/Unstuck", InputActionTrigger.Performed, HandleUnstuckPressed);
			}
			return UniTask.CompletedTask;
		}

		private void OnDestroy()
		{
			_unstuckSubscription?.Dispose();
		}

		private void HandleSceneLoadDone()
		{
			if (_connection.IsHost)
			{
				CompletePendingBeginRace();
			}
		}

		private void HandleRunnerCleanedUp()
		{
			_runs.ReleaseActiveRun();
			_boats.Clear();
			_fartBall = null;
			_pendingBeginRace = false;
		}

		public void OnUpdate(float deltaTime)
		{
			RaceState instance = RaceState.Instance;
			if (instance == null)
			{
				return;
			}
			if (instance.TeamCount != _lastTc || instance.TeamSize != _lastTs || instance.ModeId != _lastMode || instance.BoatCount != _lastBoats || instance.LapCount != _lastLaps || instance.ThemeId != _lastTheme)
			{
				_lastTc = instance.TeamCount;
				_lastTs = instance.TeamSize;
				_lastMode = instance.ModeId;
				_lastBoats = instance.BoatCount;
				_lastLaps = instance.LapCount;
				_lastTheme = instance.ThemeId;
				Changed?.Invoke();
			}
			bool flag = _pendingBeginRace || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "PuffTest";
			LauncherState launcherState = (instance.HasWinner ? LauncherState.Finished : (instance.CanMove ? LauncherState.Racing : (flag ? LauncherState.Connecting : (_connection.IsSessionRunning ? LauncherState.Lobby : _connection.State))));
			if (launcherState != _connection.State)
			{
				if (launcherState == LauncherState.Racing)
				{
					BackendBootstrap.BeginNewMatch();
				}
				if (launcherState == LauncherState.Finished && _connection.IsHost)
				{
					_runs.CompleteActiveRun();
				}
				_connection.State = launcherState;
				Changed?.Invoke();
			}
			if (_connection.IsHost && instance.CanMove)
			{
				_runs.TrackElapsed(instance.RunElapsedSeconds);
			}
		}

		private void HandleUnstuckPressed()
		{
			RaceState instance = RaceState.Instance;
			if (!(instance == null) && _connection.IsHost && instance.CanMove && !instance.HasWinner && !_input.InputBlocked && !_input.OverlayOpen)
			{
				RespawnAtCheckpoint(LocalBoat);
			}
		}

		private static void RespawnAtCheckpoint(NetworkBoat boat)
		{
			RaceTrack instance = RaceTrack.Instance;
			if (!(boat == null) && !(instance == null))
			{
				int currentCheckpointId = boat.CurrentCheckpointId;
				if (!instance.TryGetSafeRespawnPose(currentCheckpointId, 1.5f, out var position, out var rotation))
				{
					Transform checkpoint = instance.GetCheckpoint(currentCheckpointId);
					position = ((checkpoint != null) ? (checkpoint.position + Vector3.up * 1.5f) : new Vector3(0f, 2f, 0f));
					rotation = ((checkpoint != null) ? checkpoint.rotation : Quaternion.identity);
				}
				boat.RespawnAt(position, rotation, 2f, DeathCause.Void);
			}
		}

		public void BeginRace()
		{
			if (_connection.IsHost)
			{
				if (!_lobby.AllReady)
				{
					Toast?.Invoke(Loc.Get("net.toast.not_all_ready"));
				}
				else if (!_lobby.HasEnoughPlayers)
				{
					Toast?.Invoke(Loc.Get("net.toast.need_more_players", ("min", 2)));
				}
				else
				{
					_pendingBeginRace = true;
					LoadGameSceneForRace();
				}
			}
		}

		private void LoadGameSceneForRace()
		{
			try
			{
				SceneLoadData sceneLoadData = new SceneLoadData("PuffTest")
				{
					ReplaceScenes = ReplaceOption.OnlineOnly,
					PreferredActiveScene = new PreferredScene(new SceneLookupData("PuffTest"))
				};
				_connection.Network.SceneManager.LoadGlobalScenes(sceneLoadData);
			}
			catch (Exception ex)
			{
				if (!(this == null))
				{
					Debug.LogException(ex, this);
					_pendingBeginRace = false;
					_connection.SetError("net.error.start_failed", ("error", ex.Message));
					Toast?.Invoke(Loc.Get("net.error.start_failed", ("error", ex.Message)));
					Changed?.Invoke();
				}
			}
		}

		public void CompletePendingBeginRace()
		{
			if (!_pendingBeginRace)
			{
				return;
			}
			_pendingBeginRace = false;
			if (SpawnBoats())
			{
				if (_lobby.IsFootball)
				{
					SpawnFartBall();
				}
				SpawnSharks();
				MatchConfigSystem matchConfigSystem = ((Singleton<CoreSystemHandler>.Instance != null) ? Singleton<CoreSystemHandler>.Instance.GetSystem<MatchConfigSystem>() : null);
				float matchSeconds = ((_lobby.IsFootball && matchConfigSystem != null) ? matchConfigSystem.MatchDuration : 0f);
				RaceState.Instance?.BeginCountdown(_countdownSec, matchSeconds);
			}
		}

		private bool SpawnBoats()
		{
			RaceState instance = RaceState.Instance;
			RaceTrack instance2 = RaceTrack.Instance;
			if (instance == null)
			{
				return false;
			}
			LevelBuilder.Instance?.Build(instance.MapId);
			NetworkManager network = _connection.Network;
			if (_boatPrefab == null)
			{
				Debug.LogError("[RaceLauncherSystem] _boatPrefab unassigned — could not spawn boats.");
				_connection.SetError("net.error.boat_prefab_missing");
				Toast?.Invoke(Loc.Get("net.error.boat_prefab_missing"));
				_connection.Leave();
				Changed?.Invoke();
				return false;
			}
			instance.HostSetRunBaseSeconds(_runs.GetOrStartActiveRun().elapsedSeconds);
			_runs.SetMap(instance.MapId);
			try
			{
				for (int i = 0; i < instance.BoatCount; i++)
				{
					if (!_boats.TryGetValue(i, out var value) || !(value != null))
					{
						if (instance2 == null || !instance2.TryGetTeamSpawnPose(i, 1.5f, out var position, out var rotation))
						{
							Transform transform = ((instance2 != null) ? instance2.GetTeamSpawn(i) : null);
							position = ((transform != null) ? transform.position : new Vector3((float)i * 5f, 1.2f, -44f));
							rotation = ((transform != null) ? transform.rotation : Quaternion.identity);
						}
						int num = ((instance2 != null && !LevelBuilder.IsLoopingMap(instance.MapId)) ? _runs.GetCheckpoint(i) : 0);
						if (num > 0 && instance2.TryGetSafeRespawnPose(num, 1.5f, out var position2, out var rotation2))
						{
							position = position2;
							rotation = rotation2;
						}
						else
						{
							num = 0;
						}
						NetworkObject networkObject = _boatPrefab;
						if (instance.TeamSize >= 4 && _boat4Prefab != null)
						{
							networkObject = _boat4Prefab;
						}
						else if (instance.TeamSize == 3 && _boat3Prefab != null)
						{
							networkObject = _boat3Prefab;
						}
						NetworkObject networkObject2 = UnityEngine.Object.Instantiate(networkObject, position, rotation);
						if (!networkObject2.TryGetComponent<NetworkBoat>(out var component))
						{
							UnityEngine.Object.Destroy(networkObject2.gameObject);
							throw new Exception("Boat prefab " + networkObject.name + " has no NetworkBoat.");
						}
						network.ServerManager.Spawn(networkObject2);
						if (!networkObject2.IsSpawned)
						{
							throw new Exception($"Could not spawn boat {i}.");
						}
						component.BoatIndex = i;
						if (num > 0)
						{
							component.RestoreCheckpoint(num);
						}
						component.RestoreDeaths(_runs.GetDeaths(i));
						component.OnCheckpointRecorded += HandleBoatCheckpointRecorded;
						component.OnDeathRecorded += HandleBoatDeathRecorded;
						component.OwnerTeam = (GameModes.BoatsAreTeamOwned(instance.ModeId) ? i : (-1));
						_boats[i] = networkObject2;
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogError("[RaceLauncherSystem] Could not spawn boats (prefab missing from DefaultPrefabObjects?): " + ex.Message);
				_connection.SetError("net.error.boats_spawn_failed", ("error", ex.Message));
				Toast?.Invoke(Loc.Get("net.error.boats_spawn_failed", ("error", ex.Message)));
				foreach (KeyValuePair<int, NetworkObject> boat in _boats)
				{
					if (boat.Value != null && boat.Value.IsSpawned)
					{
						network.ServerManager.Despawn(boat.Value);
					}
				}
				_boats.Clear();
				_connection.Leave();
				Changed?.Invoke();
				return false;
			}
			AssignPiratesToBoats();
			return true;
		}

		private void HandleBoatCheckpointRecorded(NetworkBoat boat)
		{
			if (TryTrackRunElapsed())
			{
				_runs.RecordCheckpoint(boat.BoatIndex, boat.CurrentCheckpointId);
			}
		}

		private void HandleBoatDeathRecorded(NetworkBoat boat)
		{
			if (TryTrackRunElapsed())
			{
				_runs.RecordDeaths(boat.BoatIndex, boat.Deaths);
			}
		}

		private bool TryTrackRunElapsed()
		{
			RaceState instance = RaceState.Instance;
			if (instance == null || !instance.CanMove)
			{
				return false;
			}
			_runs.TrackElapsed(instance.RunElapsedSeconds);
			return true;
		}

		private void AssignPiratesToBoats()
		{
			RaceState instance = RaceState.Instance;
			if (!(instance == null) && _connection.IsHost)
			{
				if (instance.IsFootball)
				{
					AssignFootball(instance);
				}
				else
				{
					AssignTeamOwned(instance);
				}
				SeatStragglers(instance);
			}
		}

		private void SeatStragglers(RaceState race)
		{
			NetworkManager network = _connection.Network;
			if (network == null)
			{
				return;
			}
			foreach (int key in network.ServerManager.Clients.Keys)
			{
				if (!PiratePlayer.TryGet(key, out var _))
				{
					Debug.LogError($"[RaceLauncherSystem] Client {key} has no pirate object — this player can't join the race.");
				}
			}
			List<PiratePlayer> active = PiratePlayer.Active;
			for (int i = 0; i < active.Count; i++)
			{
				PiratePlayer piratePlayer = active[i];
				if (!(piratePlayer == null) && piratePlayer.BoatIndex < 0)
				{
					Debug.LogWarning($"[RaceLauncherSystem] Korsan (ClientId {piratePlayer.ClientId}) hiçbir bota " + $"oturtulamadı — TeamId={piratePlayer.TeamId}, TeamCount={race.TeamCount}, " + $"BoatCount={race.BoatCount}. Lobide takım atanmamış demektir; " + "0 numaralı bota alınıyor.");
					int num = ((piratePlayer.TeamId >= 0 && piratePlayer.TeamId < race.TeamCount) ? piratePlayer.TeamId : 0);
					if (piratePlayer.TeamId != num)
					{
						piratePlayer.SetTeam(num);
					}
					ReseatTeam(num, race);
				}
			}
		}

		private void AssignTeamOwned(RaceState race)
		{
			for (int i = 0; i < race.TeamCount; i++)
			{
				ReseatTeam(i, race);
			}
		}

		private void ReseatTeam(int team, RaceState race, int? leavingClientId = null)
		{
			_crew.Clear();
			List<PiratePlayer> active = PiratePlayer.Active;
			for (int i = 0; i < active.Count; i++)
			{
				PiratePlayer piratePlayer = active[i];
				if (!(piratePlayer == null) && piratePlayer.TeamId == team && (!leavingClientId.HasValue || piratePlayer.ClientId != leavingClientId.Value))
				{
					_crew.Add(piratePlayer);
				}
			}
			List<PiratePlayer> crew = _crew;
			int sectorCount = SectorCountFor(team, crew.Count, race);
			float halfWidthDeg = SectorHalfWidthFor(sectorCount);
			for (int j = 0; j < crew.Count; j++)
			{
				PiratePlayer piratePlayer2 = crew[j];
				float num = SectorCenterFor(j, sectorCount);
				piratePlayer2.SetBoatIndex(team);
				piratePlayer2.AttachmentAngle = num;
				piratePlayer2.SetSector(num, halfWidthDeg);
			}
		}

		private static int SectorCountFor(int boatIndex, int crewCount, RaceState race)
		{
			NetworkBoat networkBoat = NetworkBoat.FindByIndex(boatIndex);
			int b = ((networkBoat != null && networkBoat.CrewCapacity > 0) ? networkBoat.CrewCapacity : race.TeamSize);
			return Mathf.Max(1, Mathf.Max(crewCount, b));
		}

		private static float SectorHalfWidthFor(int sectorCount)
		{
			return Mathf.Max(5f, 180f / (float)Mathf.Max(1, sectorCount) - BoatCrewGizmo.GapDeg * 0.5f);
		}

		private static float SectorCenterFor(int sectorIndex, int sectorCount)
		{
			int num = Mathf.Max(1, sectorCount);
			int num2 = Mathf.Clamp(sectorIndex, 0, num - 1);
			float num3 = ((num >= 2) ? (360f / (float)num) : BoatCrewGizmo.StartAngleDeg);
			return Mathf.Repeat(((float)num2 + 0.5f) * (360f / (float)num) + num3, 360f);
		}

		public void ReshuffleBoatSeats(int boatIndex, bool randomize = true)
		{
			if (!_connection.IsHost)
			{
				return;
			}
			RaceState instance = RaceState.Instance;
			if (instance == null)
			{
				return;
			}
			_crew.Clear();
			List<PiratePlayer> active = PiratePlayer.Active;
			for (int i = 0; i < active.Count; i++)
			{
				if (active[i] != null && active[i].BoatIndex == boatIndex)
				{
					_crew.Add(active[i]);
				}
			}
			List<PiratePlayer> crew = _crew;
			if (crew.Count <= 1)
			{
				return;
			}
			int sectorCount = SectorCountFor(boatIndex, crew.Count, instance);
			float halfWidthDeg = SectorHalfWidthFor(sectorCount);
			if (!randomize)
			{
				crew.Sort((PiratePlayer a, PiratePlayer b) => a.SectorCenter.CompareTo(b.SectorCenter));
				float num = Mathf.DeltaAngle(SectorCenterFor(0, sectorCount), crew[0].SectorCenter);
				for (int num2 = 0; num2 < crew.Count; num2++)
				{
					float num3 = Mathf.Repeat(SectorCenterFor(num2, sectorCount) + num, 360f);
					if (!Mathf.Approximately(Mathf.DeltaAngle(crew[num2].SectorCenter, num3), 0f))
					{
						crew[num2].AttachmentAngle = num3;
					}
					crew[num2].SetSector(num3, halfWidthDeg);
				}
			}
			else
			{
				crew.Sort((PiratePlayer a, PiratePlayer b) => a.SectorCenter.CompareTo(b.SectorCenter));
				int[] array = CrewLayoutMath.Derangement(crew.Count, _crewRng);
				for (int num4 = 0; num4 < crew.Count; num4++)
				{
					PiratePlayer piratePlayer = crew[num4];
					float centerDeg = (piratePlayer.AttachmentAngle = SectorCenterFor(array[num4], sectorCount));
					piratePlayer.SetSector(centerDeg, halfWidthDeg);
				}
			}
		}

		private void AssignFootball(RaceState race)
		{
			int num = Mathf.Max(1, race.BoatCount);
			List<PiratePlayer> active = PiratePlayer.Active;
			int[] array = new int[num];
			for (int i = 0; i < 2; i++)
			{
				int num2 = 0;
				for (int j = 0; j < active.Count; j++)
				{
					PiratePlayer piratePlayer = active[j];
					if (!(piratePlayer == null) && piratePlayer.TeamId == i)
					{
						int num3 = num2 % num;
						piratePlayer.SetBoatIndex(num3);
						piratePlayer.AttachmentAngle = ((i == 0) ? 180f : 0f);
						array[num3]++;
						num2++;
					}
				}
			}
			for (int k = 0; k < active.Count; k++)
			{
				PiratePlayer piratePlayer2 = active[k];
				if (!(piratePlayer2 == null))
				{
					int boatIndex = piratePlayer2.BoatIndex;
					if (boatIndex >= 0 && boatIndex < num)
					{
						int num4 = Mathf.Max(race.TeamSize, array[boatIndex]);
						int sectorIndex = Mathf.Clamp(piratePlayer2.TeamId, 0, num4 - 1);
						piratePlayer2.SetSector(SectorCenterFor(sectorIndex, num4), SectorHalfWidthFor(num4));
					}
				}
			}
		}

		private void SpawnFartBall()
		{
			if (_connection.IsHost)
			{
				if (_fartBallPrefab == null)
				{
					Debug.LogError("[RaceLauncherSystem] _fartBallPrefab unassigned — could not spawn the FARTBALL ball.");
				}
				else if (!(_fartBall != null))
				{
					_fartBall = UnityEngine.Object.Instantiate(_fartBallPrefab, FutbolRules.BallSpawnPoint, Quaternion.identity);
					_connection.Network.ServerManager.Spawn(_fartBall);
				}
			}
		}

		private void SpawnSharks()
		{
			if (_connection.IsHost && !(_sharkPrefab == null))
			{
				SharkSpawnPoint[] array = UnityEngine.Object.FindObjectsByType<SharkSpawnPoint>(FindObjectsSortMode.None);
				foreach (SharkSpawnPoint sharkSpawnPoint in array)
				{
					NetworkObject networkObject = UnityEngine.Object.Instantiate(_sharkPrefab, sharkSpawnPoint.transform.position, sharkSpawnPoint.transform.rotation);
					_connection.Network.ServerManager.Spawn(networkObject);
					_sharks.Add(networkObject);
				}
			}
		}

		private void ReattachSinglePirate(RaceState race, PiratePlayer pirate)
		{
			if (pirate == null || !_connection.IsHost)
			{
				return;
			}
			List<PiratePlayer> active = PiratePlayer.Active;
			int teamId = pirate.TeamId;
			if (teamId < 0)
			{
				return;
			}
			if (race.IsFootball)
			{
				int num = Mathf.Max(1, race.BoatCount);
				int num2 = 0;
				for (int i = 0; i < active.Count; i++)
				{
					PiratePlayer piratePlayer = active[i];
					if (!(piratePlayer == null) && !(piratePlayer == pirate) && piratePlayer.TeamId == teamId && piratePlayer.BoatIndex >= 0)
					{
						num2++;
					}
				}
				int num3 = num2 % num;
				pirate.SetBoatIndex(num3);
				pirate.AttachmentAngle = ((teamId == 0) ? 180f : 0f);
				int num4 = 0;
				for (int j = 0; j < active.Count; j++)
				{
					if (active[j] != null && active[j].BoatIndex == num3)
					{
						num4++;
					}
				}
				int num5 = Mathf.Max(race.TeamSize, num4);
				int sectorIndex = Mathf.Clamp(pirate.TeamId, 0, Mathf.Max(1, num5) - 1);
				pirate.SetSector(SectorCenterFor(sectorIndex, num5), SectorHalfWidthFor(num5));
			}
			else
			{
				FitBoatToCrew(teamId, race);
			}
		}

		private void HandlePlayerObjectSpawned(PiratePlayer pirate, bool isReconnectMidRace)
		{
			if (isReconnectMidRace)
			{
				RaceState instance = RaceState.Instance;
				if (instance != null)
				{
					ReattachSinglePirate(instance, pirate);
				}
			}
		}

		private void HandlePlayerLeft(int clientId)
		{
			RaceState instance = RaceState.Instance;
			if (!(instance == null) && _connection.IsHost && !instance.InLobby && !instance.IsFootball && PiratePlayer.TryGet(clientId, out var pirate) && pirate.TeamId >= 0)
			{
				FitBoatToCrew(pirate.TeamId, instance, clientId);
			}
		}

		private void FitBoatToCrew(int team, RaceState race, int? leavingClientId = null)
		{
			if (race.TeamSize >= 2)
			{
				int num = 0;
				List<PiratePlayer> active = PiratePlayer.Active;
				for (int i = 0; i < active.Count; i++)
				{
					PiratePlayer piratePlayer = active[i];
					if (!(piratePlayer == null) && piratePlayer.TeamId == team && (!leavingClientId.HasValue || piratePlayer.ClientId != leavingClientId.Value))
					{
						num++;
					}
				}
				NetworkBoat networkBoat = NetworkBoat.FindByIndex(team);
				if (networkBoat != null)
				{
					networkBoat.SetCrewCapacity(Mathf.Clamp(num, 2, 4));
				}
			}
			ReseatTeam(team, race, leavingClientId);
		}

		public void Rematch()
		{
			if (!_connection.IsHost)
			{
				return;
			}
			NetworkManager network = _connection.Network;
			foreach (KeyValuePair<int, NetworkObject> boat in _boats)
			{
				if (boat.Value != null && boat.Value.IsSpawned)
				{
					network.ServerManager.Despawn(boat.Value);
				}
			}
			_boats.Clear();
			if (_fartBall != null)
			{
				if (_fartBall.IsSpawned)
				{
					network.ServerManager.Despawn(_fartBall);
				}
				_fartBall = null;
			}
			foreach (NetworkObject shark in _sharks)
			{
				if (shark != null && shark.IsSpawned)
				{
					network.ServerManager.Despawn(shark);
				}
			}
			_sharks.Clear();
			List<PiratePlayer> active = PiratePlayer.Active;
			for (int i = 0; i < active.Count; i++)
			{
				if (active[i] != null)
				{
					active[i].HostSetReady(ready: false);
				}
			}
			RaceTrack.Instance?.ResetFinishCounter();
			YarisRules.Instance?.ResetForRematch();
			RaceState.Instance?.ResetToLobby();
			Toast?.Invoke(Loc.Get("net.toast.rematch"));
			Changed?.Invoke();
		}
	}
}

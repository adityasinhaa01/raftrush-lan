using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Managing.Timing;
using FishNet.Object;
using FishNet.Transporting;
using GamesMrkt.Core;
using GamesMrkt.Localization;
using GamesMrkt.Platforms.Steam;
using GamesMrkt.PuffPirates;
using GamesMrkt.Scene;
using PuffPirates.Backend;
using PuffPirates.Boats;
using PuffPirates.Net;
using PuffPirates.Net.Steam;
using PuffPirates.Networking;
using PuffPirates.Players;
using PuffPirates.Steam;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PuffPirates.Systems
{
	[RequireSystem(typeof(PuffSteamPresence))]
	public class ConnectionSystem : MonoBehaviour, IGameSystem
	{
		public enum PlayerLeaveReason
		{
			Left = 0,
			Dropped = 1,
			Kicked = 2
		}

		private enum ConnectMode
		{
			Host = 0,
			Client = 1,
			Solo = 2,
			QuickPlay = 3
		}

		[SerializeField]
		private NetworkManager _networkManagerPrefab;

		[SerializeField]
		private NetworkObject _playerPrefab;

		[SerializeField]
		private NetworkObject _raceStatePrefab;

		[SerializeField]
		private int _maxPlayers = 4;

		private const string CodeChars = "ABCDEFGHJKLMNPRSTUVYZ23456789";

		private const int MaxReconnectAttempts = 2;

		private const string LobbySceneName = "LobbyScene";

		public const int MinPlayersToStart = 2;

		private const int LeaveAnnounceDelayMs = 250;

		private NetworkManager _nm;

		private bool _isSolo;

		private bool _sessionLocal;

		private bool _clientEverStarted;

		private int _sessionSerial;

		private int _endHandledSerial = -1;

		private readonly Dictionary<int, string> _namesByClient = new Dictionary<int, string>();

		private readonly SteamLobbyService _lobby = new SteamLobbyService();

		private string _lastRoomCode;

		private ConnectMode _lastConnectMode = ConnectMode.Solo;

		private PuffSteamPresence _presence;

		private string _steamInitError = string.Empty;

		private LauncherState _state;

		private string _lastErrorKey;

		private (string Name, object Value)[] _lastErrorArgs = NoErrorArgs;

		private string _lastErrorCode;

		private const float STEAM_ERROR_DISPLAY_SECONDS = 6f;

		private const int ERROR_CODE_SIZE_PERCENT = 65;

		private const string ERROR_CODE_ALPHA = "#99";

		private bool _intentionalLeave = true;

		private int _reconnectAttempts;

		private bool _reconnecting;

		private bool _hostClosedRoom;

		private readonly HashSet<int> _announcedLeaving = new HashSet<int>();

		private readonly HashSet<int> _kicked = new HashSet<int>();

		private NotificationSystem _notifications;

		private bool _starting;

		private bool _leaving;

		private bool _returningToMenu;

		private const int AloneGraceMs = 12000;

		private static readonly (string Name, object Value)[] NoErrorArgs = Array.Empty<(string, object)>();

		public string SystemId => "ConnectionSystem";

		public static string NetworkVersion => "v" + Application.version + "-Demo";

		public NetworkManager Network => _nm;

		public bool IsSessionRunning
		{
			get
			{
				if (_nm != null)
				{
					if (!_nm.IsServerStarted)
					{
						return _nm.IsClientStarted;
					}
					return true;
				}
				return false;
			}
		}

		public int SessionSerial => _sessionSerial;

		public LauncherState State
		{
			get
			{
				return _state;
			}
			internal set
			{
				if (_state != value)
				{
					LauncherState state = _state;
					_state = value;
					_presence?.SetRacing(value == LauncherState.Racing || value == LauncherState.Finished);
					StateChanged?.Invoke(state, value);
				}
			}
		}

		public string RoomCode { get; set; } = "PUFF01";

		public string LastError
		{
			get
			{
				if (string.IsNullOrEmpty(_lastErrorKey))
				{
					return "";
				}
				string text = Loc.Get(_lastErrorKey, _lastErrorArgs);
				if (!string.IsNullOrEmpty(_lastErrorCode))
				{
					return string.Format("{0}\n<size={1}%><alpha={2}>{3}</size>", text, 65, "#99", _lastErrorCode);
				}
				return text;
			}
		}

		public float? LastErrorDisplaySeconds
		{
			get
			{
				if (!string.IsNullOrEmpty(_lastErrorCode))
				{
					return 6f;
				}
				return null;
			}
		}

		public int MaxPlayers => _maxPlayers;

		public bool IsHost
		{
			get
			{
				if (_nm != null)
				{
					return _nm.IsServerStarted;
				}
				return false;
			}
		}

		public bool IsSolo
		{
			get
			{
				if (IsSessionRunning)
				{
					return _isSolo;
				}
				return false;
			}
		}

		public PiratePlayer LocalPirate
		{
			get
			{
				if (!IsSessionRunning)
				{
					return null;
				}
				return PiratePlayer.Local;
			}
		}

		public NetworkBoat LocalBoat
		{
			get
			{
				PiratePlayer localPirate = LocalPirate;
				if (localPirate == null || localPirate.BoatIndex < 0)
				{
					return null;
				}
				return NetworkBoat.FindByIndex(localPirate.BoatIndex);
			}
		}

		private int MapId
		{
			get
			{
				if (!(RaceState.Instance != null))
				{
					return 0;
				}
				return RaceState.Instance.MapId;
			}
		}

		public event Action Changed;

		public event Action<string> Toast;

		public event Action<PiratePlayer, bool> PlayerObjectSpawned;

		public event Action<int> PlayerObjectDespawning;

		public event Action RunnerCleanedUp;

		public event Action<LauncherState, LauncherState> StateChanged;

		public event Action ErrorChanged;

		public event Action SceneLoadDone;

		public UniTask Initialize()
		{
			this.TryGetSystem<NotificationSystem, CoreSystemHandler>(out _notifications);
			_lobby.VersionMismatchReported += (string theirVersion) =>
			{
				if (RaceState.Instance != null)
				{
					RaceState.Instance.RpcVersionNotice(theirVersion);
				}
				else
				{
					ShowVersionNotice(theirVersion);
				}
			};
			if (_networkManagerPrefab == null)
			{
				Debug.LogError("[ConnectionSystem] _networkManagerPrefab unassigned — networking is disabled.");
			}
			else
			{
				_nm = UnityEngine.Object.Instantiate(_networkManagerPrefab);
				if (_nm == null || !_nm.Initialized)
				{
					Debug.LogError("[ConnectionSystem] NetworkManager failed to initialize — see the first FishNet error above. Networking is disabled.");
					if (_nm != null)
					{
						UnityEngine.Object.Destroy(_nm.gameObject);
					}
					_nm = null;
					return UniTask.CompletedTask;
				}
				UnityEngine.Object.DontDestroyOnLoad(_nm.gameObject);
				_nm.ServerManager.OnRemoteConnectionState += OnServerRemoteConnectionState;
				_nm.ClientManager.OnClientConnectionState += OnLocalClientConnectionState;
				_nm.ClientManager.OnRemoteConnectionState += OnClientRemoteConnectionState;
				_nm.SceneManager.OnClientLoadedStartScenes += OnClientLoadedStartScenes;
				_nm.SceneManager.OnLoadStart += OnSceneLoadStart;
				_nm.SceneManager.OnLoadEnd += OnSceneLoadEnd;
			}
			ClientTelemetry.Info("app.start", "Oyun açıldı", $"sürüm={NetworkVersion}, gpu={SystemInfo.graphicsDeviceName}, ram={SystemInfo.systemMemorySize}MB, ekran={Screen.width}x{Screen.height}");
			if (this.TryGetSystem<PuffSteamPresence, CoreSystemHandler>(out _presence) && _presence.IsAvailable)
			{
				_presence.OnJoinRequested += OnSteamJoinRequested;
				if (_presence.TryTakeLaunchJoinCode(out var roomCode))
				{
					OnSteamJoinRequested(roomCode);
				}
			}
			else
			{
				string text = "";
				if (this.TryGetSystem<SteamSystem, CoreSystemHandler>(out var system))
				{
					text = system.LastInitError;
				}
				_steamInitError = text;
				ClientTelemetry.Report("steam.init_failed", "Steam is not available at boot", $"{text}; problem={SteamDiagnostics.ClassifyInitError(text)}, 64bitOs={Environment.Is64BitOperatingSystem}");
			}
			return UniTask.CompletedTask;
		}

		private void OnSteamJoinRequested(string roomCode)
		{
			RoomCode = roomCode;
			StartClient();
		}

		public void SetError(string key, params (string Name, object Value)[] args)
		{
			ApplyError(key, args, null);
		}

		private void ApplyError(string key, (string Name, object Value)[] args, string errorCode)
		{
			_lastErrorKey = key;
			_lastErrorArgs = args ?? NoErrorArgs;
			_lastErrorCode = errorCode;
			ErrorChanged?.Invoke();
		}

		private void ClearError()
		{
			if (_lastErrorKey != null)
			{
				_lastErrorKey = null;
				ErrorChanged?.Invoke();
			}
		}

		public void RaiseToast(string message)
		{
			Toast?.Invoke(message);
		}

		public void StartHost()
		{
			RoomCode = NewRoomCode();
			StartGame(ConnectMode.Host);
		}

		public void StartClient()
		{
			StartGame(ConnectMode.Client);
		}

		public void StartSolo()
		{
			StartGame(ConnectMode.Solo);
		}

		public void StartQuickPlay()
		{
			StartGame(ConnectMode.QuickPlay);
		}

		public static string NewRoomCode()
		{
			char[] array = new char[6];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = "ABCDEFGHJKLMNPRSTUVYZ23456789"[UnityEngine.Random.Range(0, "ABCDEFGHJKLMNPRSTUVYZ23456789".Length)];
			}
			return new string(array);
		}

		public void CopyRoomCode()
		{
			GUIUtility.systemCopyBuffer = RoomCode;
			Toast?.Invoke(Loc.Get("net.toast.room_code_copied", ("code", RoomCode)));
		}

		public void Kick(int clientId)
		{
			if (IsHost && (!(_nm.ClientManager.Connection != null) || clientId != _nm.ClientManager.Connection.ClientId))
			{
				_kicked.Add(clientId);
				if (_nm.ServerManager.Clients.TryGetValue(clientId, out var value))
				{
					value.Disconnect(immediately: true);
				}
			}
		}

		public void NotePlayerLeaving(int clientId)
		{
			if (IsHost)
			{
				_announcedLeaving.Add(clientId);
			}
		}

		public void NotePlayerName(int clientId, string name)
		{
			if (IsHost && !string.IsNullOrEmpty(name))
			{
				_namesByClient[clientId] = name;
			}
		}

		public void NoteHostClosing()
		{
			if (!IsHost)
			{
				_hostClosedRoom = true;
				LeaveOnHostClosingAsync(_sessionSerial).Forget();
			}
		}

		private async UniTaskVoid LeaveOnHostClosingAsync(int serial)
		{
			await UniTask.Yield(PlayerLoopTiming.Update);
			if (!(this == null) && serial == _sessionSerial && IsSessionRunning)
			{
				_intentionalLeave = true;
				SetError("net.error.host_left");
				NetTransportSelector.StopAll(_nm);
			}
		}

		public void ShowVersionNotice(string theirVersion)
		{
			if (LobbyMeta.IsNewer(theirVersion, NetworkVersion))
			{
				Toast?.Invoke(Loc.Get("net.toast.host_old_version"));
			}
			else if (LobbyMeta.IsNewer(NetworkVersion, theirVersion))
			{
				Toast?.Invoke(Loc.Get("net.toast.player_old_version"));
			}
		}

		public void ShowPlayerLeftNotice(string playerName, PlayerLeaveReason reason)
		{
			string text = Loc.Get(reason switch
			{
				PlayerLeaveReason.Kicked => "net.toast.player_kicked", 
				PlayerLeaveReason.Dropped => "net.toast.player_dropped", 
				_ => "net.toast.player_left", 
			}, ("name", playerName));
			Toast?.Invoke(text);
			_notifications?.Show(text, isGoodAlert: false);
		}

		private async void StartGame(ConnectMode mode, bool isReconnectAttempt = false)
		{
			if (_starting || IsSessionRunning || _nm == null)
			{
				return;
			}
			_starting = true;
			int serial = ++_sessionSerial;
			ClearError();
			State = LauncherState.Connecting;
			Changed?.Invoke();
			ResolveLoadingSystem()?.ShowAsync().Forget();
			try
			{
				if (!isReconnectAttempt)
				{
					_reconnectAttempts = 0;
				}
				_reconnecting = isReconnectAttempt;
				_lastConnectMode = mode;
				if (mode != ConnectMode.Solo)
				{
					_intentionalLeave = false;
				}
				_leaving = false;
				_hostClosedRoom = false;
				_announcedLeaving.Clear();
				_kicked.Clear();
				_namesByClient.Clear();
				_clientEverStarted = false;
				_isSolo = mode == ConnectMode.Solo;
				bool steamOk = _presence != null && _presence.IsAvailable;
				if (mode != ConnectMode.Solo && SteamDiagnostics.TryGetSimulatedProblem(out var problem))
				{
					Debug.LogWarning($"[ConnectionSystem] Simulating Steam problem {problem}.");
					FailSteam(problem, 0, "simulated");
					return;
				}
				if (((mode != ConnectMode.Solo) & steamOk) && !SteamUser.BLoggedOn())
				{
					Debug.LogWarning("[ConnectionSystem] Steam is not logged on; waiting up to 60 s for it to reconnect.");
					Toast?.Invoke(Loc.Get("net.toast.steam_reconnecting"));
					ClientTelemetry.Warn("steam.offline_wait", "Steam not logged on, waiting for reconnect", $"mode={mode}");
					bool flag = !(await UniTask.WaitUntil(() => this == null || serial != _sessionSerial || SteamUser.BLoggedOn()).TimeoutWithoutException(TimeSpan.FromSeconds(60.0), DelayType.Realtime));
					if (this == null || serial != _sessionSerial)
					{
						return;
					}
					if (!flag)
					{
						Debug.LogError("[ConnectionSystem] Steam did not reconnect within 60 s.");
						ClientTelemetry.Report("steam.offline_timeout", "Steam did not reconnect within 60 s", $"mode={mode}");
						FailSteam(SteamProblem.Offline, 0, "not logged on after 60 s");
						return;
					}
					Debug.Log("[ConnectionSystem] Steam reconnected, continuing.");
					ClientTelemetry.Info("steam.offline_recovered", "Steam reconnected while waiting", $"mode={mode}");
				}
				if (mode != ConnectMode.Solo && !steamOk)
				{
					Debug.LogWarning("[ConnectionSystem] Online play blocked: Steam is not available (" + _steamInitError + ").");
					FailSteam(SteamDiagnostics.ClassifyInitError(_steamInitError), 0, _steamInitError);
					return;
				}
				_sessionLocal = mode == ConnectMode.Solo || NetTransportSelector.UseLocalTransport(steamOk);
				ulong hostSteamId = 0uL;
				bool hosting;
				if (_sessionLocal)
				{
					hosting = mode != ConnectMode.Client;
				}
				else
				{
					LobbyJoinError lobbyJoinError = LobbyJoinError.None;
					hosting = mode == ConnectMode.Host;
					if ((mode == ConnectMode.Client) | isReconnectAttempt)
					{
						string code = (isReconnectAttempt ? _lastRoomCode : RoomCode);
						(hostSteamId, lobbyJoinError) = await _lobby.JoinByCodeAsync(code, NetworkVersion);
					}
					else if (mode == ConnectMode.QuickPlay)
					{
						string roomCode;
						(hostSteamId, roomCode, lobbyJoinError) = await _lobby.FindQuickPlayAsync(NetworkVersion);
						if (lobbyJoinError == LobbyJoinError.NotFound)
						{
							await UniTask.Delay(UnityEngine.Random.Range(300, 1500), ignoreTimeScale: true);
							if (this == null || serial != _sessionSerial)
							{
								return;
							}
							(hostSteamId, roomCode, lobbyJoinError) = await _lobby.FindQuickPlayAsync(NetworkVersion);
						}
						switch (lobbyJoinError)
						{
						case LobbyJoinError.NotFound:
							hosting = true;
							RoomCode = NewRoomCode();
							lobbyJoinError = LobbyJoinError.None;
							break;
						case LobbyJoinError.None:
							RoomCode = roomCode;
							break;
						}
					}
					if (this == null || serial != _sessionSerial)
					{
						return;
					}
					if (hosting && lobbyJoinError == LobbyJoinError.None)
					{
						lobbyJoinError = (await _lobby.CreateAsync(RoomCode, mode == ConnectMode.QuickPlay, _maxPlayers, MapId, NetworkVersion)).Item2;
						if (this == null || serial != _sessionSerial)
						{
							return;
						}
					}
					string text;
					string key;
					switch (lobbyJoinError)
					{
					case LobbyJoinError.SteamError:
						FailSteam(_lobby.LastProblem, _lobby.LastResultCode, $"lobby {mode}");
						return;
					case LobbyJoinError.NotFound:
						text = (isReconnectAttempt ? "net.error.host_left" : "net.error.game_not_found");
						goto IL_080f;
					case LobbyJoinError.VersionMismatch:
						text = "net.error.version_mismatch";
						goto IL_080f;
					case LobbyJoinError.HostOlder:
						text = "net.error.host_older";
						goto IL_080f;
					case LobbyJoinError.Full:
						text = "net.error.game_full";
						goto IL_080f;
					default:
						text = "net.error.unknown";
						goto IL_080f;
					case LobbyJoinError.None:
						break;
						IL_080f:
						key = text;
						FailStart(key, new (string, object)[1] { ("error", lobbyJoinError.ToString()) });
						return;
					}
				}
				_nm.TimeManager.SetPhysicsMode(PhysicsMode.TimeManager);
				bool flag2 = (hosting ? NetTransportSelector.StartHost(_nm, _sessionLocal, (mode == ConnectMode.Solo) ? 1 : _maxPlayers) : (_sessionLocal ? NetTransportSelector.StartClientLocal(_nm) : NetTransportSelector.StartClientSteam(_nm, hostSteamId)));
				bool hostRetried = false;
				if ((!flag2 & hosting) && !_sessionLocal)
				{
					Debug.LogWarning("[ConnectionSystem] Host transport failed to start, retrying once.");
					ClientTelemetry.Warn("net.host_transport_retry", "Host transport failed to start", "retrying once");
					hostRetried = true;
					NetTransportSelector.StopAll(_nm);
					await UniTask.Delay(600, DelayType.Realtime);
					if (this == null || serial != _sessionSerial)
					{
						return;
					}
					flag2 = NetTransportSelector.StartHost(_nm, _sessionLocal, _maxPlayers);
				}
				if (!flag2)
				{
					if (!_sessionLocal && SteamLobbyService.HasRelayFailed())
					{
						FailSteam(SteamProblem.RelayUnavailable, 0, "transport start, relay failed");
						return;
					}
					FailStart("net.error.start_failed", new (string, object)[1] { ("error", "transport") });
					return;
				}
				_lastRoomCode = RoomCode;
				_reconnectAttempts = 0;
				if (mode != ConnectMode.Solo)
				{
					_presence?.SetInRoom(RoomCode);
				}
				if (hosting)
				{
					bool timedOut = await UniTask.WaitUntil(() => this == null || serial != _sessionSerial || (_nm.IsServerStarted && _nm.IsClientStarted)).TimeoutWithoutException(TimeSpan.FromSeconds(10.0), DelayType.Realtime);
					if (this == null || serial != _sessionSerial)
					{
						return;
					}
					if (timedOut && !hostRetried && !_sessionLocal)
					{
						Debug.LogWarning("[ConnectionSystem] Host did not finish starting within 10 s, retrying once.");
						ClientTelemetry.Warn("net.host_start_timeout_retry", "Host did not finish starting within 10 s", "retrying once");
						NetTransportSelector.StopAll(_nm);
						await UniTask.Delay(600, DelayType.Realtime);
						if (this == null || serial != _sessionSerial)
						{
							return;
						}
						if (NetTransportSelector.StartHost(_nm, _sessionLocal, _maxPlayers))
						{
							timedOut = await UniTask.WaitUntil(() => this == null || serial != _sessionSerial || (_nm.IsServerStarted && _nm.IsClientStarted)).TimeoutWithoutException(TimeSpan.FromSeconds(10.0), DelayType.Realtime);
							if (this == null || serial != _sessionSerial)
							{
								return;
							}
						}
					}
					if (timedOut)
					{
						Debug.LogError($"[ConnectionSystem] Host did not finish starting within 10 s (server={_nm.IsServerStarted}, client={_nm.IsClientStarted}).");
						FailStart("net.error.timeout", NoErrorArgs);
						return;
					}
					if (_raceStatePrefab == null)
					{
						Debug.LogError("[ConnectionSystem] _raceStatePrefab unassigned.");
					}
					else
					{
						SpawnGlobal(_raceStatePrefab, Vector3.zero, Quaternion.identity, null);
					}
					SceneLoadData sceneLoadData = new SceneLoadData("LobbyScene")
					{
						ReplaceScenes = ReplaceOption.None,
						PreferredActiveScene = new PreferredScene(new SceneLookupData("LobbyScene"))
					};
					_nm.SceneManager.LoadGlobalScenes(sceneLoadData);
				}
				State = LauncherState.Lobby;
				Changed?.Invoke();
			}
			catch (Exception ex)
			{
				if (!(this == null) && serial == _sessionSerial)
				{
					Debug.LogException(ex, this);
					FailStart("net.error.start_failed", new (string, object)[1] { ("error", ex.Message) });
				}
			}
			finally
			{
				if (this != null && serial == _sessionSerial)
				{
					_starting = false;
					_reconnecting = false;
				}
			}
		}

		private NetworkObject SpawnGlobal(NetworkObject prefab, Vector3 position, Quaternion rotation, NetworkConnection owner)
		{
			NetworkObject networkObject = UnityEngine.Object.Instantiate(prefab, position, rotation);
			networkObject.SetIsGlobal(value: true);
			_nm.ServerManager.Spawn(networkObject, owner);
			return networkObject;
		}

		private void FailSteam(SteamProblem problem, int steamResultCode, string detail)
		{
			string errorCode = SteamDiagnostics.ErrorCodeFor(problem, steamResultCode);
			FailStart(SteamDiagnostics.MessageKeyFor(problem), NoErrorArgs, errorCode, $"problem={problem}, steamResult={steamResultCode}, {detail}");
		}

		private void FailStart(string key, (string Name, object Value)[] args, string errorCode = null, string detail = null)
		{
			string text = ((args != null && args.Length != 0) ? string.Join(", ", args.Select(((string Name, object Value) a) => $"{a.Name}={a.Value}")) : "");
			Debug.LogError("[ConnectionSystem] FailStart: " + key + ((text.Length > 0) ? (" (" + text + ")") : "") + ((detail != null) ? (" [" + detail + "]") : ""));
			ClientTelemetry.Report("net.fail_start", (errorCode != null) ? (key + " " + errorCode) : key, ((text.Length > 0) ? (text + "; ") : "") + ((detail != null) ? (detail + "; ") : "") + $"mode={_lastConnectMode}, sessionLocal={_sessionLocal}, steamAvailable={_presence != null && _presence.IsAvailable}");
			_endHandledSerial = _sessionSerial;
			_intentionalLeave = true;
			ApplyError(key, args, errorCode);
			Toast?.Invoke(LastError);
			CleanupRunner();
			ReturnToMenu();
			Changed?.Invoke();
		}

		public void Leave()
		{
			LeaveAsync().Forget();
		}

		public async UniTask LeaveAsync()
		{
			if (_leaving)
			{
				return;
			}
			_leaving = true;
			_intentionalLeave = true;
			int serial = _sessionSerial;
			if (IsSessionRunning && AnnounceLeave())
			{
				await UniTask.Delay(250, ignoreTimeScale: true);
				if (this == null || serial != _sessionSerial || !IsSessionRunning)
				{
					return;
				}
			}
			if (IsSessionRunning)
			{
				NetTransportSelector.StopAll(_nm);
			}
			else
			{
				CleanupRunner();
				ReturnToMenu();
			}
			Changed?.Invoke();
		}

		private bool AnnounceLeave()
		{
			if (IsSolo)
			{
				return false;
			}
			if (IsHost)
			{
				if (PiratePlayer.Active.Count <= 1 || RaceState.Instance == null)
				{
					return false;
				}
				RaceState.Instance.RpcHostClosing();
				return true;
			}
			PiratePlayer localPirate = LocalPirate;
			if (localPirate == null)
			{
				return false;
			}
			localPirate.RpcAnnounceLeaving();
			return true;
		}

		private void ReturnToMenu()
		{
			if (_returningToMenu)
			{
				return;
			}
			_presence?.SetInMenu();
			State = LauncherState.Menu;
			if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MenuScene")
			{
				ResolveLoadingSystem()?.HideAsync("MenuScene").Forget();
				return;
			}
			CoreSystemHandler instance = Singleton<CoreSystemHandler>.Instance;
			SceneFlowSystem sceneFlowSystem = ((instance != null) ? instance.GetSystem<SceneFlowSystem>() : null);
			LoadingSystem loading = ((instance != null) ? instance.GetSystem<LoadingSystem>() : null);
			if (sceneFlowSystem != null)
			{
				ReturnToMenuAsync(sceneFlowSystem, loading).Forget();
			}
			else
			{
				UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene", LoadSceneMode.Single);
			}
		}

		private async UniTaskVoid ReturnToMenuAsync(SceneFlowSystem sceneFlow, LoadingSystem loading)
		{
			_returningToMenu = true;
			try
			{
				if (loading != null)
				{
					await loading.ShowAsync();
				}
				await sceneFlow.LoadAsync("MenuScene");
				if (loading != null)
				{
					await loading.HideAsync("MenuScene");
				}
			}
			finally
			{
				_returningToMenu = false;
			}
		}

		private void CleanupRunner()
		{
			NetTransportSelector.StopAll(_nm);
			if (_nm != null)
			{
				_nm.TimeManager.SetPhysicsMode(PhysicsMode.Unity);
			}
			_lobby.Leave();
			_namesByClient.Clear();
			_isSolo = false;
			_starting = false;
			RunnerCleanedUp?.Invoke();
		}

		private void OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
		{
			if (!asServer)
			{
				return;
			}
			if (PiratePlayer.TryGet(conn.ClientId, out var _))
			{
				Changed?.Invoke();
				return;
			}
			if (_playerPrefab == null)
			{
				Debug.LogError("[ConnectionSystem] _playerPrefab unassigned.");
				return;
			}
			PiratePlayer component = SpawnGlobal(_playerPrefab, new Vector3(0f, 1f, -50f), Quaternion.identity, conn).GetComponent<PiratePlayer>();
			if (component == null)
			{
				Debug.LogError("[ConnectionSystem] _playerPrefab has no PiratePlayer.");
				return;
			}
			RaceState instance = RaceState.Instance;
			bool arg = instance != null && !instance.InLobby;
			PlayerObjectSpawned?.Invoke(component, arg);
			Changed?.Invoke();
		}

		private void OnServerRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
		{
			if (args.ConnectionState != RemoteConnectionState.Stopped)
			{
				Changed?.Invoke();
				return;
			}
			int clientId = conn.ClientId;
			string text = (_namesByClient.TryGetValue(clientId, out var value) ? value : null);
			if (PiratePlayer.TryGet(clientId, out var pirate))
			{
				if (!string.IsNullOrEmpty(pirate.PlayerName))
				{
					text = pirate.PlayerName;
				}
				PlayerObjectDespawning?.Invoke(clientId);
				if (pirate.IsSpawned)
				{
					_nm.ServerManager.Despawn(pirate.NetworkObject);
				}
			}
			if (string.IsNullOrEmpty(text))
			{
				text = $"Player {clientId}";
			}
			_namesByClient.Remove(clientId);
			PlayerLeaveReason playerLeaveReason = (_kicked.Remove(clientId) ? PlayerLeaveReason.Kicked : ((!_announcedLeaving.Remove(clientId)) ? PlayerLeaveReason.Dropped : PlayerLeaveReason.Left));
			if (RaceState.Instance != null)
			{
				RaceState.Instance.RpcPlayerLeftNotice(text, (int)playerLeaveReason);
			}
			else
			{
				ShowPlayerLeftNotice(text, playerLeaveReason);
			}
			LeaveIfAloneMidRace(clientId, playerLeaveReason == PlayerLeaveReason.Dropped);
			Changed?.Invoke();
		}

		private void OnClientRemoteConnectionState(RemoteConnectionStateArgs args)
		{
			Changed?.Invoke();
		}

		private void LeaveIfAloneMidRace(int leaverClientId, bool waitForReconnect)
		{
			if (IsSolo)
			{
				return;
			}
			RaceState instance = RaceState.Instance;
			if (!(instance == null) && instance.RaceRunning && PlayersOtherThan(leaverClientId) <= 1)
			{
				if (waitForReconnect)
				{
					LeaveIfStillAloneAsync(leaverClientId, _sessionSerial).Forget();
				}
				else
				{
					Leave();
				}
			}
		}

		private async UniTaskVoid LeaveIfStillAloneAsync(int leaverClientId, int serial)
		{
			await UniTask.Delay(12000, ignoreTimeScale: true);
			if (!(this == null) && serial == _sessionSerial && IsHost)
			{
				RaceState instance = RaceState.Instance;
				if (!(instance == null) && instance.RaceRunning && PlayersOtherThan(leaverClientId) <= 1)
				{
					Leave();
				}
			}
		}

		private int PlayersOtherThan(int leaverClientId)
		{
			int num = _nm.ServerManager.Clients.Count;
			if (_nm.ServerManager.Clients.ContainsKey(leaverClientId))
			{
				num--;
			}
			return num;
		}

		private void OnLocalClientConnectionState(ClientConnectionStateArgs args)
		{
			if (args.ConnectionState == LocalConnectionState.Started)
			{
				_clientEverStarted = true;
			}
			else if (args.ConnectionState == LocalConnectionState.Stopped && _endHandledSerial != _sessionSerial)
			{
				if (!_clientEverStarted && !_intentionalLeave)
				{
					_endHandledSerial = _sessionSerial;
					_intentionalLeave = true;
					SetError("net.error.timeout");
					Toast?.Invoke(Loc.Get("net.error.timeout"));
					DeferredAfterDrop(reconnect: false).Forget();
				}
				else
				{
					HandleUnexpectedDrop(IsHost ? null : Loc.Get("net.error.host_left"));
				}
			}
		}

		private void HandleUnexpectedDrop(string toastMessage)
		{
			if (_reconnecting)
			{
				return;
			}
			_endHandledSerial = _sessionSerial;
			if (_hostClosedRoom && !_intentionalLeave)
			{
				_intentionalLeave = true;
				SetError("net.error.host_left");
				DeferredAfterDrop(reconnect: false).Forget();
			}
			else if (!_intentionalLeave && _lastConnectMode != ConnectMode.Solo && !_sessionLocal && !IsHost && !string.IsNullOrEmpty(_lastRoomCode) && _reconnectAttempts < 2)
			{
				_reconnecting = true;
				_reconnectAttempts++;
				Toast?.Invoke(Loc.Get("net.toast.reconnecting", ("attempt", _reconnectAttempts), ("max", 2)));
				DeferredAfterDrop(reconnect: true).Forget();
			}
			else
			{
				_intentionalLeave = true;
				if (toastMessage != null)
				{
					Toast?.Invoke(toastMessage);
				}
				DeferredAfterDrop(reconnect: false).Forget();
			}
		}

		private async UniTaskVoid DeferredAfterDrop(bool reconnect)
		{
			await UniTask.Yield(PlayerLoopTiming.Update);
			if (!(this == null))
			{
				CleanupRunner();
				if (reconnect)
				{
					_reconnecting = false;
					StartGame(ConnectMode.Client, isReconnectAttempt: true);
				}
				else
				{
					ReturnToMenu();
					Changed?.Invoke();
				}
			}
		}

		private static LoadingSystem ResolveLoadingSystem()
		{
			if (!(Singleton<CoreSystemHandler>.Instance != null))
			{
				return null;
			}
			return Singleton<CoreSystemHandler>.Instance.GetSystem<LoadingSystem>();
		}

		private void OnSceneLoadStart(SceneLoadStartEventArgs args)
		{
			ResolveLoadingSystem()?.ShowAsync().Forget();
		}

		private void OnSceneLoadEnd(SceneLoadEndEventArgs args)
		{
			if (args.QueueData.AsServer)
			{
				SceneLoadDone?.Invoke();
				return;
			}
			LoadingSystem loadingSystem = ResolveLoadingSystem();
			if (UnityEngine.SceneManagement.SceneManager.GetSceneByName("PuffTest").isLoaded)
			{
				UnloadContentSceneIfLoaded("MenuScene");
				if (loadingSystem != null)
				{
					loadingSystem.HideAsync("PuffTest").Forget();
				}
			}
			else if (UnityEngine.SceneManagement.SceneManager.GetSceneByName("LobbyScene").isLoaded)
			{
				UnloadContentSceneIfLoaded("MenuScene");
				if (loadingSystem != null)
				{
					loadingSystem.HideAsync("LobbyScene").Forget();
				}
			}
			if (!IsServerStartedSafe())
			{
				SceneLoadDone?.Invoke();
			}
		}

		private bool IsServerStartedSafe()
		{
			if (_nm != null)
			{
				return _nm.IsServerStarted;
			}
			return false;
		}

		private static void UnloadContentSceneIfLoaded(string sceneName)
		{
			Scene sceneByName = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
			if (sceneByName.IsValid() && sceneByName.isLoaded)
			{
				UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneByName);
			}
		}
	}
}

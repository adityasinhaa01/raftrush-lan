using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GamesMrkt.Core;
using GamesMrkt.Localization;
using PuffPirates.Modes;
using PuffPirates.Networking;
using PuffPirates.Players;
using UnityEngine;

namespace PuffPirates.Systems
{
	[RequireSystem(typeof(ConnectionSystem))]
	public class LobbySystem : IGameSystem, IUpdateableSystem
	{
		public struct PlayerSlot
		{
			public int ClientId;

			public int TeamId;

			public bool IsLocal;

			public bool IsReady;
		}

		private readonly ConnectionSystem _connection;

		public readonly List<PlayerSlot> Players = new List<PlayerSlot>();

		private float _nextPlayerScan;

		public static bool EditorSoloStart;

		public string SystemId => "LobbySystem";

		public int TeamCount
		{
			get
			{
				if (!(RaceState.Instance != null))
				{
					return 2;
				}
				return RaceState.Instance.TeamCount;
			}
		}

		public int TeamSize
		{
			get
			{
				if (!(RaceState.Instance != null))
				{
					return 2;
				}
				return RaceState.Instance.TeamSize;
			}
		}

		public int MapId
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

		public int ThemeId
		{
			get
			{
				if (!(RaceState.Instance != null))
				{
					return 0;
				}
				return RaceState.Instance.ThemeId;
			}
		}

		public int ModeId
		{
			get
			{
				if (!(RaceState.Instance != null))
				{
					return 0;
				}
				return RaceState.Instance.ModeId;
			}
		}

		public int BoatCount
		{
			get
			{
				if (!(RaceState.Instance != null))
				{
					return 2;
				}
				return RaceState.Instance.BoatCount;
			}
		}

		public int LapCount
		{
			get
			{
				if (!(RaceState.Instance != null))
				{
					return 3;
				}
				return RaceState.Instance.LapCount;
			}
		}

		public bool IsFootball
		{
			get
			{
				if (RaceState.Instance != null)
				{
					return RaceState.Instance.IsFootball;
				}
				return false;
			}
		}

		public bool IsRaceMode
		{
			get
			{
				if (RaceState.Instance != null)
				{
					return RaceState.Instance.IsRace;
				}
				return false;
			}
		}

		public bool TeamsValid
		{
			get
			{
				if (Players.Count == 0)
				{
					return false;
				}
				RaceState instance = RaceState.Instance;
				if (instance == null)
				{
					return false;
				}
				int teamCount = instance.TeamCount;
				foreach (PlayerSlot player in Players)
				{
					if (player.TeamId < 0 || player.TeamId >= teamCount)
					{
						return false;
					}
				}
				if (Players.Count == 1)
				{
					return true;
				}
				if (instance.IsFootball)
				{
					int boatCount = instance.BoatCount;
					if (PlayersInTeam(0) == boatCount)
					{
						return PlayersInTeam(1) == boatCount;
					}
					return false;
				}
				int num = PlayersInTeam(0);
				if (num < 1 || num > instance.TeamSize)
				{
					return false;
				}
				for (int i = 1; i < teamCount; i++)
				{
					if (PlayersInTeam(i) != num)
					{
						return false;
					}
				}
				return true;
			}
		}

		public int TeamCapacity
		{
			get
			{
				RaceState instance = RaceState.Instance;
				if (instance == null)
				{
					return 2;
				}
				if (!instance.IsFootball)
				{
					return instance.TeamSize;
				}
				return instance.BoatCount;
			}
		}

		public bool AllReady
		{
			get
			{
				if (Players.Count == 0)
				{
					return false;
				}
				foreach (PlayerSlot player in Players)
				{
					if (!player.IsReady)
					{
						return false;
					}
				}
				return true;
			}
		}

		public bool HasEnoughPlayers
		{
			get
			{
				if (!_connection.IsSolo && !EditorSoloStart)
				{
					return Players.Count >= 2;
				}
				return true;
			}
		}

		public bool EveryoneReady
		{
			get
			{
				if (_connection.IsHost)
				{
					return AllReady;
				}
				return false;
			}
		}

		public bool CanStart
		{
			get
			{
				if (EveryoneReady)
				{
					return HasEnoughPlayers;
				}
				return false;
			}
		}

		public event Action Changed;

		public event Action<string> Toast;

		public LobbySystem(ConnectionSystem connection)
		{
			_connection = connection;
		}

		public UniTask Initialize()
		{
			_connection.PlayerObjectSpawned += HandlePlayerObjectSpawned;
			return UniTask.CompletedTask;
		}

		public void OnUpdate(float deltaTime)
		{
			RebuildPlayers();
		}

		private void RebuildPlayers()
		{
			if (!_connection.IsSessionRunning)
			{
				if (Players.Count > 0)
				{
					Players.Clear();
					Changed?.Invoke();
				}
				return;
			}
			if (_connection.State == LauncherState.Racing)
			{
				if (Time.unscaledTime < _nextPlayerScan)
				{
					return;
				}
				_nextPlayerScan = Time.unscaledTime + 0.2f;
			}
			bool flag = false;
			int num = 0;
			List<PiratePlayer> active = PiratePlayer.Active;
			for (int i = 0; i < active.Count; i++)
			{
				PiratePlayer piratePlayer = active[i];
				if (piratePlayer == null)
				{
					continue;
				}
				PlayerSlot playerSlot = new PlayerSlot
				{
					ClientId = piratePlayer.ClientId,
					TeamId = piratePlayer.TeamId,
					IsLocal = piratePlayer.IsOwner,
					IsReady = piratePlayer.IsReady
				};
				if (num < Players.Count)
				{
					PlayerSlot playerSlot2 = Players[num];
					if (playerSlot2.ClientId != playerSlot.ClientId || playerSlot2.TeamId != playerSlot.TeamId || playerSlot2.IsReady != playerSlot.IsReady)
					{
						Players[num] = playerSlot;
						flag = true;
					}
				}
				else
				{
					Players.Add(playerSlot);
					flag = true;
				}
				num++;
			}
			while (Players.Count > num)
			{
				Players.RemoveAt(Players.Count - 1);
				flag = true;
			}
			if (flag)
			{
				Changed?.Invoke();
			}
		}

		public int PlayersInTeam(int team)
		{
			if (_connection.IsSessionRunning)
			{
				return CountPlayersInTeam(team);
			}
			int num = 0;
			foreach (PlayerSlot player in Players)
			{
				if (player.TeamId == team)
				{
					num++;
				}
			}
			return num;
		}

		private static int CountPlayersInTeam(int team)
		{
			int num = 0;
			List<PiratePlayer> active = PiratePlayer.Active;
			for (int i = 0; i < active.Count; i++)
			{
				if (active[i] != null && active[i].TeamId == team)
				{
					num++;
				}
			}
			return num;
		}

		public void SetTeamCount(int n)
		{
			if (_connection.IsHost)
			{
				RaceState.Instance?.SetTeamCount(n);
				ReseatOutOfRangePlayers();
			}
		}

		public void SetTeamSize(int n)
		{
			if (_connection.IsHost)
			{
				RaceState.Instance?.SetTeamSize(n);
			}
		}

		public void SetMode(int modeId)
		{
			if (_connection.IsHost)
			{
				RaceState.Instance?.SetMode(modeId);
				ReseatOutOfRangePlayers();
				Toast?.Invoke(Loc.Get("net.toast.mode_changed", ("mode", GameModes.Name(modeId))));
			}
		}

		public void SetBoatCount(int n)
		{
			if (_connection.IsHost)
			{
				RaceState.Instance?.SetBoatCount(n);
				ReseatOutOfRangePlayers();
			}
		}

		public void SetLapCount(int n)
		{
			if (_connection.IsHost)
			{
				RaceState.Instance?.SetLapCount(n);
			}
		}

		private void ReseatOutOfRangePlayers()
		{
			if (!_connection.IsHost)
			{
				return;
			}
			int teamCount = TeamCount;
			List<PiratePlayer> active = PiratePlayer.Active;
			for (int i = 0; i < active.Count; i++)
			{
				if (active[i] != null && active[i].TeamId >= teamCount)
				{
					active[i].SetTeam(-1);
				}
			}
		}

		public void RequestTeam(int team)
		{
			PiratePlayer localPirate = _connection.LocalPirate;
			if (!(localPirate == null))
			{
				if (_connection.IsHost)
				{
					HostAssignTeam(localPirate.ClientId, team, respectCapacity: true);
				}
				else
				{
					localPirate.RpcRequestTeam(team);
				}
			}
		}

		public void HostAssignTeam(int clientId, int team, bool respectCapacity)
		{
			if (!_connection.IsHost)
			{
				return;
			}
			RaceState instance = RaceState.Instance;
			if (instance == null || !instance.InLobby)
			{
				return;
			}
			int teamCount = instance.TeamCount;
			if (team < -1 || team >= teamCount || !PiratePlayer.TryGet(clientId, out var pirate) || pirate.TeamId == team)
			{
				return;
			}
			if (respectCapacity && team >= 0 && PlayersInTeam(team) >= TeamCapacity)
			{
				if (pirate.IsOwner)
				{
					Toast?.Invoke(Loc.Get("net.toast.team_full"));
				}
			}
			else
			{
				pirate.SetTeam(team);
				Changed?.Invoke();
			}
		}

		public void ToggleReady()
		{
			PiratePlayer localPirate = _connection.LocalPirate;
			if (!(localPirate == null))
			{
				localPirate.RpcSetReady(!localPirate.IsReady);
			}
		}

		private void HandlePlayerObjectSpawned(PiratePlayer pirate, bool isReconnectMidRace)
		{
			RaceState instance = RaceState.Instance;
			pirate.TeamId = -1;
			int num = ((instance != null) ? instance.TeamCount : 2);
			int num2 = ((instance != null && !instance.IsFootball) ? 4 : TeamCapacity);
			for (int i = 0; i < num; i++)
			{
				if (PlayersInTeam(i) < num2)
				{
					pirate.TeamId = i;
					break;
				}
			}
		}
	}
}

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GamesMrkt.Audio;
using GamesMrkt.Core;
using GamesMrkt.Inputs;
using GamesMrkt.Localization;
using PuffPirates.Cosmetics;
using PuffPirates.Networking;
using PuffPirates.Players;
using PuffPirates.Steam;
using PuffPirates.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GamesMrkt.PuffPirates.UI.Lobby
{
	public class LobbyPanel : MonoBehaviour
	{
		[Header("Room code")]
		[SerializeField]
		private TMP_Text _roomCodeText;

		[SerializeField]
		private Button _copyCodeButton;

		[Header("Steam invite (hidden unless Steam is running)")]
		[Tooltip("Opens Steam's own invite-friends overlay for this room.")]
		[SerializeField]
		private Button _inviteFriendsButton;

		[Header("Ready state (two panels, toggled by IsReady — see mockup's bottom bar)")]
		[Tooltip("Single button hosting both contents below — a click toggles ready/unready (same as Space/X).")]
		[SerializeField]
		private Button _readyToggleButton;

		[SerializeField]
		private GameObject _readyContent;

		[SerializeField]
		private GameObject _unreadyContent;

		[Header("Start (host only, and only once EVERYONE in the room is ready)")]
		[SerializeField]
		private Button _startGameButton;

		[Header("Start confirmation (\"nobody can join after this\" — Yes starts, No/Esc closes)")]
		[SerializeField]
		private UIPopupAnimator _startConfirmPopup;

		[SerializeField]
		private Button _startConfirmYesButton;

		[SerializeField]
		private Button _startConfirmNoButton;

		[Tooltip("Single label showing body1 + body2 — LocalizedText takes one key, so filled here.")]
		[SerializeField]
		private TMP_Text _startConfirmBody;

		[Header("Character selection carousel (one lobby_character_selection per catalog entry)")]
		[SerializeField]
		private Transform _characterSelectionContainer;

		[SerializeField]
		private LobbyCharacterSelectionItem _characterItemPrefab;

		private ConnectionSystem _connection;

		private LobbySystem _lobby;

		private RaceLauncherSystem _raceLauncher;

		private CosmeticRelaySystem _cosmetics;

		private NotificationSystem _notifications;

		private PuffSteamPresence _steamPresence;

		private readonly List<IDisposable> _inputSubscriptions = new List<IDisposable>();

		private InputPlayer _inputPlayer;

		private readonly List<LobbyCharacterSelectionItem> _characterItems = new List<LobbyCharacterSelectionItem>();

		private int _shownCharacterId = int.MinValue;

		private bool _leftRequested;

		private bool _roomConfigPending;

		private const int CoopCrewCapacity = 4;

		private bool CanReadInput
		{
			get
			{
				if (base.isActiveAndEnabled && !_leftRequested)
				{
					return _connection != null;
				}
				return false;
			}
		}

		private bool IsLocalReady
		{
			get
			{
				if (_connection.LocalPirate != null)
				{
					return _connection.LocalPirate.IsReady;
				}
				return false;
			}
		}

		private bool IsStartConfirmOpen
		{
			get
			{
				if (_startConfirmPopup != null)
				{
					return _startConfirmPopup.IsOpen;
				}
				return false;
			}
		}

		private void Awake()
		{
			if (Singleton<CoreSystemHandler>.Instance != null)
			{
				Singleton<CoreSystemHandler>.Instance.WhenReady(ResolveSystems);
			}
			if (_copyCodeButton != null)
			{
				_copyCodeButton.onClick.AddListener(HandleCopyCode);
			}
			if (_readyToggleButton != null)
			{
				_readyToggleButton.onClick.AddListener(HandleReadyToggleClicked);
			}
			if (_startGameButton != null)
			{
				_startGameButton.onClick.AddListener(HandleStartGameClicked);
			}
			if (_startConfirmYesButton != null)
			{
				_startConfirmYesButton.onClick.AddListener(HandleConfirmYesClicked);
			}
			if (_startConfirmNoButton != null)
			{
				_startConfirmNoButton.onClick.AddListener(HandleConfirmNoClicked);
			}
			UISelection.LinkPair(_startConfirmYesButton, _startConfirmNoButton);
			SetupInviteButton();
			if (_startConfirmPopup != null)
			{
				_startConfirmPopup.Hide(instant: true);
			}
			LanguageFollow.Label(_startConfirmBody);
			ApplyStartConfirmBody();
			Loc.OnLanguageChanged += HandleLanguageChanged;
			SpawnCharacterItems();
			RefreshCharacterSelection();
		}

		private void OnDestroy()
		{
			if (_copyCodeButton != null)
			{
				_copyCodeButton.onClick.RemoveListener(HandleCopyCode);
			}
			if (_readyToggleButton != null)
			{
				_readyToggleButton.onClick.RemoveListener(HandleReadyToggleClicked);
			}
			if (_startGameButton != null)
			{
				_startGameButton.onClick.RemoveListener(HandleStartGameClicked);
			}
			if (_startConfirmYesButton != null)
			{
				_startConfirmYesButton.onClick.RemoveListener(HandleConfirmYesClicked);
			}
			if (_startConfirmNoButton != null)
			{
				_startConfirmNoButton.onClick.RemoveListener(HandleConfirmNoClicked);
			}
			if (_inviteFriendsButton != null)
			{
				_inviteFriendsButton.onClick.RemoveListener(HandleInviteFriends);
			}
			Loc.OnLanguageChanged -= HandleLanguageChanged;
			if (_connection != null)
			{
				_connection.Changed -= HandleLobbyStateChanged;
			}
			if (_lobby != null)
			{
				_lobby.Changed -= HandleLobbyStateChanged;
			}
			if (_cosmetics != null)
			{
				_cosmetics.Changed -= RefreshCharacterSelection;
			}
			foreach (IDisposable inputSubscription in _inputSubscriptions)
			{
				inputSubscription.Dispose();
			}
			_inputSubscriptions.Clear();
		}

		private void ResolveSystems()
		{
			if (!(this == null))
			{
				this.TryGetSystem<ConnectionSystem, CoreSystemHandler>(out _connection);
				this.TryGetSystem<LobbySystem, CoreSystemHandler>(out _lobby);
				this.TryGetSystem<RaceLauncherSystem, CoreSystemHandler>(out _raceLauncher);
				this.TryGetSystem<CosmeticRelaySystem, CoreSystemHandler>(out _cosmetics);
				this.TryGetSystem<NotificationSystem, CoreSystemHandler>(out _notifications);
				if (_connection != null)
				{
					_connection.Changed += HandleLobbyStateChanged;
				}
				if (_lobby != null)
				{
					_lobby.Changed += HandleLobbyStateChanged;
				}
				if (_cosmetics != null)
				{
					_cosmetics.Changed += RefreshCharacterSelection;
				}
				HandleLobbyStateChanged();
				RefreshCharacterSelection();
				SubscribeInput();
			}
		}

		private void SubscribeInput()
		{
			if (this.TryGetSystem<PlayerInputSystem, CoreSystemHandler>(out var system) && system.Player != null)
			{
				InputPlayer inputPlayer = (_inputPlayer = system.Player);
				_inputSubscriptions.Add(inputPlayer.Subscribe("Lobby/Ready", InputActionTrigger.Performed, HandleReadyPressed));
				_inputSubscriptions.Add(inputPlayer.Subscribe("Lobby/Unready", InputActionTrigger.Performed, HandleUnreadyPressed));
				_inputSubscriptions.Add(inputPlayer.Subscribe("Lobby/StartGame", InputActionTrigger.Performed, HandleStartGamePressed));
				_inputSubscriptions.Add(inputPlayer.Subscribe("Lobby/NextCharacter", InputActionTrigger.Performed, HandleNextCharacterPressed));
				_inputSubscriptions.Add(inputPlayer.Subscribe("Lobby/PreviousCharacter", InputActionTrigger.Performed, HandlePreviousCharacterPressed));
				_inputSubscriptions.Add(inputPlayer.Subscribe("UI/Cancel", InputActionTrigger.Performed, HandleCancelPressed));
			}
		}

		private void Update()
		{
			if (!_leftRequested && !(_connection == null))
			{
				if (_roomConfigPending)
				{
					_roomConfigPending = !SyncHostRoomConfig();
				}
				if (!IsStartConfirmOpen)
				{
					ClearButtonSelection();
				}
			}
		}

		private static void ClearButtonSelection()
		{
			EventSystem current = EventSystem.current;
			if (!(current == null) && !(current.currentSelectedGameObject == null) && current.currentSelectedGameObject.TryGetComponent<Button>(out var _))
			{
				current.SetSelectedGameObject(null);
			}
		}

		private void HandleLobbyStateChanged()
		{
			if (!_leftRequested && !(_connection == null))
			{
				_roomConfigPending = true;
				RefreshRoomCode();
				RefreshReadyPanel();
				RefreshStartButton();
				RefreshStartConfirm();
			}
		}

		private bool SyncHostRoomConfig()
		{
			if (_lobby == null || !_connection.IsHost)
			{
				return true;
			}
			if (RaceState.Instance == null)
			{
				return false;
			}
			int num = Mathf.Clamp(_lobby.Players.Count, 1, 4);
			if (_lobby.TeamCount != 1)
			{
				_lobby.SetTeamCount(1);
			}
			if (_lobby.TeamSize != num)
			{
				_lobby.SetTeamSize(num);
			}
			for (int i = 0; i < _lobby.Players.Count; i++)
			{
				LobbySystem.PlayerSlot playerSlot = _lobby.Players[i];
				if (playerSlot.TeamId < 0)
				{
					_lobby.HostAssignTeam(playerSlot.ClientId, 0, respectCapacity: false);
				}
			}
			return true;
		}

		private void HandleReadyPressed()
		{
			if (!CanReadInput || IsStartConfirmOpen)
			{
				return;
			}
			if (_inputPlayer != null && _inputPlayer.LastUsedDevice is Gamepad && IsLocalReady)
			{
				if (_connection.IsHost)
				{
					OpenStartConfirm();
				}
			}
			else
			{
				SetReady(ready: true);
			}
		}

		private void HandleUnreadyPressed()
		{
			if (CanReadInput && !IsStartConfirmOpen)
			{
				SetReady(ready: false);
			}
		}

		private void HandleStartGamePressed()
		{
			if (CanReadInput && !IsStartConfirmOpen)
			{
				OpenStartConfirm();
			}
		}

		private void HandleNextCharacterPressed()
		{
			if (CanReadInput && !IsStartConfirmOpen)
			{
				CycleCharacter(1);
			}
		}

		private void HandlePreviousCharacterPressed()
		{
			if (CanReadInput && !IsStartConfirmOpen)
			{
				CycleCharacter(-1);
			}
		}

		private void HandleCancelPressed()
		{
			if (CanReadInput)
			{
				if (IsStartConfirmOpen)
				{
					CloseStartConfirm();
				}
				else
				{
					HandleLeave();
				}
			}
		}

		private void SetReady(bool ready)
		{
			PiratePlayer localPirate = _connection.LocalPirate;
			if (!(localPirate == null))
			{
				localPirate.RpcSetReady(ready);
			}
		}

		private int CharacterCount()
		{
			if (!(_cosmetics != null))
			{
				return CosmeticCatalog.Count(CosmeticSlot.CharacterSkin);
			}
			return _cosmetics.Count(CosmeticSlot.CharacterSkin);
		}

		private void CycleCharacter(int direction)
		{
			int num = CharacterCount();
			if (num > 0)
			{
				int id = ((CosmeticStore.Equipped(CosmeticSlot.CharacterSkin) + direction) % num + num) % num;
				EquipCharacter(id);
			}
		}

		private void EquipCharacter(int id)
		{
			if (!(_cosmetics == null))
			{
				_cosmetics.Equip(CosmeticSlot.CharacterSkin, id, _connection.LocalPirate);
			}
		}

		private void SpawnCharacterItems()
		{
			if (!(_characterSelectionContainer == null) && !(_characterItemPrefab == null))
			{
				int num = CharacterCount();
				for (int i = 0; i < num; i++)
				{
					LobbyCharacterSelectionItem lobbyCharacterSelectionItem = UnityEngine.Object.Instantiate(_characterItemPrefab, _characterSelectionContainer);
					Sprite icon = ((_cosmetics != null) ? _cosmetics.GetLobbyIcon(i) : null);
					Color? accent = ((_cosmetics != null) ? _cosmetics.GetAccentColor(i) : ((Color?)null));
					lobbyCharacterSelectionItem.Setup(i, icon, accent, EquipCharacter);
					_characterItems.Add(lobbyCharacterSelectionItem);
				}
			}
		}

		private void RefreshCharacterSelection()
		{
			if (_characterItems.Count == 0)
			{
				return;
			}
			int num = CosmeticStore.Equipped(CosmeticSlot.CharacterSkin);
			if (num != _shownCharacterId)
			{
				_shownCharacterId = num;
				for (int i = 0; i < _characterItems.Count; i++)
				{
					_characterItems[i].SetSelected(i == num);
				}
			}
		}

		private void RefreshRoomCode()
		{
			if (_roomCodeText != null && _roomCodeText.text != _connection.RoomCode)
			{
				_roomCodeText.text = _connection.RoomCode;
			}
		}

		private void RefreshReadyPanel()
		{
			bool flag = _connection.LocalPirate != null && _connection.LocalPirate.IsReady;
			if (_readyContent != null)
			{
				_readyContent.SetActive(!flag);
			}
			if (_unreadyContent != null)
			{
				_unreadyContent.SetActive(flag);
			}
		}

		private void RefreshStartButton()
		{
			if (_startGameButton != null)
			{
				_startGameButton.gameObject.SetActive(_lobby != null && _lobby.EveryoneReady);
			}
		}

		private void SetupInviteButton()
		{
			if (_inviteFriendsButton == null)
			{
				return;
			}
			_inviteFriendsButton.gameObject.SetActive(value: false);
			_inviteFriendsButton.onClick.AddListener(HandleInviteFriends);
			this.WhenSystemReady<PuffSteamPresence, CoreSystemHandler>((PuffSteamPresence presence) =>
			{
				if (!(this == null) && !(_inviteFriendsButton == null) && presence.IsAvailable)
				{
					_steamPresence = presence;
					_inviteFriendsButton.gameObject.SetActive(value: true);
				}
			});
		}

		private void HandleInviteFriends()
		{
			if (!_leftRequested && _steamPresence != null)
			{
				AudioManager.PlaySound(CommonSound.ButtonClick);
				if (!_steamPresence.InviteFriends())
				{
					Debug.LogWarning("[LobbyPanel] Steam invite dialog could not open (no room yet, or overlay disabled).");
				}
			}
		}

		private void HandleCopyCode()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			_connection.CopyRoomCode();
			_notifications?.Show(Loc.Get("net.toast.room_code_copied", ("code", _connection.RoomCode)), isGoodAlert: true);
		}

		private void HandleReadyToggleClicked()
		{
			if (!_leftRequested && !(_connection == null))
			{
				PiratePlayer localPirate = _connection.LocalPirate;
				if (!(localPirate == null))
				{
					AudioManager.PlaySound(CommonSound.ButtonClick);
					SetReady(!localPirate.IsReady);
				}
			}
		}

		private void HandleStartGameClicked()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			OpenStartConfirm();
		}

		private void HandleConfirmYesClicked()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			HandleStartGame();
		}

		private void HandleConfirmNoClicked()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			CloseStartConfirm();
		}

		private void OpenStartConfirm()
		{
			if (_lobby == null)
			{
				return;
			}
			if (_lobby.EveryoneReady && !_lobby.HasEnoughPlayers)
			{
				_notifications?.Show(Loc.Get("net.toast.need_more_players", ("min", 2)), isGoodAlert: false);
			}
			else
			{
				if (!_lobby.CanStart)
				{
					return;
				}
				if (_startConfirmPopup == null)
				{
					HandleStartGame();
					return;
				}
				_startConfirmPopup.Show();
				UniTask.Post(() =>
				{
					if (this != null && IsStartConfirmOpen)
					{
						UISelection.Select(_startConfirmYesButton);
					}
				}, PlayerLoopTiming.LastPostLateUpdate);
			}
		}

		private void HandleLanguageChanged(string language)
		{
			ApplyStartConfirmBody();
		}

		private void ApplyStartConfirmBody()
		{
			if (!(_startConfirmBody == null))
			{
				_startConfirmBody.text = Loc.Get("lobby.startconfirm.body1");
			}
		}

		private void CloseStartConfirm()
		{
			if (_startConfirmPopup != null)
			{
				_startConfirmPopup.Hide();
			}
		}

		private void RefreshStartConfirm()
		{
			if (IsStartConfirmOpen && (_lobby == null || !_lobby.CanStart))
			{
				CloseStartConfirm();
			}
		}

		private void HandleStartGame()
		{
			if (!(_raceLauncher == null) && _lobby != null && _lobby.CanStart)
			{
				CloseStartConfirm();
				_raceLauncher.BeginRace();
			}
		}

		private void HandleLeave()
		{
			if (!_leftRequested)
			{
				_leftRequested = true;
				_connection.Leave();
			}
		}
	}
}

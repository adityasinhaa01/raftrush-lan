using GamesMrkt.Audio;
using GamesMrkt.Core;
using GamesMrkt.Localization;
using GamesMrkt.Platforms.Steam;
using PuffPirates.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GamesMrkt.PuffPirates.UI.Menu
{
	public class PlayPanel : MenuPanel
	{
		[SerializeField]
		private Button _createLobbyButton;

		[SerializeField]
		private Button _joinButton;

		[SerializeField]
		private TMP_InputField _roomCodeField;

		[SerializeField]
		private TMP_Text _errorText;

		[Tooltip("Optional. Shown before hosting (new game / continue a saved run).")]
		[SerializeField]
		private RunSelectPopup _runSelectPopup;

		private ConnectionSystem _launcher;

		private NotificationSystem _notifications;

		private string _notifiedError = "";

		public override bool CanCloseWithCancel
		{
			get
			{
				if (!(_runSelectPopup == null))
				{
					return !_runSelectPopup.IsTakingCancel;
				}
				return true;
			}
		}

		private void Awake()
		{
			if (Singleton<CoreSystemHandler>.Instance != null)
			{
				Singleton<CoreSystemHandler>.Instance.WhenReady(ResolveSystems);
			}
			if (_roomCodeField != null && !_roomCodeField.TryGetComponent<SteamDeckInputField>(out var _))
			{
				_roomCodeField.gameObject.AddComponent<SteamDeckInputField>();
			}
			if (_runSelectPopup != null)
			{
				_runSelectPopup.Hide(instant: true);
			}
			if (_createLobbyButton != null)
			{
				_createLobbyButton.onClick.AddListener(HandleCreateLobby);
			}
			if (_joinButton != null)
			{
				_joinButton.onClick.AddListener(HandleJoin);
			}
		}

		private void OnDestroy()
		{
			if (_createLobbyButton != null)
			{
				_createLobbyButton.onClick.RemoveListener(HandleCreateLobby);
			}
			if (_joinButton != null)
			{
				_joinButton.onClick.RemoveListener(HandleJoin);
			}
			if (_launcher != null)
			{
				_launcher.ErrorChanged -= HandleErrorChanged;
			}
			Loc.OnLanguageChanged -= HandleLanguageChanged;
		}

		private void ResolveSystems()
		{
			if (!(this == null))
			{
				this.TryGetSystem<NotificationSystem, CoreSystemHandler>(out _notifications);
				if (this.TryGetSystem<ConnectionSystem, CoreSystemHandler>(out _launcher))
				{
					_launcher.ErrorChanged += HandleErrorChanged;
					Loc.OnLanguageChanged += HandleLanguageChanged;
					HandleErrorChanged();
				}
			}
		}

		private void HandleErrorChanged()
		{
			string text = RefreshErrorText();
			if (text == _notifiedError)
			{
				return;
			}
			_notifiedError = text;
			if (!string.IsNullOrEmpty(text))
			{
				NotificationSystem notifications = _notifications;
				if ((object)notifications != null)
				{
					float? lastErrorDisplaySeconds = _launcher.LastErrorDisplaySeconds;
					notifications.Show(text, isGoodAlert: false, null, lastErrorDisplaySeconds);
				}
			}
		}

		private void HandleLanguageChanged(string _)
		{
			RefreshErrorText();
		}

		private string RefreshErrorText()
		{
			string text = ((_launcher != null) ? _launcher.LastError : "");
			if (_errorText != null)
			{
				_errorText.text = text;
			}
			return text;
		}

		private void HandleCreateLobby()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			if (!(_launcher == null))
			{
				if (_runSelectPopup == null)
				{
					_launcher.StartHost();
				}
				else
				{
					_runSelectPopup.Show(_launcher.StartHost);
				}
			}
		}

		private void HandleJoin()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			if (!(_launcher == null) && !(_roomCodeField == null) && !string.IsNullOrWhiteSpace(_roomCodeField.text))
			{
				_launcher.RoomCode = _roomCodeField.text.Trim().ToUpperInvariant();
				_launcher.StartClient();
			}
		}
	}
}

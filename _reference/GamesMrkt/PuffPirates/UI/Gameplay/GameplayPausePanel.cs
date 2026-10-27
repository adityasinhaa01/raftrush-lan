using System;
using Cysharp.Threading.Tasks;
using GamesMrkt.Audio;
using GamesMrkt.Core;
using GamesMrkt.Inputs;
using GamesMrkt.PuffPirates.UI.Settings;
using PuffPirates.Settings;
using PuffPirates.Systems;
using PuffPirates.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace GamesMrkt.PuffPirates.UI.Gameplay
{
	public class GameplayPausePanel : MonoBehaviour
	{
		[Header("Kökler")]
		[SerializeField]
		private GameObject _pauseRoot;

		[SerializeField]
		private SettingsPanelController _settingsPanel;

		[Tooltip("Asks before LEAVE / EXIT TO DESKTOP. Without one, both act immediately.")]
		[SerializeField]
		private ConfirmPopup _confirmPopup;

		[Header("Butonlar")]
		[SerializeField]
		private Button _continueButton;

		[SerializeField]
		private Button _settingsButton;

		[SerializeField]
		private Button _leaveButton;

		[SerializeField]
		private Button _exitButton;

		[Header("Externals")]
		[SerializeField]
		private Button _dcButton;

		[SerializeField]
		private Button _bugRepButton;

		private ConnectionSystem _connection;

		private PlayerInputSystem _input;

		private IDisposable _pauseSubscription;

		private IDisposable _cancelSubscription;

		private IDisposable _navigateSubscription;

		private int _layerChangedFrame = -1;

		private bool _pauseOpen;

		private bool _settingsOpen;

		private bool _quitting;

		public bool IsOpen
		{
			get
			{
				if (!_pauseOpen)
				{
					return _settingsOpen;
				}
				return true;
			}
		}

		private bool IsConfirmOpen
		{
			get
			{
				if (_confirmPopup != null)
				{
					return _confirmPopup.IsOpen;
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
			_continueButton.onClick.AddListener(HandleContinueClicked);
			_settingsButton.onClick.AddListener(OpenSettings);
			_leaveButton.onClick.AddListener(HandleLeaveClicked);
			_exitButton.onClick.AddListener(HandleExitClicked);
			_dcButton.onClick.AddListener(OpenDiscord);
			_bugRepButton.onClick.AddListener(GoToBugReport);
			_pauseRoot.SetActive(value: false);
			if (_confirmPopup != null)
			{
				_confirmPopup.Hide(instant: true);
			}
			if (_settingsPanel != null)
			{
				_settingsPanel.Closed += CloseSettings;
				_settingsPanel.Close();
			}
		}

		private void OnDestroy()
		{
			if (_continueButton != null)
			{
				_continueButton.onClick.RemoveListener(HandleContinueClicked);
			}
			if (_settingsButton != null)
			{
				_settingsButton.onClick.RemoveListener(OpenSettings);
			}
			if (_leaveButton != null)
			{
				_leaveButton.onClick.RemoveListener(HandleLeaveClicked);
			}
			if (_exitButton != null)
			{
				_exitButton.onClick.RemoveListener(HandleExitClicked);
			}
			if (_settingsPanel != null)
			{
				_settingsPanel.Closed -= CloseSettings;
			}
			if (_input != null)
			{
				_input.InputBlocked = false;
			}
			_pauseSubscription?.Dispose();
			_cancelSubscription?.Dispose();
			_navigateSubscription?.Dispose();
		}

		private void HandleNavigatePressed()
		{
			if (!_quitting && !GameEndPanel.IsShown && !IsConfirmOpen)
			{
				if (_settingsOpen)
				{
					_settingsPanel.RestoreSelectionIfLost();
				}
				else if (_pauseOpen && UISelection.IsLost)
				{
					UISelection.Select(_continueButton);
				}
			}
		}

		private void ResolveSystems()
		{
			if (!(this == null))
			{
				this.TryGetSystem<ConnectionSystem, CoreSystemHandler>(out _connection);
				this.TryGetSystem<PlayerInputSystem, CoreSystemHandler>(out _input);
				_pauseSubscription = _input?.Player?.Subscribe("Global/Pause", InputActionTrigger.Performed, HandlePausePressed);
				_cancelSubscription = _input?.Player?.Subscribe("UI/Cancel", InputActionTrigger.Performed, HandleCancelPressed);
				_navigateSubscription = UISelection.SubscribeNavigatePressed(_input?.Player, HandleNavigatePressed);
			}
		}

		private void Update()
		{
			if (GameEndPanel.IsShown && IsOpen && !_quitting)
			{
				Continue();
			}
			ApplyInputBlocking();
		}

		private void HandlePausePressed()
		{
			if (!_quitting && !GameEndPanel.IsShown && !KeyBindings.IsRebinding && !TryCloseTopLayer())
			{
				Open();
				_layerChangedFrame = Time.frameCount;
			}
		}

		private void HandleCancelPressed()
		{
			if (!_quitting && !GameEndPanel.IsShown && !KeyBindings.IsRebinding)
			{
				TryCloseTopLayer();
			}
		}

		private bool TryCloseTopLayer()
		{
			if (Time.frameCount == _layerChangedFrame)
			{
				return true;
			}
			if (IsConfirmOpen)
			{
				_confirmPopup.Hide();
			}
			else
			{
				if (_settingsOpen && !_settingsPanel.CanCloseWithCancel)
				{
					return true;
				}
				if (_settingsOpen)
				{
					CloseSettings();
				}
				else
				{
					if (!_pauseOpen)
					{
						return false;
					}
					Continue();
				}
			}
			_layerChangedFrame = Time.frameCount;
			return true;
		}

		private void ApplyInputBlocking()
		{
			if (_input != null)
			{
				_input.InputBlocked = IsOpen || GameEndPanel.IsShown;
			}
		}

		public void Open()
		{
			if (!_pauseOpen && !_quitting)
			{
				_pauseOpen = true;
				if (_pauseRoot != null)
				{
					_pauseRoot.SetActive(value: true);
				}
				UISelection.Select(_continueButton);
			}
		}

		public void Continue()
		{
			if (_confirmPopup != null)
			{
				_confirmPopup.Hide(instant: true);
			}
			if (_settingsOpen)
			{
				CloseSettings();
			}
			_pauseOpen = false;
			if (_pauseRoot != null)
			{
				_pauseRoot.SetActive(value: false);
			}
			UISelection.Clear();
		}

		private void HandleContinueClicked()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			Continue();
		}

		private void OpenSettings()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			if (!(_settingsPanel == null))
			{
				_settingsOpen = true;
				if (_pauseRoot != null)
				{
					_pauseRoot.SetActive(value: false);
				}
				_settingsPanel.Open();
			}
		}

		private void CloseSettings()
		{
			if (_settingsPanel != null)
			{
				_settingsPanel.Close();
			}
			_settingsOpen = false;
			if (_pauseOpen && _pauseRoot != null)
			{
				_pauseRoot.SetActive(value: true);
			}
			UISelection.Select(_settingsButton);
		}

		private void HandleLeaveClicked()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			AskThen(LeaveRace, "pause.confirm.leave.title", "pause.confirm.leave.desc", "pause.confirm.leave.yes", "pause.confirm.leave.no");
		}

		private void HandleExitClicked()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			AskThen(ExitToDesktop, "pause.confirm.exit.title", "pause.confirm.exit.desc", "pause.confirm.exit.yes", "pause.confirm.exit.no");
		}

		private void AskThen(Action action, string titleKey, string descriptionKey, string confirmKey, string cancelKey)
		{
			if (!_quitting)
			{
				if (_confirmPopup == null)
				{
					action();
				}
				else
				{
					_confirmPopup.Show(titleKey, descriptionKey, confirmKey, cancelKey, action);
				}
			}
		}

		private void LeaveRace()
		{
			if (!_quitting)
			{
				_quitting = true;
				if (_input != null)
				{
					_input.InputBlocked = false;
				}
				if (_connection != null)
				{
					_connection.Leave();
				}
			}
		}

		private void ExitToDesktop()
		{
			if (!_quitting)
			{
				_quitting = true;
				LeaveThenQuit().Forget();
			}
		}

		private async UniTaskVoid LeaveThenQuit()
		{
			if (_connection != null)
			{
				await _connection.LeaveAsync();
			}
			AppQuit.Now();
		}

		private void OpenDiscord()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			ExternalLinkHelper.OpenDiscord();
		}

		private void GoToBugReport()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			ExternalLinkHelper.OpenBugReport();
		}
	}
}

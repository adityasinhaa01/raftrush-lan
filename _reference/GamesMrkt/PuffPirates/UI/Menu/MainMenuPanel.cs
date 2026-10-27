using GamesMrkt.Audio;
using GamesMrkt.Core;
using GamesMrkt.Utils;
using PuffPirates.Systems;
using PuffPirates.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GamesMrkt.PuffPirates.UI.Menu
{
	public class MainMenuPanel : MonoBehaviour
	{
		[SerializeField]
		private Button _localCoopButton;

		[SerializeField]
		private Button _exitButton;

		[SerializeField]
		private TMP_Text _applicationInfoText;

		[Tooltip("Optional. Shown before Local Co-op starts (new game / continue a saved run).")]
		[SerializeField]
		private RunSelectPopup _runSelectPopup;

		[Header("External")]
		[SerializeField]
		private Button _dcButton;

		private ConnectionSystem _connection;

		private void Awake()
		{
			_applicationInfoText.text = CoreSystemHandler.ApplicaitonInfo;
			if (Singleton<CoreSystemHandler>.Instance != null)
			{
				this.WhenSystemReady<ConnectionSystem, CoreSystemHandler>((ConnectionSystem system) =>
				{
					if (this != null)
					{
						_connection = system;
					}
				});
			}
			if (_runSelectPopup != null)
			{
				_runSelectPopup.Hide(instant: true);
			}
			if (_localCoopButton != null)
			{
				_localCoopButton.onClick.AddListener(HandleLocalCoop);
			}
			_exitButton.onClick.AddListener(HandleExit);
			_dcButton.onClick.AddListener(OpenDiscord);
		}

		private void OnDestroy()
		{
			if (_localCoopButton != null)
			{
				_localCoopButton.onClick.RemoveListener(HandleLocalCoop);
			}
			_exitButton.onClick.RemoveListener(HandleExit);
			_dcButton.RemoveListener(OpenDiscord);
		}

		private void HandleLocalCoop()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			if (!(_connection == null))
			{
				if (_runSelectPopup == null)
				{
					_connection.StartSolo();
				}
				else
				{
					_runSelectPopup.Show(_connection.StartSolo);
				}
			}
		}

		private void HandleExit()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			AppQuit.Now();
		}

		private void OpenDiscord()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			ExternalLinkHelper.OpenDiscord();
		}
	}
}

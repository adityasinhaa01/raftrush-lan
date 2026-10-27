using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using GamesMrkt.Audio;
using GamesMrkt.Core;
using GamesMrkt.Localization;
using GamesMrkt.Platforms.Steam;
using PuffPirates.Backend;
using PuffPirates.Boats;
using PuffPirates.Networking;
using PuffPirates.Systems;
using PuffPirates.Utils;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GamesMrkt.PuffPirates.UI.Gameplay
{
	public class GameEndPanel : MonoBehaviour
	{
		[Header("Roots")]
		[SerializeField]
		private GameObject _root;

		[SerializeField]
		private CanvasGroup _background;

		[SerializeField]
		private RectTransform _content;

		[Header("Buttons")]
		[SerializeField]
		private Button _wishlistButton;

		[SerializeField]
		private Button _returnMenuButton;

		[SerializeField]
		private Button _quitButton;

		[Tooltip("Boş bırakılırsa logonun altına kodla oluşturulur.")]
		[SerializeField]
		private TMP_Text _statsLabel;

		[Header("Animation")]
		[SerializeField]
		private float _duration = 0.4f;

		[SerializeField]
		private float _contentStartScale = 0.6f;

		[SerializeField]
		private Ease _contentEase = Ease.OutBack;

		private ConnectionSystem _connection;

		private PlayerInputSystem _input;

		private SteamSystem _steam;

		private Sequence _sequence;

		private bool _quitting;

		public bool IsOpen { get; private set; }

		public static bool IsShown { get; private set; }

		private void Awake()
		{
			IsShown = false;
			if (_wishlistButton != null)
			{
				_wishlistButton.onClick.AddListener(OpenWishlist);
			}
			if (_returnMenuButton != null)
			{
				_returnMenuButton.onClick.AddListener(ReturnToMenu);
			}
			if (_quitButton != null)
			{
				_quitButton.onClick.AddListener(QuitGame);
			}
			if (_root != null)
			{
				_root.SetActive(value: false);
			}
			if (Singleton<CoreSystemHandler>.Instance != null)
			{
				Singleton<CoreSystemHandler>.Instance.WhenReady(ResolveSystems);
			}
		}

		private void OnDestroy()
		{
			if (_wishlistButton != null)
			{
				_wishlistButton.onClick.RemoveListener(OpenWishlist);
			}
			if (_returnMenuButton != null)
			{
				_returnMenuButton.onClick.RemoveListener(ReturnToMenu);
			}
			if (_quitButton != null)
			{
				_quitButton.onClick.RemoveListener(QuitGame);
			}
			KillSequence();
			if (_connection != null)
			{
				_connection.StateChanged -= OnStateChanged;
			}
			if (IsOpen)
			{
				IsShown = false;
				if (_input != null)
				{
					_input.InputBlocked = false;
				}
			}
		}

		private void ResolveSystems()
		{
			if (this == null)
			{
				return;
			}
			this.TryGetSystem<PlayerInputSystem, CoreSystemHandler>(out _input);
			this.TryGetSystem<SteamSystem, CoreSystemHandler>(out _steam);
			if (this.TryGetSystem<ConnectionSystem, CoreSystemHandler>(out _connection))
			{
				_connection.StateChanged += OnStateChanged;
				if (_connection.State == LauncherState.Finished)
				{
					Open();
				}
			}
		}

		private void OnStateChanged(LauncherState previous, LauncherState next)
		{
			if (next == LauncherState.Finished)
			{
				Open();
			}
		}

		private void LateUpdate()
		{
			if (IsOpen && _input != null)
			{
				_input.InputBlocked = true;
			}
		}

		public void Open()
		{
			if (!IsOpen && !(_root == null))
			{
				IsOpen = true;
				IsShown = true;
				KillSequence();
				SetButtonsInteractable(value: false);
				if (_background != null)
				{
					_background.alpha = 0f;
				}
				if (_content != null)
				{
					_content.localScale = Vector3.one * _contentStartScale;
				}
				_root.SetActive(value: true);
				_sequence = DOTween.Sequence().SetUpdate(isIndependentUpdate: true).SetLink(_root);
				if (_background != null)
				{
					_sequence.Join(_background.DOFade(1f, _duration).SetEase(Ease.OutQuad));
				}
				if (_content != null)
				{
					_sequence.Join(_content.DOScale(1f, _duration).SetEase(_contentEase));
				}
				_sequence.OnComplete(() =>
				{
					_sequence = null;
					SetButtonsInteractable(value: true);
					UISelection.Select(_wishlistButton);
					RefreshStats();
				});
				RefreshStats();
			}
		}

		private void RefreshStats()
		{
			if (_statsLabel == null)
			{
				if (_content == null || _wishlistButton == null)
				{
					return;
				}
				TMP_Text componentInChildren = _wishlistButton.GetComponentInChildren<TMP_Text>(includeInactive: true);
				GameObject gameObject = new GameObject("Stats", typeof(RectTransform));
				gameObject.transform.SetParent(_content, worldPositionStays: false);
				RectTransform obj = (RectTransform)gameObject.transform;
				Vector2 anchorMin = (obj.anchorMax = new Vector2(0.5f, 0f));
				obj.anchorMin = anchorMin;
				obj.anchoredPosition = new Vector2(0f, -55f);
				obj.sizeDelta = new Vector2(1200f, 70f);
				_statsLabel = gameObject.AddComponent<TextMeshProUGUI>();
				if (componentInChildren != null)
				{
					_statsLabel.font = componentInChildren.font;
					_statsLabel.fontSharedMaterial = componentInChildren.fontSharedMaterial;
				}
				_statsLabel.alignment = TextAlignmentOptions.Center;
				_statsLabel.fontSize = 52f;
				_statsLabel.fontStyle = FontStyles.Bold;
				_statsLabel.raycastTarget = false;
			}
			NetworkBoat networkBoat = ((_connection != null) ? _connection.LocalBoat : null);
			int num = ((networkBoat != null) ? networkBoat.Deaths : 0);
			int num2 = (int)(((RaceState.Instance != null) ? Mathf.Max(0f, RaceState.Instance.RunElapsedSeconds) : 0f) * 100f);
			string item = $"{num2 / 360000:00}:{num2 / 6000 % 60:00}:{num2 / 100 % 60:00}.{num2 % 100:00}";
			_statsLabel.text = Loc.Get("end.stats", "Deaths: {deaths}  ·  Time: {time}", ("deaths", num), ("time", item));
		}

		private void OpenWishlist()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			uint steamAppId = BackendConfig.SteamAppId;
			if (_steam != null && _steam.IsAvailable)
			{
				try
				{
					if (SteamUtils.IsOverlayEnabled())
					{
						SteamFriends.ActivateGameOverlayToStore(new AppId_t(steamAppId), EOverlayToStoreFlag.k_EOverlayToStoreFlag_None);
						return;
					}
				}
				catch (Exception)
				{
				}
			}
			Application.OpenURL($"https://store.steampowered.com/app/{steamAppId}/");
		}

		private void ReturnToMenu()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			if (!_quitting)
			{
				_quitting = true;
				SetButtonsInteractable(value: false);
				if (_connection != null)
				{
					_connection.Leave();
				}
			}
		}

		private void QuitGame()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			if (!_quitting)
			{
				_quitting = true;
				SetButtonsInteractable(value: false);
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

		private void SetButtonsInteractable(bool value)
		{
			if (_wishlistButton != null)
			{
				_wishlistButton.interactable = value;
			}
			if (_returnMenuButton != null)
			{
				_returnMenuButton.interactable = value;
			}
			if (_quitButton != null)
			{
				_quitButton.interactable = value;
			}
		}

		private void KillSequence()
		{
			_sequence?.Kill();
			_sequence = null;
		}
	}
}

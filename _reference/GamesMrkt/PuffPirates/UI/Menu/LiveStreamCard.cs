using System;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using GamesMrkt.Audio;
using GamesMrkt.Localization;
using PuffPirates.Backend;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace GamesMrkt.PuffPirates.UI.Menu
{
	public class LiveStreamCard : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		[Serializable]
		private struct PlatformIcon
		{
			public StreamPlatform Platform;

			public Sprite Icon;
		}

		[SerializeField]
		private CanvasGroup _canvasGroup;

		[SerializeField]
		private RawImage _thumbnail;

		[SerializeField]
		private Image _platformIcon;

		[SerializeField]
		private TMP_Text _streamerNameText;

		[SerializeField]
		private TMP_Text _streamTitleText;

		[SerializeField]
		private TMP_Text _viewerCountText;

		[SerializeField]
		private PlatformIcon[] _platformIcons;

		private const float REFRESH_INTERVAL_SECONDS = 60f;

		private const int THUMBNAIL_TIMEOUT_SECONDS = 15;

		private const int COMPACT_VIEWER_COUNT_THRESHOLD = 100000;

		private FartTogetherApi.LiveStreamDto _currentStream;

		private string _thumbnailUrl;

		private Texture2D _thumbnailTexture;

		private CancellationTokenSource _refreshCancellation;

		private void Awake()
		{
			Loc.OnLanguageChanged += HandleLanguageChanged;
		}

		private void OnEnable()
		{
			Render();
			RestartRefreshLoop();
		}

		private void OnDisable()
		{
			StopRefreshLoop();
		}

		private void OnDestroy()
		{
			Loc.OnLanguageChanged -= HandleLanguageChanged;
			if (_thumbnailTexture != null)
			{
				UnityEngine.Object.Destroy(_thumbnailTexture);
			}
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (_currentStream != null)
			{
				AudioManager.PlaySound(CommonSound.ButtonClick);
				if (Uri.TryCreate(_currentStream.channelUrl, UriKind.Absolute, out var result) && (result.Scheme == Uri.UriSchemeHttps || result.Scheme == Uri.UriSchemeHttp))
				{
					Application.OpenURL(result.AbsoluteUri);
				}
			}
		}

		private void HandleLanguageChanged(string language)
		{
			if (base.isActiveAndEnabled)
			{
				RestartRefreshLoop();
			}
		}

		private void RestartRefreshLoop()
		{
			StopRefreshLoop();
			_refreshCancellation = CancellationTokenSource.CreateLinkedTokenSource(base.destroyCancellationToken);
			RefreshLoopAsync(_refreshCancellation.Token).Forget();
		}

		private void StopRefreshLoop()
		{
			if (_refreshCancellation != null)
			{
				_refreshCancellation.Cancel();
				_refreshCancellation.Dispose();
				_refreshCancellation = null;
			}
		}

		private async UniTaskVoid RefreshLoopAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				await RefreshAsync(cancellationToken);
				if (await UniTask.Delay(TimeSpan.FromSeconds(60.0), ignoreTimeScale: true, PlayerLoopTiming.Update, cancellationToken).SuppressCancellationThrow())
				{
					break;
				}
			}
		}

		private async UniTask RefreshAsync(CancellationToken cancellationToken)
		{
			FartTogetherApi.LiveStreamDto topStream = null;
			try
			{
				topStream = await FetchTopStreamAsync();
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[LiveStreamCard] " + ex.Message);
			}
			if (!cancellationToken.IsCancellationRequested)
			{
				_currentStream = topStream;
				Render();
				if (_currentStream != null)
				{
					await LoadThumbnailAsync(_currentStream.thumbnailUrl, cancellationToken);
				}
			}
		}

		private static async UniTask<FartTogetherApi.LiveStreamDto> FetchTopStreamAsync()
		{
			FartTogetherApi api = BackendBootstrap.Api ?? new FartTogetherApi();
			FartTogetherApi.LiveStreamDto liveStreamDto = MostViewed(await api.GetLiveStreamsAsync(Loc.CurrentLanguage, ""));
			if (liveStreamDto == null && !string.IsNullOrEmpty(Loc.CurrentLanguage))
			{
				liveStreamDto = MostViewed(await api.GetLiveStreamsAsync("", ""));
			}
			return liveStreamDto;
		}

		private static FartTogetherApi.LiveStreamDto MostViewed(FartTogetherApi.LiveStreamListResponse response)
		{
			if (response?.items == null)
			{
				return null;
			}
			FartTogetherApi.LiveStreamDto liveStreamDto = null;
			FartTogetherApi.LiveStreamDto[] items = response.items;
			foreach (FartTogetherApi.LiveStreamDto liveStreamDto2 in items)
			{
				if (liveStreamDto2 != null && !string.IsNullOrEmpty(liveStreamDto2.channelName) && (liveStreamDto == null || (liveStreamDto2.featured && !liveStreamDto.featured) || (liveStreamDto2.featured == liveStreamDto.featured && liveStreamDto2.viewerCount > liveStreamDto.viewerCount)))
				{
					liveStreamDto = liveStreamDto2;
				}
			}
			return liveStreamDto;
		}

		private async UniTask LoadThumbnailAsync(string url, CancellationToken cancellationToken)
		{
			if (_thumbnail == null)
			{
				return;
			}
			if (string.IsNullOrEmpty(url))
			{
				ClearThumbnail();
			}
			else
			{
				if (url == _thumbnailUrl)
				{
					return;
				}
				_thumbnailUrl = url;
				if (_thumbnailTexture == null)
				{
					_thumbnail.enabled = false;
				}
				string uri = url.Replace("{width}", "640").Replace("{height}", "360");
				using UnityWebRequest request = UnityWebRequestTexture.GetTexture(uri);
				request.timeout = 15;
				try
				{
					await request.SendWebRequest().WithCancellation(cancellationToken);
				}
				catch (OperationCanceledException)
				{
					_thumbnailUrl = null;
					return;
				}
				catch (Exception ex2)
				{
					Debug.LogWarning("[LiveStreamCard] Thumbnail: " + ex2.Message);
					ClearThumbnail();
					return;
				}
				if (!(url != _thumbnailUrl))
				{
					Texture2D content = DownloadHandlerTexture.GetContent(request);
					if (_thumbnailTexture != null)
					{
						UnityEngine.Object.Destroy(_thumbnailTexture);
					}
					_thumbnailTexture = content;
					_thumbnail.texture = content;
					_thumbnail.enabled = true;
				}
			}
		}

		private void ClearThumbnail()
		{
			_thumbnailUrl = null;
			if (_thumbnail != null)
			{
				_thumbnail.enabled = false;
			}
		}

		private void Render()
		{
			bool flag = _currentStream != null;
			SetVisible(flag);
			if (flag)
			{
				SetText(_streamerNameText, _currentStream.channelName);
				SetText(_streamTitleText, _currentStream.title);
				SetText(_viewerCountText, FormatViewerCount(_currentStream.viewerCount));
				RenderPlatformIcon(ParsePlatform(_currentStream.platform));
			}
		}

		private void SetVisible(bool isVisible)
		{
			if (!(_canvasGroup == null))
			{
				_canvasGroup.alpha = (isVisible ? 1f : 0f);
				_canvasGroup.interactable = isVisible;
				_canvasGroup.blocksRaycasts = isVisible;
			}
		}

		private void RenderPlatformIcon(StreamPlatform platform)
		{
			if (!(_platformIcon == null))
			{
				Sprite sprite = FindPlatformIcon(platform);
				_platformIcon.sprite = sprite;
				_platformIcon.enabled = sprite != null;
			}
		}

		private Sprite FindPlatformIcon(StreamPlatform platform)
		{
			if (_platformIcons == null)
			{
				return null;
			}
			PlatformIcon[] platformIcons = _platformIcons;
			for (int i = 0; i < platformIcons.Length; i++)
			{
				PlatformIcon platformIcon = platformIcons[i];
				if (platformIcon.Platform == platform)
				{
					return platformIcon.Icon;
				}
			}
			return null;
		}

		private static StreamPlatform ParsePlatform(string platform)
		{
			if (!Enum.TryParse<StreamPlatform>(platform, ignoreCase: true, out var result))
			{
				return StreamPlatform.Unknown;
			}
			return result;
		}

		private static string FormatViewerCount(int viewerCount)
		{
			if (viewerCount >= 1000000)
			{
				return ((float)viewerCount / 1000000f).ToString("0.#", CultureInfo.InvariantCulture) + "M";
			}
			if (viewerCount >= 100000)
			{
				return (viewerCount / 1000).ToString(CultureInfo.InvariantCulture) + "K";
			}
			return viewerCount.ToString("N0", LanguageCulture());
		}

		private static CultureInfo LanguageCulture()
		{
			try
			{
				return CultureInfo.GetCultureInfo(Loc.CurrentLanguage);
			}
			catch (Exception)
			{
				return CultureInfo.InvariantCulture;
			}
		}

		private static void SetText(TMP_Text label, string text)
		{
			if (label != null)
			{
				label.text = text ?? "";
			}
		}
	}
}

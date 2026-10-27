using System;
using Cysharp.Threading.Tasks;
using GamesMrkt.Audio;
using GamesMrkt.Utils;
using UnityEngine;

namespace GamesMrkt.PuffPirates.UI
{
	public class NotificationStackView : MonoBehaviour
	{
		[SerializeField]
		private RectTransform _container;

		[SerializeField]
		private NotificationItem _itemPrefab;

		[Tooltip("0 = unlimited. Oldest item is dropped when the stack exceeds this.")]
		[SerializeField]
		private int _maxVisible = 4;

		public void Show(string text, Color color, float duration, bool isGoodAlert)
		{
			if (!string.IsNullOrEmpty(text))
			{
				if (_maxVisible > 0 && _container.childCount >= _maxVisible)
				{
					UnityEngine.Object.Destroy(_container.GetChild(_container.childCount - 1).gameObject);
				}
				AudioManager.PlaySound(isGoodAlert ? CommonSound.Success : CommonSound.Error);
				NotificationItem item = UnityEngine.Object.Instantiate(_itemPrefab, _container);
				RunItemAsync(item, text, color, duration).Forget();
			}
		}

		private async UniTaskVoid RunItemAsync(NotificationItem item, string text, Color color, float duration)
		{
			try
			{
				await item.PlayAsync(text, color, duration, item.GetCancellationTokenOnDestroy());
			}
			catch (OperationCanceledException)
			{
				return;
			}
			catch (Exception ex2)
			{
				GameLogger.Error("Notification failed: " + ex2.Message, "NotificationStackView");
			}
			if (item != null)
			{
				UnityEngine.Object.Destroy(item.gameObject);
			}
		}
	}
}

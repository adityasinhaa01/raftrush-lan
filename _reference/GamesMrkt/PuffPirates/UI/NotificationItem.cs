using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace GamesMrkt.PuffPirates.UI
{
	public class NotificationItem : MonoBehaviour
	{
		[SerializeField]
		private CanvasGroup _canvasGroup;

		[SerializeField]
		private TMP_Text _label;

		[SerializeField]
		private RectTransform _rect;

		private const float FADE_IN_DURATION = 0.25f;

		private const float FADE_OUT_DURATION = 0.3f;

		private const float SPAWN_SCALE = 0.85f;

		private Sequence _sequence;

		private void Awake()
		{
			LanguageFollow.Label(_label);
		}

		public async UniTask PlayAsync(string text, Color color, float duration, CancellationToken ct)
		{
			_label.text = text;
			_label.color = color;
			_canvasGroup.alpha = 0f;
			_rect.localScale = Vector3.one * 0.85f;
			_sequence?.Kill();
			_sequence = DOTween.Sequence().SetLink(base.gameObject);
			_sequence.Append(_canvasGroup.DOFade(1f, 0.25f));
			_sequence.Join(_rect.DOScale(1f, 0.25f).SetEase(Ease.OutBack));
			_sequence.AppendInterval(Mathf.Max(0f, duration));
			_sequence.Append(_canvasGroup.DOFade(0f, 0.3f));
			try
			{
				await _sequence.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(ct);
			}
			catch (OperationCanceledException)
			{
				_sequence?.Kill();
				throw;
			}
		}
	}
}

using System;
using DG.Tweening;
using UnityEngine;

namespace GamesMrkt.PuffPirates.UI
{
	[RequireComponent(typeof(CanvasGroup))]
	public class UIPopupAnimator : MonoBehaviour
	{
		[SerializeField]
		private CanvasGroup _canvasGroup;

		[SerializeField]
		private RectTransform _content;

		[Tooltip("Optional. Screens under the popup: not interactable while it is open, so neither clicks nor keyboard / gamepad navigation reach them (e.g. the main menu under the save popup).")]
		[SerializeField]
		private CanvasGroup[] _blockedWhileOpen;

		[Header("Show")]
		[SerializeField]
		private float _showDuration = 0.25f;

		[SerializeField]
		private float _contentStartScale = 0.8f;

		[SerializeField]
		private Ease _showEase = Ease.OutBack;

		[Header("Hide")]
		[SerializeField]
		private float _hideDuration = 0.15f;

		[SerializeField]
		private float _contentEndScale = 0.9f;

		[SerializeField]
		private Ease _hideEase = Ease.InQuad;

		private Sequence _sequence;

		private bool[] _wereInteractable;

		private bool _isBlocking;

		public bool IsOpen { get; private set; }

		public void Show()
		{
			if (IsOpen)
			{
				return;
			}
			IsOpen = true;
			KillSequence();
			SetScreensUnderBlocked(isBlocked: true);
			if (!base.gameObject.activeSelf)
			{
				_canvasGroup.alpha = 0f;
				if (_content != null)
				{
					_content.localScale = Vector3.one * _contentStartScale;
				}
				base.gameObject.SetActive(value: true);
			}
			SetInteractable(value: true);
			_sequence = DOTween.Sequence().SetUpdate(isIndependentUpdate: true).SetLink(base.gameObject);
			_sequence.Join(_canvasGroup.DOFade(1f, _showDuration).SetEase(Ease.OutQuad));
			if (_content != null)
			{
				_sequence.Join(_content.DOScale(1f, _showDuration).SetEase(_showEase));
			}
		}

		public void Hide(bool instant = false, Action onHidden = null)
		{
			IsOpen = false;
			KillSequence();
			SetInteractable(value: false);
			SetScreensUnderBlocked(isBlocked: false);
			if (instant || !base.gameObject.activeInHierarchy)
			{
				_canvasGroup.alpha = 0f;
				base.gameObject.SetActive(value: false);
				onHidden?.Invoke();
				return;
			}
			_sequence = DOTween.Sequence().SetUpdate(isIndependentUpdate: true).SetLink(base.gameObject);
			_sequence.Join(_canvasGroup.DOFade(0f, _hideDuration).SetEase(Ease.InQuad));
			if (_content != null)
			{
				_sequence.Join(_content.DOScale(_contentEndScale, _hideDuration).SetEase(_hideEase));
			}
			_sequence.OnComplete(() =>
			{
				_sequence = null;
				base.gameObject.SetActive(value: false);
				onHidden?.Invoke();
			});
		}

		private void SetInteractable(bool value)
		{
			_canvasGroup.interactable = value;
			_canvasGroup.blocksRaycasts = value;
		}

		private void SetScreensUnderBlocked(bool isBlocked)
		{
			if (_blockedWhileOpen == null || _blockedWhileOpen.Length == 0 || isBlocked == _isBlocking)
			{
				return;
			}
			_isBlocking = isBlocked;
			if (isBlocked)
			{
				_wereInteractable = new bool[_blockedWhileOpen.Length];
			}
			for (int i = 0; i < _blockedWhileOpen.Length; i++)
			{
				CanvasGroup canvasGroup = _blockedWhileOpen[i];
				if (!(canvasGroup == null))
				{
					if (isBlocked)
					{
						_wereInteractable[i] = canvasGroup.interactable;
						canvasGroup.interactable = false;
					}
					else
					{
						canvasGroup.interactable = _wereInteractable[i];
					}
				}
			}
		}

		private void KillSequence()
		{
			_sequence?.Kill();
			_sequence = null;
		}

		private void OnDestroy()
		{
			KillSequence();
			SetScreensUnderBlocked(isBlocked: false);
		}
	}
}

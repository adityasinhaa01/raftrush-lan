using System;
using DG.Tweening;
using UnityEngine;

namespace GamesMrkt.PuffPirates.UI.Menu
{
	public class MenuIntroAnimator : MonoBehaviour
	{
		[Header("Side fades")]
		[SerializeField]
		private CanvasGroup _fades;

		[SerializeField]
		private float _fadeDuration = 1f;

		[Header("Buttons (top to bottom — slide order)")]
		[SerializeField]
		private RectTransform[] _slideItems;

		[SerializeField]
		private float _slideStartDelay = 0.35f;

		[SerializeField]
		private float _slideStagger = 0.12f;

		[SerializeField]
		private float _slideDuration = 0.7f;

		[SerializeField]
		private Ease _slideEase = Ease.OutBack;

		[Tooltip("Extra distance past the canvas edge the items start from.")]
		[SerializeField]
		private float _offscreenMargin = 50f;

		[Header("Key hints (left to right — slide up from below)")]
		[SerializeField]
		private RectTransform[] _bottomItems;

		[SerializeField]
		private float _bottomStartDelay = 0.6f;

		[SerializeField]
		private float _bottomStagger = 0.12f;

		[SerializeField]
		private float _bottomDuration = 0.6f;

		[SerializeField]
		private Ease _bottomEase = Ease.OutBack;

		[Header("Outro (reverse, before a sub-screen like Settings opens)")]
		[SerializeField]
		private float _outroStagger = 0.07f;

		[SerializeField]
		private float _outroDuration = 0.4f;

		[SerializeField]
		private Ease _outroEase = Ease.InBack;

		[Tooltip("Replay every time the menu is re-shown (e.g. returning from Settings), not only on scene entry.")]
		[SerializeField]
		private bool _replayOnReenable;

		private Vector2[] _restPositions;

		private Vector2[] _bottomRestPositions;

		private Sequence _sequence;

		private bool _started;

		private CanvasGroup _inputGroup;

		private void Awake()
		{
			_inputGroup = GetComponent<CanvasGroup>();
			if (_inputGroup == null)
			{
				_inputGroup = base.gameObject.AddComponent<CanvasGroup>();
			}
			_restPositions = CaptureRest(_slideItems);
			_bottomRestPositions = CaptureRest(_bottomItems);
		}

		private void Start()
		{
			_started = true;
			Play();
		}

		private void OnEnable()
		{
			if (_started && _replayOnReenable)
			{
				Play();
			}
		}

		private void OnDisable()
		{
			Complete();
		}

		private void OnDestroy()
		{
			_sequence?.Kill();
		}

		private void Play()
		{
			_sequence?.Kill();
			SetInputEnabled(value: true);
			RestoreRest(_slideItems, _restPositions);
			RestoreRest(_bottomItems, _bottomRestPositions);
			_sequence = DOTween.Sequence().SetUpdate(isIndependentUpdate: true).SetLink(base.gameObject);
			if (_fades != null)
			{
				_fades.alpha = 0f;
				_sequence.Insert(0f, _fades.DOFade(1f, _fadeDuration).SetEase(Ease.OutQuad));
			}
			RectTransform canvasRect = GetCanvasRect();
			for (int i = 0; i < _slideItems.Length; i++)
			{
				RectTransform rectTransform = _slideItems[i];
				if (!(rectTransform == null))
				{
					Vector2 vector = _restPositions[i];
					rectTransform.anchoredPosition = vector + new Vector2(OffscreenOffsetX(rectTransform, canvasRect), 0f);
					_sequence.Insert(_slideStartDelay + (float)i * _slideStagger, rectTransform.DOAnchorPos(vector, _slideDuration).SetEase(_slideEase));
				}
			}
			for (int j = 0; j < _bottomItems.Length; j++)
			{
				RectTransform rectTransform2 = _bottomItems[j];
				if (!(rectTransform2 == null))
				{
					Vector2 vector2 = _bottomRestPositions[j];
					rectTransform2.anchoredPosition = vector2 + new Vector2(0f, OffscreenOffsetY(rectTransform2, canvasRect));
					_sequence.Insert(_bottomStartDelay + (float)j * _bottomStagger, rectTransform2.DOAnchorPos(vector2, _bottomDuration).SetEase(_bottomEase));
				}
			}
		}

		public void PlayOutro(Action onComplete)
		{
			if (!base.isActiveAndEnabled)
			{
				onComplete?.Invoke();
				return;
			}
			_sequence?.Kill();
			SetInputEnabled(value: false);
			RestoreRest(_slideItems, _restPositions);
			RestoreRest(_bottomItems, _bottomRestPositions);
			_sequence = DOTween.Sequence().SetUpdate(isIndependentUpdate: true).SetLink(base.gameObject);
			RectTransform canvasRect = GetCanvasRect();
			for (int i = 0; i < _bottomItems.Length; i++)
			{
				RectTransform rectTransform = _bottomItems[i];
				if (!(rectTransform == null))
				{
					Vector2 endValue = _bottomRestPositions[i] + new Vector2(0f, OffscreenOffsetY(rectTransform, canvasRect));
					_sequence.Insert((float)i * _outroStagger, rectTransform.DOAnchorPos(endValue, _outroDuration).SetEase(_outroEase));
				}
			}
			int num = 0;
			for (int num2 = _slideItems.Length - 1; num2 >= 0; num2--)
			{
				RectTransform rectTransform2 = _slideItems[num2];
				if (!(rectTransform2 == null))
				{
					Vector2 endValue2 = _restPositions[num2] + new Vector2(OffscreenOffsetX(rectTransform2, canvasRect), 0f);
					_sequence.Insert((float)num++ * _outroStagger, rectTransform2.DOAnchorPos(endValue2, _outroDuration).SetEase(_outroEase));
				}
			}
			if (_fades != null)
			{
				float duration = Mathf.Max(_outroDuration, _sequence.Duration());
				_sequence.Insert(0f, _fades.DOFade(0f, duration).SetEase(Ease.InQuad));
			}
			_sequence.OnComplete(() =>
			{
				_sequence = null;
				onComplete?.Invoke();
			});
		}

		private void Complete()
		{
			_sequence?.Kill();
			_sequence = null;
			SetInputEnabled(value: true);
			if (_fades != null)
			{
				_fades.alpha = 1f;
			}
			RestoreRest(_slideItems, _restPositions);
			RestoreRest(_bottomItems, _bottomRestPositions);
		}

		private void SetInputEnabled(bool value)
		{
			if (!(_inputGroup == null))
			{
				_inputGroup.interactable = value;
				_inputGroup.blocksRaycasts = value;
			}
		}

		private static Vector2[] CaptureRest(RectTransform[] items)
		{
			Vector2[] array = new Vector2[items.Length];
			for (int i = 0; i < items.Length; i++)
			{
				if (items[i] != null)
				{
					array[i] = items[i].anchoredPosition;
				}
			}
			return array;
		}

		private static void RestoreRest(RectTransform[] items, Vector2[] rest)
		{
			if (rest == null)
			{
				return;
			}
			for (int i = 0; i < items.Length; i++)
			{
				if (items[i] != null)
				{
					items[i].anchoredPosition = rest[i];
				}
			}
		}

		private RectTransform GetCanvasRect()
		{
			Canvas componentInParent = GetComponentInParent<Canvas>();
			if (!(componentInParent != null))
			{
				return null;
			}
			return (RectTransform)componentInParent.rootCanvas.transform;
		}

		private float OffscreenOffsetX(RectTransform item, RectTransform canvasRect)
		{
			if (canvasRect == null)
			{
				return -1000f;
			}
			Vector3[] array = new Vector3[4];
			item.GetWorldCorners(array);
			float x = array[2].x;
			canvasRect.GetWorldCorners(array);
			float x2 = array[0].x;
			RectTransform rectTransform = (RectTransform)item.parent;
			float x3 = x2 - x;
			return rectTransform.InverseTransformVector(new Vector3(x3, 0f, 0f)).x - _offscreenMargin;
		}

		private float OffscreenOffsetY(RectTransform item, RectTransform canvasRect)
		{
			if (canvasRect == null)
			{
				return -500f;
			}
			Vector3[] array = new Vector3[4];
			item.GetWorldCorners(array);
			float y = array[1].y;
			canvasRect.GetWorldCorners(array);
			float y2 = array[0].y;
			RectTransform rectTransform = (RectTransform)item.parent;
			float y3 = y2 - y;
			return rectTransform.InverseTransformVector(new Vector3(0f, y3, 0f)).y - _offscreenMargin;
		}
	}
}

using UnityEngine;
using UnityEngine.UI;

namespace GamesMrkt.PuffPirates.UI.LoadingScreen
{
	public class LoadingScreenAnimation : MonoBehaviour
	{
		[Header("Sprite Swap Animation")]
		[SerializeField]
		private Image _spriteSwapImage;

		[SerializeField]
		private Sprite[] _spriteSwapFrames;

		[SerializeField]
		private float _spriteSwapFramesPerSecond = 8f;

		[Header("Wobble Rotation (stop-motion style)")]
		[SerializeField]
		private RectTransform _wobbleTarget;

		[SerializeField]
		private float[] _wobbleAngles;

		[SerializeField]
		private float _wobbleFramesPerSecond = 6f;

		private float _spriteSwapElapsed;

		private int _spriteSwapFrameIndex;

		private float _wobbleElapsed;

		private int _wobbleFrameIndex;

		private void Start()
		{
			ResetSpriteSwap();
			ResetWobble();
		}

		private void Update()
		{
			UpdateSpriteSwap();
			UpdateWobble();
		}

		private void UpdateSpriteSwap()
		{
			if (!(_spriteSwapImage == null) && _spriteSwapFrames != null && _spriteSwapFrames.Length != 0)
			{
				float num = 1f / Mathf.Max(_spriteSwapFramesPerSecond, 0.0001f);
				_spriteSwapElapsed += Time.deltaTime;
				while (_spriteSwapElapsed >= num)
				{
					_spriteSwapElapsed -= num;
					_spriteSwapFrameIndex = (_spriteSwapFrameIndex + 1) % _spriteSwapFrames.Length;
					_spriteSwapImage.sprite = _spriteSwapFrames[_spriteSwapFrameIndex];
				}
			}
		}

		private void ResetSpriteSwap()
		{
			_spriteSwapElapsed = 0f;
			_spriteSwapFrameIndex = 0;
			if (_spriteSwapImage != null && _spriteSwapFrames != null && _spriteSwapFrames.Length != 0)
			{
				_spriteSwapImage.sprite = _spriteSwapFrames[0];
			}
		}

		private void UpdateWobble()
		{
			if (!(_wobbleTarget == null) && _wobbleAngles != null && _wobbleAngles.Length != 0)
			{
				float num = 1f / Mathf.Max(_wobbleFramesPerSecond, 0.0001f);
				_wobbleElapsed += Time.deltaTime;
				while (_wobbleElapsed >= num)
				{
					_wobbleElapsed -= num;
					_wobbleFrameIndex = (_wobbleFrameIndex + 1) % _wobbleAngles.Length;
					Vector3 localEulerAngles = _wobbleTarget.localEulerAngles;
					localEulerAngles.z = _wobbleAngles[_wobbleFrameIndex];
					_wobbleTarget.localEulerAngles = localEulerAngles;
				}
			}
		}

		private void ResetWobble()
		{
			_wobbleElapsed = 0f;
			_wobbleFrameIndex = 0;
			if (_wobbleTarget != null && _wobbleAngles != null && _wobbleAngles.Length != 0)
			{
				Vector3 localEulerAngles = _wobbleTarget.localEulerAngles;
				localEulerAngles.z = _wobbleAngles[0];
				_wobbleTarget.localEulerAngles = localEulerAngles;
			}
		}
	}
}

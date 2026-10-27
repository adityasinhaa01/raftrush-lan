using DG.Tweening;
using GamesMrkt.Audio;
using UnityEngine;

namespace PuffPirates.Audio
{
	public sealed class GasLoopPlayer
	{
		private readonly SoundData _high;

		private readonly SoundData _low;

		private readonly SoundData _damping;

		private readonly AudioSource _externalSource;

		private SoundHandle _handle;

		private int _category = -1;

		private bool _active;

		public bool IsActive => _active;

		public GasLoopPlayer(SoundData high, SoundData low, SoundData damping, AudioSource externalSource = null)
		{
			_high = high;
			_low = low;
			_damping = damping;
			_externalSource = externalSource;
		}

		private static int CategoryFor(float fuel01)
		{
			float num = Mathf.Clamp01(fuel01) * 100f;
			if (!(num >= GasAudioSettings.Xf1Start))
			{
				if (!(num >= GasAudioSettings.Xf2Start))
				{
					return 2;
				}
				return 1;
			}
			return 0;
		}

		private SoundData DataFor(int category)
		{
			return category switch
			{
				1 => _low, 
				0 => _high, 
				_ => null, 
			};
		}

		public void Start(float fuel01)
		{
			if (!_active)
			{
				DOTween.Kill(this);
				_active = true;
				_category = CategoryFor(fuel01);
				_handle = PlayCategory(_category, GasAudioSettings.CrossfadeDuration);
			}
		}

		public void UpdateFuel(float fuel01)
		{
			if (_active)
			{
				int num = CategoryFor(fuel01);
				if (num != _category)
				{
					_category = num;
					_handle?.Stop(GasAudioSettings.CrossfadeDuration);
					_handle = PlayCategory(num, GasAudioSettings.CrossfadeDuration);
				}
			}
		}

		public void Stop()
		{
			if (!_active)
			{
				return;
			}
			_active = false;
			_category = -1;
			SoundHandle handle = _handle;
			_handle = null;
			if (handle != null)
			{
				handle.Stop(GasAudioSettings.ReleaseFadeDuration);
				DOTween.Kill(this);
				DOTween.To(() => 0f, (float t) =>
				{
					handle.SetPitch(Mathf.LerpUnclamped(1f, GasAudioSettings.ReleasePitchTarget, t));
				}, 1f, GasAudioSettings.ReleaseFadeDuration).SetId(this);
			}
		}

		private SoundHandle PlayCategory(int category, float fadeDuration)
		{
			SoundData soundData = DataFor(category);
			if (soundData == null)
			{
				return null;
			}
			if (!(_externalSource != null))
			{
				return AudioManager.PlayLoopingSound(soundData, fadeDuration);
			}
			return AudioManager.PlayLoopingSound(_externalSource, soundData, fadeDuration);
		}
	}
}

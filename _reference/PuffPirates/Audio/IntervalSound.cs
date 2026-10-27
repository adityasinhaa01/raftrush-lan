using Cysharp.Threading.Tasks;
using GamesMrkt.Audio;
using UnityEngine;

namespace PuffPirates.Audio
{
	public sealed class IntervalSound : MonoBehaviour
	{
		[SerializeField]
		private SoundData _sound;

		[Tooltip("Seconds between plays.")]
		[SerializeField]
		[Min(0.05f)]
		private float _interval = 3f;

		[Tooltip("Play once as soon as enabled instead of waiting one interval first.")]
		[SerializeField]
		private bool _playOnEnable;

		[Tooltip("Plays the sound. Put it on the part that makes the sound and tune rolloff/distances/volume here. Empty = one is created on this GameObject using the distances below.")]
		[SerializeField]
		private AudioSource _source;

		[Tooltip("Full volume up to this distance (m). Only used when Source is empty.")]
		[SerializeField]
		[Min(0.1f)]
		private float _minDistance = 8f;

		[Tooltip("Silent beyond this distance (m). Only used when Source is empty.")]
		[SerializeField]
		[Min(1f)]
		private float _maxDistance = 80f;

		private UniTask _preload;

		private bool _ownsClips;

		private bool _ready;

		private float _nextPlayTime;

		private void Awake()
		{
			if (_source != null)
			{
				Enforce3D(_source);
			}
			else
			{
				_source = base.gameObject.AddComponent<AudioSource>();
				Sfx3D.Setup3D(_source);
				_source.minDistance = _minDistance;
				_source.maxDistance = _maxDistance;
			}
			if (_sound != null)
			{
				_preload = AudioManager.PreloadAsync(_sound).Preserve();
				_ownsClips = true;
				WaitForClips().Forget();
			}
		}

		private async UniTaskVoid WaitForClips()
		{
			await _preload;
			if (this != null)
			{
				_ready = true;
			}
		}

		private void OnEnable()
		{
			_nextPlayTime = Time.time + (_playOnEnable ? 0f : _interval);
		}

		private void Update()
		{
			if (_ready && !(Time.time < _nextPlayTime))
			{
				_nextPlayTime += _interval;
				if (_nextPlayTime < Time.time)
				{
					_nextPlayTime = Time.time + _interval;
				}
				AudioManager.PlaySound(_source, _sound);
			}
		}

		private void OnDestroy()
		{
			if (_ownsClips)
			{
				AudioManager.ReleaseClips(_sound);
			}
		}

		private static void Enforce3D(AudioSource src)
		{
			src.playOnAwake = false;
			src.loop = false;
			src.spatialBlend = 1f;
			src.dopplerLevel = 0f;
		}
	}
}

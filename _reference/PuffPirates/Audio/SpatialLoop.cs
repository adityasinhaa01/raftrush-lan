using Cysharp.Threading.Tasks;
using GamesMrkt.Audio;
using UnityEngine;

namespace PuffPirates.Audio
{
	public sealed class SpatialLoop : MonoBehaviour
	{
		[Tooltip("Plays this SoundData. Empty = the library sound below.")]
		[SerializeField]
		private SoundData _soundData;

		[Tooltip("Library sound, used only when Sound Data is empty.")]
		[SerializeField]
		private CommonSound _sound = CommonSound.FlowingWater;

		[Tooltip("Plays the loop. Put it on the moving part and tune rolloff/distances/volume here. Empty = one is created on this GameObject with the project's 3D defaults.")]
		[SerializeField]
		private AudioSource _source;

		private SoundHandle _handle;

		private UniTask _preload;

		private bool _ownsClips;

		private int _gen;

		private void Awake()
		{
			if (_source != null)
			{
				Enforce3DLoop(_source);
			}
			else
			{
				_source = base.gameObject.AddComponent<AudioSource>();
				Sfx3D.Setup3D(_source);
			}
			if (_soundData != null)
			{
				_preload = AudioManager.PreloadAsync(_soundData).Preserve();
				_ownsClips = true;
			}
		}

		private async void OnEnable()
		{
			int gen = ++_gen;
			await (_ownsClips ? _preload : AudioManager.PreloadLibraryAsync());
			if (gen == _gen && !(this == null) && base.isActiveAndEnabled)
			{
				SoundData soundData = ((_soundData != null) ? _soundData : ((AudioManager.Library != null) ? AudioManager.Library.Get(_sound) : null));
				if (!(soundData == null))
				{
					_handle = AudioManager.PlayLoopingSound(_source, soundData);
				}
			}
		}

		private void OnDisable()
		{
			_gen++;
			_handle?.Stop(0.2f);
			_handle = null;
		}

		private void OnDestroy()
		{
			if (_ownsClips)
			{
				AudioManager.ReleaseClips(_soundData);
			}
		}

		private static void Enforce3DLoop(AudioSource src)
		{
			src.playOnAwake = false;
			src.spatialBlend = 1f;
			src.dopplerLevel = 0f;
		}
	}
}

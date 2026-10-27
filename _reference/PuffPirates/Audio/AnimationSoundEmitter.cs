using System;
using Cysharp.Threading.Tasks;
using GamesMrkt.Audio;
using UnityEngine;

namespace PuffPirates.Audio
{
	public sealed class AnimationSoundEmitter : MonoBehaviour
	{
		[Serializable]
		private struct SoundEntry
		{
			[Tooltip("Matches the Animation Event's String parameter. An empty event string plays the first entry.")]
			public string Key;

			public SoundData Sound;

			[Tooltip("Plays this sound. Put it on the moving part so the sound follows it; tune rolloff and volume here. Give overlapping sounds separate sources: PlayOneShot shares the source's pitch, so a random-pitch sound would re-pitch one still playing. Empty = shared Sfx3D pool at this transform.")]
			public AudioSource Source;
		}

		[SerializeField]
		private SoundEntry[] _sounds = Array.Empty<SoundEntry>();

		private bool _preloaded;

		private void Awake()
		{
			SoundEntry[] sounds = _sounds;
			for (int i = 0; i < sounds.Length; i++)
			{
				SoundEntry soundEntry = sounds[i];
				if (soundEntry.Source != null)
				{
					Enforce3D(soundEntry.Source);
				}
			}
		}

		private void Start()
		{
			SoundEntry[] sounds = _sounds;
			for (int i = 0; i < sounds.Length; i++)
			{
				SoundEntry soundEntry = sounds[i];
				if (soundEntry.Sound != null)
				{
					AudioManager.PreloadAsync(soundEntry.Sound).Forget();
				}
			}
			_preloaded = true;
		}

		private void OnDestroy()
		{
			if (!_preloaded)
			{
				return;
			}
			SoundEntry[] sounds = _sounds;
			for (int i = 0; i < sounds.Length; i++)
			{
				SoundEntry soundEntry = sounds[i];
				if (soundEntry.Sound != null)
				{
					AudioManager.ReleaseClips(soundEntry.Sound);
				}
			}
		}

		public void PlaySound(AnimationEvent evt)
		{
			Play(evt?.stringParameter);
		}

		public void Play(string key)
		{
			int num = FindIndex(key);
			if (num < 0)
			{
				Debug.LogWarning("[AnimationSoundEmitter] No sound with key '" + key + "' on " + base.name + ".", this);
				return;
			}
			SoundEntry soundEntry = _sounds[num];
			if (!(soundEntry.Sound == null))
			{
				if (soundEntry.Source != null)
				{
					AudioManager.PlaySound(soundEntry.Source, soundEntry.Sound);
				}
				else
				{
					Sfx3D.Play(soundEntry.Sound, base.transform.position);
				}
			}
		}

		private int FindIndex(string key)
		{
			if (_sounds.Length == 0)
			{
				return -1;
			}
			if (string.IsNullOrEmpty(key))
			{
				return 0;
			}
			for (int i = 0; i < _sounds.Length; i++)
			{
				if (string.Equals(_sounds[i].Key, key, StringComparison.OrdinalIgnoreCase))
				{
					return i;
				}
			}
			return -1;
		}

		private static void Enforce3D(AudioSource src)
		{
			src.playOnAwake = false;
			src.loop = false;
			src.spatialBlend = 1f;
			src.dopplerLevel = 0f;
		}

		private void Reset()
		{
			_sounds = new SoundEntry[1]
			{
				new SoundEntry
				{
					Key = "Default"
				}
			};
		}
	}
}

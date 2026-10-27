using GamesMrkt.Audio;
using UnityEngine;

namespace PuffPirates.Audio
{
	public static class Sfx3D
	{
		public static float MinDistance = 8f;

		public static float MaxDistance = 80f;

		private const int PoolSize = 16;

		private static AudioSource[] _pool;

		private static int _next;

		public static void Play(CommonSound sound, Vector3 position, float volume01 = 1f)
		{
			Play((AudioManager.Library != null) ? AudioManager.Library.Get(sound) : null, position, volume01);
		}

		public static void Play(SoundData data, Vector3 position, float volume01 = 1f)
		{
			if (!(data == null))
			{
				AudioSource audioSource = Next();
				audioSource.transform.position = position;
				audioSource.volume = volume01;
				audioSource.minDistance = MinDistance;
				audioSource.maxDistance = MaxDistance;
				AudioManager.PlaySound(audioSource, data);
			}
		}

		public static void Setup3D(AudioSource src)
		{
			src.playOnAwake = false;
			src.spatialBlend = 1f;
			src.rolloffMode = AudioRolloffMode.Linear;
			src.dopplerLevel = 0f;
			src.minDistance = MinDistance;
			src.maxDistance = MaxDistance;
		}

		private static AudioSource Next()
		{
			if (_pool == null || _pool[0] == null)
			{
				GameObject gameObject = new GameObject("~Sfx3D");
				Object.DontDestroyOnLoad(gameObject);
				_pool = new AudioSource[16];
				for (int i = 0; i < 16; i++)
				{
					GameObject gameObject2 = new GameObject("src" + i);
					gameObject2.transform.SetParent(gameObject.transform, worldPositionStays: false);
					_pool[i] = gameObject2.AddComponent<AudioSource>();
					Setup3D(_pool[i]);
				}
			}
			return _pool[_next++ % 16];
		}
	}
}

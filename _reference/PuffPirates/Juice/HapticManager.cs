using PuffPirates.Save;
using PuffPirates.Settings;
using UnityEngine;

namespace PuffPirates.Juice
{
	public static class HapticManager
	{
		private const string PrefKey = "puff_haptics";

		private static bool _enabled = true;

		private static bool _init;

		public static bool Enabled
		{
			get
			{
				EnsureInit();
				return _enabled;
			}
			set
			{
				EnsureInit();
				_enabled = value;
				SettingsSaveScheduler.Request("haptics");
			}
		}

		internal static HapticsData Export()
		{
			return new HapticsData
			{
				enabled = _enabled
			};
		}

		internal static void Import(HapticsData d)
		{
			if (d != null)
			{
				_enabled = d.enabled;
			}
		}

		internal static HapticsData ReadLegacyPlayerPrefs()
		{
			if (!PlayerPrefs.HasKey("puff_haptics"))
			{
				return null;
			}
			return new HapticsData
			{
				enabled = (PlayerPrefs.GetInt("puff_haptics", 1) == 1)
			};
		}

		private static void EnsureInit()
		{
			if (!_init)
			{
				_init = true;
			}
		}

		public static void Impact(float intensity)
		{
			EnsureInit();
			if (_enabled)
			{
				intensity = Mathf.Clamp01(intensity);
				if (!(intensity <= 0.01f))
				{
					long ms = (long)Mathf.Lerp(12f, 45f, intensity);
					int amplitude = (int)Mathf.Lerp(80f, 255f, intensity);
					Vibrate(ms, amplitude);
				}
			}
		}

		public static void Light()
		{
			Impact(0.25f);
		}

		public static void Medium()
		{
			Impact(0.55f);
		}

		public static void Heavy()
		{
			Impact(1f);
		}

		private static void Vibrate(long ms, int amplitude)
		{
		}
	}
}

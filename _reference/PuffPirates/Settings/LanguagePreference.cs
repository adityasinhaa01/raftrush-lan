using GamesMrkt.Localization;
using PuffPirates.Save;
using UnityEngine;

namespace PuffPirates.Settings
{
	public static class LanguagePreference
	{
		private const string LegacyPrefKey = "pp_lang";

		private static string _chosen;

		public static string Current => Loc.CurrentLanguage;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			_chosen = null;
		}

		public static void Set(string code)
		{
			LocalizationSystem system = Loc.System;
			if (system != null && system.SetLanguage(code, persist: false) && !(_chosen == system.CurrentLanguage))
			{
				_chosen = system.CurrentLanguage;
				SettingsSaveScheduler.Request("language");
			}
		}

		internal static LanguageData Export()
		{
			if (_chosen != null)
			{
				return new LanguageData
				{
					code = _chosen
				};
			}
			return null;
		}

		internal static void Import(LanguageData data)
		{
			if (!string.IsNullOrEmpty(data?.code))
			{
				LocalizationSystem system = Loc.System;
				if (system != null && system.SetLanguage(data.code, persist: false))
				{
					_chosen = system.CurrentLanguage;
				}
			}
		}

		internal static LanguageData ReadLegacyPlayerPrefs()
		{
			string text = PlayerPrefs.GetString("pp_lang", "");
			if (!string.IsNullOrEmpty(text))
			{
				return new LanguageData
				{
					code = text
				};
			}
			return null;
		}
	}
}

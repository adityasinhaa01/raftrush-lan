using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GamesMrkt.Core;
using GamesMrkt.Save;
using GamesMrkt.Utils;
using PuffPirates.Backend;
using PuffPirates.Juice;
using PuffPirates.Settings;
using UnityEngine;

namespace PuffPirates.Save
{
	[RequireSystem(typeof(ISaveSystem))]
	public sealed class SettingsPersistenceSystem : IGameSystem, IQuittableSystem, IPausableSystem, IShutdownSystem
	{
		private static UniTaskCompletionSource<bool> s_loaded = new UniTaskCompletionSource<bool>();

		private readonly ISaveSystem _save;

		public string SystemId => "SettingsPersistenceSystem";

		public SettingsPersistenceSystem(ISaveSystem save)
		{
			_save = save;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			s_loaded = new UniTaskCompletionSource<bool>();
		}

		public static async UniTask<bool> WaitUntilLoadedAsync(float timeoutSeconds)
		{
			return (await UniTask.WhenAny(s_loaded.Task, UniTask.Delay(TimeSpan.FromSeconds(timeoutSeconds), ignoreTimeScale: true))).Item1;
		}

		public async UniTask Initialize()
		{
			List<string> migrated = new List<string>();
			await LoadGameSettingsAsync(migrated);
			await LoadKeyBindingsAsync(migrated);
			await LoadLanguageAsync(migrated);
			await LoadHapticsAsync(migrated);
			await LoadSessionAsync(migrated);
			GameSettings.ApplyAudio();
			GameSettings.ApplyGraphicsQuality();
			SettingsSaveScheduler.DiscardPending();
			SettingsSaveScheduler.Writer = Write;
			foreach (string item in migrated)
			{
				SettingsSaveScheduler.Request(item);
			}
			SettingsSaveScheduler.Flush();
			_ = migrated.Count;
			_ = 0;
			s_loaded.TrySetResult(result: true);
		}

		private async UniTask LoadGameSettingsAsync(List<string> migrated)
		{
			SettingsData settings = await StoreFor("settings").LoadAsync<SettingsData>("settings");
			DeviceSettingsData deviceSettingsData = await StoreFor("device_settings").LoadAsync<DeviceSettingsData>("device_settings");
			if ((settings == null || deviceSettingsData == null) && GameSettings.HasLegacyPlayerPrefs())
			{
				GameSettings.ImportLegacyPlayerPrefs();
				if (settings == null)
				{
					migrated.Add("settings");
				}
				if (deviceSettingsData == null)
				{
					migrated.Add("device_settings");
				}
			}
			GameSettings.ImportSettings(settings);
			GameSettings.ImportDevice(deviceSettingsData);
		}

		private async UniTask LoadKeyBindingsAsync(List<string> migrated)
		{
			KeyBindingsData keyBindingsData = await StoreFor("keybindings").LoadAsync<KeyBindingsData>("keybindings");
			if (keyBindingsData == null && KeyBindings.HasLegacyPlayerPrefs())
			{
				KeyBindings.ImportLegacyPlayerPrefs();
				migrated.Add("keybindings");
			}
			else
			{
				KeyBindings.Import(keyBindingsData);
			}
		}

		private async UniTask LoadLanguageAsync(List<string> migrated)
		{
			LanguageData languageData = await StoreFor("language").LoadAsync<LanguageData>("language");
			if (languageData == null && (languageData = LanguagePreference.ReadLegacyPlayerPrefs()) != null)
			{
				migrated.Add("language");
			}
			LanguagePreference.Import(languageData);
		}

		private async UniTask LoadHapticsAsync(List<string> migrated)
		{
			HapticsData hapticsData = await StoreFor("haptics").LoadAsync<HapticsData>("haptics");
			if (hapticsData == null && (hapticsData = HapticManager.ReadLegacyPlayerPrefs()) != null)
			{
				migrated.Add("haptics");
			}
			HapticManager.Import(hapticsData);
		}

		private async UniTask LoadSessionAsync(List<string> migrated)
		{
			SessionData sessionData = await StoreFor("session").LoadAsync<SessionData>("session");
			if (sessionData == null && (sessionData = BackendBootstrap.TakeLegacyPlayerPrefs()) != null)
			{
				migrated.Add("session");
			}
			BackendBootstrap.ImportSession(sessionData);
		}

		private ISaveSystem StoreFor(string key)
		{
			if (!PuffSaveKeys.IsLocal(key))
			{
				return _save;
			}
			return _save.Local;
		}

		private void Write(IReadOnlyCollection<string> keys)
		{
			foreach (string key in keys)
			{
				switch (key)
				{
				case "settings":
					Save(key, GameSettings.ExportSettings());
					break;
				case "device_settings":
					Save(key, GameSettings.ExportDevice());
					break;
				case "keybindings":
					Save(key, KeyBindings.Export());
					break;
				case "haptics":
					Save(key, HapticManager.Export());
					break;
				case "language":
				{
					LanguageData languageData = LanguagePreference.Export();
					if (languageData != null)
					{
						Save(key, languageData);
					}
					break;
				}
				case "session":
				{
					SessionData sessionData = BackendBootstrap.ExportSession();
					if (sessionData != null)
					{
						Save(key, sessionData);
					}
					else
					{
						StoreFor(key).DeleteAsync(key).Forget();
					}
					break;
				}
				default:
					GameLogger.Warning("No writer for save key '" + key + "' — change not saved.", SystemId);
					break;
				}
			}
		}

		private void Save<T>(string key, T data)
		{
			StoreFor(key).SaveAsync(key, data).Forget();
		}

		public void OnApplicationQuitting()
		{
			SettingsSaveScheduler.Flush();
		}

		public void OnPause()
		{
			SettingsSaveScheduler.Flush();
		}

		public void OnResume()
		{
		}

		public UniTask Shutdown()
		{
			SettingsSaveScheduler.Flush();
			SettingsSaveScheduler.Writer = null;
			return UniTask.CompletedTask;
		}
	}
}

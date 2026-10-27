using Cysharp.Threading.Tasks;
using GamesMrkt.Core;
using GamesMrkt.Inputs;
using GamesMrkt.Localization;
using GamesMrkt.Platforms.Steam;
using GamesMrkt.Resource;
using GamesMrkt.Save;
using GamesMrkt.Scene;
using PuffPirates.Input;
using PuffPirates.Save;
using PuffPirates.Steam;
using PuffPirates.Systems;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GamesMrkt.PuffPirates
{
	public class CoreSystemHandler : CoreCoordinator<CoreSystemHandler>
	{
		private const uint SteamAppId = 5208630u;

		[Tooltip("PuffInputActions (Assets/PuffPirates/Input). Every player gets its own copy of it.")]
		[SerializeField]
		private InputActionAsset _inputActions;

		[Tooltip("Icons for button prompts (GamesMrkt/UI/InputPrompts/GlyphConfigs). Empty = prompts show text.")]
		[SerializeField]
		private InputGlyphConfig _inputGlyphConfig;

		public static string ApplicaitonInfo => string.Format("v{0} - {1}", Application.version, Singleton<CoreSystemHandler>.Instance.GetSystem<ResourceSystem>().GetInt("build_number")) + " / Demo";

		protected override UniTask SetupSystemsAsync(SystemCollection systems)
		{
			ResourceSystem resourceSystem = new ResourceSystem();
			systems.AddSystem(resourceSystem);
			systems.AddSystem(new LocalizationSystem());
			SteamSystem steamSystem = new SteamSystem(5208630u);
			systems.AddSystem(steamSystem);
			systems.AddSystem(new SteamAuthSystem(steamSystem));
			SteamPresenceSystem steamPresenceSystem = new SteamPresenceSystem(steamSystem);
			systems.AddSystem(steamPresenceSystem);
			systems.AddSystem(new PuffSteamPresence(steamPresenceSystem));
			systems.AddSystem(new SteamAvatarSystem(steamSystem));
			systems.AddSystem(new SteamOverlaySystem(steamSystem));
			systems.AddSystem(new SteamDeckSystem(steamSystem));
			systems.AddSystem(new SteamDlcSystem(steamSystem));
			systems.AddSystem(new SteamMicroTxnSystem(steamSystem));
			SteamTimelineSystem steamTimelineSystem = new SteamTimelineSystem(steamSystem);
			systems.AddSystem(steamTimelineSystem);
			SteamCloudSaveSystem steamCloudSaveSystem = new SteamCloudSaveSystem(steamSystem, new PlayerPrefsSaveSystem());
			systems.AddSystem(steamCloudSaveSystem);
			systems.AddSystem(new SettingsPersistenceSystem(steamCloudSaveSystem));
			systems.AddSystem(new HostRunSystem(steamCloudSaveSystem));
			systems.AddSystem(new MatchConfigSystem());
			InputCoreSystem inputCoreSystem = new InputCoreSystem(_inputActions, InputPlayerMode.SinglePlayer, PuffInputNames.ALL_ACTION_MAPS);
			systems.AddSystem(inputCoreSystem);
			systems.AddSystem(new PlayerInputSystem(inputCoreSystem, steamSystem));
			InputPrompts.SetGlyphConfig(_inputGlyphConfig);
			if (systems.TryGetSystem<ConnectionSystem>(out var system))
			{
				systems.AddSystem(new LobbySystem(system));
			}
			systems.AddSystem(new SteamTimelineTracker(steamTimelineSystem, system));
			systems.AddSystem(new SurveySystem(resourceSystem, system, steamSystem));
			SceneSystem sceneSystem = new SceneSystem();
			PreloadSystem preloadSystem = new PreloadSystem();
			SceneFlowSystem system2 = new SceneFlowSystem(null, sceneSystem, preloadSystem);
			systems.AddSystem(sceneSystem);
			systems.AddSystem(preloadSystem);
			systems.AddSystem(system2);
			return base.SetupSystemsAsync(systems);
		}
	}
}

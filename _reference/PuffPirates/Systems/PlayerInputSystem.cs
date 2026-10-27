using System;
using Cysharp.Threading.Tasks;
using GamesMrkt.Core;
using GamesMrkt.Inputs;
using GamesMrkt.Platforms.Steam;
using PuffPirates.Input;
using PuffPirates.Input.Mic;
using PuffPirates.Networking;
using PuffPirates.Settings;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace PuffPirates.Systems
{
	public class PlayerInputSystem : IGameSystem, IPostInitializeSystem, IShutdownSystem
	{
		private const float CHARACTER_RIGHT_SIGN = -1f;

		private readonly IInputCoreSystem _input;

		private readonly SteamSystem _steam;

		private InputSystemUIInputModule _boundUIModule;

		private IDisposable _uiBinding;

		public string SystemId => "PlayerInputSystem";

		public bool InputBlocked { get; set; }

		public bool OverlayOpen { get; private set; }

		public bool IsRacing { get; set; }

		public bool LocalGasHeld { get; private set; }

		public InputPlayer Player => _input?.PrimaryPlayer;

		private bool Blocked
		{
			get
			{
				if (!InputBlocked && !OverlayOpen)
				{
					return !IsRacing;
				}
				return true;
			}
		}

		public PlayerInputSystem(IInputCoreSystem input, SteamSystem steam = null)
		{
			_input = input;
			_steam = steam;
			if (_steam != null)
			{
				_steam.OnOverlayToggled += HandleOverlayToggled;
			}
		}

		public UniTask Initialize()
		{
			return UniTask.CompletedTask;
		}

		public UniTask OnSystemsInitialized()
		{
			if (Player == null)
			{
				return UniTask.CompletedTask;
			}
			KeyBindings.Attach(Player);
			BindSceneUIModule();
			Player.OnControlSchemeChanged += HandleControlSchemeChanged;
			SceneManager.sceneLoaded += HandleSceneLoaded;
			return UniTask.CompletedTask;
		}

		public UniTask Shutdown()
		{
			if (_steam != null)
			{
				_steam.OnOverlayToggled -= HandleOverlayToggled;
			}
			SceneManager.sceneLoaded -= HandleSceneLoaded;
			if (Player != null)
			{
				Player.OnControlSchemeChanged -= HandleControlSchemeChanged;
			}
			_uiBinding?.Dispose();
			_uiBinding = null;
			KeyBindings.Detach();
			return UniTask.CompletedTask;
		}

		private void HandleOverlayToggled(bool open)
		{
			OverlayOpen = open;
			_input?.ResetAllDevices();
		}

		private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			BindSceneUIModule();
		}

		private void HandleControlSchemeChanged(InputPlayer player)
		{
			RefreshCursorVisibility();
		}

		public void RefreshCursorVisibility()
		{
			bool flag = Player != null && Player.ActiveControlScheme == "Gamepad";
			Cursor.visible = Cursor.lockState != CursorLockMode.Locked && !flag;
		}

		private void BindSceneUIModule()
		{
			InputSystemUIInputModule inputSystemUIInputModule = UnityEngine.Object.FindFirstObjectByType<InputSystemUIInputModule>();
			if (!(inputSystemUIInputModule == null) && !(inputSystemUIInputModule == _boundUIModule) && Player != null)
			{
				_uiBinding?.Dispose();
				_uiBinding = InputUIBinder.Bind(inputSystemUIInputModule, Player);
				_boundUIModule = inputSystemUIInputModule;
				inputSystemUIInputModule.deselectOnBackgroundClick = false;
			}
		}

		private static void ApplyMicGas(ref PirateInput input, bool isBlowing)
		{
			if (isBlowing)
			{
				input.NormalGas = true;
				input.MousePuff = true;
			}
		}

		public PirateInput BuildInput()
		{
			PirateInput input = default;
			InputPlayer player = Player;
			if (Blocked || player == null)
			{
				LocalGasHeld = false;
				return input;
			}
			bool isBlowing = GameSettings.MicGasEnabled && RaspberryMic.HasInstance && RaspberryMic.Instance.IsBlowing;
			if (player.ActiveControlScheme == "Touch")
			{
				input.CircularMovement = Mathf.Clamp(TouchControls.SteerX * GameSettings.TouchSensitivity * -1f, -3f, 3f);
				input.NormalGas = TouchControls.NormalGas;
				input.MousePuff = TouchControls.NormalGas;
				ApplyMicGas(ref input, isBlowing);
				LocalGasHeld = input.NormalGas;
				return input;
			}
			float num = player.GetFloat("Pirate/Turn");
			if (player.ActiveControlScheme == "Gamepad")
			{
				num *= GameSettings.PadSensitivity;
			}
			input.CircularMovement = Mathf.Clamp(num * -1f, -3f, 3f);
			input.NormalGas = player.GetButton("Pirate/Gas");
			input.MousePuff = input.NormalGas;
			ApplyMicGas(ref input, isBlowing);
			LocalGasHeld = input.NormalGas;
			return input;
		}
	}
}

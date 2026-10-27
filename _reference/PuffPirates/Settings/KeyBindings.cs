using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GamesMrkt.Inputs;
using GamesMrkt.Localization;
using PuffPirates.Save;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace PuffPirates.Settings
{
	public static class KeyBindings
	{
		public const int ActionCount = 3;

		public const string UNASSIGNED = "—";

		private static readonly Key[] LEGACY_KEYBOARD_DEFAULTS = new Key[3]
		{
			Key.A,
			Key.D,
			Key.Space
		};

		private static readonly GamepadButton[] LEGACY_GAMEPAD_DEFAULTS = new GamepadButton[3]
		{
			GamepadButton.DpadLeft,
			GamepadButton.DpadRight,
			GamepadButton.RightShoulder
		};

		private const string LEGACY_SCHEME_KEY = "pp_control_scheme";

		private const string GAMEPAD_CANCEL_CONTROL_PATH = "<Gamepad>/start";

		private static InputPlayer _player;

		private static KeyBindingsData _pendingData;

		private static int _rebindEndFrame = -1;

		public static bool IsGamepadScheme
		{
			get
			{
				if (_player != null)
				{
					return _player.ActiveControlScheme == "Gamepad";
				}
				return false;
			}
		}

		public static bool IsRebinding
		{
			get
			{
				if (_player == null || !_player.IsRebinding)
				{
					return Time.frameCount == _rebindEndFrame;
				}
				return true;
			}
		}

		public static event Action OnChanged;

		internal static void Attach(InputPlayer player)
		{
			Detach();
			_player = player;
			_player.OnControlSchemeChanged += HandlePlayerChanged;
			_player.OnBindingsChanged += HandlePlayerChanged;
			if (_pendingData != null)
			{
				Apply(_pendingData);
				_pendingData = null;
			}
			OnChanged?.Invoke();
		}

		internal static void Detach()
		{
			if (_player != null)
			{
				_player.OnControlSchemeChanged -= HandlePlayerChanged;
				_player.OnBindingsChanged -= HandlePlayerChanged;
				_player = null;
			}
		}

		private static void HandlePlayerChanged(InputPlayer _)
		{
			OnChanged?.Invoke();
		}

		public static string DisplayName(GameAction action, string controlScheme = null)
		{
			if (_player == null)
			{
				return "—";
			}
			if (controlScheme == null)
			{
				controlScheme = (IsGamepadScheme ? "Gamepad" : "Keyboard&Mouse");
			}
			(string actionName, string compositePart) tuple = BindingOf(action);
			string item = tuple.actionName;
			string item2 = tuple.compositePart;
			string bindingDisplayString = _player.GetBindingDisplayString(item, item2, controlScheme);
			if (string.IsNullOrEmpty(bindingDisplayString))
			{
				return "—";
			}
			if (!(bindingDisplayString == "Space"))
			{
				return bindingDisplayString;
			}
			return Loc.Get("common.space");
		}

		public static InputPromptContent Prompt(GameAction action, string controlScheme)
		{
			if (_player == null)
			{
				return default;
			}
			var (actionName, compositePart) = BindingOf(action);
			return InputPrompts.Resolve(_player, actionName, compositePart, controlScheme);
		}

		public static string Label(GameAction action)
		{
			return action switch
			{
				GameAction.MoveLeft => Loc.Get("action.move_left"), 
				GameAction.MoveRight => Loc.Get("action.move_right"), 
				GameAction.NormalGas => Loc.Get("action.normal_gas"), 
				_ => action.ToString(), 
			};
		}

		public static async UniTask<bool> RebindAsync(GameAction action, string controlScheme, CancellationToken cancellationToken)
		{
			if (_player == null || _player.IsRebinding)
			{
				return false;
			}
			bool flag = controlScheme == "Gamepad";
			(string actionName, string compositePart) tuple = BindingOf(action);
			string item = tuple.actionName;
			string item2 = tuple.compositePart;
			InputRebindOptions options = new InputRebindOptions
			{
				ControlScheme = controlScheme,
				CompositePart = item2,
				AllowedControlPaths = new string[1] { flag ? "<Gamepad>" : "<Keyboard>" },
				CancelControlPath = (flag ? "<Gamepad>/start" : null)
			};
			bool isMenuInputPaused = flag && _player.IsActionMapEnabled("UI");
			if (isMenuInputPaused)
			{
				_player.DisableActionMap("UI");
			}
			try
			{
				InputRebindResult inputRebindResult = await _player.RebindAsync(item, options, cancellationToken);
				if (inputRebindResult.IsCompleted)
				{
					Save();
				}
				return inputRebindResult.IsCompleted;
			}
			finally
			{
				_rebindEndFrame = Time.frameCount;
				if (isMenuInputPaused && _player != null)
				{
					_player.EnableActionMap("UI");
				}
			}
		}

		public static void CancelRebind()
		{
			_player?.CancelRebind();
		}

		public static void ResetDefaults()
		{
			if (_player != null)
			{
				_player.ResetAllBindings();
			}
			else
			{
				_pendingData = null;
			}
			Save();
		}

		private static (string actionName, string compositePart) BindingOf(GameAction action)
		{
			return action switch
			{
				GameAction.MoveLeft => (actionName: "Pirate/Turn", compositePart: "negative"), 
				GameAction.MoveRight => (actionName: "Pirate/Turn", compositePart: "positive"), 
				_ => (actionName: "Pirate/Gas", compositePart: null), 
			};
		}

		private static void Save()
		{
			SettingsSaveScheduler.Request("keybindings");
		}

		internal static KeyBindingsData Export()
		{
			string text = ((_player != null) ? _player.SaveBindingOverrides() : _pendingData?.overrides);
			return new KeyBindingsData
			{
				overrides = (text ?? string.Empty)
			};
		}

		internal static void Import(KeyBindingsData data)
		{
			if (_player == null)
			{
				_pendingData = data;
			}
			else
			{
				Apply(data);
			}
		}

		private static void Apply(KeyBindingsData data)
		{
			if (data == null)
			{
				_player.ResetAllBindings();
				return;
			}
			if (data.overrides != null)
			{
				_player.LoadBindingOverrides(data.overrides);
				return;
			}
			_player.ResetAllBindings();
			for (int i = 0; i < 3; i++)
			{
				var (actionName, compositePart) = BindingOf((GameAction)i);
				if (data.keyboard != null && i < data.keyboard.Length && data.keyboard[i] != LEGACY_KEYBOARD_DEFAULTS[i])
				{
					string text = KeyControlPath(data.keyboard[i]);
					if (text != null)
					{
						_player.SetBinding(actionName, text, compositePart, "Keyboard&Mouse");
					}
				}
				if (data.gamepad != null && i < data.gamepad.Length && data.gamepad[i] != LEGACY_GAMEPAD_DEFAULTS[i])
				{
					string text2 = GamepadButtonControlPath(data.gamepad[i]);
					if (text2 != null)
					{
						_player.SetBinding(actionName, text2, compositePart, "Gamepad");
					}
				}
			}
			Save();
		}

		private static string KeyControlPath(Key key)
		{
			Keyboard current = Keyboard.current;
			if (key == Key.None || current == null)
			{
				return null;
			}
			KeyControl keyControl = current[key];
			if (keyControl == null)
			{
				return null;
			}
			return "<Keyboard>/" + keyControl.name;
		}

		private static string GamepadButtonControlPath(GamepadButton button)
		{
			return button switch
			{
				GamepadButton.DpadUp => "<Gamepad>/dpad/up", 
				GamepadButton.DpadDown => "<Gamepad>/dpad/down", 
				GamepadButton.DpadLeft => "<Gamepad>/dpad/left", 
				GamepadButton.DpadRight => "<Gamepad>/dpad/right", 
				GamepadButton.North => "<Gamepad>/buttonNorth", 
				GamepadButton.East => "<Gamepad>/buttonEast", 
				GamepadButton.South => "<Gamepad>/buttonSouth", 
				GamepadButton.West => "<Gamepad>/buttonWest", 
				GamepadButton.LeftShoulder => "<Gamepad>/leftShoulder", 
				GamepadButton.RightShoulder => "<Gamepad>/rightShoulder", 
				GamepadButton.LeftTrigger => "<Gamepad>/leftTrigger", 
				GamepadButton.RightTrigger => "<Gamepad>/rightTrigger", 
				GamepadButton.LeftStick => "<Gamepad>/leftStickPress", 
				GamepadButton.RightStick => "<Gamepad>/rightStickPress", 
				GamepadButton.Start => "<Gamepad>/start", 
				GamepadButton.Select => "<Gamepad>/select", 
				_ => null, 
			};
		}

		internal static bool HasLegacyPlayerPrefs()
		{
			if (!PlayerPrefs.HasKey("pp_control_scheme"))
			{
				return PlayerPrefs.HasKey(LegacyKeyboardKey(GameAction.MoveLeft));
			}
			return true;
		}

		internal static void ImportLegacyPlayerPrefs()
		{
			KeyBindingsData keyBindingsData = new KeyBindingsData
			{
				keyboard = new Key[3],
				gamepad = new GamepadButton[3]
			};
			for (int i = 0; i < 3; i++)
			{
				keyBindingsData.keyboard[i] = (Key)PlayerPrefs.GetInt(LegacyKeyboardKey((GameAction)i), (int)LEGACY_KEYBOARD_DEFAULTS[i]);
				keyBindingsData.gamepad[i] = (GamepadButton)PlayerPrefs.GetInt(LegacyGamepadKey((GameAction)i), (int)LEGACY_GAMEPAD_DEFAULTS[i]);
			}
			Import(keyBindingsData);
		}

		private static string LegacyKeyboardKey(GameAction action)
		{
			return "pp_kb_" + action;
		}

		private static string LegacyGamepadKey(GameAction action)
		{
			return "pp_gp_" + action;
		}
	}
}

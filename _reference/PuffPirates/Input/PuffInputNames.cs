namespace PuffPirates.Input
{
	public static class PuffInputNames
	{
		public const string KEYBOARD_MOUSE_SCHEME = "Keyboard&Mouse";

		public const string GAMEPAD_SCHEME = "Gamepad";

		public const string TOUCH_SCHEME = "Touch";

		public const string PIRATE_MAP = "Pirate";

		public const string CAMERA_MAP = "Camera";

		public const string LOBBY_MAP = "Lobby";

		public const string UI_MAP = "UI";

		public const string GLOBAL_MAP = "Global";

		public const string DEBUG_MAP = "Debug";

		public static readonly string[] ALL_ACTION_MAPS = new string[6] { "Pirate", "Camera", "Lobby", "UI", "Global", "Debug" };

		public const string TURN = "Pirate/Turn";

		public const string GAS = "Pirate/Gas";

		public const string UNSTUCK = "Pirate/Unstuck";

		public const string NEGATIVE_PART = "negative";

		public const string POSITIVE_PART = "positive";

		public const string CAMERA_ORBIT = "Camera/Orbit";

		public const string CAMERA_MOUSE_ORBIT = "Camera/MouseOrbit";

		public const string CAMERA_ZOOM = "Camera/Zoom";

		public const string CAMERA_ZOOM_STEP = "Camera/ZoomStep";

		public const string CAMERA_PAN = "Camera/Pan";

		public const string CAMERA_CYCLE_MODE = "Camera/CycleMode";

		public const string CAMERA_RESET = "Camera/Reset";

		public const string LOBBY_READY = "Lobby/Ready";

		public const string LOBBY_UNREADY = "Lobby/Unready";

		public const string LOBBY_START_GAME = "Lobby/StartGame";

		public const string LOBBY_NEXT_CHARACTER = "Lobby/NextCharacter";

		public const string LOBBY_PREVIOUS_CHARACTER = "Lobby/PreviousCharacter";

		public const string UI_SUBMIT = "UI/Submit";

		public const string UI_CANCEL = "UI/Cancel";

		public const string UI_NAVIGATE = "UI/Navigate";

		public const string UI_PREVIOUS_TAB = "UI/PreviousTab";

		public const string UI_NEXT_TAB = "UI/NextTab";

		public const string PAUSE = "Global/Pause";

		public const string TOGGLE_WATER_PANEL = "Debug/ToggleWaterPanel";
	}
}

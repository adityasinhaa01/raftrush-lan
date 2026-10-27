namespace PuffPirates.Save
{
	public static class PuffSaveKeys
	{
		public const string Settings = "settings";

		public const string KeyBindings = "keybindings";

		public const string Language = "language";

		public const string HostRuns = "host_runs";

		public const string DeviceSettings = "device_settings";

		public const string Haptics = "haptics";

		public const string Session = "session";

		public static bool IsLocal(string key)
		{
			if (!(key == "device_settings") && !(key == "haptics"))
			{
				return key == "session";
			}
			return true;
		}
	}
}

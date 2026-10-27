using System;

namespace PuffPirates.Save
{
	[Serializable]
	public sealed class DeviceSettingsData
	{
		public bool micGas;

		public float micSensitivity = 1f;

		public string micDevice = "";

		public float touchSensitivity = 1f;

		public bool joystickOnLeft = true;

		public float buttonScale = 1f;

		public float buttonOpacity = 0.7f;

		public string graphicsQuality = "High";
	}
}

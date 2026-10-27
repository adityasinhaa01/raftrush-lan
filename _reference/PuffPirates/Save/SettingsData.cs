using System;

namespace PuffPirates.Save
{
	[Serializable]
	public sealed class SettingsData
	{
		public float mouseSensitivity = 0.5f;

		public float padSensitivity = 1f;

		public bool invertMouse;

		public float masterVolume = 0.8f;

		public float musicVolume = 0.8f;

		public float sfxVolume = 0.8f;

		public float fartVolume = 0.8f;

		public bool muted;

		public bool cameraShake = true;

		public float cameraMaxDistance = 30f;
	}
}

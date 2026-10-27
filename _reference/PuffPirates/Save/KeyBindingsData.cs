using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace PuffPirates.Save
{
	[Serializable]
	public sealed class KeyBindingsData
	{
		public string overrides;

		public Key[] keyboard;

		public GamepadButton[] gamepad;
	}
}

using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FartTogether.PlayerInput
{
    /// <summary>
    /// Hardware input provider for raft facing reset.
    /// Defaults to Key 'R'. Supports Unity New Input System and legacy Input fallback.
    /// Can be rebound or extended for gamepad/network control.
    /// </summary>
    public class KeyboardRaftReorientInput : MonoBehaviour, IRaftReorientInput
    {
        [Header("Key Binding")]
        [Tooltip("Legacy fallback key code for reorienting the raft (Default: R).")]
        [SerializeField] private KeyCode _legacyKey = KeyCode.R;

        [Header("Gamepad Support (Optional)")]
        [Tooltip("Whether gamepad D-Pad Up or Y/Triangle button can also trigger reorientation.")]
        [SerializeField] private bool _enableGamepad = true;

        public bool IsReorientRequested
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var kb = Keyboard.current;
                if (kb != null && kb.rKey.wasPressedThisFrame)
                {
                    return true;
                }

                if (_enableGamepad)
                {
                    var pad = Gamepad.current;
                    if (pad != null && (pad.dpad.up.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame))
                    {
                        return true;
                    }
                }
#endif
                if (UnityEngine.Input.GetKeyDown(_legacyKey))
                {
                    return true;
                }

                if (_enableGamepad && (UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton3) || UnityEngine.Input.GetKeyDown(KeyCode.R)))
                {
                    return true;
                }

                return false;
            }
        }
    }
}

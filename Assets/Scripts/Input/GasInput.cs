using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FartTogether.PlayerInput
{
    /// <summary>
    /// Keyboard implementation of IGasInputSource for 4 seats using Keys 1, 2, 3, 4.
    /// Handles gas consumption, stamina depletion lockout, cooldown delay, and regeneration.
    /// </summary>
    public class GasInput : MonoBehaviour, IGasInputSource
    {
        [System.Serializable]
        public class SeatGasState
        {
            [Tooltip("Current fuel value for this seat (0 to MaxGas).")]
            public float currentFuel = 100f;

            [Tooltip("True when fuel hits 0 and remains locked until reaching recovery threshold.")]
            public bool isDepleted = false;

            [Tooltip("Timer tracking delay before fuel regeneration starts after releasing gas.")]
            public float regenDelayTimer = 0f;

            [Tooltip("Time currently held continuously (used for thrust ramp).")]
            public float holdTimer = 0f;

            [Tooltip("Whether the gas key is currently active and delivering thrust.")]
            public bool isHeld = false;
        }

        [Header("Fuel Settings (PirateGasConfig Reference)")]
        [SerializeField] private float _maxGas = 100f;
        [SerializeField] private float _gasCostPerSec = 12f;
        [SerializeField] private float _gasRegenDelay = 1.0f;
        [SerializeField] private float _gasRegenPerSec = 18f;
        [Range(0.05f, 0.9f)]
        [SerializeField] private float _gasRecoverPercent = 0.35f;

        [Header("Thrust Ramp Settings (BoatPhysicsConfig Reference)")]
        [SerializeField] private float _gasRampSeconds = 3.0f;
        [Range(0f, 1f)]
        [SerializeField] private float _minForceFraction = 0.111f; // 100 N / 900 N

        [Header("Seat States")]
        [SerializeField] private SeatGasState[] _seats = new SeatGasState[4];

        public int SeatCount => _seats.Length;

        private void Awake()
        {
            if (_seats == null || _seats.Length != 4)
            {
                _seats = new SeatGasState[4];
            }
            for (int i = 0; i < _seats.Length; i++)
            {
                if (_seats[i] == null)
                {
                    _seats[i] = new SeatGasState();
                }
                _seats[i].currentFuel = _maxGas;
                _seats[i].isDepleted = false;
                _seats[i].regenDelayTimer = 0f;
                _seats[i].holdTimer = 0f;
                _seats[i].isHeld = false;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _seats.Length; i++)
            {
                UpdateSeat(i, dt);
            }
        }

        private void UpdateSeat(int index, float dt)
        {
            SeatGasState seat = _seats[index];
            bool keyPressed = CheckKeyPressed(index);

            // Cannot fire if depleted
            bool canFire = keyPressed && !seat.isDepleted && seat.currentFuel > 0.01f;

            if (canFire)
            {
                seat.isHeld = true;
                seat.holdTimer = Mathf.Min(seat.holdTimer + dt, _gasRampSeconds);

                // Consume fuel
                seat.currentFuel = Mathf.Max(0f, seat.currentFuel - _gasCostPerSec * dt);
                seat.regenDelayTimer = _gasRegenDelay;

                if (seat.currentFuel <= 0.01f)
                {
                    seat.currentFuel = 0f;
                    seat.isDepleted = true;
                    seat.isHeld = false;
                    seat.holdTimer = 0f;
                }
            }
            else
            {
                seat.isHeld = false;
                seat.holdTimer = 0f;

                // Handle regeneration cooldown delay
                if (seat.regenDelayTimer > 0f)
                {
                    seat.regenDelayTimer = Mathf.Max(0f, seat.regenDelayTimer - dt);
                }
                else if (seat.currentFuel < _maxGas)
                {
                    seat.currentFuel = Mathf.Min(_maxGas, seat.currentFuel + _gasRegenPerSec * dt);

                    // Recover from depletion lockout once fuel reaches the threshold
                    if (seat.isDepleted && seat.currentFuel >= _maxGas * _gasRecoverPercent)
                    {
                        seat.isDepleted = false;
                    }
                }
            }
        }

        private bool CheckKeyPressed(int index)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                switch (index)
                {
                    case 0: return kb.digit1Key.isPressed || kb.numpad1Key.isPressed;
                    case 1: return kb.digit2Key.isPressed || kb.numpad2Key.isPressed;
                    case 2: return kb.digit3Key.isPressed || kb.numpad3Key.isPressed;
                    case 3: return kb.digit4Key.isPressed || kb.numpad4Key.isPressed;
                }
            }
#endif
            // Fallback for legacy input if input system is disabled or keyboard object is null
            switch (index)
            {
                case 0: return UnityEngine.Input.GetKey(KeyCode.Alpha1) || UnityEngine.Input.GetKey(KeyCode.Keypad1);
                case 1: return UnityEngine.Input.GetKey(KeyCode.Alpha2) || UnityEngine.Input.GetKey(KeyCode.Keypad2);
                case 2: return UnityEngine.Input.GetKey(KeyCode.Alpha3) || UnityEngine.Input.GetKey(KeyCode.Keypad3);
                case 3: return UnityEngine.Input.GetKey(KeyCode.Alpha4) || UnityEngine.Input.GetKey(KeyCode.Keypad4);
                default: return false;
            }
        }

        public bool IsGasHeld(int seatIndex)
        {
            if (seatIndex < 0 || seatIndex >= _seats.Length) return false;
            return _seats[seatIndex].isHeld;
        }

        public float GetThrustFraction(int seatIndex)
        {
            if (seatIndex < 0 || seatIndex >= _seats.Length) return 0f;
            SeatGasState seat = _seats[seatIndex];
            if (!seat.isHeld) return 0f;

            float ramp = (_gasRampSeconds > 0.001f) ? Mathf.Clamp01(seat.holdTimer / _gasRampSeconds) : 1f;
            return Mathf.Lerp(_minForceFraction, 1.0f, ramp);
        }

        public float GetGasFuel(int seatIndex)
        {
            if (seatIndex < 0 || seatIndex >= _seats.Length) return 0f;
            return _seats[seatIndex].currentFuel;
        }

        public float GetMaxGasFuel(int seatIndex) => _maxGas;

        public bool IsGasDepleted(int seatIndex)
        {
            if (seatIndex < 0 || seatIndex >= _seats.Length) return false;
            return _seats[seatIndex].isDepleted;
        }

        public bool IsKeyPressed(int seatIndex)
        {
            return CheckKeyPressed(seatIndex);
        }
    }
}

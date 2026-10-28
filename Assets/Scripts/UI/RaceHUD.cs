using FartTogether.PlayerInput;
using FartTogether.Raft;
using FartTogether.Track;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FartTogether.GameUI
{
    /// <summary>
    /// HUD displaying current speed, 4 gas fuel bars/indicators, and the race timer.
    /// Freezes timer and displays the finish banner upon crossing the finish line.
    /// </summary>
    public class RaceHUD : MonoBehaviour
    {
        [Header("Target References")]
        [SerializeField] private RaftController _raft = null;
        [SerializeField] private MonoBehaviour _gasInputComponent = null;

        [Header("UI Text Displays (TextMeshPro)")]
        [SerializeField] private TextMeshProUGUI _speedText = null;
        [SerializeField] private TextMeshProUGUI _timerText = null;
        [SerializeField] private TextMeshProUGUI _finishBannerText = null;
        [SerializeField] private TextMeshProUGUI _debugText = null;
        [SerializeField] private TextMeshProUGUI _resetFacingHintText = null;

        private RaftReorient _raftReorient = null;

        [Header("Gas Fuel Displays")]
        [Tooltip("Optional Image bars set to Filled mode, or sliders for seats 1-4.")]
        [SerializeField] private Image[] _gasFuelBars = new Image[4];

        [Tooltip("Optional text labels showing fuel percentage for seats 1-4.")]
        [SerializeField] private TextMeshProUGUI[] _gasFuelTexts = new TextMeshProUGUI[4];

        [Header("Visual Colors")]
        [SerializeField] private Color _normalGasColor = new Color(0.2f, 0.85f, 0.3f);
        [SerializeField] private Color _depletedGasColor = new Color(0.9f, 0.2f, 0.2f);
        [SerializeField] private Color _activeThrustColor = new Color(1.0f, 0.7f, 0.1f);

        private IGasInputSource _inputSource;
        private bool _isRaceFinished = false;
        private float _finalRaceTime = 0f;

        private void OnEnable()
        {
            FinishZone.OnRaceFinishedGlobal += HandleRaceFinished;
        }

        private void OnDisable()
        {
            FinishZone.OnRaceFinishedGlobal -= HandleRaceFinished;
        }

        private void Start()
        {
            if (_raft == null)
            {
                _raft = FindAnyObjectByType<RaftController>();
            }

            if (_gasInputComponent is IGasInputSource source)
            {
                _inputSource = source;
            }
            else if (_raft != null && _raft.InputSource != null)
            {
                _inputSource = _raft.InputSource;
            }
            else
            {
                _inputSource = FindAnyObjectByType<GasInput>();
            }

            if (_finishBannerText != null)
            {
                _finishBannerText.gameObject.SetActive(false);
            }

            _raftReorient = FindAnyObjectByType<RaftReorient>();
        }

        private void Update()
        {
            UpdateSpeedDisplay();
            UpdateTimerDisplay();
            UpdateGasDisplays();
            UpdateDebugDisplay();
            UpdateResetFacingHintDisplay();
        }

        private void UpdateResetFacingHintDisplay()
        {
            if (_resetFacingHintText == null) return;

            if (_raftReorient == null)
            {
                _raftReorient = FindAnyObjectByType<RaftReorient>();
            }

            if (_raftReorient != null && !_raftReorient.CanReorient)
            {
                _resetFacingHintText.text = $"R: Reset facing ({_raftReorient.CooldownRemaining:F1}s)";
                _resetFacingHintText.color = new Color(0.75f, 0.75f, 0.75f, 0.45f);
            }
            else
            {
                _resetFacingHintText.text = "R: Reset facing";
                _resetFacingHintText.color = new Color(1.0f, 1.0f, 1.0f, 0.75f);
            }
        }

        private void UpdateSpeedDisplay()
        {
            if (_speedText == null) return;

            float speedMs = _raft != null ? _raft.CurrentSpeed : 0f;
            float speedKmh = speedMs * 3.6f;
            _speedText.text = $"SPEED: {speedKmh:F1} km/h";
        }

        private void UpdateTimerDisplay()
        {
            if (_timerText == null) return;

            float timeToDisplay = _isRaceFinished ? _finalRaceTime : Time.timeSinceLevelLoad;
            _timerText.text = $"TIME: {FinishZone.FormatTime(timeToDisplay)}";
        }

        private void UpdateGasDisplays()
        {
            if (_inputSource == null) return;

            for (int i = 0; i < 4; i++)
            {
                float fuel = _inputSource.GetGasFuel(i);
                float maxFuel = _inputSource.GetMaxGasFuel(i);
                float fraction = maxFuel > 0f ? Mathf.Clamp01(fuel / maxFuel) : 0f;
                bool isDepleted = _inputSource.IsGasDepleted(i);
                bool isHeld = _inputSource.IsGasHeld(i);

                Color barColor = isDepleted ? _depletedGasColor : (isHeld ? _activeThrustColor : _normalGasColor);

                if (_gasFuelBars != null && i < _gasFuelBars.Length && _gasFuelBars[i] != null)
                {
                    _gasFuelBars[i].fillAmount = fraction;
                    _gasFuelBars[i].color = barColor;
                }

                if (_gasFuelTexts != null && i < _gasFuelTexts.Length && _gasFuelTexts[i] != null)
                {
                    string status = isDepleted ? "LOCK" : (isHeld ? "FART!" : $"{Mathf.RoundToInt(fraction * 100)}%");
                    _gasFuelTexts[i].text = $"P{i + 1} [Key {i + 1}]: {status}";
                    _gasFuelTexts[i].color = barColor;
                }
            }
        }

        private void UpdateDebugDisplay()
        {
            if (_debugText == null) return;

            Vector3 pos = _raft != null ? _raft.transform.position : Vector3.zero;

            string pressedKeys = "";
            if (_inputSource != null)
            {
                for (int i = 0; i < _inputSource.SeatCount; i++)
                {
                    if (_inputSource.IsKeyPressed(i))
                    {
                        if (pressedKeys.Length > 0) pressedKeys += " ";
                        pressedKeys += $"[{i + 1}]";
                    }
                }
            }

            if (string.IsNullOrEmpty(pressedKeys))
            {
                pressedKeys = "None";
            }

            _debugText.text = $"POS: ({pos.x:F1}, {pos.y:F1}, {pos.z:F1}) | KEYS: {pressedKeys}";
        }

        private void HandleRaceFinished(float finishTime)
        {
            _isRaceFinished = true;
            _finalRaceTime = finishTime;

            if (_finishBannerText != null)
            {
                _finishBannerText.gameObject.SetActive(true);
                _finishBannerText.text = $"FINISH!\nTIME: {FinishZone.FormatTime(finishTime)}";
            }
        }
    }
}

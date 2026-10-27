using UnityEngine;

namespace FartTogether.Obstacles
{
    /// <summary>
    /// Replicates FakeWaveBob.cs from _reference/PuffPirates/Water/FakeWaveBob.cs.
    /// Simulates gentle wave bobbing on floating log and water obstacles using Perlin noise.
    /// </summary>
    [DisallowMultipleComponent]
    public class FloatingBobObstacle : MonoBehaviour
    {
        [Header("Wave Parameters (FakeWaveBob.cs)")]
        [Tooltip("World XZ travel direction of wave crests.")]
        [SerializeField] private Vector2 _waveDirection = new Vector2(1f, 0f);

        [Tooltip("Wavelength in meters (original FakeWaveBob._wavelength = 6.0m).")]
        [SerializeField] private float _wavelength = 6.0f;

        [Tooltip("Wave progression speed in m/s (original FakeWaveBob._speed = 2.5m/s).")]
        [SerializeField] private float _speed = 2.5f;

        [Tooltip("Perpendicular noise variation across the wave crest (original _crestVariation = 0.15).")]
        [SerializeField] private float _crestVariation = 0.15f;

        [Tooltip("Vertical displacement amplitude (original FakeWaveBob._amplitude = 0.25m).")]
        [SerializeField] private float _amplitude = 0.25f;

        private float _baseY;
        private bool _isInitialized;

        public float Amplitude { get => _amplitude; set => _amplitude = value; }
        public float Speed { get => _speed; set => _speed = value; }
        public float Wavelength { get => _wavelength; set => _wavelength = value; }

        private void Awake()
        {
            _baseY = transform.localPosition.y;
            _isInitialized = true;
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                _baseY = transform.localPosition.y;
                _isInitialized = true;
            }

            Vector3 worldPos = transform.position;
            Vector2 pos2D = new Vector2(worldPos.x, worldPos.z);
            Vector2 dir = _waveDirection.sqrMagnitude > 0.0001f ? _waveDirection.normalized : Vector2.right;
            Vector2 ortho = new Vector2(-dir.y, dir.x);

            float waveLengthSafe = Mathf.Max(0.1f, _wavelength);
            float u = (Vector2.Dot(pos2D, dir) - _speed * Time.time) / waveLengthSafe;
            float v = Vector2.Dot(pos2D, ortho) * _crestVariation / waveLengthSafe;

            // Authentic Perlin noise equation from FakeWaveBob.cs line 49
            float noise = Mathf.PerlinNoise(u, v) * 2f - 1f;

            Vector3 localPos = transform.localPosition;
            localPos.y = _baseY + noise * _amplitude;
            transform.localPosition = localPos;
        }

        private void OnDisable()
        {
            if (_isInitialized)
            {
                Vector3 localPos = transform.localPosition;
                localPos.y = _baseY;
                transform.localPosition = localPos;
            }
        }
    }
}

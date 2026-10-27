using UnityEngine;

namespace PuffPirates.Water
{
	[DisallowMultipleComponent]
	public sealed class FakeWaveBob : MonoBehaviour
	{
		[Header("Wave (shared across neighbours)")]
		[Tooltip("World XZ direction the waves travel towards.")]
		[SerializeField]
		private Vector2 _waveDirection = new Vector2(1f, 0f);

		[Tooltip("Distance (m) between wave crests, roughly. Larger = neighbours move more in sync.")]
		[SerializeField]
		[Min(0.1f)]
		private float _wavelength = 6f;

		[Tooltip("How fast the wave travels (m/s).")]
		[SerializeField]
		[Min(0f)]
		private float _speed = 1.5f;

		[Tooltip("Noise variation along the wave crest. 0 = straight crest lines, higher = more irregular.")]
		[SerializeField]
		[Min(0f)]
		private float _crestVariation = 0.15f;

		[Header("Per object")]
		[Tooltip("Max vertical offset (m) above/below the starting height.")]
		[SerializeField]
		[Min(0f)]
		private float _amplitude = 0.15f;

		private float _baseY;

		private void Awake()
		{
			_baseY = base.transform.localPosition.y;
		}

		private void Update()
		{
			Vector3 position = base.transform.position;
			Vector2 lhs = new Vector2(position.x, position.z);
			Vector2 rhs = ((_waveDirection.sqrMagnitude > 0.0001f) ? _waveDirection.normalized : Vector2.right);
			Vector2 rhs2 = new Vector2(0f - rhs.y, rhs.x);
			float x = (Vector2.Dot(lhs, rhs) - _speed * Time.time) / _wavelength;
			float y = Vector2.Dot(lhs, rhs2) * _crestVariation / _wavelength;
			float num = Mathf.PerlinNoise(x, y) * 2f - 1f;
			Vector3 localPosition = base.transform.localPosition;
			localPosition.y = _baseY + num * _amplitude;
			base.transform.localPosition = localPosition;
		}

		private void OnDisable()
		{
			Vector3 localPosition = base.transform.localPosition;
			localPosition.y = _baseY;
			base.transform.localPosition = localPosition;
		}
	}
}

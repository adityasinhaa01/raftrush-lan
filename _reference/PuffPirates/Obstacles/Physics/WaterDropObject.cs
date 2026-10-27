using UnityEngine;

namespace PuffPirates.Obstacles.Physics
{
	[DisallowMultipleComponent]
	public sealed class WaterDropObject : MonoBehaviour
	{
		[Tooltip("Düşüş sırasındaki salınım (tumble) hızı (derece/sn) — SADECE görsel okunabilirlik, zamanlamayı ETKİLEMEZ (bkz. BombProjectile._spinDegPerSec ile aynı felsefe). Prefab'a özeldir: her düşen-nesne varyantı (taş/fıçı/...) kendi doğal dönüş hissini burada ayarlar.")]
		[SerializeField]
		[Min(0f)]
		private float _tumbleSpeedDegPerSec = 120f;

		private Vector3 _startPos;

		private Vector3 _targetPos;

		private float _startTime;

		private float _landTime;

		public ObstaclePool<WaterDropObject> OwnerPool { get; set; }

		public void BeginFall(Vector3 startPos, Vector3 targetPos, float startTime, float landTime)
		{
			_startPos = startPos;
			_targetPos = targetPos;
			_startTime = startTime;
			_landTime = Mathf.Max(landTime, startTime + 0.01f);
			Tick(startTime);
		}

		public void Tick(float time)
		{
			float num = _landTime - _startTime;
			float num2 = ((num > 0.0001f) ? Mathf.Clamp01((time - _startTime) / num) : 1f);
			float t = num2 * num2;
			float y = Mathf.Lerp(_startPos.y, _targetPos.y, t);
			base.transform.position = new Vector3(_targetPos.x, y, _targetPos.z);
			float num3 = _tumbleSpeedDegPerSec * (time - _startTime);
			base.transform.rotation = Quaternion.Euler(num3, num3 * 0.6f, 0f);
		}
	}
}

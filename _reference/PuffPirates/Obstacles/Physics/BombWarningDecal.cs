using UnityEngine;

namespace PuffPirates.Obstacles.Physics
{
	[DisallowMultipleComponent]
	public sealed class BombWarningDecal : MonoBehaviour
	{
		[Tooltip("Alfa/renk değişecek dekal renderer'ı — boşsa çocuklarda otomatik aranır.")]
		[SerializeField]
		private Renderer _decalRenderer;

		[Tooltip("Uyarı başlangıcındaki (henüz belli belirsiz) ölçek çarpanı — 0..1, hedef ölçeğe oran.")]
		[SerializeField]
		[Range(0.05f, 1f)]
		private float _startScale01 = 0.35f;

		[Tooltip("Tam koyulaştığında (iniş anında) ulaşılan en yüksek alfa (0..1).")]
		[SerializeField]
		[Range(0f, 1f)]
		private float _maxAlpha = 0.85f;

		private Material _instanceMat;

		private float _warnStart;

		private float _landTime;

		private float _restoreEnd;

		private float _diameter;

		public ObstaclePool<BombWarningDecal> OwnerPool { get; set; }

		private void Awake()
		{
			if (_decalRenderer == null)
			{
				_decalRenderer = GetComponentInChildren<Renderer>(includeInactive: true);
			}
			if (_decalRenderer != null)
			{
				_instanceMat = _decalRenderer.material;
			}
		}

		private void OnDestroy()
		{
			if (_instanceMat != null)
			{
				Object.Destroy(_instanceMat);
			}
		}

		public void Begin(Vector3 pos, float warnStart, float landTime, float restoreEnd, float radius)
		{
			base.transform.position = pos + Vector3.up * 0.02f;
			_diameter = Mathf.Max(0.01f, radius) * 2f;
			_warnStart = warnStart;
			_landTime = Mathf.Max(landTime, warnStart + 0.0001f);
			_restoreEnd = Mathf.Max(restoreEnd, _landTime + 0.0001f);
			Tick(warnStart);
		}

		public bool Tick(float time)
		{
			float num3;
			float num4;
			if (time <= _landTime)
			{
				float num = Mathf.Max(0.0001f, _landTime - _warnStart);
				float num2 = Mathf.Clamp01((time - _warnStart) / num);
				num3 = num2;
				num4 = Mathf.Lerp(_startScale01, 1f, num2);
			}
			else
			{
				float num5 = Mathf.Max(0.0001f, _restoreEnd - _landTime);
				float num6 = Mathf.Clamp01((time - _landTime) / num5);
				num3 = 1f - num6;
				num4 = Mathf.Lerp(1f, _startScale01, num6);
			}
			float num7 = num4 * _diameter;
			base.transform.localScale = new Vector3(num7, 1f, num7);
			if (_instanceMat != null)
			{
				Color color = _instanceMat.color;
				color.a = num3 * _maxAlpha;
				_instanceMat.color = color;
			}
			return time >= _restoreEnd;
		}
	}
}

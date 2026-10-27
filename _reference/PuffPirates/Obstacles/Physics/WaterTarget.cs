using UnityEngine;

namespace PuffPirates.Obstacles.Physics
{
	[DisallowMultipleComponent]
	public sealed class WaterTarget : MonoBehaviour
	{
		[Tooltip("Alfa/renk değişecek dekal renderer'ı — boşsa çocuklarda otomatik aranır.")]
		[SerializeField]
		private Renderer _decalRenderer;

		[Tooltip("Uyarı başlangıcındaki (henüz belli belirsiz) ölçek çarpanı — 0..1, hedef ölçeğe oran.")]
		[SerializeField]
		[Range(0.05f, 1f)]
		private float _startScale01 = 0.3f;

		[Tooltip("Tam koyulaştığında (düşüş başladığında) ulaşılan en yüksek alfa (0..1).")]
		[SerializeField]
		[Range(0f, 1f)]
		private float _maxAlpha = 0.9f;

		private Material _instanceMat;

		private float _warnStart;

		private float _holdEnd;

		private float _diameter;

		public ObstaclePool<WaterTarget> OwnerPool { get; set; }

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

		public void Begin(Vector3 pos, float warnStart, float holdEnd, float radius)
		{
			base.transform.position = pos + Vector3.up * 0.02f;
			_diameter = Mathf.Max(0.01f, radius) * 2f;
			_warnStart = warnStart;
			_holdEnd = Mathf.Max(holdEnd, warnStart + 0.0001f);
			Tick(warnStart);
		}

		public void Tick(float time)
		{
			float num = ((time <= _holdEnd) ? Mathf.Clamp01((time - _warnStart) / Mathf.Max(0.0001f, _holdEnd - _warnStart)) : 1f);
			float num2 = Mathf.Lerp(_startScale01, 1f, num) * _diameter;
			base.transform.localScale = new Vector3(num2, 1f, num2);
			if (_instanceMat != null)
			{
				Color color = _instanceMat.color;
				color.a = num * _maxAlpha;
				_instanceMat.color = color;
			}
		}
	}
}

using PuffPirates.Boats;
using UnityEngine;

namespace PuffPirates.Obstacles.Physics
{
	[RequireComponent(typeof(Rigidbody))]
	[DisallowMultipleComponent]
	public sealed class Rock : MonoBehaviour
	{
		[Header("Bot İtme Ayarı")]
		[Tooltip("Bota aktarılan itkinin, kayanın momentumuna (kütle × hız) uygulanan çarpanı — büyük/hızlı kaya daha güçlü iter.")]
		[SerializeField]
		[Min(0f)]
		private float _pushForceMultiplier = 1f;

		[Tooltip("İtkiye eklenen sabit dikey (yukarı) bileşen — botu hafifçe kaldırıp çarpışmayı daha 'fiziksel' hissettirir.")]
		[SerializeField]
		[Min(0f)]
		private float _pushUpwardBoost = 1.5f;

		[Header("Çarpışma VFX (kozmetik)")]
		[Tooltip("Bota çarpınca temas noktasında oynatılır (Assets/Break_Items_Toon_VFX/Prefabs/VFX_Toxic_Hit). Kozmetik + client-yerel — 'World VFX MAY play locally' (obstacle-arch-brief) ile ayni desen, her makine kendi kayasının kendi çarpışmasında tetikler, host-gate yok.")]
		[SerializeField]
		private GameObject _hitVfxPrefab;

		[SerializeField]
		[Min(0.1f)]
		private float _hitVfxLifetime = 2f;

		private Rigidbody _rigidbody;

		public Rigidbody Body => _rigidbody;

		private void Awake()
		{
			_rigidbody = GetComponent<Rigidbody>();
		}

		public void Configure(float uniformScale, float mass, Vector3 launchImpulse)
		{
			base.transform.localScale = Vector3.one * Mathf.Max(0.05f, uniformScale);
			if (!(_rigidbody == null))
			{
				_rigidbody.mass = Mathf.Max(0.01f, mass);
				_rigidbody.linearVelocity = Vector3.zero;
				_rigidbody.angularVelocity = Vector3.zero;
				_rigidbody.WakeUp();
				_rigidbody.AddForce(launchImpulse, ForceMode.Impulse);
			}
		}

		private void OnCollisionEnter(Collision collision)
		{
			if (_rigidbody == null || collision == null || collision.collider == null)
			{
				return;
			}
			NetworkBoat componentInParent = collision.collider.GetComponentInParent<NetworkBoat>();
			if (componentInParent == null)
			{
				return;
			}
			PlayHitVfx(collision.GetContact(0).point);
			if (componentInParent.IsServerInitialized)
			{
				Rigidbody rigidbody = componentInParent.Rigidbody;
				if (!(rigidbody == null))
				{
					Vector3 obj = ((_rigidbody.linearVelocity.sqrMagnitude > 0.05f) ? _rigidbody.linearVelocity.normalized : (-collision.GetContact(0).normal));
					float num = _rigidbody.mass * _rigidbody.linearVelocity.magnitude;
					Vector3 force = obj * (num * _pushForceMultiplier) + Vector3.up * _pushUpwardBoost;
					rigidbody.AddForce(force, ForceMode.Impulse);
				}
			}
		}

		private void PlayHitVfx(Vector3 point)
		{
			if (!(_hitVfxPrefab == null))
			{
				Object.Destroy(Object.Instantiate(_hitVfxPrefab, point, Quaternion.identity), _hitVfxLifetime);
			}
		}
	}
}

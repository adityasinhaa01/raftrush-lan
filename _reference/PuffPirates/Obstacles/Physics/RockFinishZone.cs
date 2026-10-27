using UnityEngine;

namespace PuffPirates.Obstacles.Physics
{
	[RequireComponent(typeof(Collider))]
	[DisallowMultipleComponent]
	public sealed class RockFinishZone : MonoBehaviour
	{
		private RockRamp _ramp;

		private void Awake()
		{
			_ramp = GetComponentInParent<RockRamp>();
			if (_ramp == null)
			{
				Debug.LogWarning("[RockFinishZone] '" + base.name + "' bir RockRamp'in altında değil — bitiş tetikleyicisi devre dışı.");
			}
			Collider component = GetComponent<Collider>();
			if (component != null && !component.isTrigger)
			{
				Debug.LogWarning("[RockFinishZone] '" + base.name + "' collider'ı isTrigger=false — tetikleyici çalışmaz, Inspector'da işaretleyin.");
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			if (!(_ramp == null) && !(other == null))
			{
				Rock componentInParent = other.GetComponentInParent<Rock>();
				if (!(componentInParent == null))
				{
					_ramp.OnRockReachedFinish(componentInParent);
				}
			}
		}

		private void OnDrawGizmosSelected()
		{
			Collider component = GetComponent<Collider>();
			Color color = Gizmos.color;
			Gizmos.color = new Color(1f, 0.9f, 0.1f, 0.55f);
			if (component is BoxCollider boxCollider)
			{
				Gizmos.matrix = base.transform.localToWorldMatrix;
				Gizmos.DrawCube(boxCollider.center, boxCollider.size);
				Gizmos.matrix = Matrix4x4.identity;
			}
			else
			{
				Gizmos.DrawWireSphere(base.transform.position, 1f);
			}
			Gizmos.color = color;
		}
	}
}

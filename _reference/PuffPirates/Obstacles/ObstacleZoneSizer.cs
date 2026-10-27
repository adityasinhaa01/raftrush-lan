using UnityEngine;

namespace PuffPirates.Obstacles
{
	[ExecuteAlways]
	[DisallowMultipleComponent]
	public sealed class ObstacleZoneSizer : MonoBehaviour
	{
		[Tooltip("Daire (SphereCollider) için yarıçap; dikdörtgen (BoxCollider) için X/Z boyutu.")]
		[SerializeField]
		[Min(0.1f)]
		private Vector2 _size = new Vector2(8f, 8f);

		[Tooltip("Ölçeklenecek görsel/VFX kökü. Boşsa yalnız collider ölçeklenir.")]
		[SerializeField]
		private Transform _visualRoot;

		public Vector2 Size
		{
			get
			{
				return _size;
			}
			set
			{
				_size = value;
				Apply();
			}
		}

		private void OnEnable()
		{
			Apply();
		}

		private void OnValidate()
		{
			Apply();
		}

		private void Apply()
		{
			BoxCollider component2;
			if (TryGetComponent<SphereCollider>(out var component))
			{
				component.radius = _size.x;
				ApplyVisualScale(_size.x * 2f, _size.x * 2f);
			}
			else if (TryGetComponent<BoxCollider>(out component2))
			{
				Vector3 size = component2.size;
				size.x = _size.x;
				size.z = _size.y;
				component2.size = size;
				ApplyVisualScale(_size.x, _size.y);
			}
		}

		private void ApplyVisualScale(float x, float z)
		{
			if (!(_visualRoot == null))
			{
				Vector3 localScale = _visualRoot.localScale;
				localScale.x = x;
				localScale.z = z;
				_visualRoot.localScale = localScale;
			}
		}
	}
}

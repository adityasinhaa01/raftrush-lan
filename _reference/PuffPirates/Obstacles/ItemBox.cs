using PuffPirates.Boats;
using PuffPirates.Networking;
using UnityEngine;

namespace PuffPirates.Obstacles
{
	public class ItemBox : FieldObstacle
	{
		[SerializeField]
		private ItemType _itemType;

		[SerializeField]
		[Min(0.1f)]
		private float _boostSeconds = 2.5f;

		[SerializeField]
		[Min(0.1f)]
		private float _respawnSeconds = 6f;

		[Tooltip("Gizlenecek görsel kök (boşsa bu GameObject'in tüm alt Renderer'ları kullanılır).")]
		[SerializeField]
		private GameObject _visualRoot;

		[SerializeField]
		private int _boxId = -1;

		private Collider _collider;

		private bool _visibleCached = true;

		public void Configure(int boxId)
		{
			_boxId = boxId;
		}

		private void Awake()
		{
			_collider = GetComponent<Collider>();
		}

		private void Update()
		{
			bool flag = IsAvailable();
			if (flag == _visibleCached)
			{
				return;
			}
			_visibleCached = flag;
			if (_visualRoot != null)
			{
				_visualRoot.SetActive(flag);
			}
			else
			{
				Renderer[] componentsInChildren = GetComponentsInChildren<Renderer>(includeInactive: true);
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					componentsInChildren[i].enabled = flag;
				}
			}
			if (_collider != null)
			{
				_collider.enabled = flag;
			}
		}

		private bool IsAvailable()
		{
			if (_boxId < 0)
			{
				return true;
			}
			RaceState instance = RaceState.Instance;
			if (!(instance == null))
			{
				return instance.IsItemBoxAvailable(_boxId);
			}
			return true;
		}

		private void OnTriggerEnter(Collider other)
		{
			NetworkBoat networkBoat = FieldObstacle.HostBoat(other);
			if (!(networkBoat == null) && IsAvailable() && !(FieldObstacle.BodyOf(networkBoat) == null))
			{
				if (_itemType == ItemType.SpeedBoost)
				{
					networkBoat.ApplySpeedBoost(_boostSeconds);
				}
				if (_boxId >= 0)
				{
					RaceState.Instance?.PickUpItemBox(_boxId, _respawnSeconds);
				}
			}
		}
	}
}

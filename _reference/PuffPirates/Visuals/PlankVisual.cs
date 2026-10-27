using PuffPirates.Boats;
using PuffPirates.Obstacles;
using UnityEngine;

namespace PuffPirates.Visuals
{
	public class PlankVisual : MonoBehaviour
	{
		[SerializeField]
		private NetworkBoat _boat;

		[Tooltip("Kalasın bota göre yerel konumu (arka güverte).")]
		[SerializeField]
		private Vector3 _localOffset = new Vector3(0f, 0.1f, -1.4f);

		[SerializeField]
		private Vector3 _plankSize = new Vector3(0.5f, 0.15f, 1.6f);

		private GameObject _plank;

		private void Awake()
		{
			if (_boat == null)
			{
				_boat = GetComponentInParent<NetworkBoat>();
			}
		}

		private void Update()
		{
			if (!(_boat == null))
			{
				bool flag = _boat.HasStatus(BoatStatusFlags.Plank);
				if (flag && _plank == null)
				{
					CreatePlank();
				}
				else if (!flag && _plank != null)
				{
					Object.Destroy(_plank);
					_plank = null;
				}
			}
		}

		private void CreatePlank()
		{
			_plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
			_plank.name = "~PlankVisual";
			Collider component = _plank.GetComponent<Collider>();
			if (component != null)
			{
				Object.Destroy(component);
			}
			_plank.transform.SetParent(base.transform, worldPositionStays: false);
			_plank.transform.localPosition = _localOffset;
			_plank.transform.localRotation = Quaternion.identity;
			_plank.transform.localScale = _plankSize;
			Renderer component2 = _plank.GetComponent<Renderer>();
			if (component2 != null)
			{
				RendererColorUtility.SetColor(component2, PickupCatalog.Get(PickupEffectType.Plank).Color);
			}
		}

		private void OnDisable()
		{
			if (_plank != null)
			{
				Object.Destroy(_plank);
				_plank = null;
			}
		}
	}
}

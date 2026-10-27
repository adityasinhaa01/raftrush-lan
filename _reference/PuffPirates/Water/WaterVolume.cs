using UnityEngine;

namespace PuffPirates.Water
{
	[RequireComponent(typeof(Collider))]
	[DisallowMultipleComponent]
	public sealed class WaterVolume : MonoBehaviour
	{
		[Tooltip("Bağlı kanal. Boşsa parent'tan (GetComponentInParent) bulunur.")]
		[SerializeField]
		private WaterChannel _channel;

		private void Reset()
		{
			_channel = GetComponentInParent<WaterChannel>();
			Collider component = GetComponent<Collider>();
			if (component != null)
			{
				component.isTrigger = true;
			}
		}

		private void OnEnable()
		{
			if (_channel == null)
			{
				_channel = GetComponentInParent<WaterChannel>();
			}
			Collider component = GetComponent<Collider>();
			if (component != null)
			{
				component.isTrigger = true;
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			if (!(_channel == null))
			{
				BuoyantBody componentInParent = other.GetComponentInParent<BuoyantBody>();
				if (componentInParent != null)
				{
					componentInParent.EnterChannel(_channel);
				}
			}
		}

		private void OnTriggerExit(Collider other)
		{
			if (!(_channel == null))
			{
				BuoyantBody componentInParent = other.GetComponentInParent<BuoyantBody>();
				if (componentInParent != null)
				{
					componentInParent.ExitChannel(_channel);
				}
			}
		}
	}
}

using FishNet.Object;
using UnityEngine;

namespace PuffPirates.Obstacles
{
	public sealed class ElevatorSpawner : NetworkPropSpawner
	{
		[SerializeField]
		private Transform _end;

		protected override void OnSpawned(NetworkObject spawned)
		{
			if (_end != null && spawned.TryGetComponent<Elevator>(out var component))
			{
				component.SetEndPoint(_end.position);
			}
		}

		private void OnDrawGizmosSelected()
		{
			if (!(_end == null))
			{
				Gizmos.color = Color.yellow;
				Gizmos.DrawLine(base.transform.position, _end.position);
				Gizmos.DrawWireCube(_end.position, new Vector3(6f, 0.4f, 6f));
			}
		}
	}
}

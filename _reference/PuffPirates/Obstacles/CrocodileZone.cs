using FishNet.Object;
using PuffPirates.Boats;
using UnityEngine;

namespace PuffPirates.Obstacles
{
	public class CrocodileZone : MonoBehaviour
	{
		[SerializeField]
		private NetworkObject _crocodilePrefab;

		[SerializeField]
		private Collider _entry;

		[SerializeField]
		private Collider _exit;

		[SerializeField]
		private Transform _spawnPoint;

		private NetworkObject _croc;

		private bool _used;

		private static bool Inside(Collider c, Vector3 p)
		{
			return (c.ClosestPoint(p) - p).sqrMagnitude < 1E-06f;
		}

		private void Update()
		{
			if (_used && _croc == null)
			{
				return;
			}
			foreach (NetworkBoat item in NetworkBoat.All)
			{
				if (item == null || !item.IsServerInitialized)
				{
					continue;
				}
				Vector3 position = item.Rigidbody.position;
				if (!_used && Inside(_entry, position))
				{
					_used = true;
					_croc = Object.Instantiate(_crocodilePrefab, _spawnPoint.position, _spawnPoint.rotation);
					item.NetworkManager.ServerManager.Spawn(_croc);
					break;
				}
				if (_croc != null && Inside(_exit, position))
				{
					if (_croc.IsSpawned)
					{
						item.NetworkManager.ServerManager.Despawn(_croc);
					}
					_croc = null;
					break;
				}
			}
		}
	}
}

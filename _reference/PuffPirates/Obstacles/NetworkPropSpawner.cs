using System.Collections.Generic;
using FishNet.Managing;
using FishNet.Object;
using UnityEngine;

namespace PuffPirates.Obstacles
{
	public class NetworkPropSpawner : MonoBehaviour
	{
		[SerializeField]
		private NetworkObject _prefab;

		private NetworkObject _spawned;

		private float _nextTryAt;

		private void Update()
		{
			if (!Application.isPlaying || _prefab == null || Time.unscaledTime < _nextTryAt)
			{
				return;
			}
			_nextTryAt = Time.unscaledTime + 0.25f;
			IReadOnlyList<NetworkManager> instances = NetworkManager.Instances;
			NetworkManager networkManager = ((instances.Count > 0) ? instances[0] : null);
			if (networkManager == null || !networkManager.IsServerStarted)
			{
				_spawned = null;
			}
			else if (!(_spawned != null) || !_spawned.IsSpawned)
			{
				if (_spawned != null)
				{
					Object.Destroy(_spawned.gameObject);
				}
				_spawned = Object.Instantiate(_prefab, base.transform.position, base.transform.rotation);
				networkManager.ServerManager.Spawn(_spawned, null, base.gameObject.scene);
				OnSpawned(_spawned);
			}
		}

		protected virtual void OnSpawned(NetworkObject spawned)
		{
		}

		private void OnDisable()
		{
			Despawn();
		}

		private void OnDestroy()
		{
			Despawn();
		}

		private void Despawn()
		{
			if (Application.isPlaying && _spawned != null && _spawned.IsSpawned && _spawned.IsServerInitialized)
			{
				_spawned.Despawn();
			}
			_spawned = null;
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);
			Gizmos.DrawWireSphere(base.transform.position, 0.6f);
			Gizmos.DrawLine(base.transform.position, base.transform.position + base.transform.forward * 2f);
		}
	}
}

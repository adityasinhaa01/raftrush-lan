using FishNet.Managing;
using FishNet.Object;
using FishNet.Serializing;
using FishNet.Transporting;
using PuffPirates.Boats;
using UnityEngine;
using UnityEngine.AI;

namespace PuffPirates.Obstacles
{
	[RequireComponent(typeof(NavMeshAgent))]
	public class Crocodile : NetworkBehaviour
	{
		[SerializeField]
		private float _speed = 5f;

		[SerializeField]
		private float _detectRange = 14f;

		[SerializeField]
		private float _biteRange = 3f;

		[SerializeField]
		private float _biteCooldown = 1.5f;

		[SerializeField]
		private float _respawnInvuln = 2f;

		private Animator _anim;

		private NavMeshAgent _agent;

		private float _biteReadyAt;

		private float _nextRepath;

		private bool NetworkInitialize___EarlyPuffPirates_002EObstacles_002ECrocodileAssembly_002DCSharp_002Edll_Excuted;

		private bool NetworkInitialize___LatePuffPirates_002EObstacles_002ECrocodileAssembly_002DCSharp_002Edll_Excuted;

		public virtual void Awake()
		{
			NetworkInitialize___Early();
			Awake_UserLogic_PuffPirates_002EObstacles_002ECrocodile_Assembly_002DCSharp_002Edll();
			NetworkInitialize___Late();
		}

		public override void OnStartServer()
		{
			base.OnStartServer();
			if (NavMesh.SamplePosition(base.transform.position, out var hit, 10f, -1))
			{
				base.transform.position = hit.position;
			}
			else
			{
				Debug.LogWarning("[Crocodile] " + base.name + ": yakında NavMesh yok — level'a NavMeshSurface ekleyip Bake edin.", this);
			}
			_agent.enabled = true;
			_agent.speed = _speed;
		}

		private void Update()
		{
			if (!base.IsServerInitialized || !_agent.isOnNavMesh)
			{
				return;
			}
			NetworkBoat networkBoat = null;
			float num = _detectRange * _detectRange;
			foreach (NetworkBoat item in NetworkBoat.All)
			{
				if (!(item == null) && item.IsServerInitialized && !item.IsInvulnerable)
				{
					Vector3 vector = item.Rigidbody.position - base.transform.position;
					vector.y = 0f;
					if (vector.sqrMagnitude < num)
					{
						num = vector.sqrMagnitude;
						networkBoat = item;
					}
				}
			}
			if (networkBoat == null)
			{
				_agent.ResetPath();
				return;
			}
			Vector3 position = networkBoat.Rigidbody.position;
			Vector3 forward = position - base.transform.position;
			forward.y = 0f;
			if (forward.sqrMagnitude > _biteRange * _biteRange)
			{
				if (Time.time >= _nextRepath)
				{
					_nextRepath = Time.time + 0.2f;
					_agent.SetDestination(position);
				}
				return;
			}
			_agent.ResetPath();
			if (forward.sqrMagnitude > 0.0001f)
			{
				base.transform.rotation = Quaternion.Slerp(base.transform.rotation, Quaternion.LookRotation(forward), 10f * Time.deltaTime);
			}
			if (!(Time.time < _biteReadyAt))
			{
				_biteReadyAt = Time.time + _biteCooldown;
				RpcBite();
				Shark.Kill(networkBoat, DeathCause.Crocodile, _respawnInvuln);
			}
		}

		[ObserversRpc]
		private void RpcBite()
		{
			RpcWriter___Observers_RpcBite___2166136261();
		}

		public override void NetworkInitialize___Early()
		{
			if (!NetworkInitialize___EarlyPuffPirates_002EObstacles_002ECrocodileAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___EarlyPuffPirates_002EObstacles_002ECrocodileAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Early();
				RegisterObserversRpc(0u, RpcReader___Observers_RpcBite___2166136261);
			}
		}

		public override void NetworkInitialize___Late()
		{
			if (!NetworkInitialize___LatePuffPirates_002EObstacles_002ECrocodileAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___LatePuffPirates_002EObstacles_002ECrocodileAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Late();
			}
		}

		public override void NetworkInitializeIfDisabled()
		{
			NetworkInitialize___Early();
			NetworkInitialize___Late();
		}

		private void RpcWriter___Observers_RpcBite___2166136261()
		{
			if (!base.IsServerInitialized)
			{
				NetworkManager networkManager = base.NetworkManager;
				networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
				return;
			}
			Channel channel = Channel.Reliable;
			PooledWriter pooledWriter = WriterPool.Retrieve();
			SendObserversRpc(0u, pooledWriter, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			pooledWriter.Store();
		}

		private void RpcLogic___RpcBite___2166136261()
		{
			if (_anim != null)
			{
				_anim.SetTrigger("Bite");
			}
		}

		private void RpcReader___Observers_RpcBite___2166136261(PooledReader PooledReader0, Channel channel)
		{
			if (base.IsClientInitialized)
			{
				RpcLogic___RpcBite___2166136261();
			}
		}

		private void Awake_UserLogic_PuffPirates_002EObstacles_002ECrocodile_Assembly_002DCSharp_002Edll()
		{
			_anim = GetComponentInChildren<Animator>();
			_agent = GetComponent<NavMeshAgent>();
			_agent.enabled = false;
		}
	}
}

using FishNet.Component.Transforming;
using FishNet.Object;
using PuffPirates.Boats;
using PuffPirates.Track;
using UnityEngine;
using UnityEngine.AI;

namespace PuffPirates.Obstacles
{
	[RequireComponent(typeof(NavMeshAgent), typeof(Rigidbody))]
	public class Shark : NetworkBehaviour
	{
		[Header("Hız")]
		[SerializeField]
		private float _patrolSpeed = 3.5f;

		[SerializeField]
		private float _chaseSpeed = 6f;

		[Tooltip("Dar/uzun koridorda hız çarpanı.")]
		[SerializeField]
		private float _narrowSpeedMul = 1.1f;

		[Tooltip("Koridor sayılması için iki duvar arası yarı-genişlik eşiği (m).")]
		[SerializeField]
		private float _narrowHalfWidth = 3f;

		[Tooltip("Dar yerde bu kadar sn kalınca 'uzun koridor' sayılır → hız artar.")]
		[SerializeField]
		private float _narrowSeconds = 1.5f;

		[Header("Görüş (üçgen: tepe köpek balığında)")]
		[SerializeField]
		private float _viewLength = 14f;

		[Tooltip("Üçgenin uzak ucundaki yarı genişlik (m).")]
		[SerializeField]
		private float _viewHalfWidth = 6f;

		[Tooltip("Hedef üçgenden çıkınca kovalamayı bu kadar sn sonra bırak.")]
		[SerializeField]
		private float _giveUpSeconds = 3f;

		[Header("Isırma")]
		[SerializeField]
		private float _biteRange = 2.2f;

		[SerializeField]
		private float _biteCooldown = 3f;

		[SerializeField]
		private float _respawnInvuln = 2f;

		[Header("Gezinme")]
		[SerializeField]
		private float _wanderRadius = 25f;

		[SerializeField]
		private Vector2 _wanderPause = new Vector2(0.5f, 3f);

		[Header("Akıntı / düşme")]
		[Tooltip("GustZone kuvveti (N) → hız (m/s) çevrimi. 950 N × 0.005 ≈ 4.75 m/s.")]
		[SerializeField]
		private float _currentPush = 0.005f;

		[Tooltip("Düşünce ışınlanılan nokta botlardan en az bu kadar uzak olsun (m).")]
		[SerializeField]
		private float _minPlayerDistance = 20f;

		private NavMeshAgent _agent;

		private NetworkBoat _target;

		private Vector3 _lastGood;

		private Vector3 _flow;

		private float _flowUntil;

		private float _lostTimer;

		private float _narrowTime;

		private float _nextCheck;

		private float _nextRepath;

		private float _nextWander;

		private float _biteReadyAt;

		private bool NetworkInitialize___EarlyPuffPirates_002EObstacles_002ESharkAssembly_002DCSharp_002Edll_Excuted;

		private bool NetworkInitialize___LatePuffPirates_002EObstacles_002ESharkAssembly_002DCSharp_002Edll_Excuted;

		public virtual void Awake()
		{
			NetworkInitialize___Early();
			Awake_UserLogic_PuffPirates_002EObstacles_002EShark_Assembly_002DCSharp_002Edll();
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
				Debug.LogWarning("[Shark] " + base.name + ": yakında NavMesh yok — level'a NavMeshSurface ekleyip Bake edin.", this);
			}
			_agent.enabled = true;
			_lastGood = base.transform.position;
		}

		public void AddCurrent(Vector3 force)
		{
			if (base.IsServerInitialized)
			{
				_flow = new Vector3(force.x, 0f, force.z) * _currentPush;
				_flowUntil = Time.time + 0.1f;
			}
		}

		private void Update()
		{
			if (!base.IsServerInitialized || !_agent.enabled)
			{
				return;
			}
			if (!_agent.isOnNavMesh && !_agent.isOnOffMeshLink)
			{
				Recover();
				return;
			}
			if (!_agent.isOnOffMeshLink)
			{
				_lastGood = base.transform.position;
			}
			if (Time.time < _flowUntil)
			{
				_agent.Move(_flow * Time.deltaTime);
			}
			if (Time.time >= _nextCheck)
			{
				_nextCheck = Time.time + 0.25f;
				UpdateNarrow(0.25f);
			}
			_agent.speed = ((_target != null) ? _chaseSpeed : _patrolSpeed) * ((_narrowTime >= _narrowSeconds) ? _narrowSpeedMul : 1f);
			if (_target != null)
			{
				Chase();
				return;
			}
			Look();
			Wander();
		}

		private bool InView(Vector3 p)
		{
			Vector3 lhs = p - base.transform.position;
			float num = Vector3.Dot(lhs, base.transform.forward);
			if (num <= 0f || num > _viewLength)
			{
				return false;
			}
			return Mathf.Abs(Vector3.Dot(lhs, base.transform.right)) <= _viewHalfWidth * num / _viewLength;
		}

		private static bool Targetable(NetworkBoat b)
		{
			if (b != null && b.IsServerInitialized)
			{
				return !b.IsInvulnerable;
			}
			return false;
		}

		private void Look()
		{
			if (Time.time < _biteReadyAt)
			{
				return;
			}
			float num = float.MaxValue;
			foreach (NetworkBoat item in NetworkBoat.All)
			{
				if (Targetable(item))
				{
					Vector3 position = item.Rigidbody.position;
					float sqrMagnitude = (position - base.transform.position).sqrMagnitude;
					if (sqrMagnitude < num && InView(position))
					{
						num = sqrMagnitude;
						_target = item;
						_lostTimer = 0f;
					}
				}
			}
		}

		private void Chase()
		{
			if (!Targetable(_target))
			{
				_target = null;
				return;
			}
			Vector3 position = _target.Rigidbody.position;
			_lostTimer = (InView(position) ? 0f : (_lostTimer + Time.deltaTime));
			if (_lostTimer > _giveUpSeconds)
			{
				_target = null;
			}
			else if ((position - base.transform.position).sqrMagnitude <= _biteRange * _biteRange)
			{
				Bite(_target);
			}
			else if (Time.time >= _nextRepath)
			{
				_nextRepath = Time.time + 0.2f;
				_agent.SetDestination(position);
			}
		}

		private void Bite(NetworkBoat boat)
		{
			_target = null;
			_biteReadyAt = Time.time + _biteCooldown;
			_agent.ResetPath();
			Kill(boat, DeathCause.Shark, _respawnInvuln);
		}

		public static void Kill(NetworkBoat boat, DeathCause cause, float invuln)
		{
			RaceTrack instance = RaceTrack.Instance;
			if (!(instance == null))
			{
				int currentCheckpointId = boat.CurrentCheckpointId;
				if (!instance.TryGetSafeRespawnPose(currentCheckpointId, 1.5f, out var position, out var rotation))
				{
					Transform checkpoint = instance.GetCheckpoint(currentCheckpointId);
					position = ((checkpoint != null) ? (checkpoint.position + Vector3.up * 1.5f) : new Vector3(0f, 2f, 0f));
					rotation = ((checkpoint != null) ? checkpoint.rotation : Quaternion.identity);
				}
				boat.RespawnAt(position, rotation, invuln, cause);
			}
		}

		private void Wander()
		{
			if (!_agent.pathPending && !(Time.time < _nextWander) && (!_agent.hasPath || !(_agent.remainingDistance > 0.5f)))
			{
				_nextWander = Time.time + Random.Range(_wanderPause.x, _wanderPause.y);
				if (NavMesh.SamplePosition(base.transform.position + Random.insideUnitSphere * _wanderRadius, out var hit, 5f, -1))
				{
					_agent.SetDestination(hit.position);
				}
			}
		}

		private void UpdateNarrow(float dt)
		{
			bool flag = false;
			Vector3 position = base.transform.position;
			if (NavMesh.FindClosestEdge(position, out var hit, -1) && hit.distance < _narrowHalfWidth)
			{
				Vector3 vector = position - hit.position;
				vector.y = 0f;
				if (vector.sqrMagnitude > 0.0001f)
				{
					flag = NavMesh.Raycast(position, position + vector.normalized * (2f * _narrowHalfWidth), out var _, -1);
				}
			}
			_narrowTime = (flag ? (_narrowTime + dt) : 0f);
		}

		private void Recover()
		{
			_target = null;
			Vector3 newPosition = PickRecoverPoint(_lastGood);
			_agent.Warp(newPosition);
			NetworkTransform component = GetComponent<NetworkTransform>();
			if (component != null)
			{
				component.Teleport();
			}
		}

		private Vector3 PickRecoverPoint(Vector3 origin)
		{
			for (int i = 0; i < 16; i++)
			{
				Vector3 sourcePosition = origin;
				if (i > 0)
				{
					Vector2 vector = Random.insideUnitCircle.normalized * Random.Range(_minPlayerDistance, _minPlayerDistance + 20f);
					sourcePosition += new Vector3(vector.x, 0f, vector.y);
				}
				if (NavMesh.SamplePosition(sourcePosition, out var hit, 6f, -1))
				{
					Vector3 vector2 = PathCenter(hit.position);
					if (FarFromBoats(vector2))
					{
						return vector2;
					}
				}
			}
			return PathCenter(origin);
		}

		private bool FarFromBoats(Vector3 p)
		{
			float num = _minPlayerDistance * _minPlayerDistance;
			foreach (NetworkBoat item in NetworkBoat.All)
			{
				if (item != null && (item.transform.position - p).sqrMagnitude < num)
				{
					return false;
				}
			}
			return true;
		}

		private static Vector3 PathCenter(Vector3 p)
		{
			if (!NavMesh.FindClosestEdge(p, out var hit, -1))
			{
				return p;
			}
			Vector3 vector = p - hit.position;
			vector.y = 0f;
			if (vector.sqrMagnitude < 0.0001f)
			{
				vector = -hit.normal;
			}
			vector.Normalize();
			if (!NavMesh.Raycast(p, p + vector * 60f, out var hit2, -1))
			{
				return p;
			}
			if (!NavMesh.SamplePosition((hit.position + hit2.position) * 0.5f, out var hit3, 3f, -1))
			{
				return p;
			}
			return hit3.position;
		}

		public override void NetworkInitialize___Early()
		{
			if (!NetworkInitialize___EarlyPuffPirates_002EObstacles_002ESharkAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___EarlyPuffPirates_002EObstacles_002ESharkAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Early();
			}
		}

		public override void NetworkInitialize___Late()
		{
			if (!NetworkInitialize___LatePuffPirates_002EObstacles_002ESharkAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___LatePuffPirates_002EObstacles_002ESharkAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Late();
			}
		}

		public override void NetworkInitializeIfDisabled()
		{
			NetworkInitialize___Early();
			NetworkInitialize___Late();
		}

		private void Awake_UserLogic_PuffPirates_002EObstacles_002EShark_Assembly_002DCSharp_002Edll()
		{
			_agent = GetComponent<NavMeshAgent>();
			_agent.enabled = false;
			Rigidbody component = GetComponent<Rigidbody>();
			component.isKinematic = true;
			component.useGravity = false;
		}
	}
}

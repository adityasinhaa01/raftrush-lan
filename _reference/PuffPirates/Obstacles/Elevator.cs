using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using PuffPirates.Boats;
using PuffPirates.Obstacles.Physics;
using PuffPirates.Track;
using UnityEngine;

namespace PuffPirates.Obstacles
{
	[RequireComponent(typeof(Rigidbody))]
	public class Elevator : NetworkBehaviour
	{
		[SerializeField]
		private Collider _board;

		[SerializeField]
		private float _speed = 2.5f;

		[Header("Kapı")]
		[Tooltip("Asansörün içindeki kapı (placeholder; kendi kapı prefab'ınızı bunun yerine/altına koyun).")]
		[SerializeField]
		private Transform _door;

		[Tooltip("Kapı açılınca yerel kayma (m).")]
		[SerializeField]
		private Vector3 _doorOpenOffset = new Vector3(0f, 4f, 0f);

		[SerializeField]
		private float _doorSpeed = 4f;

		[Header("Engel temizliği")]
		[SerializeField]
		private float _fadeSeconds = 0.6f;

		public readonly SyncVar<bool> _doorOpen = new SyncVar<bool>(new SyncTypeSettings(0f));

		private static readonly Type[] ObstacleTypes = new Type[6]
		{
			typeof(BarrierKill),
			typeof(FieldObstacle),
			typeof(MovingObstacle),
			typeof(PhysicsObstacleBase),
			typeof(AnimatedRock),
			typeof(JointRotor)
		};

		private Rigidbody _rb;

		private Vector3 _end;

		private bool _hasEnd;

		private bool _started;

		private bool _arrived;

		private Vector3 _doorClosedPos;

		private readonly HashSet<GameObject> _clearing = new HashSet<GameObject>();

		private bool NetworkInitialize___EarlyPuffPirates_002EObstacles_002EElevatorAssembly_002DCSharp_002Edll_Excuted;

		private bool NetworkInitialize___LatePuffPirates_002EObstacles_002EElevatorAssembly_002DCSharp_002Edll_Excuted;

		public virtual void Awake()
		{
			NetworkInitialize___Early();
			Awake_UserLogic_PuffPirates_002EObstacles_002EElevator_Assembly_002DCSharp_002Edll();
			NetworkInitialize___Late();
		}

		public void SetEndPoint(Vector3 worldEnd)
		{
			_end = worldEnd;
			_hasEnd = true;
		}

		private void FixedUpdate()
		{
			if (!base.IsServerInitialized || !_hasEnd || _arrived)
			{
				return;
			}
			if (!_started)
			{
				foreach (NetworkBoat item in NetworkBoat.All)
				{
					if (item != null && (_board.ClosestPoint(item.transform.position) - item.transform.position).sqrMagnitude < 1E-06f)
					{
						_started = true;
						break;
					}
				}
				return;
			}
			Vector3 vector = Vector3.MoveTowards(_rb.position, _end, _speed * Time.fixedDeltaTime);
			_rb.MovePosition(vector);
			if ((vector - _end).sqrMagnitude < 0.0001f)
			{
				_arrived = true;
				_doorOpen.Value = true;
			}
		}

		private void Update()
		{
			if (!(_door == null))
			{
				Vector3 target = _doorClosedPos + (_doorOpen.Value ? _doorOpenOffset : Vector3.zero);
				_door.localPosition = Vector3.MoveTowards(_door.localPosition, target, _doorSpeed * Time.deltaTime);
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			if (other.transform.IsChildOf(base.transform))
			{
				return;
			}
			Type[] obstacleTypes = ObstacleTypes;
			foreach (Type t in obstacleTypes)
			{
				Component componentInParent = other.GetComponentInParent(t);
				if (!(componentInParent == null))
				{
					if (_clearing.Add(componentInParent.gameObject))
					{
						StartCoroutine(Clear(componentInParent.gameObject));
					}
					break;
				}
			}
		}

		private IEnumerator Clear(GameObject go)
		{
			Collider[] componentsInChildren = go.GetComponentsInChildren<Collider>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].enabled = false;
			}
			List<Material> mats = new List<Material>();
			Renderer[] componentsInChildren2 = go.GetComponentsInChildren<Renderer>();
			foreach (Renderer renderer in componentsInChildren2)
			{
				if (!(renderer is ParticleSystemRenderer))
				{
					mats.AddRange(renderer.materials);
				}
			}
			foreach (Material item in mats)
			{
				MakeTransparent(item);
			}
			for (float t = 0f; t < _fadeSeconds; t += Time.deltaTime)
			{
				SetAlpha(mats, 1f - t / _fadeSeconds);
				yield return null;
			}
			_clearing.Remove(go);
			if (go == null)
			{
				yield break;
			}
			NetworkObject componentInParent = go.GetComponentInParent<NetworkObject>();
			if (componentInParent != null)
			{
				if (base.IsServerInitialized && componentInParent.IsSpawned)
				{
					componentInParent.Despawn();
				}
			}
			else
			{
				go.SetActive(value: false);
			}
		}

		private static void MakeTransparent(Material m)
		{
			if (m.HasProperty("_Surface"))
			{
				m.SetFloat("_Surface", 1f);
			}
			if (m.HasProperty("_SrcBlend"))
			{
				m.SetFloat("_SrcBlend", 5f);
			}
			if (m.HasProperty("_DstBlend"))
			{
				m.SetFloat("_DstBlend", 10f);
			}
			if (m.HasProperty("_ZWrite"))
			{
				m.SetFloat("_ZWrite", 0f);
			}
			m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
			m.SetOverrideTag("RenderType", "Transparent");
			m.renderQueue = 3000;
		}

		private static void SetAlpha(List<Material> mats, float a)
		{
			foreach (Material mat in mats)
			{
				string text = (mat.HasProperty("_BaseColor") ? "_BaseColor" : (mat.HasProperty("_Color") ? "_Color" : null));
				if (text != null)
				{
					Color color = mat.GetColor(text);
					color.a = a;
					mat.SetColor(text, color);
				}
			}
		}

		public override void NetworkInitialize___Early()
		{
			if (!NetworkInitialize___EarlyPuffPirates_002EObstacles_002EElevatorAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___EarlyPuffPirates_002EObstacles_002EElevatorAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Early();
				_doorOpen.InitializeEarly(this, 0u, isSyncObject: false);
			}
		}

		public override void NetworkInitialize___Late()
		{
			if (!NetworkInitialize___LatePuffPirates_002EObstacles_002EElevatorAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___LatePuffPirates_002EObstacles_002EElevatorAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Late();
				_doorOpen.InitializeLate();
			}
		}

		public override void NetworkInitializeIfDisabled()
		{
			NetworkInitialize___Early();
			NetworkInitialize___Late();
		}

		private void Awake_UserLogic_PuffPirates_002EObstacles_002EElevator_Assembly_002DCSharp_002Edll()
		{
			_rb = GetComponent<Rigidbody>();
			_rb.isKinematic = true;
			_rb.useGravity = false;
			if (_door != null)
			{
				_doorClosedPos = _door.localPosition;
			}
		}
	}
}

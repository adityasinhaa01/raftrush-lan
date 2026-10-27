using System;
using System.Collections.Generic;
using PuffPirates.Boats;
using UnityEngine;

namespace PuffPirates.Obstacles.Physics
{
	[DisallowMultipleComponent]
	public abstract class PhysicsObstacleBase : MonoBehaviour, ITickedObstacle
	{
		[Header("Aktiflik")]
		[Tooltip("Awake'te başlangıç aktiflik durumu.")]
		[SerializeField]
		private bool _startActive = true;

		[Header("Bot Filtresi (opsiyonel optimizasyon)")]
		[Tooltip("OverlapSphere sorgusunu daraltmak için katman maskesi. Projede özel bir Boat katmanı YOK — gerçek/otoriter filtre NetworkBoat component kontrolüdür (QueryHostBoats zaten bunu yapar). Bu alan yalnızca kalabalık sahnelerde fizik sorgusunu ucuzlatan bir optimizasyondur; varsayılan 'Everything' güvenlidir.")]
		[SerializeField]
		protected LayerMask _boatMask = -1;

		[Header("Deterministik RNG")]
		[Tooltip("Bu örneğe özel tohum — aynı sahnedeki iki engel aynı tick'te FARKLI (ama yine deterministik) rastgelelik üretsin diye. Aynı (seed, tick, salt) HER makinede AYNI sonucu verir.")]
		[SerializeField]
		private int _seed;

		public static readonly List<ITickedObstacle> Ticked = new List<ITickedObstacle>();

		private static readonly Collider[] _overlapBuffer = new Collider[64];

		private readonly HashSet<NetworkBoat> _seenBoats = new HashSet<NetworkBoat>();

		public bool IsActive { get; private set; }

		protected virtual void Awake()
		{
			IsActive = _startActive;
		}

		protected virtual void OnEnable()
		{
			if (!Ticked.Contains(this))
			{
				Ticked.Add(this);
			}
		}

		protected virtual void OnDisable()
		{
			Ticked.Remove(this);
		}

		public void SetActive(bool on)
		{
			if (IsActive != on)
			{
				IsActive = on;
				if (on)
				{
					OnActivated();
				}
				else
				{
					OnDeactivated();
				}
			}
		}

		protected virtual void OnActivated()
		{
		}

		protected virtual void OnDeactivated()
		{
		}

		void ITickedObstacle.ObstacleTick(int tick, float dt, float time)
		{
			if (IsActive)
			{
				OnTick(tick, dt, time);
			}
		}

		protected virtual void OnTick(int tick, float dt, float time)
		{
		}

		protected uint HashTick(int tick, int salt)
		{
			uint num = (uint)(_seed ^ tick ^ salt);
			num = num ^ 0x3D ^ (num >> 16);
			num += num << 3;
			num ^= num >> 4;
			num *= 668265261;
			return num ^ (num >> 15);
		}

		protected float RandRange(int tick, int salt, float min, float max)
		{
			float t = (float)HashTick(tick, salt) / 4.2949673E+09f;
			return Mathf.Lerp(min, max, t);
		}

		protected int RandInt(int tick, int salt, int minInclusive, int maxExclusive)
		{
			if (maxExclusive <= minInclusive)
			{
				return minInclusive;
			}
			uint num = (uint)(maxExclusive - minInclusive);
			return minInclusive + (int)(HashTick(tick, salt) % num);
		}

		protected int QueryHostBoats(Vector3 center, float radius, List<NetworkBoat> results)
		{
			results.Clear();
			if (radius <= 0f)
			{
				return 0;
			}
			_seenBoats.Clear();
			int num = UnityEngine.Physics.OverlapSphereNonAlloc(center, radius, _overlapBuffer, _boatMask, QueryTriggerInteraction.Ignore);
			for (int i = 0; i < num; i++)
			{
				Collider collider = _overlapBuffer[i];
				if (!(collider == null))
				{
					NetworkBoat componentInParent = collider.GetComponentInParent<NetworkBoat>();
					if (!(componentInParent == null) && componentInParent.IsServerInitialized && _seenBoats.Add(componentInParent))
					{
						results.Add(componentInParent);
					}
				}
			}
			return results.Count;
		}

		protected static void ApplyRadialImpulse(NetworkBoat boat, Vector3 origin, float horizontalForce, float upwardForce, float upwardAngleDeg, bool distanceFalloff, float falloffRadius)
		{
			if (!(boat == null) && boat.IsServerInitialized && !(boat.Rigidbody == null))
			{
				Rigidbody rigidbody = boat.Rigidbody;
				Vector3 vector = rigidbody.position - origin;
				float magnitude = new Vector2(vector.x, vector.z).magnitude;
				Vector3 vector2 = new Vector3(vector.x, 0f, vector.z);
				if (vector2.sqrMagnitude < 0.0001f)
				{
					vector2 = Vector3.forward;
				}
				else
				{
					vector2.Normalize();
				}
				Vector3 vector3 = Vector3.Cross(Vector3.up, vector2);
				if (vector3.sqrMagnitude < 0.0001f)
				{
					vector3 = Vector3.right;
				}
				Vector3 vector4 = Quaternion.AngleAxis(0f - upwardAngleDeg, vector3.normalized) * vector2;
				float num = 1f;
				if (distanceFalloff && falloffRadius > 0.001f)
				{
					num = Mathf.Lerp(1f, 0f, Mathf.Clamp01(magnitude / falloffRadius));
				}
				Vector3 force = vector4 * (horizontalForce * num) + Vector3.up * (upwardForce * num);
				rigidbody.AddForce(force, ForceMode.Impulse);
			}
		}

		internal static void GizmoSphere(Vector3 c, float r, Color color)
		{
			Color color2 = Gizmos.color;
			Gizmos.color = color;
			Gizmos.DrawWireSphere(c, r);
			Gizmos.color = color2;
		}

		internal static void GizmoDisc(Vector3 c, float r, Vector3 normal, Color color)
		{
			if (!(r <= 0f))
			{
				Vector3 lhs = ((normal.sqrMagnitude > 0.0001f) ? normal.normalized : Vector3.up);
				Vector3 vector = Vector3.Cross(lhs, Vector3.up);
				if (vector.sqrMagnitude < 0.0001f)
				{
					vector = Vector3.Cross(lhs, Vector3.right);
				}
				vector.Normalize();
				Vector3 vector2 = Vector3.Cross(lhs, vector);
				Color color2 = Gizmos.color;
				Gizmos.color = color;
				Vector3 vector3 = c + vector * r;
				for (int i = 1; i <= 32; i++)
				{
					float f = (float)i / 32f * MathF.PI * 2f;
					Vector3 vector4 = c + (vector * Mathf.Cos(f) + vector2 * Mathf.Sin(f)) * r;
					Gizmos.DrawLine(vector3, vector4);
					vector3 = vector4;
				}
				Gizmos.color = color2;
			}
		}

		internal static void GizmoArrow(Vector3 from, Vector3 dir, Color color)
		{
			if (!(dir.sqrMagnitude < 0.0001f))
			{
				Color color2 = Gizmos.color;
				Gizmos.color = color;
				Vector3 vector = from + dir;
				Gizmos.DrawLine(from, vector);
				Vector3 vector2 = -dir.normalized;
				Vector3 vector3 = Vector3.Cross(dir.normalized, Vector3.up);
				if (vector3.sqrMagnitude < 0.0001f)
				{
					vector3 = Vector3.Cross(dir.normalized, Vector3.right);
				}
				vector3.Normalize();
				float num = Mathf.Min(0.3f, dir.magnitude * 0.25f);
				Gizmos.DrawLine(vector, vector + (vector2 + vector3).normalized * num);
				Gizmos.DrawLine(vector, vector + (vector2 - vector3).normalized * num);
				Gizmos.color = color2;
			}
		}
	}
}

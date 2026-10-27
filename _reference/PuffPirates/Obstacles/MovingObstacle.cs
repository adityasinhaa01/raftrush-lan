using System.Collections.Generic;
using UnityEngine;

namespace PuffPirates.Obstacles
{
	[RequireComponent(typeof(Rigidbody))]
	public abstract class MovingObstacle : MonoBehaviour
	{
		public static readonly List<MovingObstacle> All = new List<MovingObstacle>();

		[Tooltip("Faz kaydırma (sn) — aynı tip engelleri birbirinden ayırmak için.")]
		[SerializeField]
		protected float _phase;

		protected Rigidbody _rb;

		protected Vector3 _origin;

		protected Quaternion _originRot;

		protected virtual void Awake()
		{
			_rb = GetComponent<Rigidbody>();
			_rb.isKinematic = true;
			_rb.useGravity = false;
			_rb.interpolation = RigidbodyInterpolation.Interpolate;
			_rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
			_origin = base.transform.position;
			_originRot = base.transform.rotation;
		}

		protected virtual void OnEnable()
		{
			if (!All.Contains(this))
			{
				All.Add(this);
			}
		}

		protected virtual void OnDisable()
		{
			All.Remove(this);
		}

		public abstract void TickObstacle(float t);
	}
}

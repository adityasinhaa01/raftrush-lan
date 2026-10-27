using System;
using UnityEngine;

namespace PuffPirates.Boats
{
	public class BoatGroundStabilizer
	{
		private readonly RaycastHit[] _hits = new RaycastHit[8];

		private readonly Rigidbody _rb;

		private readonly NetworkBoat _boat;

		private readonly LayerMask _surfaceMask;

		private readonly Func<RaycastHit, bool> _isSurfaceHit;

		public bool ConformToGround = true;

		public float AlignSpring = 9f;

		public float AlignDamping = 3f;

		public float AlignMinUpDot = 0.2f;

		public float MaxTiltDeg = 30f;

		public float FlipUprightDot = -0.25f;

		public float FlipRecoverLift = 1.5f;

		public float TiltClampMargin = 12f;

		public float TiltClampLerp = 0.35f;

		public float WaterProbeAboveHull = 0.45f;

		public float WaterRideHeight = 0.3f;

		public BoatGroundStabilizer(Rigidbody rb, NetworkBoat boat, LayerMask surfaceMask, Func<RaycastHit, bool> isSurfaceHit)
		{
			_rb = rb;
			_boat = boat;
			_surfaceMask = surfaceMask;
			_isSurfaceHit = isSurfaceHit;
		}

		public void ApplyGroundAlign(Transform transform)
		{
			if (!ConformToGround || _rb == null)
			{
				return;
			}
			float num = Mathf.Max(0.1f, WaterProbeAboveHull);
			int num2 = Physics.RaycastNonAlloc(transform.position + Vector3.up * num, maxDistance: num + Mathf.Max(0.05f, WaterRideHeight) + 1.5f, direction: Vector3.down, results: _hits, layerMask: _surfaceMask, queryTriggerInteraction: QueryTriggerInteraction.Ignore);
			float num3 = float.PositiveInfinity;
			Vector3 vector = Vector3.up;
			bool flag = false;
			for (int i = 0; i < num2; i++)
			{
				RaycastHit arg = _hits[i];
				if (_isSurfaceHit(arg) && !(Vector3.Dot(arg.normal, Vector3.up) < AlignMinUpDot) && !(arg.distance >= num3))
				{
					num3 = arg.distance;
					vector = arg.normal;
					flag = true;
				}
			}
			if (!flag)
			{
				return;
			}
			Vector3 rhs = vector;
			if (MaxTiltDeg > 0.01f)
			{
				float f = MaxTiltDeg * (MathF.PI / 180f);
				float num4 = Mathf.Cos(f);
				if (vector.y < num4)
				{
					float num5 = Mathf.Sqrt(Mathf.Max(1E-08f, 1f - vector.y * vector.y));
					float num6 = Mathf.Sin(f) / num5;
					rhs = new Vector3(vector.x * num6, num4, vector.z * num6);
				}
			}
			Vector3 angularVelocity = _rb.angularVelocity;
			Vector3 vector2 = new Vector3(angularVelocity.x, 0f, angularVelocity.z);
			Vector3 vector3 = Vector3.Cross(transform.up, rhs);
			float num7 = Mathf.Clamp(vector3.magnitude, 0f, 1f);
			Vector3 vector4 = ((num7 > 0.001f) ? (vector3 / num7 * (Mathf.Asin(num7) * AlignSpring)) : Vector3.zero);
			_rb.AddTorque(vector4 - vector2 * AlignDamping, ForceMode.Acceleration);
		}

		public void RecoverIfFlipped(Transform transform)
		{
			if (!(_rb == null) && !(Vector3.Dot(transform.up, Vector3.up) > FlipUprightDot))
			{
				Vector3 forward = transform.forward;
				forward.y = 0f;
				if (forward.sqrMagnitude < 0.0001f)
				{
					forward = Vector3.forward;
				}
				Quaternion rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
				Vector3 position = _rb.position + Vector3.up * FlipRecoverLift;
				if (_boat != null)
				{
					_boat.Teleport(position, rotation);
				}
				else
				{
					_rb.position = position;
					_rb.rotation = rotation;
				}
				_rb.linearVelocity = Vector3.zero;
				_rb.angularVelocity = Vector3.zero;
			}
		}

		public void ClampTilt(Transform transform)
		{
			if (!(_rb == null) && !(MaxTiltDeg <= 0.01f))
			{
				Vector3 up = transform.up;
				float num = MaxTiltDeg * (MathF.PI / 180f);
				float num2 = num + TiltClampMargin * (MathF.PI / 180f);
				if (!(Mathf.Acos(Mathf.Clamp(up.y, -1f, 1f)) <= num2))
				{
					float magnitude = new Vector3(up.x, 0f, up.z).magnitude;
					Vector3 toDirection = ((magnitude > 0.0001f) ? new Vector3(up.x / magnitude * Mathf.Sin(num), Mathf.Cos(num), up.z / magnitude * Mathf.Sin(num)) : Vector3.up);
					Quaternion b = Quaternion.FromToRotation(up, toDirection) * _rb.rotation;
					_rb.rotation = Quaternion.Slerp(_rb.rotation, b, TiltClampLerp);
					Vector3 angularVelocity = _rb.angularVelocity;
					angularVelocity.x *= 0.5f;
					angularVelocity.z *= 0.5f;
					_rb.angularVelocity = angularVelocity;
				}
			}
		}
	}
}

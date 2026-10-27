using System;
using UnityEngine;

namespace PuffPirates.Boats
{
	public class BoatWaterRide
	{
		private readonly RaycastHit[] _hits = new RaycastHit[8];

		private readonly Rigidbody _rb;

		private readonly NetworkBoat _boat;

		private readonly LayerMask _surfaceMask;

		private readonly Func<RaycastHit, bool> _isSurfaceHit;

		private readonly Vector2 _waveDir;

		private readonly float _phaseOffset;

		public float RideHeight = 0.3f;

		public float ProbeAboveHull = 0.45f;

		public float Spring = 42f;

		public float Damping = 7.5f;

		public float SampleRadius = 0.32f;

		public float MinUpDot = 0.55f;

		public float RideRayStartHeight = 2.5f;

		public float AntiTunnelSnap = 0.5f;

		public int PointCount = 8;

		private const float DebugArrowScale = 0.15f;

		public float BobAmplitude;

		public float BobFrequency = 0.5f;

		public float BobWaveLength = 0.6f;

		public float BobSpeedThreshold = 3f;

		public float BobFadeRange = 2f;

		public BoatWaterRide(Rigidbody rb, NetworkBoat boat, LayerMask surfaceMask, Func<RaycastHit, bool> isSurfaceHit)
		{
			_rb = rb;
			_boat = boat;
			_surfaceMask = surfaceMask;
			_isSurfaceHit = isSurfaceHit;
			float f = UnityEngine.Random.Range(0f, MathF.PI * 2f);
			_waveDir = new Vector2(Mathf.Cos(f), Mathf.Sin(f));
			_phaseOffset = UnityEngine.Random.Range(0f, 100f);
		}

		public void Apply(Transform transform, bool drawForceArrows = false)
		{
			if (_rb == null)
			{
				return;
			}
			float num = Mathf.Clamp(SampleRadius, 0.1f, 1.1f);
			float num2 = Mathf.Max(ProbeAboveHull, RideRayStartHeight);
			float maxDistance = num2 + Mathf.Max(0.05f, RideHeight) + 0.75f;
			int num3 = Mathf.Max(1, PointCount);
			Vector3 linearVelocity = _rb.linearVelocity;
			float magnitude = new Vector2(linearVelocity.x, linearVelocity.z).magnitude;
			float num4 = 1f - Mathf.Clamp01((magnitude - BobSpeedThreshold) / Mathf.Max(0.01f, BobFadeRange));
			float bobAmplitude = BobAmplitude * num4;
			float maxPenetration = 0f;
			for (int i = 0; i < num3; i++)
			{
				float f = (float)i / (float)num3 * MathF.PI * 2f;
				float localX = Mathf.Cos(f) * num;
				float localZ = Mathf.Sin(f) * num;
				ApplyPoint(transform, localX, localZ, num2, maxDistance, bobAmplitude, drawForceArrows, ref maxPenetration);
			}
			if (maxPenetration > AntiTunnelSnap)
			{
				Vector3 linearVelocity2 = _rb.linearVelocity;
				if (linearVelocity2.y < 0f)
				{
					linearVelocity2.y = 0f;
					_rb.linearVelocity = linearVelocity2;
				}
				Vector3 position = _rb.position + Vector3.up * (maxPenetration - AntiTunnelSnap);
				if (_boat != null)
				{
					_boat.Teleport(position, _rb.rotation);
				}
				else
				{
					_rb.position = position;
				}
			}
		}

		private void ApplyPoint(Transform transform, float localX, float localZ, float probeAbove, float maxDistance, float bobAmplitude, bool drawForceArrow, ref float maxPenetration)
		{
			Vector3 vector = transform.TransformPoint(new Vector3(localX, -0.5f, localZ));
			int num = Physics.RaycastNonAlloc(vector + Vector3.up * probeAbove, Vector3.down, _hits, maxDistance, _surfaceMask, QueryTriggerInteraction.Ignore);
			float num2 = float.PositiveInfinity;
			RaycastHit raycastHit = default;
			bool flag = false;
			for (int i = 0; i < num; i++)
			{
				RaycastHit raycastHit2 = _hits[i];
				if (_isSurfaceHit(raycastHit2) && !(Vector3.Dot(raycastHit2.normal, Vector3.up) < MinUpDot) && !(raycastHit2.distance >= num2))
				{
					num2 = raycastHit2.distance;
					raycastHit = raycastHit2;
					flag = true;
				}
			}
			if (!flag)
			{
				return;
			}
			float num3 = raycastHit.distance - probeAbove;
			float num4 = 0f - num3;
			if (num4 > maxPenetration)
			{
				maxPenetration = num4;
			}
			float num5 = RideHeight;
			if (bobAmplitude > 0f)
			{
				float num6 = Mathf.Max(0.05f, BobWaveLength);
				float num7 = vector.x - transform.position.x;
				float num8 = vector.z - transform.position.z;
				float f = ((num7 * _waveDir.x + num8 * _waveDir.y) / num6 + Time.time * BobFrequency + _phaseOffset) * MathF.PI * 2f;
				num5 += Mathf.Sin(f) * bobAmplitude;
			}
			float num9 = num5 - num3;
			if (!(num9 <= 0f))
			{
				float num10 = Vector3.Dot(_rb.GetPointVelocity(vector), raycastHit.normal);
				float num11 = num9 * Spring - num10 * Damping;
				if (!(num11 <= 0f))
				{
					float num12 = num11 * _rb.mass / (float)Mathf.Max(1, PointCount);
					Vector3 force = raycastHit.normal * num12;
					_rb.AddForceAtPosition(force, vector, ForceMode.Force);
				}
			}
		}
	}
}

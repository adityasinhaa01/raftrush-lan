using System;
using UnityEngine;

namespace FartTogether.Raft
{
    /// <summary>
    /// Authentically replicates the water buoyancy and gentle wave bobbing mechanics from
    /// _reference/PuffPirates/Boats/BoatWaterRide.cs, BoatPhysicsConfig.cs, and BuoyantBody.cs.
    /// 
    /// Keeps the raft floating on water channels via multi-point spring-damper suspension
    /// while allowing it to glide and slide cleanly on dry track surfaces.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public class RaftBuoyancy : MonoBehaviour
    {
        [Header("Water Ride Tuning (BoatPhysicsConfig.cs)")]
        [Tooltip("Target hull-bottom height above the water surface (BoatPhysicsConfig._waterRideHeight = 0.3f).")]
        [SerializeField] private float _rideHeight = 0.3f;

        [Tooltip("Raycast start offset above hull (BoatPhysicsConfig._waterProbeAboveHull = 0.45f).")]
        [SerializeField] private float _probeAboveHull = 0.45f;

        [Tooltip("Mass-independent spring acceleration (BoatPhysicsConfig._waterSpring = 42f).")]
        [SerializeField] private float _spring = 42f;

        [Tooltip("Damper coefficient opposing vertical velocity (BoatPhysicsConfig._waterDamping = 7.5f).")]
        [SerializeField] private float _damping = 7.5f;

        [Tooltip("Radius of sample circle around the rim (BoatWaterRide.SampleRadius = 1.0f).")]
        [SerializeField] private float _sampleRadius = 1.0f;

        [Tooltip("Number of sample points evenly spaced around circular raft (BoatPhysicsConfig._waterSamplePointCount = 8).")]
        [SerializeField] private int _pointCount = 8;

        [Tooltip("Minimum upward dot product for surface normal (BoatWaterRide.MinUpDot = 0.55f).")]
        [SerializeField] private float _minUpDot = 0.55f;

        [Tooltip("Max downward penetration before snapping hull upward (BoatWaterRide.AntiTunnelSnap = 0.5f).")]
        [SerializeField] private float _antiTunnelSnap = 0.5f;

        [Header("Wave Bobbing (BoatPhysicsConfig.cs & BoatWaterRide.cs)")]
        [Tooltip("Fake wave chop amplitude in meters (BoatPhysicsConfig._waterBobAmplitude = 0.06f).")]
        [SerializeField] private float _bobAmplitude = 0.06f;

        [Tooltip("Speed of wave bob cycles (BoatPhysicsConfig._waterBobFrequency = 0.5f).")]
        [SerializeField] private float _bobFrequency = 0.5f;

        [Tooltip("Distance between wave crests (BoatPhysicsConfig._waterBobWaveLength = 0.6f).")]
        [SerializeField] private float _bobWaveLength = 0.6f;

        [Tooltip("Speed threshold above which bobbing fades out (BoatPhysicsConfig._waterBobSpeedThreshold = 3f).")]
        [SerializeField] private float _bobSpeedThreshold = 3.0f;

        [Tooltip("Speed range over which bobbing fades to 0 (BoatPhysicsConfig._waterBobFadeRange = 2f).")]
        [SerializeField] private float _bobFadeRange = 2.0f;

        [Header("Water Drag (BuoyantBody.cs)")]
        [Tooltip("Linear drag applied while in water (BuoyantBody._linearDrag = 1.2f).")]
        [SerializeField] private float _waterLinearDrag = 1.2f;

        [Tooltip("Angular drag applied while in water (BuoyantBody._angularDrag = 0.35f).")]
        [SerializeField] private float _waterAngularDrag = 0.35f;

        [Header("Surface Detection")]
        [Tooltip("Mask for water surface layers (Default includes Water layer 4).")]
        [SerializeField] private LayerMask _waterSurfaceMask = -1;

        [Tooltip("Specific water surface collider (e.g. ~SurfaceProbe). Auto-located if null.")]
        [SerializeField] private Collider _waterSurfaceCollider;

        private Rigidbody _rb;
        private Vector2 _waveDir;
        private float _phaseOffset;
        private readonly RaycastHit[] _rayHits = new RaycastHit[8];
        private bool _isOverWater;

        public bool IsOverWater => _isOverWater;
        public float RideHeight { get => _rideHeight; set => _rideHeight = value; }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();

            // Random wave propagation vector & phase offset (BoatWaterRide.cs lines 58-60)
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            _waveDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            _phaseOffset = UnityEngine.Random.Range(0f, 100f);

            FindWaterSurfaceCollider();
        }

        private void FindWaterSurfaceCollider()
        {
            if (_waterSurfaceCollider == null)
            {
                GameObject probeObj = GameObject.Find("~SurfaceProbe");
                if (probeObj != null)
                {
                    _waterSurfaceCollider = probeObj.GetComponent<Collider>();
                }
            }
        }

        private void FixedUpdate()
        {
            if (_rb == null) return;
            if (_waterSurfaceCollider == null)
            {
                FindWaterSurfaceCollider();
            }

            ApplyWaterRide();
        }

        /// <summary>
        /// Resets buoyancy state immediately following a respawn.
        /// </summary>
        public void ResetBuoyancy()
        {
            _isOverWater = true;
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
        }

        private void ApplyWaterRide()
        {
            float radius = Mathf.Clamp(_sampleRadius, 0.1f, 1.2f);

            // Quoted from _reference/PuffPirates/Boats/BoatWaterRide.cs lines 70-71:
            // float num2 = Mathf.Max(ProbeAboveHull, RideRayStartHeight); // RideRayStartHeight = 2.5f
            // float maxDistance = num2 + Mathf.Max(0.05f, RideHeight) + 0.75f;
            float probeAbove = Mathf.Max(_probeAboveHull, 2.5f);
            float maxDistance = probeAbove + Mathf.Max(0.05f, _rideHeight) + 0.75f;
            int points = Mathf.Max(3, _pointCount);

            Vector3 linVel = _rb.linearVelocity;
            float horizontalSpeed = new Vector2(linVel.x, linVel.z).magnitude;

            // Fade wave bobbing when moving fast (BoatWaterRide.cs lines 75-76)
            float bobSpeedFraction = 1f - Mathf.Clamp01((horizontalSpeed - _bobSpeedThreshold) / Mathf.Max(0.01f, _bobFadeRange));
            float currentBobAmp = _bobAmplitude * bobSpeedFraction;

            float maxPenetration = 0f;
            int waterContactPoints = 0;

            for (int i = 0; i < points; i++)
            {
                float angle = (float)i / points * Mathf.PI * 2f;
                float localX = Mathf.Cos(angle) * radius;
                float localZ = Mathf.Sin(angle) * radius;

                if (ApplySamplePoint(localX, localZ, probeAbove, maxDistance, currentBobAmp, ref maxPenetration))
                {
                    waterContactPoints++;
                }
            }

            _isOverWater = waterContactPoints > 0;

            if (_isOverWater)
            {
                // Anti-tunnel snapping if dropped or pushed deep (BoatWaterRide.cs lines 85-101)
                if (maxPenetration > _antiTunnelSnap)
                {
                    Vector3 vel = _rb.linearVelocity;
                    if (vel.y < 0f)
                    {
                        vel.y = 0f;
                        _rb.linearVelocity = vel;
                    }
                    _rb.position += Vector3.up * (maxPenetration - _antiTunnelSnap);
                }

                // Authentic water drag while floating (BuoyantBody.cs lines 258-266)
                float fraction = (float)waterContactPoints / points;
                Vector3 dragForce = -linVel * (_waterLinearDrag * fraction * 0.5f);
                _rb.AddForce(dragForce, ForceMode.Acceleration);

                if (_waterAngularDrag > 0f)
                {
                    _rb.angularVelocity *= Mathf.Clamp01(1f - _waterAngularDrag * Time.fixedDeltaTime);
                }
            }
        }

        private bool ApplySamplePoint(float localX, float localZ, float probeAbove, float maxDistance, float bobAmp, ref float maxPenetration)
        {
            // Point at bottom of raft hull (hull base is y = -0.4f relative to raft root)
            Vector3 sampleWorld = transform.TransformPoint(new Vector3(localX, -0.4f, localZ));
            Vector3 rayOrigin = sampleWorld + Vector3.up * probeAbove;

            bool hitFound = false;
            float hitDistance = float.PositiveInfinity;
            Vector3 hitNormal = Vector3.up;

            // 1. Direct raycast against curved water channel mesh collider (~SurfaceProbe)
            if (_waterSurfaceCollider != null)
            {
                Ray downRay = new Ray(rayOrigin, Vector3.down);
                if (_waterSurfaceCollider.Raycast(downRay, out RaycastHit probeHit, maxDistance))
                {
                    hitFound = true;
                    hitDistance = probeHit.distance;
                    hitNormal = probeHit.normal;
                }
            }

            // 2. Fallback: Physics raycast against water surface mask if no specific collider hit
            // Quoted from BoatPhysicsController.cs line 590 (IsSurfaceHit): static non-rigidbody upward surface
            if (!hitFound)
            {
                int hitCount = Physics.RaycastNonAlloc(rayOrigin, Vector3.down, _rayHits, maxDistance, _waterSurfaceMask, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < hitCount; i++)
                {
                    RaycastHit h = _rayHits[i];
                    if (h.collider == null || h.rigidbody == _rb) continue;
                    if (h.collider.transform.IsChildOf(transform)) continue;

                    bool isSurface = h.collider.gameObject.layer == 4 ||
                                     h.collider.name.IndexOf("Water", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     h.collider.name.IndexOf("SurfaceProbe", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     h.collider.attachedRigidbody == null;

                    if (isSurface && Vector3.Dot(h.normal, Vector3.up) >= _minUpDot && h.distance < hitDistance)
                    {
                        hitFound = true;
                        hitDistance = h.distance;
                        hitNormal = h.normal;
                    }
                }
            }

            if (!hitFound) return false;

            // Authentic spring-damper suspension calculation (BoatWaterRide.cs lines 126-152)
            float distFromHull = hitDistance - probeAbove;
            float penetration = -distFromHull;
            if (penetration > maxPenetration)
            {
                maxPenetration = penetration;
            }

            float targetHeight = _rideHeight;

            // Wave bobbing equation (BoatWaterRide.cs lines 133-140)
            if (bobAmp > 0f)
            {
                float waveLen = Mathf.Max(0.05f, _bobWaveLength);
                float dx = sampleWorld.x - transform.position.x;
                float dz = sampleWorld.z - transform.position.z;
                float phase = ((dx * _waveDir.x + dz * _waveDir.y) / waveLen + Time.time * _bobFrequency + _phaseOffset) * Mathf.PI * 2f;
                targetHeight += Mathf.Sin(phase) * bobAmp;
            }

            float compression = targetHeight - distFromHull;
            if (compression > 0f)
            {
                float pointVelAlongNormal = Vector3.Dot(_rb.GetPointVelocity(sampleWorld), hitNormal);
                float springDamperForce = compression * _spring - pointVelAlongNormal * _damping;

                if (springDamperForce > 0f)
                {
                    float forcePerPoint = springDamperForce * _rb.mass / Mathf.Max(1, _pointCount);
                    Vector3 forceVec = hitNormal * forcePerPoint;
                    _rb.AddForceAtPosition(forceVec, sampleWorld, ForceMode.Force);
                }
            }

            return true;
        }

        private void OnDrawGizmosSelected()
        {
            if (_rb == null) return;

            Gizmos.color = _isOverWater ? Color.cyan : Color.gray;
            float radius = Mathf.Clamp(_sampleRadius, 0.1f, 1.2f);
            int points = Mathf.Max(3, _pointCount);

            for (int i = 0; i < points; i++)
            {
                float angle = (float)i / points * Mathf.PI * 2f;
                float localX = Mathf.Cos(angle) * radius;
                float localZ = Mathf.Sin(angle) * radius;
                Vector3 sampleWorld = transform.TransformPoint(new Vector3(localX, -0.4f, localZ));
                Gizmos.DrawSphere(sampleWorld, 0.08f);
            }
        }
    }
}

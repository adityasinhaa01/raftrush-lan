using System;
using FishNet.Object.Synchronizing;
using FishNet.Utility.Template;
using UnityEngine;

namespace PuffPirates.Obstacles.Physics
{
	[RequireComponent(typeof(Rigidbody))]
	[RequireComponent(typeof(ConfigurableJoint))]
	[DisallowMultipleComponent]
	public sealed class SeesawDisk : TickNetworkBehaviour
	{
		[Header("Aktiflik")]
		[Tooltip("Kapalıyken disk KİNEMATİK dondurulur (host yazar, NetworkTransform replike eder) — tepki vermez, seviyede durur.")]
		[SerializeField]
		private bool _startActive = true;

		[Header("Disk Geometrisi (varsayılan — SeesawSpawner.Configure() spawn sonrası ezebilir)")]
		[Tooltip("Disk yarıçapı (m). Görsel mesh + convex collider ölçeği buradan türer.")]
		[SerializeField]
		[Min(0.5f)]
		private float _defaultDiskRadius = 4f;

		[Tooltip("Disk kalınlığı (m).")]
		[SerializeField]
		[Min(0.05f)]
		private float _defaultDiskThickness = 0.4f;

		[Tooltip("Rigidbody mass (kg) — only the host simulates the disk, so this is not synced. VARSAYILAN 180kg BİLEREK: en ağır bot bile (BoatSizes XL ≈ 90kg) diskin YARISI kadar — disk botlardan HAFİF olursa tek bot bindiğinde bile aşırı/anlık savrulur (deneyle doğrulandı: 40kg disk + 200kg/2.5m düşme testinde limit aşıldı).")]
		[SerializeField]
		[Min(0.1f)]
		private float _defaultMass = 180f;

		[Header("Eklem — Eğim (X/Z ekseni, yay geri-döndürür)")]
		[Tooltip("Maksimum eğim açısı (°) — ConfigurableJoint'in angularX/Z limitine yazılır. Bunun ÖTESİNE ASLA eğilmez.")]
		[SerializeField]
		[Range(1f, 45f)]
		private float _defaultMaxTiltAngleDeg = 18f;

		[Tooltip("Eğimi seviyeye geri çeken açısal yay kuvveti (JointDrive.positionSpring — 'spring force').")]
		[SerializeField]
		[Min(0f)]
		private float _defaultTiltSpringForce = 900f;

		[Tooltip("Eğim salınımını söndüren açısal damper (JointDrive.positionDamper — 'damping') — jitter/aşırı-sallanma önler. VARSAYILAN 60'TAN 350'YE YÜKSELTİLDİ: deneyle doğrulandı — 60 ile yük kalktıktan 8+ sn SONRA BİLE disk sönmeyen bir salınımla titreşiyordu. 350, diskin dönme eylemsizliği (180kg, 4m yarıçap) için kritik sönümlemenin altında ama makul ölçüde güçlü — çok daha yüksek değerler (test edildi: 1100) solver'ı GEREĞİNDEN FAZLA sertleştirip beklenmedik büyük-açı kararsızlığına yol açtı.")]
		[SerializeField]
		[Min(0f)]
		private float _defaultTiltDamping = 350f;

		[Tooltip("Geri-dönüş sürücüsünün üretebileceği MAKSİMUM tork (JointDrive.maximumForce — 'return-to-centre speed'): yüksek değer hızlı/güçlü toparlanma, düşük değer yumuşak toparlanma. Aynı zamanda ani darbede patlamayı önleyen bir TAVAN.")]
		[SerializeField]
		[Min(1f)]
		private float _defaultReturnToCenterForce = 3000f;

		[Header("Dönme Direnci (Yaw/Spin — Y ekseni HER ZAMAN kilitli)")]
		[Tooltip("Rigidbody.angularDamping — TÜM eksenlerdeki açısal hızı genel olarak söndürür (tilt salınımı dahil, 'rotation resistance'). Yaw zaten daima Locked; bu alan EK genel direnç/anti-jitter.")]
		[SerializeField]
		[Min(0f)]
		private float _defaultRotationResistance = 5f;

		[Header("Anti-Jitter / Solver")]
		[SerializeField]
		[Min(1f)]
		private float _maxAngularVelocity = 6f;

		[SerializeField]
		[Range(1f, 20f)]
		private int _solverIterations = 10;

		[SerializeField]
		[Range(1f, 20f)]
		private int _solverVelocityIterations = 10;

		public readonly SyncVar<float> _svDiskRadius = new SyncVar<float>(new SyncTypeSettings(0f));

		public readonly SyncVar<float> _svDiskThickness = new SyncVar<float>(new SyncTypeSettings(0f));

		public readonly SyncVar<float> _svMaxTiltAngleDeg = new SyncVar<float>(new SyncTypeSettings(0f));

		public readonly SyncVar<float> _svTiltSpringForce = new SyncVar<float>(new SyncTypeSettings(0f));

		public readonly SyncVar<float> _svTiltDamping = new SyncVar<float>(new SyncTypeSettings(0f));

		public readonly SyncVar<float> _svReturnToCenterForce = new SyncVar<float>(new SyncTypeSettings(0f));

		public readonly SyncVar<float> _svRotationResistance = new SyncVar<float>(new SyncTypeSettings(0f));

		public readonly SyncVar<bool> _svIsActive = new SyncVar<bool>(new SyncTypeSettings(0f));

		private const float BakedDiskRadius = 4f;

		private const float BakedDiskThickness = 0.4f;

		[Header("Geometri Referansları (bütün Transform scale değerleri 1,1,1)")]
		[SerializeField]
		private Transform _collisionGeometry;

		[SerializeField]
		private MeshFilter _collisionMeshFilter;

		[SerializeField]
		private MeshCollider _collisionCollider;

		[SerializeField]
		private Mesh _collisionBaseMesh;

		[Tooltip("Collision KALINLIĞI (m) — GÖRSELDEN BAĞIMSIZ. TÜNELLEME FIX: ince görsel diske (0.4m) hızlı/ağır bot delip geçiyordu; collision KALIN yapılır (fazlası AŞAĞI iner, üst yüzey görselle hizalı) → penetrasyon dolacak hacim var, bot durur. Direkle çakışmayı SeesawSpawner Physics.IgnoreCollision önler.")]
		[SerializeField]
		[Min(0.5f)]
		private float _collisionThickness = 2f;

		[SerializeField]
		private Transform _visualRoot;

		[SerializeField]
		private MeshFilter[] _visualMeshFilters;

		[SerializeField]
		private Mesh[] _visualBaseMeshes;

		private Mesh _runtimeCollisionMesh;

		private Mesh[] _runtimeVisualMeshes;

		[Header("Direk Ankraji (dahili)")]
		[Tooltip("~JointAnchor çocuğu — KİNEMATİK, ağa hiç girmeyen sabit-direk referans gövdesi. ConfigurableJoint buna bağlanır (connectedBody=null/dünya-uzayı YERİNE — deneyle doğrulandı: connectedBody=null aşırı sert kısıt kombinasyonuyla [Locked öteleme + Locked yaw + Limited tilt + yüksek spring/damper] sayısal olarak KARARSIZ, açısal hız kontrolsüzce büyüyordu. Gerçek bir kinematik Rigidbody'ye bağlamak PhysX'in çok daha iyi desteklediği/test ettiği bir yoldur.")]
		[SerializeField]
		private Rigidbody _anchorRb;

		private ConfigurableJoint _joint;

		private Rigidbody _rb;

		private float _appliedRadius = float.NaN;

		private float _appliedThickness = float.NaN;

		private float _appliedTilt = float.NaN;

		private float _appliedSpring = float.NaN;

		private float _appliedDamping = float.NaN;

		private float _appliedReturn = float.NaN;

		private float _appliedResistance = float.NaN;

		private Vector3 _spawnPosition;

		private Quaternion _spawnRotation;

		private bool NetworkInitialize___EarlyPuffPirates_002EObstacles_002EPhysics_002ESeesawDiskAssembly_002DCSharp_002Edll_Excuted;

		private bool NetworkInitialize___LatePuffPirates_002EObstacles_002EPhysics_002ESeesawDiskAssembly_002DCSharp_002Edll_Excuted;

		public float DiskRadius
		{
			get
			{
				return _svDiskRadius.Value;
			}
			private set
			{
				_svDiskRadius.Value = value;
			}
		}

		public float DiskThickness
		{
			get
			{
				return _svDiskThickness.Value;
			}
			private set
			{
				_svDiskThickness.Value = value;
			}
		}

		public float MaxTiltAngleDeg
		{
			get
			{
				return _svMaxTiltAngleDeg.Value;
			}
			private set
			{
				_svMaxTiltAngleDeg.Value = value;
			}
		}

		public float TiltSpringForce
		{
			get
			{
				return _svTiltSpringForce.Value;
			}
			private set
			{
				_svTiltSpringForce.Value = value;
			}
		}

		public float TiltDamping
		{
			get
			{
				return _svTiltDamping.Value;
			}
			private set
			{
				_svTiltDamping.Value = value;
			}
		}

		public float ReturnToCenterForce
		{
			get
			{
				return _svReturnToCenterForce.Value;
			}
			private set
			{
				_svReturnToCenterForce.Value = value;
			}
		}

		public float RotationResistance
		{
			get
			{
				return _svRotationResistance.Value;
			}
			private set
			{
				_svRotationResistance.Value = value;
			}
		}

		public bool IsActive
		{
			get
			{
				return _svIsActive.Value;
			}
			private set
			{
				_svIsActive.Value = value;
			}
		}

		public Collider CollisionCollider => _collisionCollider;

		public override void Awake()
		{
			NetworkInitialize___Early();
			Awake_UserLogic_PuffPirates_002EObstacles_002EPhysics_002ESeesawDisk_Assembly_002DCSharp_002Edll();
			NetworkInitialize___Late();
		}

		public override void OnStartNetwork()
		{
			base.OnStartNetwork();
			_joint = GetComponent<ConfigurableJoint>();
			_rb = GetComponent<Rigidbody>();
			if (_anchorRb != null)
			{
				_anchorRb.isKinematic = true;
				_anchorRb.useGravity = false;
			}
			_spawnPosition = base.transform.position;
			_spawnRotation = base.transform.rotation;
		}

		public override void OnStartServer()
		{
			base.OnStartServer();
			DiskRadius = _defaultDiskRadius;
			DiskThickness = _defaultDiskThickness;
			MaxTiltAngleDeg = _defaultMaxTiltAngleDeg;
			TiltSpringForce = _defaultTiltSpringForce;
			TiltDamping = _defaultTiltDamping;
			ReturnToCenterForce = _defaultReturnToCenterForce;
			RotationResistance = _defaultRotationResistance;
			IsActive = _startActive;
			if (_rb != null)
			{
				_rb.mass = _defaultMass;
			}
			ApplyGeometryAndJoint();
			if (_rb != null)
			{
				_rb.isKinematic = !IsActive;
			}
		}

		public override void OnStartClient()
		{
			base.OnStartClient();
			if (!base.IsServerInitialized)
			{
				ApplyGeometryAndJoint();
			}
		}

		protected override void TimeManager_OnUpdate()
		{
			if (base.IsSpawned)
			{
				bool num = DiskRadius != _appliedRadius || DiskThickness != _appliedThickness;
				bool flag = MaxTiltAngleDeg != _appliedTilt || TiltSpringForce != _appliedSpring || TiltDamping != _appliedDamping || ReturnToCenterForce != _appliedReturn || RotationResistance != _appliedResistance;
				if (num)
				{
					ApplyGeometryAndJoint();
				}
				else if (flag)
				{
					ApplyJointTuning();
				}
			}
		}

		public void Configure(float diskRadius, float diskThickness, float mass, float maxTiltAngleDeg, float tiltSpringForce, float tiltDamping, float returnToCenterForce, float rotationResistance)
		{
			if (base.IsServerInitialized)
			{
				DiskRadius = Mathf.Max(0.5f, diskRadius);
				DiskThickness = Mathf.Max(0.05f, diskThickness);
				MaxTiltAngleDeg = Mathf.Clamp(maxTiltAngleDeg, 1f, 45f);
				TiltSpringForce = Mathf.Max(0f, tiltSpringForce);
				TiltDamping = Mathf.Max(0f, tiltDamping);
				ReturnToCenterForce = Mathf.Max(1f, returnToCenterForce);
				RotationResistance = Mathf.Max(0f, rotationResistance);
				if (_rb != null)
				{
					_rb.mass = Mathf.Max(0.1f, mass);
				}
				ApplyGeometryAndJoint();
			}
		}

		public void SetTuning(float maxTiltDeg, float spring, float damping, float returnForce)
		{
			if (base.IsServerInitialized)
			{
				MaxTiltAngleDeg = Mathf.Clamp(maxTiltDeg, 1f, 45f);
				TiltSpringForce = Mathf.Max(0f, spring);
				TiltDamping = Mathf.Max(0f, damping);
				ReturnToCenterForce = Mathf.Max(1f, returnForce);
				ApplyJointTuning();
			}
		}

		public void SetActive(bool on)
		{
			if (!base.IsServerInitialized || IsActive == on)
			{
				return;
			}
			IsActive = on;
			if (!(_rb == null))
			{
				_rb.isKinematic = !on;
				if (!on)
				{
					_rb.linearVelocity = Vector3.zero;
					_rb.angularVelocity = Vector3.zero;
					_rb.position = _spawnPosition;
					_rb.rotation = _spawnRotation;
				}
			}
		}

		protected override void TimeManager_OnTick()
		{
			if (!base.IsServerInitialized || _rb == null || !IsActive)
			{
				return;
			}
			Vector3 angularVelocity = _rb.angularVelocity;
			Vector3 position = _rb.position;
			if (float.IsNaN(angularVelocity.x) || float.IsNaN(angularVelocity.y) || float.IsNaN(angularVelocity.z) || float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z) || angularVelocity.sqrMagnitude > _maxAngularVelocity * _maxAngularVelocity * 4f)
			{
				_rb.angularVelocity = Vector3.zero;
				_rb.linearVelocity = Vector3.zero;
				_rb.position = _spawnPosition;
				_rb.rotation = _spawnRotation;
				Debug.LogWarning("[SeesawDisk] Anormal fizik durumu algılandı — disk merkeze sıfırlandı.", this);
				return;
			}
			float num = Quaternion.Angle(_spawnRotation, _rb.rotation);
			if (num > MaxTiltAngleDeg)
			{
				_rb.MoveRotation(Quaternion.RotateTowards(_spawnRotation, _rb.rotation, MaxTiltAngleDeg));
				_rb.angularVelocity *= 0.5f;
			}
			else if (num > 1f && angularVelocity.sqrMagnitude < 0.0025f)
			{
				(Quaternion.Inverse(_rb.rotation) * _spawnRotation).ToAngleAxis(out var angle, out var axis);
				if (angle > 180f)
				{
					angle -= 360f;
				}
				if (!float.IsNaN(axis.x) && axis.sqrMagnitude > 0.0001f)
				{
					Vector3 vector = _rb.rotation * axis.normalized;
					_rb.AddTorque(vector * (angle * (MathF.PI / 180f) * TiltSpringForce * 0.3f), ForceMode.Force);
				}
			}
		}

		private void ApplyGeometryAndJoint()
		{
			_appliedRadius = DiskRadius;
			_appliedThickness = DiskThickness;
			float radius = ((DiskRadius > 0f) ? DiskRadius : _defaultDiskRadius);
			float thickness = ((DiskThickness > 0f) ? DiskThickness : _defaultDiskThickness);
			base.transform.localScale = Vector3.one;
			if (_collisionGeometry != null)
			{
				_collisionGeometry.localPosition = Vector3.zero;
				_collisionGeometry.localRotation = Quaternion.identity;
				_collisionGeometry.localScale = Vector3.one;
			}
			if (_visualRoot != null)
			{
				_visualRoot.localPosition = Vector3.zero;
				_visualRoot.localRotation = Quaternion.identity;
				_visualRoot.localScale = Vector3.one;
			}
			ApplyUnitScaleMeshes(radius, thickness);
			if (_rb != null)
			{
				_rb.maxAngularVelocity = _maxAngularVelocity;
				_rb.interpolation = RigidbodyInterpolation.None;
				_rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
				_rb.solverIterations = _solverIterations;
				_rb.solverVelocityIterations = _solverVelocityIterations;
				_rb.ResetCenterOfMass();
				_rb.ResetInertiaTensor();
				_rb.inertiaTensorRotation = Quaternion.identity;
				_rb.sleepThreshold = 0f;
			}
			ApplyJointTuning();
			if (!(_joint == null))
			{
				_joint.autoConfigureConnectedAnchor = false;
				_joint.anchor = Vector3.zero;
				if (_anchorRb != null)
				{
					_joint.connectedBody = _anchorRb;
					_joint.connectedAnchor = Vector3.zero;
				}
				else
				{
					_joint.connectedBody = null;
					_joint.connectedAnchor = _spawnPosition;
				}
				_joint.xMotion = ConfigurableJointMotion.Locked;
				_joint.yMotion = ConfigurableJointMotion.Locked;
				_joint.zMotion = ConfigurableJointMotion.Locked;
				_joint.angularXMotion = ConfigurableJointMotion.Limited;
				_joint.angularYMotion = ConfigurableJointMotion.Locked;
				_joint.angularZMotion = ConfigurableJointMotion.Limited;
				_joint.projectionMode = JointProjectionMode.PositionAndRotation;
				_joint.projectionDistance = 0.01f;
				_joint.projectionAngle = 0.5f;
			}
		}

		private void ApplyJointTuning()
		{
			_appliedTilt = MaxTiltAngleDeg;
			_appliedSpring = TiltSpringForce;
			_appliedDamping = TiltDamping;
			_appliedReturn = ReturnToCenterForce;
			_appliedResistance = RotationResistance;
			float num = ((MaxTiltAngleDeg > 0f) ? MaxTiltAngleDeg : _defaultMaxTiltAngleDeg);
			float positionSpring = ((TiltSpringForce > 0f) ? TiltSpringForce : _defaultTiltSpringForce);
			float positionDamper = ((TiltDamping > 0f) ? TiltDamping : _defaultTiltDamping);
			float maximumForce = ((ReturnToCenterForce > 0f) ? ReturnToCenterForce : _defaultReturnToCenterForce);
			float angularDamping = ((RotationResistance > 0f) ? RotationResistance : _defaultRotationResistance);
			if (_rb != null)
			{
				_rb.angularDamping = angularDamping;
			}
			if (!(_joint == null))
			{
				_joint.lowAngularXLimit = new SoftJointLimit
				{
					limit = 0f - num
				};
				_joint.highAngularXLimit = new SoftJointLimit
				{
					limit = num
				};
				_joint.angularZLimit = new SoftJointLimit
				{
					limit = num
				};
				_joint.rotationDriveMode = RotationDriveMode.XYAndZ;
				_joint.targetRotation = Quaternion.identity;
				JointDrive jointDrive = new JointDrive
				{
					positionSpring = positionSpring,
					positionDamper = positionDamper,
					maximumForce = maximumForce
				};
				_joint.angularXDrive = jointDrive;
				_joint.angularYZDrive = jointDrive;
			}
		}

		private void ApplyUnitScaleMeshes(float radius, float thickness)
		{
			Vector3 requestedScale = new Vector3(radius / 4f, thickness / 0.4f, radius / 4f);
			float num = Mathf.Max(thickness, _collisionThickness);
			Mesh mesh = SeesawUnitScaleMesh.Apply(requestedScale: new Vector3(radius / 4f, num / 0.4f, radius / 4f), baseMesh: _collisionBaseMesh, generatedMesh: ref _runtimeCollisionMesh, generatedName: "SeesawCollision_Runtime");
			if (_collisionMeshFilter != null && mesh != null)
			{
				_collisionMeshFilter.sharedMesh = mesh;
			}
			if (_collisionCollider != null && mesh != null)
			{
				_collisionCollider.sharedMesh = null;
				_collisionCollider.sharedMesh = mesh;
			}
			if (_collisionGeometry != null)
			{
				_collisionGeometry.localPosition = new Vector3(0f, (0f - (num - thickness)) * 0.5f, 0f);
			}
			int a = ((_visualMeshFilters != null) ? _visualMeshFilters.Length : 0);
			int b = ((_visualBaseMeshes != null) ? _visualBaseMeshes.Length : 0);
			int num2 = Mathf.Min(a, b);
			if (_runtimeVisualMeshes == null || _runtimeVisualMeshes.Length != num2)
			{
				SeesawUnitScaleMesh.ReleaseAll(_runtimeVisualMeshes);
				_runtimeVisualMeshes = new Mesh[num2];
			}
			for (int i = 0; i < num2; i++)
			{
				MeshFilter meshFilter = _visualMeshFilters[i];
				Mesh mesh2 = _visualBaseMeshes[i];
				if (!(meshFilter == null) && !(mesh2 == null))
				{
					Mesh mesh3 = SeesawUnitScaleMesh.Apply(mesh2, ref _runtimeVisualMeshes[i], requestedScale, mesh2.name + "_Runtime");
					if (mesh3 != null)
					{
						meshFilter.sharedMesh = mesh3;
					}
				}
			}
		}

		private void OnDestroy()
		{
			SeesawUnitScaleMesh.Release(ref _runtimeCollisionMesh);
			SeesawUnitScaleMesh.ReleaseAll(_runtimeVisualMeshes);
			_runtimeVisualMeshes = null;
		}

		private void OnDrawGizmosSelected()
		{
			int num;
			float num2;
			if (Application.isPlaying && base.IsSpawned)
			{
				num = ((DiskRadius > 0f) ? 1 : 0);
				if (num != 0)
				{
					num2 = DiskRadius;
					goto IL_0030;
				}
			}
			else
			{
				num = 0;
			}
			num2 = _defaultDiskRadius;
			goto IL_0030;
			IL_0030:
			float num3 = num2;
			float angle = ((num != 0) ? MaxTiltAngleDeg : _defaultMaxTiltAngleDeg);
			Vector3 position = base.transform.position;
			PhysicsObstacleBase.GizmoDisc(position, num3, Vector3.up, new Color(0.2f, 0.8f, 1f, 0.9f));
			PhysicsObstacleBase.GizmoArrow(position, Vector3.up * 1.5f, Color.yellow);
			Color color = new Color(1f, 0.4f, 0.1f, 0.85f);
			Vector3[] array = new Vector3[4]
			{
				Vector3.right,
				-Vector3.right,
				Vector3.forward,
				-Vector3.forward
			};
			foreach (Vector3 vector in array)
			{
				Vector3 normalized = Vector3.Cross(Vector3.up, vector).normalized;
				Vector3 dir = Quaternion.AngleAxis(angle, normalized) * (vector * num3);
				PhysicsObstacleBase.GizmoArrow(position, dir, color);
			}
		}

		public override void NetworkInitialize___Early()
		{
			if (!NetworkInitialize___EarlyPuffPirates_002EObstacles_002EPhysics_002ESeesawDiskAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___EarlyPuffPirates_002EObstacles_002EPhysics_002ESeesawDiskAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Early();
				_svIsActive.InitializeEarly(this, 7u, isSyncObject: false);
				_svRotationResistance.InitializeEarly(this, 6u, isSyncObject: false);
				_svReturnToCenterForce.InitializeEarly(this, 5u, isSyncObject: false);
				_svTiltDamping.InitializeEarly(this, 4u, isSyncObject: false);
				_svTiltSpringForce.InitializeEarly(this, 3u, isSyncObject: false);
				_svMaxTiltAngleDeg.InitializeEarly(this, 2u, isSyncObject: false);
				_svDiskThickness.InitializeEarly(this, 1u, isSyncObject: false);
				_svDiskRadius.InitializeEarly(this, 0u, isSyncObject: false);
			}
		}

		public override void NetworkInitialize___Late()
		{
			if (!NetworkInitialize___LatePuffPirates_002EObstacles_002EPhysics_002ESeesawDiskAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___LatePuffPirates_002EObstacles_002EPhysics_002ESeesawDiskAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Late();
				_svIsActive.InitializeLate();
				_svRotationResistance.InitializeLate();
				_svReturnToCenterForce.InitializeLate();
				_svTiltDamping.InitializeLate();
				_svTiltSpringForce.InitializeLate();
				_svMaxTiltAngleDeg.InitializeLate();
				_svDiskThickness.InitializeLate();
				_svDiskRadius.InitializeLate();
			}
		}

		public override void NetworkInitializeIfDisabled()
		{
			NetworkInitialize___Early();
			NetworkInitialize___Late();
		}

		private void Awake_UserLogic_PuffPirates_002EObstacles_002EPhysics_002ESeesawDisk_Assembly_002DCSharp_002Edll()
		{
			SetTickCallbacks(TickCallback.Tick | TickCallback.Update);
		}
	}
}

using System;
using System.Collections.Generic;
using FishNet.Component.Transforming;
using FishNet.Managing;
using FishNet.Managing.Timing;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Serializing;
using FishNet.Transporting;
using FishNet.Utility.Template;
using GamesMrkt.Audio;
using GamesMrkt.Core;
using GamesMrkt.Particles;
using GamesMrkt.PuffPirates;
using PuffPirates.Audio;
using PuffPirates.CameraRig;
using PuffPirates.Cosmetics;
using PuffPirates.Juice;
using PuffPirates.Net;
using PuffPirates.Networking;
using PuffPirates.Obstacles;
using PuffPirates.Players;
using PuffPirates.Systems;
using PuffPirates.Visuals;
using UnityEngine;

namespace PuffPirates.Boats
{
	[RequireComponent(typeof(Rigidbody))]
	[DisallowMultipleComponent]
	public class NetworkBoat : TickNetworkBehaviour
	{
		[SerializeField]
		private BoatDefinition _definition;

		[Header("Görsel Model (Ship)")]
		[Tooltip("Gövde modeli. Fizik ayrı düşük-poligon hull collider'dadır; bu referans görsel ve authored poz kaynağıdır.")]
		[SerializeField]
		private Transform _visual;

		[Tooltip("Açık: kök BoxCollider yerine gövde meshinden CONVEX collider kurulur (bot şekline uygun, performanslı). Ship mesh Read/Write açık olmalı.")]
		[SerializeField]
		private bool _buildHullCollider = true;

		[Tooltip("Opsiyonel: hull collider için GÖRSELDEN AYRI mesh. Boşsa görsel mesh kullanılır (eski davranış). Dolu ise görsel serbestçe değiştirilebilir, collider bu mesh'ten kurulur. Bu mesh de Read/Write açık olmalı; convex'e çevrileceği için LOW-POLY öner. Görselle AYNI yerel uzayda (aynı modelden) export edilmeli ki hizalı otursun.")]
		[SerializeField]
		private Mesh _hullColliderMesh;

		[Header("Mürettebat kapasitesi (oyuncu çıkınca bot küçülür)")]
		[Tooltip("Görsel gövdenin MeshFilter'ı — kapasite değişince mesh'i değişir.")]
		[SerializeField]
		private MeshFilter _hullFilter;

		[Tooltip("Kapasiteye göre görsel gövde mesh'leri: [0]=2 kişilik, [1]=3, [2]=4. Boşsa mesh değişmez.")]
		[SerializeField]
		private Mesh[] _capacityMeshes;

		[Tooltip("Kapasiteye göre gövde mesh'inin yerel rotasyonu (Euler), _capacityMeshes ile aynı sıra. 2 kişilik mesh FBX'ten 90° X döndürülmüş gelir; 3/4 kişilik dik durur (Boat_ThreeP/FourP bunu identity'ye override eder).")]
		[SerializeField]
		private Vector3[] _capacityHullEuler = new Vector3[3]
		{
			new Vector3(90f, 0f, 0f),
			Vector3.zero,
			Vector3.zero
		};

		[Tooltip("Opsiyonel: ölüm patlaması VFX'i (ör. AllIn1 Toon Explosion). Atanırsa prosedürel patlama yerine bu tek-atım efekt DÜNYA'da (kopuk) doğar; boşsa eski koddan-üretilen patlama.")]
		[SerializeField]
		private GameObject _explosionVfxPrefab;

		[Header("Death sounds (3D, per cause)")]
		[Tooltip("Empty = falls back to CommonSound.BoatCrash.")]
		[SerializeField]
		private SoundData _wallDeathSound;

		[Tooltip("Empty = falls back to CommonSound.BoatCrash.")]
		[SerializeField]
		private SoundData _obstacleDeathSound;

		[Tooltip("Empty = falls back to CommonSound.BoatCrash.")]
		[SerializeField]
		private SoundData _voidDeathSound;

		private bool _hullBuilt;

		private Transform _hullTransform;

		[Header("Su Görsel Esnemesi")]
		[Tooltip("Yalnız görsel modelde çalışır; collider ve ağ fiziğini bozmaz.")]
		[SerializeField]
		[Min(0f)]
		private float _waterVisualBob = 0.035f;

		[SerializeField]
		[Min(0f)]
		private float _waterVisualPitch = 0.55f;

		[SerializeField]
		[Min(0f)]
		private float _waterVisualRoll = 0.9f;

		[SerializeField]
		[Range(0f, 0.05f)]
		private float _waterVisualSquash = 0.012f;

		[SerializeField]
		[Min(0.1f)]
		private float _waterVisualFrequency = 1.4f;

		[SerializeField]
		[Min(0.1f)]
		private float _waterVisualLerp = 8f;

		[Header("Çarpışma Sallanması (yalnız görsel)")]
		[SerializeField]
		[Min(0.1f)]
		private float _softImpactThreshold = 1.1f;

		[SerializeField]
		[Min(0f)]
		private float _softImpactKick = 2.2f;

		[SerializeField]
		[Min(0.1f)]
		private float _softSpring = 24f;

		[SerializeField]
		[Min(0f)]
		private float _softDamping = 7f;

		[SerializeField]
		[Range(0f, 12f)]
		private float _softMaxTilt = 9f;

		[SerializeField]
		[Range(0f, 0.4f)]
		private float _softImpactHoldSeconds = 0.04f;

		[SerializeField]
		[Min(0.1f)]
		private float _softImpactRecoverSeconds = 0.4f;

		public readonly SyncVar<int> _svCurrentCheckpointId = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<int> _svDeaths = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<int> _svCrewCapacity = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<BoatStatusFlags> _svActiveStatusFlags = new SyncVar<BoatStatusFlags>(new SyncTypeSettings(0f));

		public readonly SyncVar<int> _svVisualImpactSequence = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<Vector3> _svVisualImpactLocal = new SyncVar<Vector3>(new SyncTypeSettings(0f));

		public readonly SyncVar<Vector3> _svVisualImpactPosition = new SyncVar<Vector3>(new SyncTypeSettings(0f));

		public readonly SyncVar<int> _svDeathSequence = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<Vector3> _svDeathPosition = new SyncVar<Vector3>(new SyncTypeSettings(0f));

		public readonly SyncVar<DeathCause> _svLastDeathCause = new SyncVar<DeathCause>(new SyncTypeSettings(0f));

		private TickTimer RandomizeCooldown;

		private const float RandomizeCooldownSec = 3f;

		public readonly SyncVar<bool> _svIsRespawning = new SyncVar<bool>(new SyncTypeSettings(0f));

		[Tooltip("Seconds a dead boat waits, hidden and frozen at the death spot, before it respawns. The camera follows the boat, so it lingers on the explosion meanwhile. 0 = respawn instantly.")]
		[SerializeField]
		[Min(0f)]
		private float _respawnDelaySec = 1.5f;

		public readonly SyncVar<bool> _svIsDead = new SyncVar<bool>(new SyncTypeSettings(0f));

		private TickTimer _pendingRespawnTimer;

		private Vector3 _pendingRespawnPosition;

		private Quaternion _pendingRespawnRotation;

		private float _pendingRespawnInvuln;

		private bool _wasKinematicBeforeDeath;

		private readonly List<Renderer> _deadHiddenRenderers = new List<Renderer>();

		private readonly List<Collider> _deadDisabledColliders = new List<Collider>();

		private bool _deadStateApplied;

		private bool _deadUnhidePending;

		private float _deadUnhideDeadline;

		private const float DeadUnhideTimeout = 0.5f;

		private const float DeadUnhideMoveSqr = 4f;

		public readonly SyncVar<int> _svFinishRank = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<int> _svSizeId = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<int> _svSkinId = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<int> _svBoatIndex = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<int> _svOwnerTeam = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<int> _svLap = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<int> _svHeldItem = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<TickTimer> _svInkTimer = new SyncVar<TickTimer>(new SyncTypeSettings(0f));

		public readonly SyncVar<TickTimer> _svMiniTimer = new SyncVar<TickTimer>(new SyncTypeSettings(0f));

		public readonly SyncVar<int> _svPreMiniSizeId = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<TickTimer> _svSpeedBoostTimer = new SyncVar<TickTimer>(new SyncTypeSettings(0f));

		private int BoostPadSfxSequence;

		private readonly TickTimer[] StatusEndTicks = new TickTimer[10];

		private Rigidbody _rb;

		private NetworkTransform _netTransform;

		private Vector3 _visualBasePosition;

		private Vector3 _visualBaseScale = Vector3.one;

		private Quaternion _visualBaseRotation = Quaternion.identity;

		private bool _visualBaseCaptured;

		private int _lastImpactSequence;

		private Vector3 _softWobble;

		private Vector3 _softWobbleVelocity;

		private float _softImpactEnvelope;

		private float _softImpactHoldRemaining;

		private int _lastSizeId = -1;

		private int _lastCrewCapacity = -1;

		private int _lastSkinId = -1;

		private int _lastCheckpointId;

		private int _lastDeathSequence;

		private TickTimer _lastSpeedBoostTimer;

		private const int RespawnVelocityLockSimulations = 2;

		private int _respawnVelocityLockRemaining;

		private TimeManager _physicsTm;

		private BoatStatusFlags _lastStatusFlags;

		private RaceLauncherSystem _audioLauncherCache;

		private RigidbodyConstraints _baseConstraints;

		public static readonly List<NetworkBoat> All = new List<NetworkBoat>();

		private float _baseMass;

		private bool NetworkInitialize___EarlyPuffPirates_002EBoats_002ENetworkBoatAssembly_002DCSharp_002Edll_Excuted;

		private bool NetworkInitialize___LatePuffPirates_002EBoats_002ENetworkBoatAssembly_002DCSharp_002Edll_Excuted;

		public int CurrentCheckpointId
		{
			get
			{
				return _svCurrentCheckpointId.Value;
			}
			private set
			{
				_svCurrentCheckpointId.Value = value;
			}
		}

		public int Deaths
		{
			get
			{
				return _svDeaths.Value;
			}
			private set
			{
				_svDeaths.Value = value;
			}
		}

		public int CrewCapacity
		{
			get
			{
				return _svCrewCapacity.Value;
			}
			private set
			{
				_svCrewCapacity.Value = value;
			}
		}

		public BoatStatusFlags ActiveStatusFlags
		{
			get
			{
				return _svActiveStatusFlags.Value;
			}
			private set
			{
				_svActiveStatusFlags.Value = value;
			}
		}

		private int VisualImpactSequence
		{
			get
			{
				return _svVisualImpactSequence.Value;
			}
			set
			{
				_svVisualImpactSequence.Value = value;
			}
		}

		private Vector3 VisualImpactLocal
		{
			get
			{
				return _svVisualImpactLocal.Value;
			}
			set
			{
				_svVisualImpactLocal.Value = value;
			}
		}

		private Vector3 VisualImpactPosition
		{
			get
			{
				return _svVisualImpactPosition.Value;
			}
			set
			{
				_svVisualImpactPosition.Value = value;
			}
		}

		public int ImpactSequence => VisualImpactSequence;

		public Vector3 ImpactLocalDirection => VisualImpactLocal;

		private int DeathSequence
		{
			get
			{
				return _svDeathSequence.Value;
			}
			set
			{
				_svDeathSequence.Value = value;
			}
		}

		private Vector3 DeathPosition
		{
			get
			{
				return _svDeathPosition.Value;
			}
			set
			{
				_svDeathPosition.Value = value;
			}
		}

		private DeathCause LastDeathCause
		{
			get
			{
				return _svLastDeathCause.Value;
			}
			set
			{
				_svLastDeathCause.Value = value;
			}
		}

		public bool IsRespawning
		{
			get
			{
				return _svIsRespawning.Value;
			}
			private set
			{
				_svIsRespawning.Value = value;
			}
		}

		public bool IsDead
		{
			get
			{
				return _svIsDead.Value;
			}
			private set
			{
				_svIsDead.Value = value;
			}
		}

		public int FinishRank
		{
			get
			{
				return _svFinishRank.Value;
			}
			private set
			{
				_svFinishRank.Value = value;
			}
		}

		public int SizeId
		{
			get
			{
				return _svSizeId.Value;
			}
			private set
			{
				_svSizeId.Value = value;
			}
		}

		public int SkinId
		{
			get
			{
				return _svSkinId.Value;
			}
			private set
			{
				_svSkinId.Value = value;
			}
		}

		public int BoatIndex
		{
			get
			{
				return _svBoatIndex.Value;
			}
			set
			{
				_svBoatIndex.Value = value;
			}
		}

		public int OwnerTeam
		{
			get
			{
				return _svOwnerTeam.Value;
			}
			set
			{
				_svOwnerTeam.Value = value;
			}
		}

		public int Lap
		{
			get
			{
				return _svLap.Value;
			}
			private set
			{
				_svLap.Value = value;
			}
		}

		public int HeldItem
		{
			get
			{
				return _svHeldItem.Value;
			}
			private set
			{
				_svHeldItem.Value = value;
			}
		}

		public TickTimer InkTimer
		{
			get
			{
				return _svInkTimer.Value;
			}
			private set
			{
				_svInkTimer.Value = value;
			}
		}

		public TickTimer MiniTimer
		{
			get
			{
				return _svMiniTimer.Value;
			}
			private set
			{
				_svMiniTimer.Value = value;
			}
		}

		public int PreMiniSizeId
		{
			get
			{
				return _svPreMiniSizeId.Value;
			}
			private set
			{
				_svPreMiniSizeId.Value = value;
			}
		}

		public TickTimer SpeedBoostTimer
		{
			get
			{
				return _svSpeedBoostTimer.Value;
			}
			private set
			{
				_svSpeedBoostTimer.Value = value;
			}
		}

		public float AttachmentRadius => BoatSizes.Get(SizeId).AttachRadius * BoatCrewGizmo.RadiusMul;

		public float MaxSpeed => BoatSizes.Get(SizeId).MaxSpeed;

		public bool IsSpeedBoosted => !SpeedBoostTimer.ExpiredOrNotRunning(base.TimeManager);

		public BoatDefinition Definition => _definition;

		public Rigidbody Rigidbody
		{
			get
			{
				if (!(_rb != null))
				{
					return _rb = GetComponent<Rigidbody>();
				}
				return _rb;
			}
		}

		public bool IsInvulnerable
		{
			get
			{
				if (!IsDead)
				{
					return HasStatus(BoatStatusFlags.Invulnerable);
				}
				return true;
			}
		}

		public bool IsLocalBoat
		{
			get
			{
				if (_audioLauncherCache == null)
				{
					this.TryGetSystem<RaceLauncherSystem, CoreSystemHandler>(out _audioLauncherCache);
				}
				if (_audioLauncherCache != null)
				{
					return _audioLauncherCache.LocalBoat == this;
				}
				return false;
			}
		}

		public float BaseMass => _baseMass;

		public event Action StatusFlagsChanged;

		public event Action RespawnedVisual;

		public event Action<Vector3, DeathCause> Died;

		public event Action<NetworkBoat> OnCheckpointRecorded;

		public event Action<NetworkBoat> OnDeathRecorded;

		public void Teleport(Vector3 position, Quaternion rotation)
		{
			if (base.IsServerInitialized)
			{
				if (_netTransform == null)
				{
					_netTransform = GetComponent<NetworkTransform>();
				}
				if (_netTransform != null)
				{
					_netTransform.Teleport();
				}
				Rigidbody.position = position;
				Rigidbody.rotation = rotation;
				base.transform.SetPositionAndRotation(position, rotation);
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetAllRegistry()
		{
			All.Clear();
		}

		public static NetworkBoat FindByIndex(int boatIndex)
		{
			for (int num = All.Count - 1; num >= 0; num--)
			{
				NetworkBoat networkBoat = All[num];
				if (networkBoat == null)
				{
					All.RemoveAt(num);
				}
				else if (networkBoat.BoatIndex == boatIndex)
				{
					return networkBoat;
				}
			}
			return null;
		}

		public override void Awake()
		{
			NetworkInitialize___Early();
			Awake_UserLogic_PuffPirates_002EBoats_002ENetworkBoat_Assembly_002DCSharp_002Edll();
			NetworkInitialize___Late();
		}

		public override void OnStartNetwork()
		{
			base.OnStartNetwork();
			if (!All.Contains(this))
			{
				All.Add(this);
			}
			if (Rigidbody != null)
			{
				_baseConstraints = Rigidbody.constraints;
			}
			_softWobble = (_softWobbleVelocity = Vector3.zero);
			_softImpactEnvelope = 0f;
			_softImpactHoldRemaining = 0f;
			if (_visual != null)
			{
				_visualBaseRotation = _visual.localRotation;
			}
			BuildHullCollider();
			ApplySyncedVisuals();
		}

		public override void OnStartServer()
		{
			base.OnStartServer();
			CurrentCheckpointId = 0;
			ActiveStatusFlags = BoatStatusFlags.None;
			VisualImpactSequence = 0;
			VisualImpactLocal = Vector3.zero;
			VisualImpactPosition = base.transform.position;
			DeathSequence = 0;
			Deaths = 0;
			DeathPosition = base.transform.position;
			IsRespawning = false;
			IsDead = false;
			_pendingRespawnTimer = TickTimer.None;
			FinishRank = 0;
			SizeId = 2;
			for (int i = 0; i < StatusEndTicks.Length; i++)
			{
				StatusEndTicks[i] = TickTimer.None;
			}
			Lap = 0;
			HeldItem = 0;
			InkTimer = TickTimer.None;
			MiniTimer = TickTimer.None;
			PreMiniSizeId = SizeId;
			SpeedBoostTimer = TickTimer.None;
			BoostPadSfxSequence = 0;
			RandomizeCooldown = TickTimer.None;
			_respawnVelocityLockRemaining = 0;
			_physicsTm = base.TimeManager;
			_physicsTm.OnPostPhysicsSimulation += HandlePostPhysics;
			ApplySyncedVisuals();
		}

		public override void OnStopServer()
		{
			base.OnStopServer();
			if (_physicsTm != null)
			{
				_physicsTm.OnPostPhysicsSimulation -= HandlePostPhysics;
				_physicsTm = null;
			}
		}

		public override void OnStartClient()
		{
			base.OnStartClient();
			ApplySyncedVisuals();
		}

		private void ApplySyncedVisuals()
		{
			ApplySize();
			ApplySkin();
			ApplyCrewCapacity();
			_lastSizeId = SizeId;
			_lastSkinId = SkinId;
			_lastCrewCapacity = CrewCapacity;
			_lastCheckpointId = CurrentCheckpointId;
			_lastDeathSequence = DeathSequence;
			_lastSpeedBoostTimer = SpeedBoostTimer;
			_lastStatusFlags = ActiveStatusFlags;
			_lastImpactSequence = VisualImpactSequence;
		}

		private void HandlePostPhysics(float delta)
		{
			OnAfterPhysicsSimulate();
		}

		private void SetStatusEnd(int slot, TickTimer timer)
		{
			if ((uint)slot < (uint)StatusEndTicks.Length)
			{
				StatusEndTicks[slot] = timer;
			}
		}

		public override void OnStopNetwork()
		{
			base.OnStopNetwork();
			if (_deadStateApplied)
			{
				ApplyDeadState(dead: false);
			}
			if (base.IsServerInitialized && IsDead && Rigidbody != null)
			{
				Rigidbody.isKinematic = _wasKinematicBeforeDeath;
			}
			PiratePlayer[] componentsInChildren = GetComponentsInChildren<PiratePlayer>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				if (componentsInChildren[i] != null && componentsInChildren[i].transform.parent != null)
				{
					componentsInChildren[i].transform.SetParent(null, worldPositionStays: true);
				}
			}
			All.Remove(this);
		}

		private void OnAfterPhysicsSimulate()
		{
			if (_respawnVelocityLockRemaining > 0)
			{
				_respawnVelocityLockRemaining--;
				Rigidbody rigidbody = Rigidbody;
				if (!(rigidbody == null) && !rigidbody.isKinematic)
				{
					rigidbody.linearVelocity = Vector3.zero;
					rigidbody.angularVelocity = Vector3.zero;
				}
			}
		}

		protected override void TimeManager_OnTick()
		{
			if (!base.IsServerInitialized)
			{
				return;
			}
			if (IsDead)
			{
				if (_pendingRespawnTimer.Expired(base.TimeManager))
				{
					CompleteRespawn(_pendingRespawnPosition, _pendingRespawnRotation, _pendingRespawnInvuln);
				}
				return;
			}
			RaceState instance = RaceState.Instance;
			ApplyRotationLocks(instance);
			SweepExpiredStatus();
			if (MiniTimer.Expired(base.TimeManager))
			{
				MiniTimer = TickTimer.None;
				SetSize(PreMiniSizeId);
			}
		}

		private void ApplyRotationLocks(RaceState race)
		{
			if (!(race == null) && !(Rigidbody == null))
			{
				RigidbodyConstraints rigidbodyConstraints = _baseConstraints & (RigidbodyConstraints)(-113);
				if (race.LockRotX)
				{
					rigidbodyConstraints |= RigidbodyConstraints.FreezeRotationX;
				}
				if (race.LockRotY)
				{
					rigidbodyConstraints |= RigidbodyConstraints.FreezeRotationY;
				}
				Rigidbody rigidbody = Rigidbody;
				if (rigidbody.constraints != rigidbodyConstraints)
				{
					rigidbody.constraints = rigidbodyConstraints;
					rigidbody.angularVelocity = Vector3.zero;
				}
			}
		}

		public void SetSize(int sizeId)
		{
			if (base.IsServerInitialized)
			{
				SizeId = Mathf.Clamp(sizeId, 0, BoatSizes.Count - 1);
				ApplySize();
			}
		}

		public void SetSkin(int skinId)
		{
			if (base.IsServerInitialized)
			{
				SkinId = CosmeticCatalog.Clamp(CosmeticSlot.BoatSkin, skinId);
			}
		}

		private void ApplySkin()
		{
			if (_visual == null)
			{
				return;
			}
			Color color = CosmeticCatalog.BoatSkinColor(SkinId);
			Renderer[] componentsInChildren = _visual.GetComponentsInChildren<Renderer>(includeInactive: true);
			foreach (Renderer renderer in componentsInChildren)
			{
				if (!(renderer is ParticleSystemRenderer) && !(renderer is LineRenderer))
				{
					RendererColorUtility.SetColor(renderer, color);
				}
			}
		}

		private void ApplySize()
		{
			BoatSizeDef boatSizeDef = BoatSizes.Get(SizeId);
			base.transform.localScale = Vector3.one * Mathf.Max(0.01f, boatSizeDef.Diameter);
			_baseMass = boatSizeDef.Mass;
			if (Rigidbody != null && base.IsServerInitialized)
			{
				Rigidbody.mass = boatSizeDef.Mass;
			}
			SyncVisualAndHullPose();
		}

		private void SyncVisualAndHullPose()
		{
			if (!(_visual == null))
			{
				if (!_visualBaseCaptured)
				{
					_visualBaseScale = _visual.localScale;
					_visualBasePosition = _visual.localPosition;
					_visualBaseRotation = _visual.localRotation;
					_visualBaseCaptured = true;
				}
				if (_hullTransform != null)
				{
					_hullTransform.localScale = _visualBaseScale;
					_hullTransform.localPosition = _visualBasePosition;
					_hullTransform.localRotation = _visualBaseRotation;
				}
			}
		}

		private void BuildHullCollider()
		{
			if (_hullBuilt)
			{
				return;
			}
			bool flag = _visual != null;
			Mesh mesh = (((_hullColliderMesh != null) & flag) ? _hullColliderMesh : ((!flag) ? null : _visual.GetComponentInChildren<MeshFilter>(includeInactive: true)?.sharedMesh));
			bool flag2 = mesh != null && mesh.isReadable;
			if ((flag2 && _buildHullCollider) & flag)
			{
				_hullBuilt = true;
				GameObject gameObject = new GameObject("~HullCollider");
				gameObject.transform.SetParent(base.transform, worldPositionStays: false);
				gameObject.transform.localPosition = _visualBasePosition;
				gameObject.transform.localRotation = _visualBaseRotation;
				gameObject.transform.localScale = _visualBaseScale;
				_hullTransform = gameObject.transform;
				Transform transform = gameObject.transform;
				MeshFilter componentInChildren = _visual.GetComponentInChildren<MeshFilter>(includeInactive: true);
				if (componentInChildren != null && componentInChildren.transform != _visual)
				{
					GameObject obj = new GameObject("~HullMesh");
					obj.transform.SetParent(gameObject.transform, worldPositionStays: false);
					obj.transform.localPosition = componentInChildren.transform.localPosition;
					obj.transform.localRotation = componentInChildren.transform.localRotation;
					obj.transform.localScale = componentInChildren.transform.localScale;
					transform = obj.transform;
				}
				MeshCollider meshCollider = transform.gameObject.AddComponent<MeshCollider>();
				meshCollider.sharedMesh = mesh;
				meshCollider.convex = true;
				BoxCollider component = GetComponent<BoxCollider>();
				PhysicsMaterial physicsMaterial = component?.sharedMaterial;
				if (component != null)
				{
					UnityEngine.Object.Destroy(component);
				}
				if (physicsMaterial == null)
				{
					physicsMaterial = new PhysicsMaterial
					{
						dynamicFriction = 0.15f,
						staticFriction = 0.15f,
						bounciness = 0.1f
					};
				}
				meshCollider.sharedMaterial = physicsMaterial;
			}
			else if (GetComponentInChildren<MeshCollider>(includeInactive: true) != null)
			{
				_hullBuilt = true;
			}
			else
			{
				BoxCollider boxCollider = GetComponent<BoxCollider>();
				if (boxCollider == null)
				{
					boxCollider = base.gameObject.AddComponent<BoxCollider>();
				}
				boxCollider.isTrigger = false;
				Vector3 localScale = base.transform.localScale;
				boxCollider.size = new Vector3(Mathf.Abs(localScale.x) * 4f, Mathf.Abs(localScale.y) * 4f, Mathf.Abs(localScale.z) * 4f);
				boxCollider.center = Vector3.zero;
				if (boxCollider.sharedMaterial == null)
				{
					PhysicsMaterial sharedMaterial = new PhysicsMaterial
					{
						dynamicFriction = 0.15f,
						staticFriction = 0.15f,
						bounciness = 0.1f
					};
					boxCollider.sharedMaterial = sharedMaterial;
				}
				if (_buildHullCollider && !flag)
				{
					Debug.LogWarning("[NetworkBoat] _visual boş → BoxCollider fallback kullanılıyor. _visual referansını kontrol edin.", this);
				}
				else if (_buildHullCollider && mesh != null && !flag2)
				{
					Debug.LogWarning("[NetworkBoat] Hull mesh Read/Write KAPALI → basit BoxCollider kullanılıyor. FBX importer'da 'Read/Write Enabled' açılmalı; convex hull çarpışmaz.", this);
				}
				_hullBuilt = true;
			}
		}

		private void ApplyWaterVisual()
		{
			if (_lastImpactSequence != VisualImpactSequence)
			{
				_lastImpactSequence = VisualImpactSequence;
				Vector3 visualImpactLocal = VisualImpactLocal;
				float num = Mathf.Clamp01(visualImpactLocal.magnitude);
				if (num > 0.001f)
				{
					if (IsLocalBoat)
					{
						BoatCameraRig.Shake(Mathf.Lerp(0.25f, 0.7f, num));
						HapticManager.Impact(Mathf.Lerp(0.2f, 0.7f, num));
					}
					ParticleManager.PlayAt(CommonParticle.CollisionParticle, VisualImpactPosition);
					if (_visual != null)
					{
						Vector3 vector = visualImpactLocal / num;
						_softImpactEnvelope = Mathf.Max(_softImpactEnvelope, num);
						_softImpactHoldRemaining = _softImpactHoldSeconds;
						Vector3 vector2 = new Vector3(0f - vector.z, vector.x, 0f) * num;
						_softWobble += vector2 * 0.1f;
						_softWobbleVelocity += vector2 * _softImpactKick;
					}
				}
			}
			if (!(_visual == null))
			{
				UpdateSoftBody();
				float num2 = Time.time * _waterVisualFrequency + (float)BoatIndex * 1.731f;
				float num3 = Mathf.Sin(num2);
				float num4 = Mathf.Sin(num2 * 0.83f + 1.2f);
				Vector3 direction = ((Rigidbody != null) ? Rigidbody.linearVelocity : Vector3.zero);
				Vector3 vector3 = base.transform.InverseTransformDirection(direction);
				float num5 = num3 * _waterVisualBob + Mathf.Clamp(direction.y * 0.008f, -0.025f, 0.025f);
				float num6 = Mathf.Clamp((0f - vector3.z) * 0.035f + num4 * _waterVisualPitch, 0f - _waterVisualPitch, _waterVisualPitch);
				float num7 = Mathf.Clamp(vector3.x * 0.035f + num3 * _waterVisualRoll, 0f - _waterVisualRoll, _waterVisualRoll);
				float num8 = Mathf.Clamp(num3 * _waterVisualSquash - direction.y * 0.002f, 0f - _waterVisualSquash, _waterVisualSquash);
				float num9 = Mathf.Clamp(_softWobble.x, -1f, 1f) * _softMaxTilt;
				float num10 = Mathf.Clamp(_softWobble.y, -1f, 1f) * _softMaxTilt;
				Vector3 b = _visualBasePosition + Vector3.up * num5;
				Quaternion b2 = _visualBaseRotation * Quaternion.Euler(num6 + num9, 0f, num7 + num10);
				Vector3 b3 = Vector3.Scale(_visualBaseScale, new Vector3(1f + num8 * 0.5f, 1f - num8, 1f + num8 * 0.5f));
				float num11 = _waterVisualLerp + _softImpactEnvelope * 8f;
				float t = 1f - Mathf.Exp((0f - num11) * Time.deltaTime);
				_visual.localPosition = Vector3.Lerp(_visual.localPosition, b, t);
				_visual.localRotation = Quaternion.Slerp(_visual.localRotation, b2, t);
				_visual.localScale = Vector3.Lerp(_visual.localScale, b3, t);
			}
		}

		public void ResetProgress()
		{
			if (base.IsServerInitialized)
			{
				CurrentCheckpointId = 0;
				Lap = 0;
			}
		}

		public void RestoreCheckpoint(int checkpointId)
		{
			if (base.IsServerInitialized)
			{
				CurrentCheckpointId = Mathf.Max(0, checkpointId);
			}
		}

		public void RestoreDeaths(int deaths)
		{
			if (base.IsServerInitialized)
			{
				Deaths = Mathf.Max(0, deaths);
			}
		}

		public void SetCrewCapacity(int capacity)
		{
			if (base.IsServerInitialized)
			{
				CrewCapacity = Mathf.Clamp(capacity, 2, 4);
			}
		}

		private void ApplyCrewCapacity()
		{
			if (!(_hullFilter == null) && _capacityMeshes != null && CrewCapacity > 0)
			{
				int num = Mathf.Clamp(CrewCapacity, 2, 4) - 2;
				if (num < _capacityMeshes.Length && _capacityMeshes[num] != null)
				{
					_hullFilter.sharedMesh = _capacityMeshes[num];
				}
				if (_capacityHullEuler != null && num < _capacityHullEuler.Length)
				{
					_hullFilter.transform.localRotation = Quaternion.Euler(_capacityHullEuler[num]);
				}
			}
		}

		public void SetCheckpoint(int checkpointId)
		{
			if (base.IsServerInitialized && checkpointId > CurrentCheckpointId)
			{
				CurrentCheckpointId = checkpointId;
			}
		}

		public void PassCheckpoint(int checkpointId, int checkpointCount, bool looping)
		{
			if (!base.IsServerInitialized)
			{
				return;
			}
			if (!looping)
			{
				if (checkpointId > CurrentCheckpointId)
				{
					CurrentCheckpointId = checkpointId;
					OnCheckpointRecorded?.Invoke(this);
				}
			}
			else
			{
				if (checkpointCount <= 0)
				{
					return;
				}
				int num = (CurrentCheckpointId + 1) % checkpointCount;
				if (checkpointId == num)
				{
					CurrentCheckpointId = checkpointId;
					if (checkpointId == 0)
					{
						Lap++;
					}
				}
			}
		}

		public void SetHeldItem(int item)
		{
			if (base.IsServerInitialized)
			{
				HeldItem = Mathf.Clamp(item, 0, 6);
			}
		}

		public void ApplyInk(float seconds)
		{
			if (base.IsServerInitialized && !IsInvulnerable)
			{
				InkTimer = TickTimer.CreateFromSeconds(base.TimeManager, seconds);
			}
		}

		public void ApplyMini(float seconds, int miniSizeId)
		{
			if (base.IsServerInitialized && !IsInvulnerable && !MiniTimer.IsRunning)
			{
				PreMiniSizeId = SizeId;
				MiniTimer = TickTimer.CreateFromSeconds(base.TimeManager, seconds);
				SetSize(Mathf.Clamp(miniSizeId, 0, BoatSizes.Count - 1));
			}
		}

		public void ApplySpeedBoost(float seconds)
		{
			if (base.IsServerInitialized)
			{
				SpeedBoostTimer = TickTimer.CreateFromSeconds(base.TimeManager, seconds);
			}
		}

		public void PokeBoostPadSfx()
		{
			if (base.IsServerInitialized)
			{
				BoostPadSfxSequence++;
			}
		}

		public void ApplyStatus(BoatStatusSlot slot, BoatStatusFlags flag, float seconds)
		{
			if (base.IsServerInitialized && (uint)slot < 10u && (!IsInvulnerable || flag == BoatStatusFlags.Invulnerable))
			{
				ActiveStatusFlags |= flag;
				SetStatusEnd((int)slot, TickTimer.CreateFromSeconds(base.TimeManager, seconds));
			}
		}

		public void ApplyPickupEffect(PickupEffectType type)
		{
			if (base.IsServerInitialized)
			{
				PickupDef pickupDef = PickupCatalog.Get(type);
				if (type == PickupEffectType.SpeedBoost)
				{
					ApplySpeedBoost(pickupDef.Seconds);
				}
				else if (pickupDef.Slot != BoatStatusSlot.Count)
				{
					ApplyStatus(pickupDef.Slot, pickupDef.Flag, pickupDef.Seconds);
				}
			}
		}

		public bool HasStatus(BoatStatusFlags flag)
		{
			return (ActiveStatusFlags & flag) != 0;
		}

		public void SetFinished(int rank)
		{
			if (base.IsServerInitialized && FinishRank == 0)
			{
				FinishRank = rank;
			}
		}

		public void RespawnAt(Vector3 position, Quaternion rotation, float invulnSeconds, DeathCause cause)
		{
			if (!base.IsServerInitialized)
			{
				return;
			}
			if (cause == DeathCause.None)
			{
				CompleteRespawn(position, rotation, invulnSeconds);
			}
			else if (!IsDead)
			{
				DeathPosition = base.transform.position;
				LastDeathCause = cause;
				DeathSequence++;
				Deaths++;
				OnDeathRecorded?.Invoke(this);
				if (_respawnDelaySec <= 0f)
				{
					CompleteRespawn(position, rotation, invulnSeconds);
					return;
				}
				IsDead = true;
				IsRespawning = true;
				_pendingRespawnPosition = position;
				_pendingRespawnRotation = rotation;
				_pendingRespawnInvuln = invulnSeconds;
				_pendingRespawnTimer = TickTimer.CreateFromSeconds(base.TimeManager, _respawnDelaySec);
				Rigidbody rigidbody = Rigidbody;
				rigidbody.linearVelocity = Vector3.zero;
				rigidbody.angularVelocity = Vector3.zero;
				_wasKinematicBeforeDeath = rigidbody.isKinematic;
				rigidbody.isKinematic = true;
				ResetCrewGas();
				ApplyDeadState(dead: true);
			}
		}

		private void CompleteRespawn(Vector3 position, Quaternion rotation, float invulnSeconds)
		{
			bool isDead = IsDead;
			if (isDead)
			{
				IsDead = false;
				_pendingRespawnTimer = TickTimer.None;
				Rigidbody.isKinematic = _wasKinematicBeforeDeath;
			}
			bool flag = RandomizeCooldown.ExpiredOrNotRunning(base.TimeManager);
			if (flag)
			{
				RandomizeCooldown = TickTimer.CreateFromSeconds(base.TimeManager, 3f);
				rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
			}
			Teleport(position, rotation);
			Rigidbody.linearVelocity = Vector3.zero;
			Rigidbody.angularVelocity = Vector3.zero;
			_respawnVelocityLockRemaining = 2;
			ActiveStatusFlags = BoatStatusFlags.None;
			for (int i = 0; i < StatusEndTicks.Length; i++)
			{
				SetStatusEnd(i, TickTimer.None);
			}
			IsRespawning = true;
			ActiveStatusFlags |= BoatStatusFlags.Invulnerable;
			SetStatusEnd(4, TickTimer.CreateFromSeconds(base.TimeManager, invulnSeconds));
			SpeedBoostTimer = TickTimer.None;
			InkTimer = TickTimer.None;
			if (MiniTimer.IsRunning)
			{
				MiniTimer = TickTimer.None;
				SetSize(PreMiniSizeId);
			}
			ResetCrewGas();
			if (isDead)
			{
				ApplyDeadState(dead: false);
			}
			RpcRespawnedFx();
			if (_audioLauncherCache == null)
			{
				this.TryGetSystem<RaceLauncherSystem, CoreSystemHandler>(out _audioLauncherCache);
			}
			if (_audioLauncherCache != null)
			{
				_audioLauncherCache.ReshuffleBoatSeats(BoatIndex, flag);
			}
		}

		private void ResetCrewGas()
		{
			List<PiratePlayer> active = PiratePlayer.Active;
			for (int i = 0; i < active.Count; i++)
			{
				PiratePlayer piratePlayer = active[i];
				if (piratePlayer != null && piratePlayer.BoatIndex == BoatIndex && piratePlayer.Gas != null)
				{
					piratePlayer.Gas.ResetForRespawn();
				}
			}
		}

		private void ApplyDeadState(bool dead)
		{
			_deadStateApplied = dead;
			_deadUnhidePending = false;
			if (dead)
			{
				GetComponentsInChildren(includeInactive: false, _deadHiddenRenderers);
				for (int num = _deadHiddenRenderers.Count - 1; num >= 0; num--)
				{
					if (_deadHiddenRenderers[num].enabled)
					{
						_deadHiddenRenderers[num].enabled = false;
					}
					else
					{
						_deadHiddenRenderers.RemoveAt(num);
					}
				}
				GetComponentsInChildren(includeInactive: false, _deadDisabledColliders);
				for (int num2 = _deadDisabledColliders.Count - 1; num2 >= 0; num2--)
				{
					if (_deadDisabledColliders[num2].enabled)
					{
						_deadDisabledColliders[num2].enabled = false;
					}
					else
					{
						_deadDisabledColliders.RemoveAt(num2);
					}
				}
				return;
			}
			for (int i = 0; i < _deadHiddenRenderers.Count; i++)
			{
				if (_deadHiddenRenderers[i] != null)
				{
					_deadHiddenRenderers[i].enabled = true;
				}
			}
			for (int j = 0; j < _deadDisabledColliders.Count; j++)
			{
				if (_deadDisabledColliders[j] != null)
				{
					_deadDisabledColliders[j].enabled = true;
				}
			}
			_deadHiddenRenderers.Clear();
			_deadDisabledColliders.Clear();
		}

		private void UpdateDeadState()
		{
			if (IsDead)
			{
				if (!_deadStateApplied)
				{
					ApplyDeadState(dead: true);
				}
				_deadUnhidePending = false;
			}
			else if (_deadStateApplied)
			{
				if (!_deadUnhidePending)
				{
					_deadUnhidePending = true;
					_deadUnhideDeadline = Time.unscaledTime + 0.5f;
				}
				if ((base.transform.position - DeathPosition).sqrMagnitude > 4f || Time.unscaledTime >= _deadUnhideDeadline)
				{
					ApplyDeadState(dead: false);
				}
			}
		}

		private void SweepExpiredStatus()
		{
			BoatStatusFlags boatStatusFlags = ActiveStatusFlags;
			for (int i = 0; i < StatusEndTicks.Length; i++)
			{
				if (StatusEndTicks[i].Expired(base.TimeManager))
				{
					SetStatusEnd(i, TickTimer.None);
					boatStatusFlags &= ~SlotToFlag((BoatStatusSlot)i);
				}
			}
			if (boatStatusFlags != ActiveStatusFlags)
			{
				ActiveStatusFlags = boatStatusFlags;
			}
			if (IsRespawning && !HasStatus(BoatStatusFlags.Invulnerable))
			{
				IsRespawning = false;
			}
		}

		private static BoatStatusFlags SlotToFlag(BoatStatusSlot slot)
		{
			return slot switch
			{
				BoatStatusSlot.Slow => BoatStatusFlags.Slow, 
				BoatStatusSlot.ForcedGas => BoatStatusFlags.ForcedGas, 
				BoatStatusSlot.Electric => BoatStatusFlags.Electric, 
				BoatStatusSlot.Knockback => BoatStatusFlags.Knockback, 
				BoatStatusSlot.Invulnerable => BoatStatusFlags.Invulnerable, 
				BoatStatusSlot.GasBoost => BoatStatusFlags.GasBoost, 
				BoatStatusSlot.GasNerf => BoatStatusFlags.GasNerf, 
				BoatStatusSlot.MassHeavy => BoatStatusFlags.MassHeavy, 
				BoatStatusSlot.MassLight => BoatStatusFlags.MassLight, 
				BoatStatusSlot.Plank => BoatStatusFlags.Plank, 
				_ => BoatStatusFlags.None, 
			};
		}

		protected override void TimeManager_OnUpdate()
		{
			UpdateDeadState();
			if (ActiveStatusFlags != _lastStatusFlags)
			{
				_lastStatusFlags = ActiveStatusFlags;
				StatusFlagsChanged?.Invoke();
			}
			if (SizeId != _lastSizeId)
			{
				_lastSizeId = SizeId;
				ApplySize();
			}
			if (CrewCapacity != _lastCrewCapacity)
			{
				_lastCrewCapacity = CrewCapacity;
				ApplyCrewCapacity();
			}
			if (SkinId != _lastSkinId)
			{
				_lastSkinId = SkinId;
				ApplySkin();
			}
			if (CurrentCheckpointId != _lastCheckpointId)
			{
				_lastCheckpointId = CurrentCheckpointId;
				if (IsLocalBoat)
				{
					AudioManager.PlaySound(CommonSound.Checkpoint);
				}
			}
			if (SpeedBoostTimer != _lastSpeedBoostTimer)
			{
				_lastSpeedBoostTimer = SpeedBoostTimer;
				if (SpeedBoostTimer.IsRunning && IsLocalBoat)
				{
					HapticManager.Medium();
					BoatCameraRig.Shake(0.4f);
				}
			}
			if (DeathSequence != _lastDeathSequence)
			{
				_lastDeathSequence = DeathSequence;
				BoatExplosionFx.Play(DeathPosition, CosmeticCatalog.BoatSkinColor(SkinId), _explosionVfxPrefab);
				SoundData soundData = DeathSoundFor(LastDeathCause);
				if (soundData != null)
				{
					Sfx3D.Play(soundData, DeathPosition);
				}
				else
				{
					Sfx3D.Play(CommonSound.BoatCrash, DeathPosition);
				}
				if (IsLocalBoat)
				{
					BoatCameraRig.Shake(0.9f);
					HapticManager.Heavy();
				}
				Died?.Invoke(DeathPosition, LastDeathCause);
			}
			ApplyWaterVisual();
		}

		[ObserversRpc]
		private void RpcRespawnedFx(Channel channel = Channel.Unreliable)
		{
			RpcWriter___Observers_RpcRespawnedFx___526521220(channel);
		}

		public void PlayBounceSfx(Vector3 obstaclePosition)
		{
			if (base.IsServerInitialized)
			{
				RpcBounceSfx(obstaclePosition);
			}
		}

		[ObserversRpc]
		private void RpcBounceSfx(Vector3 obstaclePosition, Channel channel = Channel.Unreliable)
		{
			RpcWriter___Observers_RpcBounceSfx___858605841(obstaclePosition, channel);
		}

		private SoundData DeathSoundFor(DeathCause cause)
		{
			return cause switch
			{
				DeathCause.Wall => _wallDeathSound, 
				DeathCause.Obstacle => _obstacleDeathSound, 
				DeathCause.Void => _voidDeathSound, 
				_ => null, 
			};
		}

		private void OnCollisionEnter(Collision collision)
		{
			if (!base.IsServerInitialized || collision == null)
			{
				return;
			}
			float magnitude = collision.relativeVelocity.magnitude;
			if (!(magnitude < _softImpactThreshold))
			{
				Vector3 vector = collision.impulse;
				if (vector.sqrMagnitude < 0.0001f)
				{
					vector = -collision.relativeVelocity;
				}
				if (!(vector.sqrMagnitude < 0.0001f))
				{
					float t = Mathf.InverseLerp(_softImpactThreshold, _softImpactThreshold + 6f, magnitude);
					float num = Mathf.Lerp(0.35f, 1f, t);
					VisualImpactLocal = base.transform.InverseTransformDirection(vector.normalized) * num;
					VisualImpactPosition = collision.GetContact(0).point;
					VisualImpactSequence++;
				}
			}
		}

		private void UpdateSoftBody()
		{
			float num = Mathf.Min(Time.deltaTime, 0.05f);
			if (!(num <= 0f))
			{
				if (_softImpactHoldRemaining > 0f)
				{
					_softImpactHoldRemaining = Mathf.Max(0f, _softImpactHoldRemaining - num);
				}
				else
				{
					_softImpactEnvelope = Mathf.MoveTowards(_softImpactEnvelope, 0f, num / Mathf.Max(0.1f, _softImpactRecoverSeconds));
				}
				Vector3 vector = -_softWobble * _softSpring - _softWobbleVelocity * _softDamping;
				_softWobbleVelocity += vector * num;
				_softWobble += _softWobbleVelocity * num;
				_softWobble = Vector3.ClampMagnitude(_softWobble, 1f);
				_softWobbleVelocity = Vector3.ClampMagnitude(_softWobbleVelocity, 10f);
				if (_softImpactEnvelope <= 0f && _softWobble.sqrMagnitude < 1E-06f && _softWobbleVelocity.sqrMagnitude < 1E-06f)
				{
					_softWobble = Vector3.zero;
					_softWobbleVelocity = Vector3.zero;
				}
			}
		}

		public override void NetworkInitialize___Early()
		{
			if (!NetworkInitialize___EarlyPuffPirates_002EBoats_002ENetworkBoatAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___EarlyPuffPirates_002EBoats_002ENetworkBoatAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Early();
				_svSpeedBoostTimer.InitializeEarly(this, 22u, isSyncObject: false);
				_svPreMiniSizeId.InitializeEarly(this, 21u, isSyncObject: false);
				_svMiniTimer.InitializeEarly(this, 20u, isSyncObject: false);
				_svInkTimer.InitializeEarly(this, 19u, isSyncObject: false);
				_svHeldItem.InitializeEarly(this, 18u, isSyncObject: false);
				_svLap.InitializeEarly(this, 17u, isSyncObject: false);
				_svOwnerTeam.InitializeEarly(this, 16u, isSyncObject: false);
				_svBoatIndex.InitializeEarly(this, 15u, isSyncObject: false);
				_svSkinId.InitializeEarly(this, 14u, isSyncObject: false);
				_svSizeId.InitializeEarly(this, 13u, isSyncObject: false);
				_svFinishRank.InitializeEarly(this, 12u, isSyncObject: false);
				_svIsDead.InitializeEarly(this, 11u, isSyncObject: false);
				_svIsRespawning.InitializeEarly(this, 10u, isSyncObject: false);
				_svLastDeathCause.InitializeEarly(this, 9u, isSyncObject: false);
				_svDeathPosition.InitializeEarly(this, 8u, isSyncObject: false);
				_svDeathSequence.InitializeEarly(this, 7u, isSyncObject: false);
				_svVisualImpactPosition.InitializeEarly(this, 6u, isSyncObject: false);
				_svVisualImpactLocal.InitializeEarly(this, 5u, isSyncObject: false);
				_svVisualImpactSequence.InitializeEarly(this, 4u, isSyncObject: false);
				_svActiveStatusFlags.InitializeEarly(this, 3u, isSyncObject: false);
				_svCrewCapacity.InitializeEarly(this, 2u, isSyncObject: false);
				_svDeaths.InitializeEarly(this, 1u, isSyncObject: false);
				_svCurrentCheckpointId.InitializeEarly(this, 0u, isSyncObject: false);
				RegisterObserversRpc(0u, RpcReader___Observers_RpcRespawnedFx___526521220);
				RegisterObserversRpc(1u, RpcReader___Observers_RpcBounceSfx___858605841);
			}
		}

		public override void NetworkInitialize___Late()
		{
			if (!NetworkInitialize___LatePuffPirates_002EBoats_002ENetworkBoatAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___LatePuffPirates_002EBoats_002ENetworkBoatAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Late();
				_svSpeedBoostTimer.InitializeLate();
				_svPreMiniSizeId.InitializeLate();
				_svMiniTimer.InitializeLate();
				_svInkTimer.InitializeLate();
				_svHeldItem.InitializeLate();
				_svLap.InitializeLate();
				_svOwnerTeam.InitializeLate();
				_svBoatIndex.InitializeLate();
				_svSkinId.InitializeLate();
				_svSizeId.InitializeLate();
				_svFinishRank.InitializeLate();
				_svIsDead.InitializeLate();
				_svIsRespawning.InitializeLate();
				_svLastDeathCause.InitializeLate();
				_svDeathPosition.InitializeLate();
				_svDeathSequence.InitializeLate();
				_svVisualImpactPosition.InitializeLate();
				_svVisualImpactLocal.InitializeLate();
				_svVisualImpactSequence.InitializeLate();
				_svActiveStatusFlags.InitializeLate();
				_svCrewCapacity.InitializeLate();
				_svDeaths.InitializeLate();
				_svCurrentCheckpointId.InitializeLate();
			}
		}

		public override void NetworkInitializeIfDisabled()
		{
			NetworkInitialize___Early();
			NetworkInitialize___Late();
		}

		private void RpcWriter___Observers_RpcRespawnedFx___526521220(Channel channel = Channel.Unreliable)
		{
			if (!base.IsServerInitialized)
			{
				NetworkManager networkManager = base.NetworkManager;
				networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
				return;
			}
			Channel channel2 = channel;
			PooledWriter pooledWriter = WriterPool.Retrieve();
			SendObserversRpc(0u, pooledWriter, channel2, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			pooledWriter.Store();
		}

		private void RpcLogic___RpcRespawnedFx___526521220(Channel P_0)
		{
			RespawnedVisual?.Invoke();
		}

		private void RpcReader___Observers_RpcRespawnedFx___526521220(PooledReader PooledReader0, Channel channel)
		{
			if (base.IsClientInitialized)
			{
				RpcLogic___RpcRespawnedFx___526521220(channel);
			}
		}

		private void RpcWriter___Observers_RpcBounceSfx___858605841(Vector3 obstaclePosition, Channel channel = Channel.Unreliable)
		{
			if (!base.IsServerInitialized)
			{
				NetworkManager networkManager = base.NetworkManager;
				networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
				return;
			}
			Channel channel2 = channel;
			PooledWriter pooledWriter = WriterPool.Retrieve();
			pooledWriter.WriteVector3(obstaclePosition);
			SendObserversRpc(1u, pooledWriter, channel2, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			pooledWriter.Store();
		}

		private void RpcLogic___RpcBounceSfx___858605841(Vector3 P_0, Channel P_1)
		{
			BounceObstacle.PlaySfxNear(P_0);
		}

		private void RpcReader___Observers_RpcBounceSfx___858605841(PooledReader PooledReader0, Channel channel)
		{
			Vector3 vector = PooledReader0.ReadVector3();
			if (base.IsClientInitialized)
			{
				RpcLogic___RpcBounceSfx___858605841(vector, channel);
			}
		}

		private void Awake_UserLogic_PuffPirates_002EBoats_002ENetworkBoat_Assembly_002DCSharp_002Edll()
		{
			SetTickCallbacks(TickCallback.Tick | TickCallback.Update);
		}
	}
}

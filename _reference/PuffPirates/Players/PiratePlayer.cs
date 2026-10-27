using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Object.Prediction.Delegating;
using FishNet.Object.Synchronizing;
using FishNet.Serializing;
using FishNet.Transporting;
using FishNet.Utility.Template;
using GameKit.Dependencies.Utilities;
using GameKit.Dependencies.Utilities.Types;
using GamesMrkt.Core;
using GamesMrkt.Platforms.Steam;
using GamesMrkt.PuffPirates;
using PuffPirates.Boats;
using PuffPirates.Net;
using PuffPirates.Networking;
using PuffPirates.Systems;
using UnityEngine;

namespace PuffPirates.Players
{
	[DisallowMultipleComponent]
	public class PiratePlayer : TickNetworkBehaviour
	{
		private static readonly List<PiratePlayer> ActivePirates = new List<PiratePlayer>();

		[Header("Crew Blocking")]
		[SerializeField]
		[Range(0f, 45f)]
		private float _crewBlockGapDeg = 12f;

		private float _tickStartAngle;

		private static uint s_snapshotTick = uint.MaxValue;

		[Header("Boundary Lean Torque")]
		[Tooltip("If a pirate leans into the edge of their slot (SectorHalfWidth) and keeps pushing outward, the boat is torqued around Y by the excess — leaning into one side turns the boat that way. Only effective while NetworkBoat's Y rotation is UNLOCKED (race.LockRotY=false); Unity's torque is absorbed when it's locked.")]
		[SerializeField]
		[Min(0f)]
		private float _boundaryLeanTorque = 8000f;

		[SerializeField]
		private float _angularSpeedDeg = 140f;

		[SerializeField]
		private float _heightOffset = 0.55f;

		private NetworkBoat _boat;

		public readonly SyncVar<float> _attachmentAngle = new SyncVar<float>(new SyncTypeSettings(0f));

		public readonly SyncVar<bool> _isStunned = new SyncVar<bool>(new SyncTypeSettings(0f));

		public readonly SyncVar<int> _teamId = new SyncVar<int>(new SyncTypeSettings(0f));

		public readonly SyncVar<int> _boatIndex = new SyncVar<int>(-1, new SyncTypeSettings(0f));

		public readonly SyncVar<string> _playerName = new SyncVar<string>(new SyncTypeSettings(0f));

		public readonly SyncVar<float> _sectorCenter = new SyncVar<float>(new SyncTypeSettings(0f));

		public readonly SyncVar<float> _sectorHalfWidth = new SyncVar<float>(180f, new SyncTypeSettings(0f));

		private TickTimer StunTimer;

		public readonly SyncVar<bool> _isReady = new SyncVar<bool>(new SyncTypeSettings(0f));

		private string _sentName;

		private float _nextNameCheck;

		private const int MaxRepeatTicks = 12;

		private PirateInput _serverInput;

		private uint _serverInputTick;

		private bool _hasServerInput;

		private float _angle;

		private float _prevTickAngle;

		private PlayerInputSystem _inputSystem;

		private ConnectionSystem _connectionSystem;

		private Transform _boatAnchor;

		private float _renderAngle;

		private bool _renderAngleInit;

		private float _seatSnapUntil;

		private Vector3 _lastBoatPos;

		private bool _hasLastBoatPos;

		private float _visualAngle;

		private const float SeatSmoothLerp = 14f;

		private const float SeatSnapWindow = 0.3f;

		private const float SeatTeleportSqr = 64f;

		private ReplicateUserLogicDelegate<PirateInput> _replicateULDelegate___Simulate;

		private ReconcileUserLogicDelegate<PirateSeatState> _reconcileULDelegate___Reconcile;

		private RingBuffer<ReplicateDataContainer<PirateInput>> _replicatesHistory___Simulate;

		private BasicQueue<ReplicateDataContainer<PirateInput>> _replicatesQueue___Simulate;

		private RingBuffer<LocalReconcile<PirateSeatState>> _reconcilesHistory___Reconcile;

		private PirateSeatState _lastReadReconcile___Reconcile;

		private ReplicateDataContainer<PirateInput> _lastReadReplicate___Simulate;

		private bool NetworkInitialize___EarlyPuffPirates_002EPlayers_002EPiratePlayerAssembly_002DCSharp_002Edll_Excuted;

		private bool NetworkInitialize___LatePuffPirates_002EPlayers_002EPiratePlayerAssembly_002DCSharp_002Edll_Excuted;

		public static List<PiratePlayer> Active => ActivePirates;

		public int ClientId => base.OwnerId;

		public static PiratePlayer Local { get; private set; }

		public PirateGas Gas { get; private set; }

		public PirateCosmetics Cosmetics { get; private set; }

		public float AttachmentAngle
		{
			get
			{
				return _attachmentAngle.Value;
			}
			set
			{
				_attachmentAngle.Value = value;
				if (base.IsServerInitialized)
				{
					_angle = (_prevTickAngle = value);
				}
			}
		}

		public bool IsStunned
		{
			get
			{
				return _isStunned.Value;
			}
			set
			{
				_isStunned.Value = value;
			}
		}

		public int TeamId
		{
			get
			{
				return _teamId.Value;
			}
			set
			{
				_teamId.Value = value;
			}
		}

		public int BoatIndex
		{
			get
			{
				return _boatIndex.Value;
			}
			private set
			{
				_boatIndex.Value = value;
			}
		}

		public string PlayerName
		{
			get
			{
				return _playerName.Value;
			}
			set
			{
				_playerName.Value = value;
			}
		}

		public float SectorCenter
		{
			get
			{
				return _sectorCenter.Value;
			}
			private set
			{
				_sectorCenter.Value = value;
			}
		}

		public float SectorHalfWidth
		{
			get
			{
				return _sectorHalfWidth.Value;
			}
			private set
			{
				_sectorHalfWidth.Value = value;
			}
		}

		public bool IsReady
		{
			get
			{
				return _isReady.Value;
			}
			private set
			{
				_isReady.Value = value;
			}
		}

		public NetworkBoat Boat
		{
			get
			{
				if (_boat == null || _boat.BoatIndex != BoatIndex)
				{
					_boat = NetworkBoat.FindByIndex(BoatIndex);
				}
				return _boat;
			}
		}

		public float VisualAngle => _visualAngle;

		public static bool TryGet(int clientId, out PiratePlayer pirate)
		{
			for (int i = 0; i < ActivePirates.Count; i++)
			{
				if (ActivePirates[i].ClientId == clientId)
				{
					pirate = ActivePirates[i];
					return true;
				}
			}
			pirate = null;
			return false;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetActivePirates()
		{
			ActivePirates.Clear();
			Local = null;
		}

		[ServerRpc]
		public void RpcSetName(string name)
		{
			RpcWriter___Server_RpcSetName___3615296227(name);
		}

		[ServerRpc]
		public void RpcAnnounceLeaving()
		{
			RpcWriter___Server_RpcAnnounceLeaving___2166136261();
		}

		public void SetSector(float centerDeg, float halfWidthDeg)
		{
			if (base.IsServerInitialized)
			{
				SectorCenter = Mathf.Repeat(centerDeg, 360f);
				SectorHalfWidth = Mathf.Clamp(halfWidthDeg, 5f, 180f);
			}
		}

		[ServerRpc]
		public void RpcSetReady(bool ready)
		{
			RpcWriter___Server_RpcSetReady___1140765316(ready);
		}

		public void HostSetReady(bool ready)
		{
			if (base.IsServerInitialized)
			{
				IsReady = ready;
			}
		}

		[ServerRpc]
		public void RpcRequestTeam(int team)
		{
			RpcWriter___Server_RpcRequestTeam___3316948804(team);
		}

		public void SetTeam(int team)
		{
			if (base.IsServerInitialized)
			{
				TeamId = team;
			}
		}

		public void SetBoatIndex(int index)
		{
			if (base.IsServerInitialized)
			{
				BoatIndex = index;
			}
		}

		public override void Awake()
		{
			NetworkInitialize___Early();
			Awake_UserLogic_PuffPirates_002EPlayers_002EPiratePlayer_Assembly_002DCSharp_002Edll();
			NetworkInitialize___Late();
		}

		public override void OnStartNetwork()
		{
			base.OnStartNetwork();
			Gas = GetComponent<PirateGas>();
			Cosmetics = GetComponent<PirateCosmetics>();
			if (!ActivePirates.Contains(this))
			{
				int i;
				for (i = 0; i < ActivePirates.Count && ActivePirates[i].ClientId < ClientId; i++)
				{
				}
				ActivePirates.Insert(i, this);
			}
		}

		public override void OnStartServer()
		{
			base.OnStartServer();
			BoatIndex = -1;
			SectorCenter = 0f;
			SectorHalfWidth = 180f;
			StunTimer = TickTimer.None;
			_hasServerInput = false;
			_angle = (_prevTickAngle = AttachmentAngle);
		}

		public override void OnStartClient()
		{
			base.OnStartClient();
			_angle = (_prevTickAngle = AttachmentAngle);
			if (base.IsOwner)
			{
				Local = this;
				_sentName = null;
				SendNameIfChanged();
			}
		}

		private string ResolveLocalName()
		{
			string text = null;
			if (this.TryGetSystem<SteamSystem, CoreSystemHandler>(out var system))
			{
				text = system.PersonaName;
			}
			if (string.IsNullOrEmpty(text))
			{
				text = $"Player {base.OwnerId}";
			}
			if (text.Length > 16)
			{
				text = (char.IsHighSurrogate(text[15]) ? text.Substring(0, 15) : text.Substring(0, 16));
			}
			return text;
		}

		private void SendNameIfChanged()
		{
			string text = ResolveLocalName();
			if (!(text == _sentName))
			{
				_sentName = text;
				RpcSetName(text);
			}
		}

		public override void OnStopNetwork()
		{
			base.OnStopNetwork();
			if (base.transform.parent != null)
			{
				base.transform.SetParent(null, worldPositionStays: true);
			}
			ActivePirates.Remove(this);
			if (Local == this)
			{
				Local = null;
			}
			_boat = null;
		}

		public bool TryGetServerInput(out PirateInput input)
		{
			input = _serverInput;
			if (_hasServerInput)
			{
				return base.TimeManager.Tick - _serverInputTick <= 12;
			}
			return false;
		}

		protected override void TimeManager_OnTick()
		{
			SnapshotTickStartAngle();
			if (base.IsServerInitialized && IsStunned && StunTimer.Expired(base.TimeManager))
			{
				StunTimer = TickTimer.None;
				IsStunned = false;
			}
			Simulate(base.IsOwner ? BuildOwnerInput() : default(PirateInput));
		}

		protected override void TimeManager_OnPostTick()
		{
			CreateReconcile();
		}

		public override void CreateReconcile()
		{
			Reconcile(new PirateSeatState
			{
				Angle = _angle
			});
		}

		private PirateInput BuildOwnerInput()
		{
			if (_inputSystem == null)
			{
				this.TryGetSystem<PlayerInputSystem, CoreSystemHandler>(out _inputSystem);
			}
			if (_connectionSystem == null)
			{
				this.TryGetSystem<ConnectionSystem, CoreSystemHandler>(out _connectionSystem);
			}
			if (_inputSystem == null)
			{
				return default;
			}
			_inputSystem.IsRacing = _connectionSystem != null && _connectionSystem.State == LauncherState.Racing;
			return _inputSystem.BuildInput();
		}

		[Replicate]
		private void Simulate(PirateInput input, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
		{
			Replicate_Current(_replicateULDelegate___Simulate, 0u, _replicatesQueue___Simulate, _replicatesHistory___Simulate, new ReplicateDataContainer<PirateInput>(input, channel));
		}

		[Reconcile]
		private void Reconcile(PirateSeatState state, Channel channel = Channel.Unreliable)
		{
			Reconcile_Current(0u, ref _lastReadReconcile___Reconcile, _reconcilesHistory___Reconcile, state, channel);
		}

		private float ApplySectorLimit(float current, float delta, RaceState race, out float excess)
		{
			excess = 0f;
			if (!(race != null) || !race.SectorLock || !(SectorHalfWidth < 179.9f))
			{
				return Mathf.Repeat(current + delta, 360f);
			}
			float num = Mathf.DeltaAngle(SectorCenter, current) + delta;
			float num2 = Mathf.Clamp(num, 0f - SectorHalfWidth, SectorHalfWidth);
			excess = num - num2;
			return Mathf.Repeat(SectorCenter + num2, 360f);
		}

		private void ApplyBoundaryLeanTorque(float excess, RaceState race)
		{
			if (!(race == null) && race.BoundaryLeanEnabled && !race.LockRotY && !Mathf.Approximately(excess, 0f))
			{
				NetworkBoat boat = Boat;
				Rigidbody rigidbody = ((boat != null) ? boat.Rigidbody : null);
				if (!(rigidbody == null))
				{
					rigidbody.AddTorque(Vector3.up * (excess * (MathF.PI / 180f) * _boundaryLeanTorque), ForceMode.Force);
				}
			}
		}

		private void SnapshotTickStartAngle()
		{
			uint tick = base.TimeManager.Tick;
			if (tick == s_snapshotTick)
			{
				return;
			}
			s_snapshotTick = tick;
			for (int i = 0; i < ActivePirates.Count; i++)
			{
				PiratePlayer piratePlayer = ActivePirates[i];
				if (piratePlayer != null)
				{
					piratePlayer._tickStartAngle = ((piratePlayer == this && base.IsOwner) ? _angle : piratePlayer.AttachmentAngle);
				}
			}
		}

		private float ApplyCrewBlocking(float current, float sectorLimitedTarget, float requestedDelta, RaceState race)
		{
			if (race == null || !race.CrewBlocking || BoatIndex < 0)
			{
				return sectorLimitedTarget;
			}
			float num = Mathf.Sign(requestedDelta);
			if (Mathf.Approximately(num, 0f))
			{
				return sectorLimitedTarget;
			}
			float num2 = ((!race.SectorLock || !(SectorHalfWidth < 179.9f)) ? Mathf.Min(Mathf.Abs(requestedDelta), 360f) : ((num > 0f) ? Mathf.Repeat(sectorLimitedTarget - current, 360f) : Mathf.Repeat(current - sectorLimitedTarget, 360f)));
			if (num2 <= 0.0001f)
			{
				return sectorLimitedTarget;
			}
			float num3 = 360f;
			for (int i = 0; i < ActivePirates.Count; i++)
			{
				PiratePlayer piratePlayer = ActivePirates[i];
				if (!(piratePlayer == null) && !(piratePlayer == this) && piratePlayer.BoatIndex == BoatIndex)
				{
					float num4 = ((num > 0f) ? Mathf.Repeat(piratePlayer._tickStartAngle - current, 360f) : Mathf.Repeat(current - piratePlayer._tickStartAngle, 360f));
					if (num4 > 0.001f && num4 < num3)
					{
						num3 = num4;
					}
				}
			}
			float num5 = Mathf.Max(0f, num3 - _crewBlockGapDeg);
			if (num2 <= num5)
			{
				return sectorLimitedTarget;
			}
			return Mathf.Repeat(current + num * num5, 360f);
		}

		public void Stun(float seconds)
		{
			if (base.IsServerInitialized)
			{
				IsStunned = true;
				Gas.ForceStopGassing();
				StunTimer = TickTimer.CreateFromSeconds(base.TimeManager, seconds);
			}
		}

		public Vector3 GetAttachWorldPos(Transform boat, float radius)
		{
			float f = AttachmentAngle * (MathF.PI / 180f);
			Vector3 vector = new Vector3(Mathf.Sin(f), 0f, Mathf.Cos(f)) * radius;
			return boat.position + boat.rotation * vector;
		}

		public Vector3 GetOutwardDir(Transform boat)
		{
			float f = AttachmentAngle * (MathF.PI / 180f);
			Vector3 vector = new Vector3(Mathf.Sin(f), 0f, Mathf.Cos(f));
			Vector3 vector2 = boat.rotation * vector;
			vector2.y = 0f;
			if (!(vector2.sqrMagnitude > 0.0001f))
			{
				return Vector3.forward;
			}
			return vector2.normalized;
		}

		protected override void TimeManager_OnUpdate()
		{
			if (base.IsOwner && Time.unscaledTime >= _nextNameCheck)
			{
				_nextNameCheck = Time.unscaledTime + 1f;
				SendNameIfChanged();
			}
			NetworkBoat boat = Boat;
			if (boat == null)
			{
				if (base.transform.parent != null)
				{
					base.transform.SetParent(null, worldPositionStays: true);
				}
				if (RaceState.Instance != null && RaceState.Instance.InLobby)
				{
					int seat = Mathf.Max(0, ActivePirates.IndexOf(this));
					base.transform.position = LobbyPreviewSeats.WorldPosition(seat);
					base.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
				}
				return;
			}
			Transform boatAnchor = GetBoatAnchor(boat.transform);
			if (base.transform.parent != boatAnchor)
			{
				base.transform.SetParent(boatAnchor, worldPositionStays: false);
			}
			float attachmentRadius = boat.AttachmentRadius;
			float num = (0f - BoatSizes.Get(boat.SizeId).Height) * 0.5f + _heightOffset;
			Vector3 position = boat.transform.position;
			if (_hasLastBoatPos && (position - _lastBoatPos).sqrMagnitude > 64f)
			{
				_seatSnapUntil = Time.unscaledTime + 0.3f;
			}
			_lastBoatPos = position;
			_hasLastBoatPos = true;
			float num2 = (base.IsOwner ? Mathf.LerpAngle(_prevTickAngle, _angle, (float)base.TimeManager.GetTickPercentAsDouble()) : AttachmentAngle);
			float num3;
			if (base.IsOwner)
			{
				num3 = num2;
				_renderAngle = num2;
				_renderAngleInit = true;
			}
			else if (!_renderAngleInit || Time.unscaledTime < _seatSnapUntil)
			{
				num3 = (_renderAngle = num2);
				_renderAngleInit = true;
			}
			else
			{
				_renderAngle = Mathf.LerpAngle(_renderAngle, num2, 1f - Mathf.Exp(-14f * Time.deltaTime));
				num3 = _renderAngle;
			}
			_visualAngle = num3;
			float f = num3 * (MathF.PI / 180f);
			Vector3 vector = new Vector3(Mathf.Sin(f), 0f, Mathf.Cos(f));
			base.transform.localPosition = vector * attachmentRadius + Vector3.up * num;
			base.transform.localRotation = Quaternion.LookRotation(-vector, Vector3.up);
		}

		private Transform GetBoatAnchor(Transform boat)
		{
			if (_boatAnchor == null || _boatAnchor.parent != boat)
			{
				Transform transform = boat.Find("~PirateAnchor");
				if (transform != null)
				{
					_boatAnchor = transform;
				}
				else
				{
					GameObject gameObject = new GameObject("~PirateAnchor");
					_boatAnchor = gameObject.transform;
					_boatAnchor.SetParent(boat, worldPositionStays: false);
					_boatAnchor.localPosition = Vector3.zero;
					_boatAnchor.localRotation = Quaternion.identity;
				}
			}
			Vector3 localScale = boat.localScale;
			_boatAnchor.localScale = new Vector3(1f / Mathf.Max(0.0001f, localScale.x), 1f / Mathf.Max(0.0001f, localScale.y), 1f / Mathf.Max(0.0001f, localScale.z));
			return _boatAnchor;
		}

		public override void NetworkInitialize___Early()
		{
			if (!NetworkInitialize___EarlyPuffPirates_002EPlayers_002EPiratePlayerAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___EarlyPuffPirates_002EPlayers_002EPiratePlayerAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Early();
				_isReady.InitializeEarly(this, 7u, isSyncObject: false);
				_sectorHalfWidth.InitializeEarly(this, 6u, isSyncObject: false);
				_sectorCenter.InitializeEarly(this, 5u, isSyncObject: false);
				_playerName.InitializeEarly(this, 4u, isSyncObject: false);
				_boatIndex.InitializeEarly(this, 3u, isSyncObject: false);
				_teamId.InitializeEarly(this, 2u, isSyncObject: false);
				_isStunned.InitializeEarly(this, 1u, isSyncObject: false);
				_attachmentAngle.InitializeEarly(this, 0u, isSyncObject: false);
				_reconcileULDelegate___Reconcile = Reconcile___UL;
				_replicateULDelegate___Simulate = Simulate___UL;
				_reconcilesHistory___Reconcile = new RingBuffer<LocalReconcile<PirateSeatState>>(60);
				_replicatesHistory___Simulate = new RingBuffer<ReplicateDataContainer<PirateInput>>(60);
				_replicatesQueue___Simulate = new BasicQueue<ReplicateDataContainer<PirateInput>>();
				RegisterReplicateRpc(0u, Reader_Replicate___Simulate);
				RegisterReconcileRpc(0u, Reader_Reconcile___Reconcile);
				RegisterServerRpc(1u, RpcReader___Server_RpcSetName___3615296227);
				RegisterServerRpc(2u, RpcReader___Server_RpcAnnounceLeaving___2166136261);
				RegisterServerRpc(3u, RpcReader___Server_RpcSetReady___1140765316);
				RegisterServerRpc(4u, RpcReader___Server_RpcRequestTeam___3316948804);
			}
		}

		public override void NetworkInitialize___Late()
		{
			if (!NetworkInitialize___LatePuffPirates_002EPlayers_002EPiratePlayerAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___LatePuffPirates_002EPlayers_002EPiratePlayerAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Late();
				_isReady.InitializeLate();
				_sectorHalfWidth.InitializeLate();
				_sectorCenter.InitializeLate();
				_playerName.InitializeLate();
				_boatIndex.InitializeLate();
				_teamId.InitializeLate();
				_isStunned.InitializeLate();
				_attachmentAngle.InitializeLate();
			}
		}

		public override void NetworkInitializeIfDisabled()
		{
			NetworkInitialize___Early();
			NetworkInitialize___Late();
		}

		private void Simulate___UL(PirateInput input, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
		{
			if (state.IsFuture())
			{
				return;
			}
			if (base.IsServerInitialized)
			{
				PirateInput input2;
				if (state.ContainsCreated())
				{
					_serverInput = input;
					_serverInputTick = base.TimeManager.Tick;
					_hasServerInput = true;
				}
				else if (TryGetServerInput(out input2))
				{
					input = input2;
				}
			}
			if (state.ContainsTicked() && !state.ContainsReplayed())
			{
				_prevTickAngle = _angle;
			}
			if (!IsStunned)
			{
				RaceState instance = RaceState.Instance;
				float num = ((instance != null) ? instance.TurnSpeedDeg : _angularSpeedDeg);
				float num2 = input.CircularMovement * num * (float)base.TimeManager.TickDelta;
				float sectorLimitedTarget = ApplySectorLimit(_angle, num2, instance, out var excess);
				_angle = ApplyCrewBlocking(_angle, sectorLimitedTarget, num2, instance);
				if (base.IsServerInitialized)
				{
					_attachmentAngle.Value = _angle;
					ApplyBoundaryLeanTorque(excess, instance);
				}
			}
		}

		private void Reconcile___UL(PirateSeatState state, Channel channel = Channel.Unreliable)
		{
			_angle = state.Angle;
		}

		public override void EmptyReplicatesQueueIntoHistory_Start()
		{
			EmptyReplicatesQueueIntoHistory(_replicatesQueue___Simulate, _replicatesHistory___Simulate);
		}

		public override void Reconcile_Client_Start()
		{
			Reconcile_Client(_reconcileULDelegate___Reconcile, _replicatesHistory___Simulate, _reconcilesHistory___Reconcile, _lastReadReconcile___Reconcile);
		}

		public override void Replicate_Replay_Start(uint replayTick0)
		{
			Replicate_Replay(replayTick0, _replicateULDelegate___Simulate, _replicatesHistory___Simulate);
		}

		public override void ClearReplicateCache()
		{
			base.ClearReplicateCache();
			ClearReplicateCache_Internal(_replicatesQueue___Simulate, _replicatesHistory___Simulate, _reconcilesHistory___Reconcile, ref Unsafe.As<ReplicateDataContainer<PirateInput>, PirateInput>(ref _lastReadReplicate___Simulate), ref _lastReadReconcile___Reconcile);
		}

		private void Reader_Replicate___Simulate(PooledReader PooledReader0, NetworkConnection NetworkConnection1, Channel Channel2)
		{
			Replicate_Reader(0u, PooledReader0, NetworkConnection1, ref _lastReadReplicate___Simulate, _replicatesQueue___Simulate, _replicatesHistory___Simulate, Channel2);
		}

		private void Reader_Reconcile___Reconcile(PooledReader PooledReader0, Channel Channel1)
		{
			Reconcile_Reader_Remote(PooledReader0, ref _lastReadReconcile___Reconcile);
		}

		private void RpcWriter___Server_RpcSetName___3615296227(string name)
		{
			if (!base.IsClientInitialized)
			{
				NetworkManager networkManager = base.NetworkManager;
				networkManager.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
				return;
			}
			if (!base.IsOwner)
			{
				NetworkManager networkManager2 = base.NetworkManager;
				networkManager2.LogWarning("Cannot complete action because you are not the owner of this object. .");
				return;
			}
			Channel channel = Channel.Reliable;
			PooledWriter pooledWriter = WriterPool.Retrieve();
			pooledWriter.WriteString(name);
			SendServerRpc(1u, pooledWriter, channel, DataOrderType.Default);
			pooledWriter.Store();
		}

		public void RpcLogic___RpcSetName___3615296227(string P_0)
		{
			if (P_0 != null && P_0.Length > 16)
			{
				P_0 = (char.IsHighSurrogate(P_0[15]) ? P_0.Substring(0, 15) : P_0.Substring(0, 16));
			}
			PlayerName = P_0;
			if (this.TryGetSystem<ConnectionSystem, CoreSystemHandler>(out var system))
			{
				system.NotePlayerName(base.OwnerId, PlayerName);
			}
		}

		private void RpcReader___Server_RpcSetName___3615296227(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
		{
			string text = PooledReader0.ReadStringAllocated();
			if (base.IsServerInitialized && OwnerMatches(conn))
			{
				RpcLogic___RpcSetName___3615296227(text);
			}
		}

		private void RpcWriter___Server_RpcAnnounceLeaving___2166136261()
		{
			if (!base.IsClientInitialized)
			{
				NetworkManager networkManager = base.NetworkManager;
				networkManager.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
				return;
			}
			if (!base.IsOwner)
			{
				NetworkManager networkManager2 = base.NetworkManager;
				networkManager2.LogWarning("Cannot complete action because you are not the owner of this object. .");
				return;
			}
			Channel channel = Channel.Reliable;
			PooledWriter pooledWriter = WriterPool.Retrieve();
			SendServerRpc(2u, pooledWriter, channel, DataOrderType.Default);
			pooledWriter.Store();
		}

		public void RpcLogic___RpcAnnounceLeaving___2166136261()
		{
			if (this.TryGetSystem<ConnectionSystem, CoreSystemHandler>(out var system))
			{
				system.NotePlayerLeaving(base.OwnerId);
			}
		}

		private void RpcReader___Server_RpcAnnounceLeaving___2166136261(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
		{
			if (base.IsServerInitialized && OwnerMatches(conn))
			{
				RpcLogic___RpcAnnounceLeaving___2166136261();
			}
		}

		private void RpcWriter___Server_RpcSetReady___1140765316(bool ready)
		{
			if (!base.IsClientInitialized)
			{
				NetworkManager networkManager = base.NetworkManager;
				networkManager.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
				return;
			}
			if (!base.IsOwner)
			{
				NetworkManager networkManager2 = base.NetworkManager;
				networkManager2.LogWarning("Cannot complete action because you are not the owner of this object. .");
				return;
			}
			Channel channel = Channel.Reliable;
			PooledWriter pooledWriter = WriterPool.Retrieve();
			pooledWriter.WriteBoolean(ready);
			SendServerRpc(3u, pooledWriter, channel, DataOrderType.Default);
			pooledWriter.Store();
		}

		public void RpcLogic___RpcSetReady___1140765316(bool P_0)
		{
			IsReady = P_0;
		}

		private void RpcReader___Server_RpcSetReady___1140765316(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
		{
			bool flag = PooledReader0.ReadBoolean();
			if (base.IsServerInitialized && OwnerMatches(conn))
			{
				RpcLogic___RpcSetReady___1140765316(flag);
			}
		}

		private void RpcWriter___Server_RpcRequestTeam___3316948804(int team)
		{
			if (!base.IsClientInitialized)
			{
				NetworkManager networkManager = base.NetworkManager;
				networkManager.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
				return;
			}
			if (!base.IsOwner)
			{
				NetworkManager networkManager2 = base.NetworkManager;
				networkManager2.LogWarning("Cannot complete action because you are not the owner of this object. .");
				return;
			}
			Channel channel = Channel.Reliable;
			PooledWriter pooledWriter = WriterPool.Retrieve();
			pooledWriter.WriteInt32(team);
			SendServerRpc(4u, pooledWriter, channel, DataOrderType.Default);
			pooledWriter.Store();
		}

		public void RpcLogic___RpcRequestTeam___3316948804(int P_0)
		{
			if (this.TryGetSystem<LobbySystem, CoreSystemHandler>(out var system))
			{
				system.HostAssignTeam(base.OwnerId, P_0, respectCapacity: true);
			}
		}

		private void RpcReader___Server_RpcRequestTeam___3316948804(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
		{
			int num = PooledReader0.ReadInt32();
			if (base.IsServerInitialized && OwnerMatches(conn))
			{
				RpcLogic___RpcRequestTeam___3316948804(num);
			}
		}

		private void Awake_UserLogic_PuffPirates_002EPlayers_002EPiratePlayer_Assembly_002DCSharp_002Edll()
		{
			SetTickCallbacks(TickCallback.Tick | TickCallback.PostTick | TickCallback.Update);
		}
	}
}

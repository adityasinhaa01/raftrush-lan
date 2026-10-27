using FishNet.Component.Transforming;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using PuffPirates.Net;
using UnityEngine;

namespace PuffPirates.Football
{
	[RequireComponent(typeof(Rigidbody))]
	[DisallowMultipleComponent]
	public class FartBall : NetworkBehaviour
	{
		[Tooltip("Gol sonrası kilit: aynı golün iki kez sayılmasını engeller (sn).")]
		[SerializeField]
		private float _goalCooldown = 1.5f;

		private Rigidbody _rb;

		private NetworkTransform _netTransform;

		public readonly SyncVar<TickTimer> _svGoalLock = new SyncVar<TickTimer>(new SyncTypeSettings(0f));

		private bool NetworkInitialize___EarlyPuffPirates_002EFootball_002EFartBallAssembly_002DCSharp_002Edll_Excuted;

		private bool NetworkInitialize___LatePuffPirates_002EFootball_002EFartBallAssembly_002DCSharp_002Edll_Excuted;

		public static FartBall Instance { get; private set; }

		private TickTimer GoalLock
		{
			get
			{
				return _svGoalLock.Value;
			}
			set
			{
				_svGoalLock.Value = value;
			}
		}

		public bool IsLocked
		{
			get
			{
				if (GoalLock.IsRunning)
				{
					return !GoalLock.Expired(base.TimeManager);
				}
				return false;
			}
		}

		public override void OnStartNetwork()
		{
			base.OnStartNetwork();
			Instance = this;
			_rb = GetComponent<Rigidbody>();
			_netTransform = GetComponent<NetworkTransform>();
		}

		public override void OnStartServer()
		{
			base.OnStartServer();
			GoalLock = TickTimer.None;
		}

		public override void OnStopNetwork()
		{
			base.OnStopNetwork();
			if (Instance == this)
			{
				Instance = null;
			}
		}

		public void ResetToCenter(Vector3 center)
		{
			if (base.IsServerInitialized && !(_rb == null))
			{
				if (_netTransform != null)
				{
					_netTransform.Teleport();
				}
				_rb.position = center;
				_rb.rotation = Quaternion.identity;
				base.transform.SetPositionAndRotation(center, Quaternion.identity);
				_rb.linearVelocity = Vector3.zero;
				_rb.angularVelocity = Vector3.zero;
				GoalLock = TickTimer.CreateFromSeconds(base.TimeManager, _goalCooldown);
			}
		}

		public override void NetworkInitialize___Early()
		{
			if (!NetworkInitialize___EarlyPuffPirates_002EFootball_002EFartBallAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___EarlyPuffPirates_002EFootball_002EFartBallAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Early();
				_svGoalLock.InitializeEarly(this, 0u, isSyncObject: false);
			}
		}

		public override void NetworkInitialize___Late()
		{
			if (!NetworkInitialize___LatePuffPirates_002EFootball_002EFartBallAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___LatePuffPirates_002EFootball_002EFartBallAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Late();
				_svGoalLock.InitializeLate();
			}
		}

		public override void NetworkInitializeIfDisabled()
		{
			NetworkInitialize___Early();
			NetworkInitialize___Late();
		}

		public virtual void Awake()
		{
			NetworkInitialize___Early();
			NetworkInitialize___Late();
		}
	}
}

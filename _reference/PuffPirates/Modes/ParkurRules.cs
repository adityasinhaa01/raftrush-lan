using FishNet.Object;
using PuffPirates.Boats;
using PuffPirates.Networking;
using UnityEngine;

namespace PuffPirates.Modes
{
	[DisallowMultipleComponent]
	public class ParkurRules : NetworkBehaviour
	{
		private const int MyMode = 0;

		private bool NetworkInitialize___EarlyPuffPirates_002EModes_002EParkurRulesAssembly_002DCSharp_002Edll_Excuted;

		private bool NetworkInitialize___LatePuffPirates_002EModes_002EParkurRulesAssembly_002DCSharp_002Edll_Excuted;

		public static ParkurRules Instance { get; private set; }

		private bool Active
		{
			get
			{
				if (base.IsServerInitialized && RaceState.Instance != null)
				{
					return RaceState.Instance.ModeId == 0;
				}
				return false;
			}
		}

		public override void OnStartNetwork()
		{
			base.OnStartNetwork();
			Instance = this;
		}

		public override void OnStopNetwork()
		{
			base.OnStopNetwork();
			if (Instance == this)
			{
				Instance = null;
			}
		}

		public void OnBoatFinished(NetworkBoat boat, int rank)
		{
			if (Active && !(boat == null))
			{
				RaceState.Instance.ReportWinner(boat.OwnerTeam);
			}
		}

		public override void NetworkInitialize___Early()
		{
			if (!NetworkInitialize___EarlyPuffPirates_002EModes_002EParkurRulesAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___EarlyPuffPirates_002EModes_002EParkurRulesAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Early();
			}
		}

		public override void NetworkInitialize___Late()
		{
			if (!NetworkInitialize___LatePuffPirates_002EModes_002EParkurRulesAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___LatePuffPirates_002EModes_002EParkurRulesAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Late();
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

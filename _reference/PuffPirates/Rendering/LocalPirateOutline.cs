using EPOOutline;
using FishNet.Object;
using UnityEngine;

namespace PuffPirates.Rendering
{
	[DisallowMultipleComponent]
	public class LocalPirateOutline : NetworkBehaviour
	{
		[Tooltip("Outlinables shown only on the local player's pirate. Leave empty to use every Outlinable on this GameObject.")]
		[SerializeField]
		private Outlinable[] _outlinables;

		private bool NetworkInitialize___EarlyPuffPirates_002ERendering_002ELocalPirateOutlineAssembly_002DCSharp_002Edll_Excuted;

		private bool NetworkInitialize___LatePuffPirates_002ERendering_002ELocalPirateOutlineAssembly_002DCSharp_002Edll_Excuted;

		public override void OnStartClient()
		{
			base.OnStartClient();
			if (_outlinables == null || _outlinables.Length == 0)
			{
				_outlinables = GetComponents<Outlinable>();
			}
			bool isOwner = base.IsOwner;
			Outlinable[] outlinables = _outlinables;
			foreach (Outlinable outlinable in outlinables)
			{
				if (outlinable != null)
				{
					outlinable.enabled = isOwner;
				}
			}
		}

		public override void NetworkInitialize___Early()
		{
			if (!NetworkInitialize___EarlyPuffPirates_002ERendering_002ELocalPirateOutlineAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___EarlyPuffPirates_002ERendering_002ELocalPirateOutlineAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Early();
			}
		}

		public override void NetworkInitialize___Late()
		{
			if (!NetworkInitialize___LatePuffPirates_002ERendering_002ELocalPirateOutlineAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___LatePuffPirates_002ERendering_002ELocalPirateOutlineAssembly_002DCSharp_002Edll_Excuted = true;
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

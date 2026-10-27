using System;
using FishNet.Object;
using PuffPirates.Boats;
using PuffPirates.Track;
using UnityEngine;

namespace PuffPirates.Obstacles
{
	[RequireComponent(typeof(Rigidbody))]
	public class JointRotor : NetworkBehaviour
	{
		[Tooltip("Çarpan botu patlatan kol collider'ları. Listede olmayan kollar sadece fiziksel tepki verir.")]
		[SerializeField]
		private Collider[] _deadlyArms;

		[SerializeField]
		private float _respawnInvuln = 2f;

		[Tooltip("Kontrolsüz dönmeyi sınırlayan maksimum açısal hız (rad/sn).")]
		[SerializeField]
		private float _maxSpin = 4f;

		private bool NetworkInitialize___EarlyPuffPirates_002EObstacles_002EJointRotorAssembly_002DCSharp_002Edll_Excuted;

		private bool NetworkInitialize___LatePuffPirates_002EObstacles_002EJointRotorAssembly_002DCSharp_002Edll_Excuted;

		public virtual void Awake()
		{
			NetworkInitialize___Early();
			Awake_UserLogic_PuffPirates_002EObstacles_002EJointRotor_Assembly_002DCSharp_002Edll();
			NetworkInitialize___Late();
		}

		private void OnCollisionEnter(Collision c)
		{
			if (!base.IsServerInitialized || !BarrierKill.GloballyEnabled)
			{
				return;
			}
			NetworkBoat componentInParent = c.collider.GetComponentInParent<NetworkBoat>();
			if (componentInParent == null || componentInParent.IsInvulnerable)
			{
				return;
			}
			for (int i = 0; i < c.contactCount; i++)
			{
				if (Array.IndexOf(_deadlyArms, c.GetContact(i).thisCollider) >= 0)
				{
					Shark.Kill(componentInParent, DeathCause.Obstacle, _respawnInvuln);
					break;
				}
			}
		}

		public override void NetworkInitialize___Early()
		{
			if (!NetworkInitialize___EarlyPuffPirates_002EObstacles_002EJointRotorAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___EarlyPuffPirates_002EObstacles_002EJointRotorAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Early();
			}
		}

		public override void NetworkInitialize___Late()
		{
			if (!NetworkInitialize___LatePuffPirates_002EObstacles_002EJointRotorAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___LatePuffPirates_002EObstacles_002EJointRotorAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Late();
			}
		}

		public override void NetworkInitializeIfDisabled()
		{
			NetworkInitialize___Early();
			NetworkInitialize___Late();
		}

		private void Awake_UserLogic_PuffPirates_002EObstacles_002EJointRotor_Assembly_002DCSharp_002Edll()
		{
			GetComponent<Rigidbody>().maxAngularVelocity = _maxSpin;
		}
	}
}

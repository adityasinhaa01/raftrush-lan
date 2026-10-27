using System.Collections.Generic;
using FishNet.Utility.Template;
using PuffPirates.Obstacles.Physics;
using UnityEngine;

namespace PuffPirates.Obstacles
{
	[DisallowMultipleComponent]
	public class ObstacleSystem : TickNetworkBehaviour
	{
		private bool NetworkInitialize___EarlyPuffPirates_002EObstacles_002EObstacleSystemAssembly_002DCSharp_002Edll_Excuted;

		private bool NetworkInitialize___LatePuffPirates_002EObstacles_002EObstacleSystemAssembly_002DCSharp_002Edll_Excuted;

		public override void Awake()
		{
			NetworkInitialize___Early();
			Awake_UserLogic_PuffPirates_002EObstacles_002EObstacleSystem_Assembly_002DCSharp_002Edll();
			NetworkInitialize___Late();
		}

		protected override void TimeManager_OnTick()
		{
			DriveObstacles();
		}

		private void DriveObstacles()
		{
			float num = (float)base.TimeManager.TickDelta;
			float num2 = (float)base.TimeManager.Tick * num;
			List<MovingObstacle> all = MovingObstacle.All;
			for (int num3 = all.Count - 1; num3 >= 0; num3--)
			{
				MovingObstacle movingObstacle = all[num3];
				if (movingObstacle == null)
				{
					all.RemoveAt(num3);
				}
				else
				{
					movingObstacle.TickObstacle(num2);
				}
			}
			List<ITickedObstacle> ticked = PhysicsObstacleBase.Ticked;
			for (int num4 = ticked.Count - 1; num4 >= 0; num4--)
			{
				ITickedObstacle tickedObstacle = ticked[num4];
				if (tickedObstacle == null)
				{
					ticked.RemoveAt(num4);
				}
				else
				{
					tickedObstacle.ObstacleTick((int)base.TimeManager.Tick, num, num2);
				}
			}
		}

		public override void NetworkInitialize___Early()
		{
			if (!NetworkInitialize___EarlyPuffPirates_002EObstacles_002EObstacleSystemAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___EarlyPuffPirates_002EObstacles_002EObstacleSystemAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Early();
			}
		}

		public override void NetworkInitialize___Late()
		{
			if (!NetworkInitialize___LatePuffPirates_002EObstacles_002EObstacleSystemAssembly_002DCSharp_002Edll_Excuted)
			{
				NetworkInitialize___LatePuffPirates_002EObstacles_002EObstacleSystemAssembly_002DCSharp_002Edll_Excuted = true;
				base.NetworkInitialize___Late();
			}
		}

		public override void NetworkInitializeIfDisabled()
		{
			NetworkInitialize___Early();
			NetworkInitialize___Late();
		}

		private void Awake_UserLogic_PuffPirates_002EObstacles_002EObstacleSystem_Assembly_002DCSharp_002Edll()
		{
			SetTickCallbacks(TickCallback.Tick);
		}
	}
}

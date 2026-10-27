using PuffPirates.Boats;
using PuffPirates.Modes;
using UnityEngine;

namespace PuffPirates.Football
{
	[RequireComponent(typeof(Collider))]
	[DisallowMultipleComponent]
	public class GoalZone : MonoBehaviour
	{
		[SerializeField]
		private int _ownerTeam;

		public int OwnerTeam => _ownerTeam;

		public void Configure(int ownerTeam)
		{
			_ownerTeam = ownerTeam;
		}

		private void OnTriggerEnter(Collider other)
		{
			int scoringTeam = 1 - _ownerTeam;
			NetworkBoat componentInParent = other.GetComponentInParent<NetworkBoat>();
			if (componentInParent != null)
			{
				if (componentInParent.IsServerInitialized)
				{
					FutbolRules.Instance?.ScoreGoal(scoringTeam, componentInParent);
				}
				return;
			}
			FartBall componentInParent2 = other.GetComponentInParent<FartBall>();
			if (componentInParent2 != null && componentInParent2.IsServerInitialized)
			{
				FutbolRules.Instance?.ScoreGoalByBall(scoringTeam, componentInParent2);
			}
		}
	}
}

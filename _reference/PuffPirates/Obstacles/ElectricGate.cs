using System.Collections.Generic;
using PuffPirates.Boats;
using PuffPirates.Players;
using UnityEngine;

namespace PuffPirates.Obstacles
{
	public class ElectricGate : FieldObstacle
	{
		[SerializeField]
		private float _stunSeconds = 1.4f;

		[SerializeField]
		private float _electricSeconds = 1.4f;

		private void OnTriggerEnter(Collider other)
		{
			NetworkBoat networkBoat = FieldObstacle.HostBoat(other);
			if (networkBoat == null || networkBoat.IsInvulnerable)
			{
				return;
			}
			networkBoat.ApplyStatus(BoatStatusSlot.Electric, BoatStatusFlags.Electric, _electricSeconds);
			List<PiratePlayer> active = PiratePlayer.Active;
			for (int i = 0; i < active.Count; i++)
			{
				PiratePlayer piratePlayer = active[i];
				if (!(piratePlayer == null) && piratePlayer.BoatIndex == networkBoat.BoatIndex)
				{
					piratePlayer.Stun(_stunSeconds);
				}
			}
		}
	}
}

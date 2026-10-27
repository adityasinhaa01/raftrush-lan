using UnityEngine;

namespace PuffPirates.Obstacles
{
	public class RotorArm : MovingObstacle
	{
		[SerializeField]
		private float _degPerSec = 70f;

		public void Configure(float degPerSec, float phase)
		{
			_degPerSec = degPerSec;
			_phase = phase;
		}

		public override void TickObstacle(float t)
		{
			float y = (t + _phase) * _degPerSec;
			_rb.MoveRotation(_originRot * Quaternion.Euler(0f, y, 0f));
		}
	}
}

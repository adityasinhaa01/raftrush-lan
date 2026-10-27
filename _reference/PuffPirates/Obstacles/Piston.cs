using System;
using UnityEngine;

namespace PuffPirates.Obstacles
{
	public class Piston : MovingObstacle
	{
		[SerializeField]
		private Vector3 _axis = Vector3.right;

		[SerializeField]
		private float _travel = 6f;

		[SerializeField]
		private float _period = 3f;

		public void Configure(Vector3 axis, float travel, float period, float phase)
		{
			_axis = axis.normalized;
			_travel = travel;
			_period = Mathf.Max(0.2f, period);
			_phase = phase;
		}

		public override void TickObstacle(float t)
		{
			float num = Mathf.Sin((t + _phase) * MathF.PI * 2f / _period);
			_rb.MovePosition(_origin + _axis * (num * _travel));
		}
	}
}

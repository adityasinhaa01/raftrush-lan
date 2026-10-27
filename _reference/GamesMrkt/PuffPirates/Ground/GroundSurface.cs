using UnityEngine;

namespace GamesMrkt.PuffPirates.Ground
{
	public class GroundSurface : MonoBehaviour, IGround
	{
		[SerializeField]
		private GroundType _groundType;

		public GroundType GroundType => _groundType;
	}
}

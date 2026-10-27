using UnityEngine;

namespace PuffPirates.Obstacles
{
	public class SharkSpawnPoint : MonoBehaviour
	{
		private void OnDrawGizmos()
		{
			Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.9f);
			Gizmos.DrawWireSphere(base.transform.position, 0.8f);
			Gizmos.DrawLine(base.transform.position, base.transform.position + base.transform.forward * 2f);
		}
	}
}

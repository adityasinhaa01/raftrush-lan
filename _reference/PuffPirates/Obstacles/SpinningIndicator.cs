using UnityEngine;

namespace PuffPirates.Obstacles
{
	public sealed class SpinningIndicator : MonoBehaviour
	{
		[SerializeField]
		private float _degPerSec = 180f;

		[SerializeField]
		private Vector3 _axis = Vector3.up;

		private void Update()
		{
			base.transform.Rotate(_axis, _degPerSec * Time.deltaTime, Space.Self);
		}
	}
}

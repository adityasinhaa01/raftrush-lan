using UnityEngine;

namespace GamesMrkt.PuffPirates.UI
{
	public class LoadingCoverView : MonoBehaviour
	{
		[SerializeField]
		private GameObject _root;

		public void Show()
		{
			if (_root != null)
			{
				_root.SetActive(value: true);
			}
		}

		public void Hide()
		{
			if (_root != null)
			{
				_root.SetActive(value: false);
			}
		}
	}
}

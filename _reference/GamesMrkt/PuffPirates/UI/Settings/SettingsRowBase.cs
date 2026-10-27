using TMPro;
using UnityEngine;

namespace GamesMrkt.PuffPirates.UI.Settings
{
	public abstract class SettingsRowBase : MonoBehaviour
	{
		[SerializeField]
		private TMP_Text _title;

		public SettingsTabType TabType { get; private set; }

		public SettingsRowSelectable RowSelectable { get; private set; }

		public virtual bool CanEdit => false;

		public virtual void Submit()
		{
		}

		public virtual void Step(int direction)
		{
		}

		public void Setup(SettingsTabType tabType)
		{
			TabType = tabType;
			RowSelectable = GetComponent<SettingsRowSelectable>();
		}

		public void SetTitle(string title)
		{
			if (_title != null)
			{
				_title.text = title;
			}
		}
	}
}

using System;
using System.Collections.Generic;
using GamesMrkt.Audio;
using TMPro;
using UnityEngine;

namespace GamesMrkt.PuffPirates.UI.Settings
{
	public class DropdownSettingsRow : SettingsRowBase
	{
		[SerializeField]
		private TMP_Dropdown _dropdown;

		public event Action<int> OnSelectionChanged;

		public void SetOptions(List<string> options, int selectedIndex)
		{
			_dropdown.ClearOptions();
			_dropdown.AddOptions(options);
			_dropdown.SetValueWithoutNotify(selectedIndex);
		}

		private void Awake()
		{
			_dropdown.onValueChanged.AddListener(HandleValueChanged);
		}

		private void OnDestroy()
		{
			_dropdown.onValueChanged.RemoveListener(HandleValueChanged);
		}

		private void HandleValueChanged(int index)
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			OnSelectionChanged?.Invoke(index);
		}
	}
}

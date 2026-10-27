using System;
using System.Collections.Generic;
using UnityEngine;

namespace GamesMrkt.PuffPirates.UI.Settings
{
	public class SettingsRowFactory : MonoBehaviour
	{
		[Serializable]
		private struct RowPrefabEntry
		{
			public SettingsRowType RowType;

			public SettingsRowBase Prefab;
		}

		[SerializeField]
		private List<RowPrefabEntry> _prefabs;

		public T CreateRow<T>(SettingsRowType rowType, SettingsTabType tabType, Transform parent) where T : SettingsRowBase
		{
			SettingsRowBase settingsRowBase = null;
			foreach (RowPrefabEntry prefab in _prefabs)
			{
				if (prefab.RowType == rowType)
				{
					settingsRowBase = prefab.Prefab;
					break;
				}
			}
			if (settingsRowBase == null)
			{
				throw new ArgumentException($"No prefab registered for row type {rowType}");
			}
			T obj = (T)UnityEngine.Object.Instantiate(settingsRowBase, parent);
			obj.Setup(tabType);
			return obj;
		}
	}
}

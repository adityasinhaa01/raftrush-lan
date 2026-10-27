using GamesMrkt.Localization;
using TMPro;
using UnityEngine;

namespace GamesMrkt.PuffPirates.UI
{
	public static class LanguageFollow
	{
		public static void Label(TMP_Text label)
		{
			if (!(label == null) && !(label.GetComponent<LocalizedText>() != null) && !(label.GetComponent<LanguageNameText>() != null))
			{
				label.gameObject.AddComponent<LocalizedText>();
			}
		}

		public static void AllLabels(Component root)
		{
			if (!(root == null))
			{
				TMP_Text[] componentsInChildren = root.GetComponentsInChildren<TMP_Text>(includeInactive: true);
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					Label(componentsInChildren[i]);
				}
			}
		}
	}
}

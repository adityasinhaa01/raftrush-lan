using System;
using PuffPirates.Settings;
using TMPro;
using UnityEngine;

namespace GamesMrkt.PuffPirates.UI.Gameplay
{
	public class ControlHintsPanel : MonoBehaviour
	{
		[Serializable]
		private class HintBinding
		{
			[Tooltip("The row's LEFT (key) text. The action text on the right is not touched.")]
			public TMP_Text KeyText;

			[Tooltip("The action(s) this row shows. Several are joined with a comma.")]
			public GameAction[] Actions = new GameAction[1] { GameAction.NormalGas };
		}

		[SerializeField]
		private HintBinding[] _bindings;

		private void Awake()
		{
			if (_bindings == null)
			{
				return;
			}
			HintBinding[] bindings = _bindings;
			foreach (HintBinding hintBinding in bindings)
			{
				if (hintBinding != null)
				{
					LanguageFollow.Label(hintBinding.KeyText);
				}
			}
		}

		private void OnEnable()
		{
			Refresh();
			KeyBindings.OnChanged += Refresh;
		}

		private void OnDisable()
		{
			KeyBindings.OnChanged -= Refresh;
		}

		public void Refresh()
		{
			if (_bindings == null)
			{
				return;
			}
			HintBinding[] bindings = _bindings;
			foreach (HintBinding hintBinding in bindings)
			{
				if (hintBinding != null && !(hintBinding.KeyText == null) && hintBinding.Actions != null && hintBinding.Actions.Length != 0)
				{
					hintBinding.KeyText.text = BuildText(hintBinding.Actions);
				}
			}
		}

		private static string BuildText(GameAction[] actions)
		{
			string text = KeyBindings.DisplayName(actions[0]);
			for (int i = 1; i < actions.Length; i++)
			{
				text = text + ", " + KeyBindings.DisplayName(actions[i]);
			}
			return text;
		}
	}
}

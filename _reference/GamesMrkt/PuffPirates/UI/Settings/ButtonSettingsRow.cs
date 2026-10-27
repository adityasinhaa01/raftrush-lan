using System;
using GamesMrkt.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GamesMrkt.PuffPirates.UI.Settings
{
	public class ButtonSettingsRow : SettingsRowBase
	{
		[SerializeField]
		private Button _button;

		[SerializeField]
		private TMP_Text _buttonText;

		public event Action OnClicked;

		public override void Submit()
		{
			HandleClick();
		}

		public void SetButtonText(string text)
		{
			if (_buttonText != null)
			{
				_buttonText.text = text;
			}
		}

		private void Awake()
		{
			_button.onClick.AddListener(HandleClick);
		}

		private void OnDestroy()
		{
			_button.onClick.RemoveListener(HandleClick);
		}

		private void HandleClick()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			OnClicked?.Invoke();
		}
	}
}

using System;
using GamesMrkt.Audio;
using PuffPirates.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GamesMrkt.PuffPirates.UI.Settings
{
	public class KeybindSettingsRow : SettingsRowBase
	{
		[SerializeField]
		private TMP_Text _description;

		[SerializeField]
		private Button _keyButton;

		[SerializeField]
		private Image _keyButtonImage;

		[SerializeField]
		private TMP_Text _keyText;

		[SerializeField]
		private Image _keyIcon;

		[SerializeField]
		private Color _normalColor;

		[SerializeField]
		private Color _listeningColor;

		private bool _isListening;

		public event Action OnRebindRequested;

		public override void Submit()
		{
			HandleButtonClicked();
		}

		public void SetDescription(string description)
		{
			if (_description != null)
			{
				_description.text = description;
			}
		}

		public void SetKey(Sprite icon, string text)
		{
			bool flag = icon != null && _keyIcon != null;
			if (_keyIcon != null)
			{
				_keyIcon.sprite = icon;
				_keyIcon.enabled = flag;
			}
			if (_keyText != null)
			{
				_keyText.text = text;
				_keyText.enabled = !flag;
			}
		}

		public void SetListening(bool isListening)
		{
			_isListening = isListening;
			if (_keyButtonImage != null)
			{
				_keyButtonImage.color = (isListening ? _listeningColor : _normalColor);
			}
		}

		private void Awake()
		{
			_keyButton.onClick.AddListener(HandleButtonClicked);
			SetListening(isListening: false);
		}

		private void OnDestroy()
		{
			_keyButton.onClick.RemoveListener(HandleButtonClicked);
		}

		private void OnDisable()
		{
			if (_isListening)
			{
				KeyBindings.CancelRebind();
			}
		}

		private void HandleButtonClicked()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			OnRebindRequested?.Invoke();
		}
	}
}

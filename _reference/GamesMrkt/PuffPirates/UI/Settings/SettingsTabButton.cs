using System;
using GamesMrkt.Audio;
using GamesMrkt.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GamesMrkt.PuffPirates.UI.Settings
{
	public class SettingsTabButton : MonoBehaviour
	{
		[SerializeField]
		private SettingsTabType _type;

		[SerializeField]
		private Button _button;

		[SerializeField]
		private Image _background;

		[SerializeField]
		private TMP_Text _label;

		[SerializeField]
		private Color _selectedColor;

		[SerializeField]
		private Color _unselectedColor;

		public SettingsTabType Type => _type;

		public event Action<SettingsTabType> OnSelected;

		private void Awake()
		{
			_button.onClick.AddListener(HandleClick);
			Loc.OnLanguageChanged += HandleLanguageChanged;
			LanguageFollow.Label(_label);
			ApplyText();
		}

		private void OnDestroy()
		{
			_button.onClick.RemoveListener(HandleClick);
			Loc.OnLanguageChanged -= HandleLanguageChanged;
		}

		public void SetSelected(bool selected)
		{
			if (_background != null)
			{
				_background.color = (selected ? _selectedColor : _unselectedColor);
			}
		}

		private void HandleLanguageChanged(string language)
		{
			ApplyText();
		}

		private void ApplyText()
		{
			if (_label != null)
			{
				_label.text = Loc.Get(TitleKey(_type));
			}
		}

		private static string TitleKey(SettingsTabType type)
		{
			return type switch
			{
				SettingsTabType.Graphic => "settings.tab.graphic", 
				SettingsTabType.Sound => "settings.tab.sound", 
				SettingsTabType.Control => "settings.tab.control", 
				_ => "settings.tab.general", 
			};
		}

		private void HandleClick()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			OnSelected?.Invoke(_type);
		}
	}
}

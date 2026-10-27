using System;
using GamesMrkt.Audio;
using PuffPirates.Cosmetics;
using UnityEngine;
using UnityEngine.UI;

namespace GamesMrkt.PuffPirates.UI.Lobby
{
	public class LobbyCharacterSelectionItem : MonoBehaviour
	{
		[SerializeField]
		private Image _icon;

		[SerializeField]
		private Image _background;

		[SerializeField]
		private GameObject _selectedFrame;

		[SerializeField]
		private Button _button;

		private int _characterId;

		private Action<int> _onClick;

		public void Setup(int characterId, Sprite icon, Color? accent, Action<int> onClick)
		{
			_characterId = characterId;
			_onClick = onClick;
			if (_background != null && accent.HasValue)
			{
				_background.color = accent.Value;
			}
			if (_icon != null)
			{
				if (icon != null)
				{
					_icon.sprite = icon;
					_icon.color = Color.white;
				}
				else
				{
					_icon.color = CosmeticCatalog.SwatchColor(CosmeticSlot.CharacterSkin, characterId);
				}
			}
			if (_button != null)
			{
				_button.onClick.RemoveAllListeners();
				_button.onClick.AddListener(HandleClick);
			}
		}

		public void SetSelected(bool selected)
		{
			if (_selectedFrame != null)
			{
				_selectedFrame.SetActive(selected);
			}
		}

		private void HandleClick()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			_onClick?.Invoke(_characterId);
		}
	}
}

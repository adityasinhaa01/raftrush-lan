using System;
using GamesMrkt.Audio;
using GamesMrkt.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GamesMrkt.PuffPirates.UI.Settings
{
	public class ToggleSettingsRow : SettingsRowBase
	{
		[SerializeField]
		private Toggle _toggle;

		[SerializeField]
		private GameObject _onVisual;

		[SerializeField]
		private GameObject _offVisual;

		[Tooltip("AÇIK/KAPALI yazıları. Boş bırakılırsa prefab'daki yazı olduğu gibi kalır.")]
		[SerializeField]
		private TMP_Text _onLabel;

		[SerializeField]
		private TMP_Text _offLabel;

		public event Action<bool> OnToggled;

		public override void Submit()
		{
			_toggle.isOn = !_toggle.isOn;
		}

		public void SetValue(bool value)
		{
			_toggle.SetIsOnWithoutNotify(value);
			ApplyVisual(value);
		}

		private void Awake()
		{
			_toggle.onValueChanged.AddListener(HandleValueChanged);
			if (_onLabel != null)
			{
				_onLabel.text = Loc.Get("common.on");
			}
			if (_offLabel != null)
			{
				_offLabel.text = Loc.Get("common.off");
			}
		}

		private void OnDestroy()
		{
			_toggle.onValueChanged.RemoveListener(HandleValueChanged);
		}

		private void HandleValueChanged(bool value)
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			ApplyVisual(value);
			OnToggled?.Invoke(value);
		}

		private void ApplyVisual(bool value)
		{
			if (_onVisual != null)
			{
				_onVisual.SetActive(value);
			}
			if (_offVisual != null)
			{
				_offVisual.SetActive(!value);
			}
		}
	}
}

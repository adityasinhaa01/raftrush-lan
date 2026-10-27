using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GamesMrkt.PuffPirates.UI.Settings
{
	public class SliderSettingsRow : SettingsRowBase
	{
		[SerializeField]
		private Slider _slider;

		[SerializeField]
		private TMP_Text _valueText;

		private Func<float, string> _formatter = (float value) => value.ToString("0");

		private const float EDIT_STEPS_PER_RANGE = 20f;

		public override bool CanEdit => true;

		public event Action<float> OnValueChanged;

		public override void Step(int direction)
		{
			float num = (_slider.wholeNumbers ? 1f : ((_slider.maxValue - _slider.minValue) / 20f));
			_slider.value += (float)direction * num;
		}

		public void SetRange(float min, float max, float current, Func<float, string> formatter = null)
		{
			if (formatter != null)
			{
				_formatter = formatter;
			}
			_slider.minValue = min;
			_slider.maxValue = max;
			_slider.SetValueWithoutNotify(current);
			UpdateValueText(current);
		}

		private void Awake()
		{
			_slider.onValueChanged.AddListener(HandleValueChanged);
		}

		private void OnDestroy()
		{
			_slider.onValueChanged.RemoveListener(HandleValueChanged);
		}

		private void HandleValueChanged(float value)
		{
			UpdateValueText(value);
			OnValueChanged?.Invoke(value);
		}

		private void UpdateValueText(float value)
		{
			if (_valueText != null)
			{
				_valueText.text = _formatter(value);
			}
		}
	}
}

using System;
using System.Collections.Generic;
using GamesMrkt.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GamesMrkt.PuffPirates.UI.Settings
{
	public class NextPreviousSettingsRow : SettingsRowBase
	{
		[SerializeField]
		private TMP_Text _valueText;

		[SerializeField]
		private Button _previousButton;

		[SerializeField]
		private Button _nextButton;

		private List<string> _options;

		private int _index;

		public override bool CanEdit => true;

		public TMP_Text ValueText => _valueText;

		public event Action<int> OnIndexChanged;

		public override void Step(int direction)
		{
			if (direction < 0)
			{
				HandlePrevious();
			}
			else
			{
				HandleNext();
			}
		}

		public void SetOptions(List<string> options, int startIndex)
		{
			_options = options;
			_index = startIndex;
			UpdateValueText();
		}

		private void Awake()
		{
			_previousButton.onClick.AddListener(HandlePrevious);
			_nextButton.onClick.AddListener(HandleNext);
		}

		private void OnDestroy()
		{
			_previousButton.onClick.RemoveListener(HandlePrevious);
			_nextButton.onClick.RemoveListener(HandleNext);
		}

		private void HandlePrevious()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			_index--;
			if (_index < 0)
			{
				_index = _options.Count - 1;
			}
			UpdateValueText();
			OnIndexChanged?.Invoke(_index);
		}

		private void HandleNext()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			_index++;
			if (_index >= _options.Count)
			{
				_index = 0;
			}
			UpdateValueText();
			OnIndexChanged?.Invoke(_index);
		}

		private void UpdateValueText()
		{
			if (_valueText != null)
			{
				_valueText.text = _options[_index];
			}
		}
	}
}

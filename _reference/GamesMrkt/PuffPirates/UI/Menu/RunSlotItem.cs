using System;
using System.Globalization;
using GamesMrkt.Audio;
using GamesMrkt.Localization;
using PuffPirates.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GamesMrkt.PuffPirates.UI.Menu
{
	public class RunSlotItem : MonoBehaviour
	{
		[Tooltip("Optional. Leave empty on a display-only item (the run details).")]
		[SerializeField]
		private Button _selectButton;

		[SerializeField]
		private LocalizedText _mapLabel;

		[SerializeField]
		private TMP_Text _dateLabel;

		[SerializeField]
		private TMP_Text _timeLabel;

		public string RunId { get; private set; }

		public Selectable SelectButton => _selectButton;

		public event Action<RunSlotItem> OnSelected;

		private void Awake()
		{
			if (_selectButton != null)
			{
				_selectButton.onClick.AddListener(HandleSelectClicked);
			}
		}

		private void OnDestroy()
		{
			if (_selectButton != null)
			{
				_selectButton.onClick.RemoveListener(HandleSelectClicked);
			}
		}

		public void Bind(HostRunData run)
		{
			RunId = run.id;
			if (_mapLabel != null)
			{
				_mapLabel.SetKey(MapNameKey(run.mapId));
			}
			if (_dateLabel != null)
			{
				_dateLabel.text = FormatDate(run.updatedAt);
			}
			if (_timeLabel != null)
			{
				_timeLabel.text = FormatTime(run.elapsedSeconds);
			}
		}

		private void HandleSelectClicked()
		{
			AudioManager.PlaySound(CommonSound.ButtonClick);
			OnSelected?.Invoke(this);
		}

		private static string MapNameKey(int mapId)
		{
			if (mapId == 7)
			{
				return "map.platform";
			}
			return "map.parkur";
		}

		private static string FormatDate(long unixMilliseconds)
		{
			return DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds).ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}

		private static string FormatTime(float seconds)
		{
			int num = Mathf.Max(0, (int)seconds);
			int num2 = num / 3600;
			int num3 = num / 60 % 60;
			int num4 = num % 60;
			if (num2 <= 0)
			{
				return $"{num3}m{num4:00}s";
			}
			return $"{num2}h{num3:00}m{num4:00}s";
		}
	}
}

using Cysharp.Threading.Tasks;
using GamesMrkt.Core;
using GamesMrkt.PuffPirates.UI;
using UnityEngine;

namespace PuffPirates.Systems
{
	public class NotificationSystem : MonoBehaviour, IGameSystem
	{
		[SerializeField]
		private NotificationStackView _stackPrefab;

		[Tooltip("Show() için varsayılan ekran süresi (saniye).")]
		[SerializeField]
		private float _defaultDuration = 2f;

		[Tooltip("Renk verilmediğinde iyi/kötü bildirimin alacağı renkler.")]
		[SerializeField]
		private Color _goodColor = new Color(0.44f, 0.87f, 0.44f);

		[SerializeField]
		private Color _badColor = new Color(0.93f, 0.33f, 0.33f);

		private NotificationStackView _stack;

		public string SystemId => "NotificationSystem";

		public UniTask Initialize()
		{
			if (_stackPrefab == null)
			{
				return UniTask.CompletedTask;
			}
			_stack = Object.Instantiate(_stackPrefab);
			_stack.name = "NotificationStackView";
			Object.DontDestroyOnLoad(_stack.gameObject);
			return UniTask.CompletedTask;
		}

		public void Show(string text, bool isGoodAlert, Color? color = null, float? duration = null)
		{
			if (!(_stack == null))
			{
				_stack.Show(text, color ?? (isGoodAlert ? _goodColor : _badColor), duration ?? _defaultDuration, isGoodAlert);
			}
		}
	}
}

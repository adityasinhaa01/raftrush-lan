using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuffPirates.Settings
{
	internal static class SettingsSaveScheduler
	{
		private const double SaveDelaySeconds = 0.3;

		private static readonly HashSet<string> _dirty = new HashSet<string>();

		private static Action<IReadOnlyCollection<string>> _writer;

		private static bool _scheduled;

		private static double _saveAt;

		internal static Action<IReadOnlyCollection<string>> Writer
		{
			get
			{
				return _writer;
			}
			set
			{
				_writer = value;
				if (_writer == null)
				{
					Unschedule();
				}
			}
		}

		internal static void Request(string saveKey)
		{
			_dirty.Add(saveKey);
			if (_writer != null && Application.isPlaying)
			{
				_saveAt = Time.realtimeSinceStartupAsDouble + 0.3;
				if (!_scheduled)
				{
					_scheduled = true;
					Application.onBeforeRender += TryFlush;
					Application.focusChanged += OnFocusChanged;
				}
			}
		}

		internal static void Flush()
		{
			Unschedule();
			if (_writer != null && _dirty.Count != 0)
			{
				List<string> obj = new List<string>(_dirty);
				_dirty.Clear();
				_writer(obj);
			}
		}

		internal static void DiscardPending()
		{
			Unschedule();
			_dirty.Clear();
		}

		private static void Unschedule()
		{
			if (_scheduled)
			{
				_scheduled = false;
				Application.onBeforeRender -= TryFlush;
				Application.focusChanged -= OnFocusChanged;
			}
		}

		private static void TryFlush()
		{
			if (Time.realtimeSinceStartupAsDouble >= _saveAt)
			{
				Flush();
			}
		}

		private static void OnFocusChanged(bool focused)
		{
			if (!focused)
			{
				Flush();
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetState()
		{
			Unschedule();
			_dirty.Clear();
			_writer = null;
			_saveAt = 0.0;
		}
	}
}

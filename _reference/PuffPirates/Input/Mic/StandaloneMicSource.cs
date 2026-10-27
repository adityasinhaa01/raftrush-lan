using PuffPirates.Settings;
using UnityEngine;

namespace PuffPirates.Input.Mic
{
	public sealed class StandaloneMicSource : IMicSource
	{
		private const int RequestRate = 16000;

		private const int LengthSec = 1;

		private string _device;

		private AudioClip _clip;

		private int _rate;

		private float[] _ring;

		private bool _running;

		public bool Available
		{
			get
			{
				try
				{
					return Microphone.devices != null && Microphone.devices.Length != 0;
				}
				catch
				{
					return false;
				}
			}
		}

		public void Start()
		{
			if (_running || !Available)
			{
				return;
			}
			try
			{
				_device = PickDevice();
				Microphone.GetDeviceCaps(_device, out var minFreq, out var maxFreq);
				_rate = ((minFreq == 0 && maxFreq == 0) ? 16000 : Mathf.Clamp(16000, (minFreq == 0) ? 16000 : minFreq, (maxFreq == 0) ? 16000 : maxFreq));
				_clip = Microphone.Start(_device, loop: true, 1, _rate);
				_ring = new float[_rate];
				_running = _clip != null;
			}
			catch
			{
				_running = false;
				_clip = null;
			}
		}

		private static string PickDevice()
		{
			string[] devices = Microphone.devices;
			string micDeviceName = GameSettings.MicDeviceName;
			if (!string.IsNullOrEmpty(micDeviceName))
			{
				string[] array = devices;
				foreach (string text in array)
				{
					if (text == micDeviceName)
					{
						return text;
					}
				}
			}
			return devices[0];
		}

		public void Stop()
		{
			if (!_running)
			{
				return;
			}
			_running = false;
			_clip = null;
			try
			{
				Microphone.End(_device);
			}
			catch
			{
			}
		}

		public bool TryReadWindow(float[] dst, out int sampleRate)
		{
			sampleRate = _rate;
			if (!_running || _clip == null)
			{
				return false;
			}
			int position;
			try
			{
				position = Microphone.GetPosition(_device);
				if (position < 0)
				{
					return false;
				}
				_clip.GetData(_ring, 0);
			}
			catch
			{
				return false;
			}
			int num = dst.Length;
			for (int i = 0; i < num; i++)
			{
				int num2 = position - num + i;
				if (num2 < 0)
				{
					num2 += _ring.Length;
				}
				dst[i] = _ring[num2];
			}
			return true;
		}
	}
}

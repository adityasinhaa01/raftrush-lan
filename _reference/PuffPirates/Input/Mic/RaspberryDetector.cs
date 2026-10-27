using System;

namespace PuffPirates.Input.Mic
{
	public static class RaspberryDetector
	{
		private const float LowRatioThreshold = 0.6f;

		private const float SoftMargin = 2f;

		private const float HardMargin = 6f;

		private const int N = 1024;

		private static readonly float[] _re = new float[1024];

		private static readonly float[] _im = new float[1024];

		private static readonly float[] _env = new float[1024];

		public static bool IsBlowing(float rms, float lowRatio, float noiseFloor, float sensitivity)
		{
			float num = 2f / Math.Max(0.25f, sensitivity);
			if (rms > noiseFloor * num)
			{
				return lowRatio > 0.6f;
			}
			return false;
		}

		public static float LevelOf(float rms, float noiseFloor, float sensitivity)
		{
			float num = noiseFloor * (2f / Math.Max(0.25f, sensitivity));
			float num2 = noiseFloor * (6f / Math.Max(0.25f, sensitivity));
			if (num2 <= num)
			{
				if (!(rms > num))
				{
					return 0f;
				}
				return 1f;
			}
			float num3 = (rms - num) / (num2 - num);
			num3 = ((num3 < 0f) ? 0f : ((num3 > 1f) ? 1f : num3));
			return num3 * num3 * (3f - 2f * num3);
		}

		public static bool HysteresisStep(bool prev, float level, float onT, float offT)
		{
			if (prev)
			{
				return level > offT;
			}
			return level > onT;
		}

		public static float Rms(float[] x, int count)
		{
			if (count <= 0)
			{
				return 0f;
			}
			double num = 0.0;
			for (int i = 0; i < count; i++)
			{
				num += (double)x[i] * (double)x[i];
			}
			return (float)Math.Sqrt(num / (double)count);
		}

		public static float LowBandRatio(float[] x, int sampleRate)
		{
			float[] re = _re;
			float[] im = _im;
			Array.Clear(im, 0, 1024);
			for (int i = 0; i < 1024; i++)
			{
				float num = 0.5f - 0.5f * (float)Math.Cos(Math.PI * 2.0 * (double)i / 1023.0);
				re[i] = x[i] * num;
			}
			FFT.Forward(re, im);
			int num2 = (int)(512000.0 / (double)sampleRate);
			double num3 = 0.0;
			double num4 = 0.0;
			for (int j = 1; j < 512; j++)
			{
				double num5 = (double)re[j] * (double)re[j] + (double)im[j] * (double)im[j];
				num4 += num5;
				if (j <= num2)
				{
					num3 += num5;
				}
			}
			if (!(num4 > 1E-12))
			{
				return 0f;
			}
			return (float)(num3 / num4);
		}

		public static float FlutterScore(float[] x, int sampleRate)
		{
			float[] env = _env;
			double num = 0.0;
			for (int i = 0; i < 1024; i++)
			{
				env[i] = Math.Abs(x[i]);
				num += (double)env[i];
			}
			num /= 1024.0;
			float[] re = _re;
			float[] im = _im;
			Array.Clear(im, 0, 1024);
			for (int j = 0; j < 1024; j++)
			{
				re[j] = (float)((double)env[j] - num);
			}
			FFT.Forward(re, im);
			int num2 = (int)(15360.0 / (double)sampleRate);
			int num3 = (int)(61440.0 / (double)sampleRate);
			if (num2 < 1)
			{
				num2 = 1;
			}
			double num4 = 0.0;
			double num5 = 0.0;
			for (int k = 1; k < 512; k++)
			{
				double num6 = (double)re[k] * (double)re[k] + (double)im[k] * (double)im[k];
				num5 += num6;
				if (k >= num2 && k <= num3)
				{
					num4 += num6;
				}
			}
			if (!(num5 > 1E-12))
			{
				return 0f;
			}
			return (float)(num4 / num5);
		}
	}
}

using System;

namespace PuffPirates.Input.Mic
{
	public static class FFT
	{
		public static void Forward(float[] re, float[] im)
		{
			int num = re.Length;
			int i = 1;
			int num2 = 0;
			for (; i < num; i++)
			{
				int num3 = num >> 1;
				while ((num2 & num3) != 0)
				{
					num2 ^= num3;
					num3 >>= 1;
				}
				num2 ^= num3;
				if (i < num2)
				{
					ref float reference = ref re[i];
					ref float reference2 = ref re[num2];
					float num4 = re[num2];
					float num5 = re[i];
					reference = num4;
					reference2 = num5;
					reference = ref im[i];
					ref float reference3 = ref im[num2];
					num5 = im[num2];
					num4 = im[i];
					reference = num5;
					reference3 = num4;
				}
			}
			for (int num6 = 2; num6 <= num; num6 <<= 1)
			{
				double num7 = Math.PI * -2.0 / (double)num6;
				float num8 = (float)Math.Cos(num7);
				float num9 = (float)Math.Sin(num7);
				for (int j = 0; j < num; j += num6)
				{
					float num10 = 1f;
					float num11 = 0f;
					for (int k = 0; k < num6 / 2; k++)
					{
						int num12 = j + k;
						int num13 = j + k + num6 / 2;
						float num14 = num10 * re[num13] - num11 * im[num13];
						float num15 = num10 * im[num13] + num11 * re[num13];
						re[num13] = re[num12] - num14;
						im[num13] = im[num12] - num15;
						re[num12] += num14;
						im[num12] += num15;
						float num16 = num10 * num8 - num11 * num9;
						num11 = num10 * num9 + num11 * num8;
						num10 = num16;
					}
				}
			}
		}
	}
}

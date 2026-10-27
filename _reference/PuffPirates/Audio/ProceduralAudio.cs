using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuffPirates.Audio
{
	public static class ProceduralAudio
	{
		private const int SampleRate = 44100;

		private static readonly Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();

		public static AudioClip UiClick => GetOrBuild("ui-click", BuildUiClick);

		public static AudioClip ImpactThud => GetOrBuild("impact-thud", BuildImpactThud);

		public static AudioClip Explosion => GetOrBuild("explosion", BuildExplosion);

		public static AudioClip ElectricZap => GetOrBuild("electric-zap", BuildElectricZap);

		public static AudioClip BoostWhoosh => GetOrBuild("boost-whoosh", BuildBoostWhoosh);

		public static AudioClip CheckpointBlip => GetOrBuild("checkpoint-blip", BuildCheckpointBlip);

		public static AudioClip LapChime => GetOrBuild("lap-chime", BuildLapChime);

		public static AudioClip CountdownBeep => GetOrBuild("countdown-beep", BuildCountdownBeep);

		public static AudioClip GoChord => GetOrBuild("go-chord", BuildGoChord);

		public static AudioClip GoalFanfare => GetOrBuild("goal-fanfare", BuildGoalFanfare);

		public static AudioClip WinStinger => GetOrBuild("win-stinger", BuildWinStinger);

		public static AudioClip LoseStinger => GetOrBuild("lose-stinger", BuildLoseStinger);

		public static AudioClip GasThrust => GetOrBuild("gas-thrust", BuildGasThrust);

		public static AudioClip FartBrap => GetOrBuild("fart-brap", BuildFartBrap);

		public static AudioClip FartLoop => GetOrBuild("fart-loop", BuildFartLoop);

		public static AudioClip FartOut => GetOrBuild("fart-out", BuildFartOut);

		public static AudioClip MusicBed => GetOrBuild("music-bed", BuildMusicBed);

		private static AudioClip GetOrBuild(string key, Func<AudioClip> factory)
		{
			if (_cache.TryGetValue(key, out var value) && value != null)
			{
				return value;
			}
			value = factory();
			_cache[key] = value;
			return value;
		}

		private static int SamplesFor(float seconds)
		{
			return Mathf.Max(1, Mathf.RoundToInt(seconds * 44100f));
		}

		private static float Env(float t, float dur, float attack, float release)
		{
			float a = ((attack <= 0f) ? 1f : Mathf.Clamp01(t / attack));
			float b = ((release <= 0f) ? 1f : Mathf.Clamp01((dur - t) / release));
			return Mathf.Min(a, b);
		}

		private static AudioClip Build(string name, float seconds, Func<float, float> waveform, float attack, float release)
		{
			int num = SamplesFor(seconds);
			float[] array = new float[num];
			for (int i = 0; i < num; i++)
			{
				float num2 = (float)i / 44100f;
				float num3 = Env(num2, seconds, attack, release);
				array[i] = Mathf.Clamp(waveform(num2) * num3, -1f, 1f);
			}
			AudioClip audioClip = AudioClip.Create(name, num, 1, 44100, stream: false);
			audioClip.SetData(array, 0);
			return audioClip;
		}

		private static AudioClip BuildUiClick()
		{
			System.Random rnd = new System.Random(1);
			double phase = 0.0;
			return Build("ui-click", 0.05f, Wave, 0.002f, 0.04f);
			float Wave(float t)
			{
				phase += 0.040816325694322586;
				float num = Mathf.Sin((float)(phase * 2.0 * Math.PI));
				float num2 = (float)(rnd.NextDouble() * 2.0 - 1.0);
				return num * 0.55f + num2 * 0.35f;
			}
		}

		private static AudioClip BuildImpactThud()
		{
			System.Random rnd = new System.Random(2);
			double phase = 0.0;
			return Build("impact-thud", 0.28f, Wave, 0.002f, 0.24f);
			float Wave(float t)
			{
				float num = Mathf.Lerp(140f, 55f, Mathf.Clamp01(t / 0.22f));
				phase += num / 44100f;
				float num2 = Mathf.Sin((float)(phase * 2.0 * Math.PI));
				float num3 = (float)(rnd.NextDouble() * 2.0 - 1.0) * Mathf.Exp((0f - t) / 0.05f);
				return num2 * 0.8f + num3 * 0.5f;
			}
		}

		private static AudioClip BuildExplosion()
		{
			System.Random rnd = new System.Random(3);
			double phase = 0.0;
			float lp = 0f;
			return Build("explosion", 0.9f, Wave, 0.005f, 0.75f);
			float Wave(float t)
			{
				float b = (float)(rnd.NextDouble() * 2.0 - 1.0);
				lp = Mathf.Lerp(lp, b, 0.15f);
				phase += 0.0012471654918044806;
				float num = Mathf.Sin((float)(phase * 2.0 * Math.PI));
				return lp * 0.8f + num * 0.5f;
			}
		}

		private static AudioClip BuildElectricZap()
		{
			System.Random rnd = new System.Random(4);
			double phase = 0.0;
			return Build("electric-zap", 0.35f, Wave, 0.001f, 0.3f);
			float Wave(float t)
			{
				float num = 90f + (float)rnd.NextDouble() * 900f;
				phase += num / 44100f;
				float num2 = Mathf.Sign(Mathf.Sin((float)(phase * 2.0 * Math.PI)));
				float num3 = (float)(rnd.NextDouble() * 2.0 - 1.0);
				return num2 * 0.5f + num3 * 0.4f;
			}
		}

		private static AudioClip BuildBoostWhoosh()
		{
			System.Random rnd = new System.Random(5);
			double phase = 0.0;
			return Build("boost-whoosh", 0.45f, Wave, 0.05f, 0.25f);
			float Wave(float t)
			{
				float num = Mathf.Lerp(150f, 650f, t / 0.45f);
				phase += num / 44100f;
				float num2 = Mathf.Sin((float)(phase * 2.0 * Math.PI));
				float num3 = (float)(rnd.NextDouble() * 2.0 - 1.0) * 0.3f;
				return num2 * 0.6f + num3;
			}
		}

		private static AudioClip BuildCheckpointBlip()
		{
			double phase = 0.0;
			return Build("checkpoint-blip", 0.12f, Wave, 0.005f, 0.08f);
			float Wave(float t)
			{
				phase += 0.023730158805847168;
				return Mathf.Sin((float)(phase * 2.0 * Math.PI));
			}
		}

		private static AudioClip BuildLapChime()
		{
			double phase = 0.0;
			return Build("lap-chime", 0.35f, Wave, 0.005f, 0.18f);
			float Wave(float t)
			{
				float num = ((t < 0.16f) ? 659.25f : 987.77f);
				phase += num / 44100f;
				return Mathf.Sin((float)(phase * 2.0 * Math.PI));
			}
		}

		private static AudioClip BuildCountdownBeep()
		{
			double phase = 0.0;
			return Build("countdown-beep", 0.15f, Wave, 0.004f, 0.1f);
			float Wave(float t)
			{
				phase += 0.01995464786887169;
				return Mathf.Sin((float)(phase * 2.0 * Math.PI));
			}
		}

		private static AudioClip BuildGoChord()
		{
			double p1 = 0.0;
			double p2 = 0.0;
			double p3 = 0.0;
			return Build("go-chord", 0.4f, Wave, 0.01f, 0.3f);
			float Wave(float t)
			{
				p1 += 0.011865079402923584;
				p2 += 0.014948979951441288;
				p3 += 0.01777755096554756;
				return Mathf.Sin((float)(p1 * 2.0 * Math.PI)) * 0.4f + Mathf.Sin((float)(p2 * 2.0 * Math.PI)) * 0.35f + Mathf.Sin((float)(p3 * 2.0 * Math.PI)) * 0.35f;
			}
		}

		private static AudioClip BuildGoalFanfare()
		{
			double phase = 0.0;
			float[] notes = new float[4] { 523.25f, 659.25f, 783.99f, 1046.5f };
			return Build("goal-fanfare", (float)notes.Length * 0.22f, Wave, 0.005f, 0.05f);
			float Wave(float t)
			{
				int num = Mathf.Clamp(Mathf.FloorToInt(t / 0.22f), 0, notes.Length - 1);
				phase += notes[num] / 44100f;
				float num2 = t - (float)num * 0.22f;
				float num3 = Mathf.Clamp01(num2 / 0.01f) * Mathf.Exp((0f - num2) / 0.15f);
				return Mathf.Sin((float)(phase * 2.0 * Math.PI)) * num3;
			}
		}

		private static AudioClip BuildWinStinger()
		{
			double phase = 0.0;
			double c1 = 0.0;
			double c2 = 0.0;
			double c3 = 0.0;
			float[] notes = new float[3] { 523.25f, 659.25f, 783.99f };
			float seqDur = (float)notes.Length * 0.16f;
			return Build("win-stinger", seqDur + 0.6f, Wave, 0.01f, 0.3f);
			float Wave(float t)
			{
				if (t < seqDur)
				{
					int num = Mathf.Clamp(Mathf.FloorToInt(t / 0.16f), 0, notes.Length - 1);
					phase += notes[num] / 44100f;
					float num2 = t - (float)num * 0.16f;
					float num3 = Mathf.Clamp01(num2 / 0.01f) * Mathf.Exp((0f - num2) / 0.12f);
					return Mathf.Sin((float)(phase * 2.0 * Math.PI)) * num3;
				}
				float num4 = t - seqDur;
				c1 += 0.01777755096554756;
				c2 += 0.022398412227630615;
				c3 += 0.026636281982064247;
				float num5 = Mathf.Clamp01(num4 / 0.02f) * Mathf.Exp((0f - num4) / 0.4f);
				return (Mathf.Sin((float)(c1 * 2.0 * Math.PI)) * 0.4f + Mathf.Sin((float)(c2 * 2.0 * Math.PI)) * 0.3f + Mathf.Sin((float)(c3 * 2.0 * Math.PI)) * 0.3f) * num5;
			}
		}

		private static AudioClip BuildLoseStinger()
		{
			double phase = 0.0;
			return Build("lose-stinger", 1f, Wave, 0.02f, 0.6f);
			float Wave(float t)
			{
				float num = Mathf.Lerp(300f, 110f, Mathf.Clamp01(t / 0.8f));
				phase += num / 44100f;
				return Mathf.Sin((float)(phase * 2.0 * Math.PI));
			}
		}

		private static AudioClip BuildGasThrust()
		{
			int num = SamplesFor(0.5f);
			float[] array = new float[num];
			float[] array2 = new float[num];
			System.Random random = new System.Random(6);
			float a = 0f;
			for (int i = 0; i < num; i++)
			{
				float num2 = (float)i / 44100f;
				float b = (float)(random.NextDouble() * 2.0 - 1.0);
				a = (array2[i] = Mathf.Lerp(a, b, 0.2f));
				float num3 = 96f * num2 % 1f * 2f - 1f;
				float num4 = 0.6f + 0.4f * Mathf.Sin(num2 * 2f * MathF.PI * 24f);
				array[i] = num3 * num4;
			}
			int num5 = Mathf.Max(1, num / 10);
			for (int j = 0; j < num5; j++)
			{
				float t = (float)j / (float)num5;
				array2[j] = Mathf.Lerp(array2[num - num5 + j], array2[j], t);
			}
			float[] array3 = new float[num];
			for (int k = 0; k < num; k++)
			{
				array3[k] = Mathf.Clamp((array[k] * 0.55f + array2[k] * 0.45f) * 0.5f, -1f, 1f);
			}
			AudioClip audioClip = AudioClip.Create("gas-thrust", num, 1, 44100, stream: false);
			audioClip.SetData(array3, 0);
			return audioClip;
		}

		private static AudioClip BuildFartBrap()
		{
			System.Random rnd = new System.Random(7);
			double phase = 0.0;
			return Build("fart-brap", 0.55f, Wave, 0.008f, 0.12f);
			float Wave(float t)
			{
				float t2 = t / 0.55f;
				float num = Mathf.Lerp(120f, 68f, t2);
				float num2 = 1f + 0.18f * Mathf.Sin(t * 2f * MathF.PI * 22f);
				phase += num * num2 / 44100f;
				float num3 = (float)(phase - Math.Floor(phase)) * 2f - 1f;
				float num4 = 0.7f + 0.3f * Mathf.Sin(t * 2f * MathF.PI * 26f);
				float num5 = (float)(rnd.NextDouble() * 2.0 - 1.0) * 0.24f;
				return (num3 * 0.92f + num5) * num4;
			}
		}

		private static AudioClip BuildFartLoop()
		{
			int num = SamplesFor(1f);
			float[] array = new float[num];
			float[] array2 = new float[num];
			System.Random random = new System.Random(11);
			float a = 0f;
			double num2 = 0.0;
			for (int i = 0; i < num; i++)
			{
				float num3 = (float)i / 44100f;
				float b = (float)(random.NextDouble() * 2.0 - 1.0);
				a = (array2[i] = Mathf.Lerp(a, b, 0.12f));
				float num4 = 62f + 8f * Mathf.Sin(num3 * 2f * MathF.PI * 3f);
				num2 += (double)(num4 / 44100f);
				float num5 = (float)(num2 - Math.Floor(num2)) * 2f - 1f;
				float num6 = Mathf.Clamp01(0.5f + 0.3f * Mathf.Sin(num3 * 2f * MathF.PI * 11f) + 0.2f * Mathf.Sin(num3 * 2f * MathF.PI * 7f));
				array[i] = num5 * num6;
				array2[i] *= num6;
			}
			int num7 = Mathf.Max(1, num / 12);
			for (int j = 0; j < num7; j++)
			{
				float t = (float)j / (float)num7;
				array2[j] = Mathf.Lerp(array2[num - num7 + j], array2[j], t);
			}
			float[] array3 = new float[num];
			for (int k = 0; k < num; k++)
			{
				array3[k] = Mathf.Clamp((array[k] * 0.6f + array2[k] * 0.55f) * 0.6f, -1f, 1f);
			}
			AudioClip audioClip = AudioClip.Create("fart-loop", num, 1, 44100, stream: false);
			audioClip.SetData(array3, 0);
			return audioClip;
		}

		private static AudioClip BuildFartOut()
		{
			System.Random rnd = new System.Random(12);
			double phase = 0.0;
			return Build("fart-out", 0.18f, Wave, 0.004f, 0.06f);
			float Wave(float t)
			{
				float t2 = t / 0.18f;
				float num = Mathf.Lerp(85f, 45f, t2);
				float num2 = 1f + 0.2f * Mathf.Sin(t * 2f * MathF.PI * 20f);
				phase += num * num2 / 44100f;
				float num3 = (float)(phase - Math.Floor(phase)) * 2f - 1f;
				float num4 = 0.6f + 0.4f * Mathf.Sin(t * 2f * MathF.PI * 30f);
				float num5 = (float)(rnd.NextDouble() * 2.0 - 1.0) * 0.3f;
				return (num3 * 0.9f + num5) * num4;
			}
		}

		private static AudioClip BuildMusicBed()
		{
			double p1 = 0.0;
			double p2 = 0.0;
			double p3 = 0.0;
			return Build("music-bed", 4f, Wave, 0f, 0f);
			float Wave(float t)
			{
				p1 += 0.002494330983608961;
				p2 += 0.0037414967082440853;
				p3 += 0.004988661967217922;
				return (Mathf.Sin((float)(p1 * 2.0 * Math.PI)) * 0.5f + Mathf.Sin((float)(p2 * 2.0 * Math.PI)) * 0.3f + Mathf.Sin((float)(p3 * 2.0 * Math.PI)) * 0.2f) * 0.35f;
			}
		}
	}
}

using System;
using UnityEngine;

namespace PuffPirates.Utils
{
	public static class CrewLayoutMath
	{
		public static int[] DistinctSkins(int count, int skinCount, System.Random rng)
		{
			skinCount = Mathf.Max(1, skinCount);
			int[] array = new int[skinCount];
			for (int i = 0; i < skinCount; i++)
			{
				array[i] = i;
			}
			for (int num = skinCount - 1; num > 0; num--)
			{
				int num2 = rng.Next(num + 1);
				ref int reference = ref array[num];
				ref int reference2 = ref array[num2];
				int num3 = array[num2];
				int num4 = array[num];
				reference = num3;
				reference2 = num4;
			}
			int[] array2 = new int[Mathf.Max(0, count)];
			for (int j = 0; j < array2.Length; j++)
			{
				array2[j] = array[j % skinCount];
			}
			return array2;
		}

		public static int[] Derangement(int count, System.Random rng)
		{
			int[] array = new int[Mathf.Max(0, count)];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = i;
			}
			for (int num = array.Length - 1; num > 0; num--)
			{
				int num2 = rng.Next(num);
				ref int reference = ref array[num];
				ref int reference2 = ref array[num2];
				int num3 = array[num2];
				int num4 = array[num];
				reference = num3;
				reference2 = num4;
			}
			return array;
		}
	}
}

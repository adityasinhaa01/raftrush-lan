using System;
using GamesMrkt.Audio;
using PuffPirates.Track;
using UnityEngine;

namespace PuffPirates.Audio
{
	[DisallowMultipleComponent]
	public sealed class SurfaceSound : MonoBehaviour
	{
		[SerializeField]
		private SurfaceKind _kind;

		private static readonly string[] StoneTokens = new string[6] { "rock", "stone", "boulder", "cliff", "kaya", "taş" };

		private static readonly string[] SoilTokens = new string[10] { "road", "ground", "soil", "dirt", "terrain", "floor", "grass", "yol", "zemin", "toprak" };

		public static CommonSound SoundFor(SurfaceKind kind)
		{
			return kind switch
			{
				SurfaceKind.Stone => CommonSound.BumpStone, 
				SurfaceKind.Soil => CommonSound.BumpSoil, 
				_ => CommonSound.BumpMaterial, 
			};
		}

		public static SurfaceKind Classify(Collider c)
		{
			if (c == null)
			{
				return SurfaceKind.Material;
			}
			SurfaceSound componentInParent = c.GetComponentInParent<SurfaceSound>();
			if (componentInParent != null)
			{
				return componentInParent._kind;
			}
			Transform parent = c.transform;
			int num = 0;
			while (num < 4 && parent != null && !(parent.GetComponent<LevelDefinition>() != null))
			{
				string text = parent.name;
				Renderer component = parent.GetComponent<Renderer>();
				if (component != null && component.sharedMaterial != null)
				{
					text = text + " " + component.sharedMaterial.name;
				}
				if (Has(text, StoneTokens))
				{
					return SurfaceKind.Stone;
				}
				if (Has(text, SoilTokens))
				{
					return SurfaceKind.Soil;
				}
				num++;
				parent = parent.parent;
			}
			return SurfaceKind.Material;
		}

		private static bool Has(string s, string[] tokens)
		{
			foreach (string value in tokens)
			{
				if (s.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}
			return false;
		}
	}
}

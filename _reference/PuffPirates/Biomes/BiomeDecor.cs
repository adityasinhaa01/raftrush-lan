using System;
using PuffPirates.Visuals;
using UnityEngine;

namespace PuffPirates.Biomes
{
	public static class BiomeDecor
	{
		private static readonly Color TrunkBrown = new Color(0.42f, 0.3f, 0.18f, 1f);

		public static GameObject Build(DecorKind kind, int count, Color tint, float innerR, float outerR, int seed)
		{
			GameObject gameObject = new GameObject("BiomeDecor");
			if (kind == DecorKind.None || count <= 0)
			{
				return gameObject;
			}
			System.Random random = new System.Random(seed);
			float num = Mathf.Max(0f, outerR - innerR);
			for (int i = 0; i < count; i++)
			{
				double num2 = random.NextDouble() * Math.PI * 2.0;
				double num3 = (double)innerR + random.NextDouble() * (double)num;
				float x = (float)(Math.Cos(num2) * num3);
				float z = (float)(Math.Sin(num2) * num3);
				float y = GroundY(x, z);
				GameObject gameObject2 = MakeProp(kind, tint, random);
				if (!(gameObject2 == null))
				{
					gameObject2.transform.SetParent(gameObject.transform, worldPositionStays: false);
					gameObject2.transform.position = new Vector3(x, y, z);
					gameObject2.transform.rotation = Quaternion.Euler(0f, (float)(random.NextDouble() * 360.0), 0f);
					float num4 = 0.8f + (float)random.NextDouble() * 0.8f;
					gameObject2.transform.localScale = Vector3.one * num4;
				}
			}
			return gameObject;
		}

		private static float GroundY(float x, float z)
		{
			if (Physics.Raycast(new Vector3(x, 200f, z), Vector3.down, out var hitInfo, 400f, -1, QueryTriggerInteraction.Ignore))
			{
				return hitInfo.point.y;
			}
			return 0f;
		}

		private static GameObject MakeProp(DecorKind kind, Color tint, System.Random rnd)
		{
			return kind switch
			{
				DecorKind.Reeds => MakeReeds(tint, rnd), 
				DecorKind.Palms => MakePalm(tint, rnd), 
				DecorKind.Cacti => MakeCactus(tint, rnd), 
				_ => null, 
			};
		}

		private static GameObject MakeReeds(Color tint, System.Random rnd)
		{
			GameObject gameObject = new GameObject("Reeds");
			int num = 3 + rnd.Next(4);
			for (int i = 0; i < num; i++)
			{
				float x = (float)(rnd.NextDouble() * 0.7 - 0.35);
				float z = (float)(rnd.NextDouble() * 0.7 - 0.35);
				float y = 1.4f + (float)rnd.NextDouble() * 1.6f;
				Prim(PrimitiveType.Cylinder, gameObject.transform, new Vector3(x, y, z), new Vector3(0.12f, y, 0.12f), tint);
			}
			return gameObject;
		}

		private static GameObject MakePalm(Color tint, System.Random rnd)
		{
			GameObject gameObject = new GameObject("Palm");
			float num = 2.4f + (float)rnd.NextDouble() * 1.6f;
			Prim(PrimitiveType.Cylinder, gameObject.transform, new Vector3(0f, num, 0f), new Vector3(0.28f, num, 0.28f), TrunkBrown);
			Prim(PrimitiveType.Sphere, gameObject.transform, new Vector3(0f, num * 2f, 0f), new Vector3(1.8f, 0.6f, 1.8f), tint);
			return gameObject;
		}

		private static GameObject MakeCactus(Color tint, System.Random rnd)
		{
			GameObject gameObject = new GameObject("Cactus");
			float num = 1.6f + (float)rnd.NextDouble() * 1.4f;
			Prim(PrimitiveType.Capsule, gameObject.transform, new Vector3(0f, num, 0f), new Vector3(0.5f, num, 0.5f), tint);
			if (rnd.NextDouble() > 0.4)
			{
				Prim(PrimitiveType.Capsule, gameObject.transform, new Vector3(0.5f, num * 1.1f, 0f), new Vector3(0.3f, num * 0.5f, 0.3f), tint);
			}
			return gameObject;
		}

		private static GameObject Prim(PrimitiveType pt, Transform parent, Vector3 localPos, Vector3 localScale, Color col)
		{
			GameObject gameObject = GameObject.CreatePrimitive(pt);
			Collider component = gameObject.GetComponent<Collider>();
			if (component != null)
			{
				UnityEngine.Object.Destroy(component);
			}
			gameObject.transform.SetParent(parent, worldPositionStays: false);
			gameObject.transform.localPosition = localPos;
			gameObject.transform.localScale = localScale;
			Renderer component2 = gameObject.GetComponent<Renderer>();
			if (component2 != null)
			{
				RendererColorUtility.SetColor(component2, col);
			}
			return gameObject;
		}
	}
}

using UnityEngine;

namespace PuffPirates.Visuals
{
	internal static class RendererColorUtility
	{
		private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

		private static readonly int ColorId = Shader.PropertyToID("_Color");

		private static readonly MaterialPropertyBlock PropertyBlock = new MaterialPropertyBlock();

		public static void SetColor(Renderer renderer, Color color)
		{
			if (renderer == null)
			{
				return;
			}
			Material sharedMaterial = renderer.sharedMaterial;
			if (sharedMaterial == null)
			{
				return;
			}
			bool flag = sharedMaterial.HasProperty(BaseColorId);
			bool flag2 = sharedMaterial.HasProperty(ColorId);
			if (flag || flag2)
			{
				renderer.GetPropertyBlock(PropertyBlock);
				if (flag)
				{
					PropertyBlock.SetColor(BaseColorId, color);
				}
				if (flag2)
				{
					PropertyBlock.SetColor(ColorId, color);
				}
				renderer.SetPropertyBlock(PropertyBlock);
				PropertyBlock.Clear();
			}
		}
	}
}

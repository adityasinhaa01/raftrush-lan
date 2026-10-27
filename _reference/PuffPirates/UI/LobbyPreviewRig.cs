using PuffPirates.Networking;
using UnityEngine;

namespace PuffPirates.UI
{
	public class LobbyPreviewRig : MonoBehaviour
	{
		private const float CameraDistance = 2.4f;

		private const float CameraHeight = 0.65f;

		private const int TextureSize = 256;

		private const int TextureDepthBits = 16;

		private const float CameraFov = 35f;

		private const float CameraNearClip = 0.3f;

		private const float CameraFarClip = 20f;

		private const float BgColorR = 0.06f;

		private const float BgColorG = 0.08f;

		private const float BgColorB = 0.14f;

		private const float BgColorA = 1f;

		private RenderTexture[] _textures;

		public RenderTexture GetSlotTexture(int seat)
		{
			if (_textures == null || seat < 0 || seat >= _textures.Length)
			{
				return null;
			}
			return _textures[seat];
		}

		public static LobbyPreviewRig Create()
		{
			GameObject obj = new GameObject("~LobbyPreviewRig");
			Object.DontDestroyOnLoad(obj);
			LobbyPreviewRig lobbyPreviewRig = obj.AddComponent<LobbyPreviewRig>();
			lobbyPreviewRig.Setup();
			return lobbyPreviewRig;
		}

		private void Setup()
		{
			int num = 4;
			_textures = new RenderTexture[num];
			for (int i = 0; i < num; i++)
			{
				RenderTexture renderTexture = new RenderTexture(256, 256, 16)
				{
					name = $"LobbyPreviewRT_{i}"
				};
				_textures[i] = renderTexture;
				GameObject obj = new GameObject($"LobbyPreviewCam_{i}");
				obj.transform.SetParent(base.transform, worldPositionStays: false);
				Vector3 vector = LobbyPreviewSeats.WorldPosition(i);
				obj.transform.position = vector + new Vector3(0f, 0.65f, -2.4f);
				obj.transform.LookAt(vector + Vector3.up * 0.65f);
				Camera camera = obj.AddComponent<Camera>();
				camera.targetTexture = renderTexture;
				camera.fieldOfView = 35f;
				camera.nearClipPlane = 0.3f;
				camera.farClipPlane = 20f;
				camera.clearFlags = CameraClearFlags.Color;
				camera.backgroundColor = new Color(0.06f, 0.08f, 0.14f, 1f);
			}
		}

		private void OnDestroy()
		{
			if (_textures == null)
			{
				return;
			}
			RenderTexture[] textures = _textures;
			foreach (RenderTexture renderTexture in textures)
			{
				if (renderTexture != null)
				{
					renderTexture.Release();
				}
			}
		}
	}
}

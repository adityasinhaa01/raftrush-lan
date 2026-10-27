using System.Collections.Generic;
using PuffPirates.Boats;
using PuffPirates.CameraRig;
using PuffPirates.Players;
using UnityEngine;
using UnityEngine.Rendering;

namespace PuffPirates.Obstacles
{
	[RequireComponent(typeof(Collider))]
	public sealed class CaveZone : MonoBehaviour
	{
		private const float DefaultFogDensity = 0.12f;

		private const float DefaultCamFarClip = 40f;

		[Header("Mağara Ortamı")]
		[SerializeField]
		private Color _fogColor = new Color(0.02f, 0.02f, 0.03f);

		[SerializeField]
		[Min(0f)]
		private float _fogDensity = 0.12f;

		[SerializeField]
		private Color _ambientColor = new Color(0.03f, 0.03f, 0.04f);

		[SerializeField]
		[Min(1f)]
		private float _camFarClip = 40f;

		private static int _insideCount;

		private static bool _prevFog;

		private static Color _prevFogColor;

		private static FogMode _prevFogMode;

		private static float _prevFogDensity;

		private static float _prevFogStart;

		private static float _prevFogEnd;

		private static AmbientMode _prevAmbientMode;

		private static Color _prevAmbientLight;

		private static float _prevAmbientIntensity;

		private static float _prevFarClip;

		private static bool _prevFarClipCaptured;

		private void OnTriggerEnter(Collider other)
		{
			if (IsLocalBoat(other) && _insideCount++ == 0)
			{
				EnterCave();
			}
		}

		private void OnTriggerExit(Collider other)
		{
			if (IsLocalBoat(other))
			{
				_insideCount = Mathf.Max(0, _insideCount - 1);
				if (_insideCount == 0)
				{
					ExitCave();
				}
			}
		}

		private void EnterCave()
		{
			_prevFog = RenderSettings.fog;
			_prevFogColor = RenderSettings.fogColor;
			_prevFogMode = RenderSettings.fogMode;
			_prevFogDensity = RenderSettings.fogDensity;
			_prevFogStart = RenderSettings.fogStartDistance;
			_prevFogEnd = RenderSettings.fogEndDistance;
			_prevAmbientMode = RenderSettings.ambientMode;
			_prevAmbientLight = RenderSettings.ambientLight;
			_prevAmbientIntensity = RenderSettings.ambientIntensity;
			RenderSettings.fog = true;
			RenderSettings.fogMode = FogMode.ExponentialSquared;
			RenderSettings.fogColor = _fogColor;
			RenderSettings.fogDensity = _fogDensity;
			RenderSettings.ambientMode = AmbientMode.Flat;
			RenderSettings.ambientLight = _ambientColor;
			BoatCameraRig active = BoatCameraRig.Active;
			_prevFarClipCaptured = active != null;
			if (active != null)
			{
				_prevFarClip = active.FarClipPlane;
				active.FarClipPlane = _camFarClip;
			}
		}

		private void ExitCave()
		{
			RenderSettings.fog = _prevFog;
			RenderSettings.fogColor = _prevFogColor;
			RenderSettings.fogMode = _prevFogMode;
			RenderSettings.fogDensity = _prevFogDensity;
			RenderSettings.fogStartDistance = _prevFogStart;
			RenderSettings.fogEndDistance = _prevFogEnd;
			RenderSettings.ambientMode = _prevAmbientMode;
			RenderSettings.ambientLight = _prevAmbientLight;
			RenderSettings.ambientIntensity = _prevAmbientIntensity;
			if (_prevFarClipCaptured && BoatCameraRig.Active != null)
			{
				BoatCameraRig.Active.FarClipPlane = _prevFarClip;
			}
		}

		private static bool IsLocalBoat(Collider other)
		{
			NetworkBoat componentInParent = other.GetComponentInParent<NetworkBoat>();
			if (componentInParent == null)
			{
				return false;
			}
			List<PiratePlayer> active = PiratePlayer.Active;
			for (int i = 0; i < active.Count; i++)
			{
				PiratePlayer piratePlayer = active[i];
				if (piratePlayer != null && piratePlayer.BoatIndex == componentInParent.BoatIndex && piratePlayer.IsSpawned && piratePlayer.IsOwner)
				{
					return true;
				}
			}
			return false;
		}
	}
}

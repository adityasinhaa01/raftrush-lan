using PuffPirates.Water;
using UnityEngine;
using UnityEngine.Rendering;

namespace PuffPirates.Boats
{
	[RequireComponent(typeof(NetworkBoat))]
	[DisallowMultipleComponent]
	public sealed class BoatRippleTrail : MonoBehaviour
	{
		[Tooltip("Parçacıkların bulunduğu katman — WaterRippleCam'in kamerası da aynı katmana bakmalı.")]
		[SerializeField]
		private string _rippleLayerName = "RippleFX";

		[SerializeField]
		[Min(0.1f)]
		private float _particleWorldSize = 3f;

		[SerializeField]
		[Min(0.1f)]
		private float _particleLifetime = 2.5f;

		[SerializeField]
		[Min(0f)]
		private float _emissionRate = 14f;

		private BuoyantBody _buoy;

		private ParticleSystem _ps;

		private ParticleSystem.EmissionModule _emission;

		private void OnEnable()
		{
			_buoy = GetComponentInChildren<BuoyantBody>();
			int num = LayerMask.NameToLayer(_rippleLayerName);
			if (num < 0)
			{
				Debug.LogWarning("BoatRippleTrail (" + base.name + "): '" + _rippleLayerName + "' katmanı Project Settings > Tags and Layers'ta yok — iz devre dışı kaldı.");
				base.enabled = false;
				return;
			}
			GameObject gameObject = new GameObject("RippleEmitter");
			gameObject.layer = num;
			gameObject.transform.SetParent(base.transform, worldPositionStays: false);
			_ps = gameObject.AddComponent<ParticleSystem>();
			ParticleSystem.MainModule main = _ps.main;
			main.loop = true;
			main.simulationSpace = ParticleSystemSimulationSpace.World;
			main.startLifetime = _particleLifetime;
			main.startSize = _particleWorldSize;
			main.startSpeed = 0f;
			main.startColor = Color.white;
			main.gravityModifier = 0f;
			main.maxParticles = 1024;
			_emission = _ps.emission;
			_emission.rateOverTime = 0f;
			ParticleSystem.ShapeModule shape = _ps.shape;
			shape.enabled = false;
			ParticleSystem.ColorOverLifetimeModule colorOverLifetime = _ps.colorOverLifetime;
			colorOverLifetime.enabled = true;
			Gradient gradient = new Gradient();
			gradient.SetKeys(new GradientColorKey[2]
			{
				new GradientColorKey(Color.white, 0f),
				new GradientColorKey(Color.white, 1f)
			}, new GradientAlphaKey[2]
			{
				new GradientAlphaKey(1f, 0f),
				new GradientAlphaKey(0f, 1f)
			});
			colorOverLifetime.color = gradient;
			ParticleSystemRenderer component = gameObject.GetComponent<ParticleSystemRenderer>();
			component.renderMode = ParticleSystemRenderMode.Billboard;
			component.shadowCastingMode = ShadowCastingMode.Off;
			component.receiveShadows = false;
			Shader shader = Shader.Find("Sprites/Default");
			if (shader != null)
			{
				Material material = new Material(shader)
				{
					name = "RippleParticleMat"
				};
				material.mainTexture = BuildSoftDotTexture(64);
				component.sharedMaterial = material;
			}
			gameObject.SetActive(value: false);
		}

		private void Update()
		{
			if (!(_ps == null))
			{
				bool flag = _buoy != null && _buoy.InWater;
				_emission.rateOverTime = (flag ? _emissionRate : 0f);
			}
		}

		private static Texture2D BuildSoftDotTexture(int size)
		{
			Texture2D texture2D = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false)
			{
				wrapMode = TextureWrapMode.Clamp
			};
			Color32[] array = new Color32[size * size];
			Vector2 b = new Vector2((float)(size - 1) * 0.5f, (float)(size - 1) * 0.5f);
			float num = (float)size * 0.5f;
			for (int i = 0; i < size; i++)
			{
				for (int j = 0; j < size; j++)
				{
					float num2 = Vector2.Distance(new Vector2(j, i), b) / num;
					float num3 = Mathf.Clamp01(1f - num2);
					byte b2 = (byte)(num3 * num3 * 255f);
					array[i * size + j] = new Color32(b2, b2, b2, b2);
				}
			}
			texture2D.SetPixels32(array);
			texture2D.Apply();
			return texture2D;
		}

		private void OnDisable()
		{
			if (_ps != null)
			{
				Object.Destroy(_ps.gameObject);
			}
		}
	}
}

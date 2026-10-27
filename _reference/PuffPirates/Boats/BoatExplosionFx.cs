using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PuffPirates.Boats
{
	public static class BoatExplosionFx
	{
		private sealed class MaterialCleanup : MonoBehaviour
		{
			private readonly List<Material> _mats = new List<Material>();

			public void Track(Material m)
			{
				if (m != null)
				{
					_mats.Add(m);
				}
			}

			private void OnDestroy()
			{
				for (int i = 0; i < _mats.Count; i++)
				{
					if (_mats[i] != null)
					{
						Object.Destroy(_mats[i]);
					}
				}
				_mats.Clear();
			}
		}

		private sealed class FlashFade : MonoBehaviour
		{
			private Light _light;

			private float _t;

			private float _start;

			private void Awake()
			{
				_light = GetComponent<Light>();
				_start = ((_light != null) ? _light.intensity : 0f);
			}

			private void Update()
			{
				_t += Time.deltaTime;
				float num = Mathf.Clamp01(_t / 0.14f);
				if (_light != null)
				{
					_light.intensity = Mathf.Lerp(_start, 0f, num);
				}
				if (num >= 1f)
				{
					Object.Destroy(this);
				}
			}
		}

		public static void Play(Vector3 position, Color tint, GameObject vfxPrefab = null)
		{
			if (vfxPrefab != null)
			{
				GameObject gameObject = Object.Instantiate(vfxPrefab, position, Quaternion.identity);
				if (gameObject != null)
				{
					Object.Destroy(gameObject, 4f);
				}
			}
			else
			{
				GameObject gameObject2 = new GameObject("~BoatExplosionFx");
				gameObject2.transform.position = position;
				MaterialCleanup cleanup = gameObject2.AddComponent<MaterialCleanup>();
				BuildDebris(gameObject2.transform, tint, cleanup);
				BuildSmoke(gameObject2.transform, cleanup);
				BuildFlash(gameObject2.transform, tint);
				Object.Destroy(gameObject2, 1.6f);
			}
		}

		private static void BuildDebris(Transform parent, Color tint, MaterialCleanup cleanup)
		{
			GameObject gameObject = new GameObject("Debris");
			gameObject.transform.SetParent(parent, worldPositionStays: false);
			ParticleSystem particleSystem = gameObject.AddComponent<ParticleSystem>();
			particleSystem.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
			ParticleSystem.MainModule main = particleSystem.main;
			main.duration = 0.4f;
			main.loop = false;
			main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
			main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 11f);
			main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.35f);
			main.gravityModifier = 1.6f;
			main.startColor = new ParticleSystem.MinMaxGradient(tint, Color.Lerp(tint, Color.white, 0.5f));
			bool isMobilePlatform = Application.isMobilePlatform;
			main.maxParticles = (isMobilePlatform ? 32 : 60);
			ParticleSystem.EmissionModule emission = particleSystem.emission;
			emission.enabled = true;
			emission.rateOverTime = 0f;
			emission.SetBursts(new ParticleSystem.Burst[1]
			{
				new ParticleSystem.Burst(0f, (short)(isMobilePlatform ? 24 : 42))
			});
			ParticleSystem.ShapeModule shape = particleSystem.shape;
			shape.enabled = true;
			shape.shapeType = ParticleSystemShapeType.Sphere;
			shape.radius = 0.35f;
			Paint(particleSystem, tint, 0.5f, cleanup);
			particleSystem.Play(withChildren: true);
		}

		private static void BuildSmoke(Transform parent, MaterialCleanup cleanup)
		{
			GameObject gameObject = new GameObject("Smoke");
			gameObject.transform.SetParent(parent, worldPositionStays: false);
			ParticleSystem particleSystem = gameObject.AddComponent<ParticleSystem>();
			particleSystem.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
			ParticleSystem.MainModule main = particleSystem.main;
			main.duration = 0.5f;
			main.loop = false;
			main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
			main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 2.2f);
			main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
			main.gravityModifier = -0.15f;
			main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.7f, 0.7f, 0.72f, 0.75f), new Color(0.45f, 0.45f, 0.47f, 0.6f));
			bool isMobilePlatform = Application.isMobilePlatform;
			main.maxParticles = (isMobilePlatform ? 20 : 40);
			ParticleSystem.EmissionModule emission = particleSystem.emission;
			emission.enabled = true;
			emission.rateOverTime = 0f;
			emission.SetBursts(new ParticleSystem.Burst[1]
			{
				new ParticleSystem.Burst(0f, (short)(isMobilePlatform ? 10 : 18))
			});
			ParticleSystem.ShapeModule shape = particleSystem.shape;
			shape.enabled = true;
			shape.shapeType = ParticleSystemShapeType.Sphere;
			shape.radius = 0.5f;
			ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particleSystem.sizeOverLifetime;
			sizeOverLifetime.enabled = true;
			sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, SizeGrowCurve());
			Paint(particleSystem, new Color(0.6f, 0.6f, 0.62f), 0.7f, cleanup);
			particleSystem.Play(withChildren: true);
		}

		private static void BuildFlash(Transform parent, Color tint)
		{
			GameObject gameObject = new GameObject("Flash");
			gameObject.transform.SetParent(parent, worldPositionStays: false);
			Light light = gameObject.AddComponent<Light>();
			bool isMobilePlatform = Application.isMobilePlatform;
			light.type = LightType.Point;
			light.color = Color.Lerp(tint, Color.white, 0.6f);
			light.range = (isMobilePlatform ? 6f : 8f);
			light.intensity = (isMobilePlatform ? 3.5f : 6f);
			gameObject.AddComponent<FlashFade>();
		}

		private static AnimationCurve SizeGrowCurve()
		{
			return new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(1f, 1.5f));
		}

		private static void Paint(ParticleSystem ps, Color color, float alpha, MaterialCleanup cleanup)
		{
			ParticleSystemRenderer component = ps.GetComponent<ParticleSystemRenderer>();
			Material material = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color"));
			Color color2 = color;
			color2.a = alpha;
			material.color = color2;
			component.material = material;
			component.shadowCastingMode = ShadowCastingMode.Off;
			component.receiveShadows = false;
			cleanup?.Track(material);
		}
	}
}

using UnityEngine;

namespace PuffPirates.Biomes
{
	[DisallowMultipleComponent]
	public sealed class BiomeWeather : MonoBehaviour
	{
		private float _fwdOffset = 8f;

		private float _upOffset = 20f;

		private bool _facePrefab;

		private Camera _cam;

		private static Material _particleMat;

		public static GameObject Build(WeatherKind kind, Color color, float rate, Vector3 wind)
		{
			GameObject obj = new GameObject("BiomeWeather");
			ParticleSystem ps = obj.AddComponent<ParticleSystem>();
			obj.AddComponent<BiomeWeather>().Configure(ps, kind, color, rate, wind);
			return obj;
		}

		public static GameObject BuildPrefab(GameObject prefab, float followHeight)
		{
			GameObject gameObject = new GameObject("BiomeWeather");
			BiomeWeather biomeWeather = gameObject.AddComponent<BiomeWeather>();
			biomeWeather._fwdOffset = 16f;
			biomeWeather._upOffset = -2f;
			biomeWeather._facePrefab = true;
			if (prefab != null)
			{
				GameObject obj = Object.Instantiate(prefab, gameObject.transform);
				obj.transform.localPosition = Vector3.zero;
				obj.transform.localRotation = Quaternion.identity;
			}
			return gameObject;
		}

		private void Configure(ParticleSystem ps, WeatherKind kind, Color color, float rate, Vector3 wind)
		{
			ps.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
			ParticleSystem.MainModule main = ps.main;
			main.simulationSpace = ParticleSystemSimulationSpace.World;
			main.playOnAwake = true;
			main.maxParticles = 2500;
			main.startColor = color;
			main.startSpeed = 0f;
			ParticleSystem.EmissionModule emission = ps.emission;
			emission.rateOverTime = rate;
			ParticleSystem.ShapeModule shape = ps.shape;
			shape.shapeType = ParticleSystemShapeType.Box;
			ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = ps.velocityOverLifetime;
			velocityOverLifetime.enabled = true;
			velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
			ParticleSystemRenderer component = ps.GetComponent<ParticleSystemRenderer>();
			component.material = DefaultParticleMaterial();
			switch (kind)
			{
			case WeatherKind.Rain:
				main.startLifetime = 1.2f;
				main.startSize = 0.08f;
				shape.scale = new Vector3(60f, 1f, 60f);
				velocityOverLifetime.x = wind.x;
				velocityOverLifetime.y = -32f + wind.y;
				velocityOverLifetime.z = wind.z;
				component.renderMode = ParticleSystemRenderMode.Stretch;
				component.lengthScale = 6f;
				component.velocityScale = 0.12f;
				_fwdOffset = 8f;
				_upOffset = 22f;
				break;
			case WeatherKind.Snow:
				main.startLifetime = 6f;
				main.startSize = 0.2f;
				shape.scale = new Vector3(60f, 1f, 60f);
				velocityOverLifetime.x = wind.x + 0.5f;
				velocityOverLifetime.y = -3f;
				velocityOverLifetime.z = wind.z;
				component.renderMode = ParticleSystemRenderMode.Billboard;
				_fwdOffset = 8f;
				_upOffset = 22f;
				break;
			case WeatherKind.Dust:
				main.startLifetime = 4f;
				main.startSize = 0.14f;
				shape.scale = new Vector3(60f, 18f, 60f);
				velocityOverLifetime.x = 4f + wind.x;
				velocityOverLifetime.y = 0.4f;
				velocityOverLifetime.z = wind.z;
				component.renderMode = ParticleSystemRenderMode.Billboard;
				_fwdOffset = 10f;
				_upOffset = 3f;
				break;
			case WeatherKind.Wind:
			{
				main.startLifetime = 1.5f;
				main.startSize = 0.5f;
				shape.scale = new Vector3(45f, 22f, 45f);
				Vector3 vector = ((wind.sqrMagnitude > 0.01f) ? wind : new Vector3(12f, 0f, 3f));
				velocityOverLifetime.x = vector.x * 3f;
				velocityOverLifetime.y = vector.y;
				velocityOverLifetime.z = vector.z * 3f;
				component.renderMode = ParticleSystemRenderMode.Stretch;
				component.lengthScale = 3.5f;
				component.velocityScale = 0.35f;
				_fwdOffset = 6f;
				_upOffset = 5f;
				break;
			}
			}
			ps.Play();
		}

		private void LateUpdate()
		{
			Camera camera = ResolveCamera();
			if (!(camera == null))
			{
				Transform transform = camera.transform;
				Vector3 forward = transform.forward;
				forward.y = 0f;
				if (forward.sqrMagnitude < 0.01f)
				{
					forward = Vector3.forward;
				}
				else
				{
					forward.Normalize();
				}
				base.transform.position = transform.position + forward * _fwdOffset + Vector3.up * _upOffset;
				if (_facePrefab)
				{
					base.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
				}
			}
		}

		private Camera ResolveCamera()
		{
			if (_cam != null)
			{
				return _cam;
			}
			if (Camera.main != null)
			{
				return _cam = Camera.main;
			}
			Camera[] allCameras = Camera.allCameras;
			for (int i = 0; i < allCameras.Length; i++)
			{
				if (allCameras[i] != null && allCameras[i].enabled)
				{
					return _cam = allCameras[i];
				}
			}
			return null;
		}

		private static Material DefaultParticleMaterial()
		{
			if (_particleMat != null)
			{
				return _particleMat;
			}
			_particleMat = new Material(Shader.Find("Sprites/Default"));
			return _particleMat;
		}
	}
}

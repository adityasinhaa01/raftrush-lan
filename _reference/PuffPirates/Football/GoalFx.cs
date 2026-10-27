using PuffPirates.Modes;
using UnityEngine;

namespace PuffPirates.Football
{
	[DisallowMultipleComponent]
	public class GoalFx : MonoBehaviour
	{
		[Tooltip("Kendi patlama prefabın varsa buraya ver; boşsa kod üretir.")]
		[SerializeField]
		private ParticleSystem _customFx;

		[SerializeField]
		private Color _redTeam = new Color(1f, 0.3f, 0.25f);

		[SerializeField]
		private Color _blueTeam = new Color(0.3f, 0.55f, 1f);

		[SerializeField]
		private int _burstCount = 220;

		[SerializeField]
		private float _speed = 26f;

		[SerializeField]
		private float _lifetime = 1.6f;

		private ParticleSystem _fx;

		private void OnEnable()
		{
			FutbolRules.GoalScored += OnGoal;
		}

		private void OnDisable()
		{
			FutbolRules.GoalScored -= OnGoal;
		}

		private void OnGoal(int scoringTeam, Vector3 where)
		{
			ParticleSystem particleSystem = EnsureFx();
			if (!(particleSystem == null))
			{
				particleSystem.transform.position = where + Vector3.up * 1.5f;
				ParticleSystem.MainModule main = particleSystem.main;
				main.startColor = ((scoringTeam == 0) ? _redTeam : _blueTeam);
				particleSystem.Clear(withChildren: true);
				particleSystem.Play(withChildren: true);
			}
		}

		private ParticleSystem EnsureFx()
		{
			if (_fx != null)
			{
				return _fx;
			}
			if (_customFx != null)
			{
				_fx = Object.Instantiate(_customFx);
				_fx.gameObject.name = "GoalFx";
				return _fx;
			}
			GameObject gameObject = new GameObject("GoalFx");
			_fx = gameObject.AddComponent<ParticleSystem>();
			_fx.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
			ParticleSystem.MainModule main = _fx.main;
			main.duration = 1f;
			main.loop = false;
			main.playOnAwake = false;
			main.startLifetime = _lifetime;
			main.startSpeed = _speed;
			main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 2.2f);
			main.gravityModifier = 0.55f;
			main.maxParticles = _burstCount * 2;
			main.simulationSpace = ParticleSystemSimulationSpace.World;
			ParticleSystem.EmissionModule emission = _fx.emission;
			emission.rateOverTime = 0f;
			emission.SetBursts(new ParticleSystem.Burst[1]
			{
				new ParticleSystem.Burst(0f, (short)_burstCount)
			});
			ParticleSystem.ShapeModule shape = _fx.shape;
			shape.shapeType = ParticleSystemShapeType.Sphere;
			shape.radius = 1.2f;
			ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = _fx.sizeOverLifetime;
			sizeOverLifetime.enabled = true;
			sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
			ParticleSystem.ColorOverLifetimeModule colorOverLifetime = _fx.colorOverLifetime;
			colorOverLifetime.enabled = true;
			Gradient gradient = new Gradient();
			gradient.SetKeys(new GradientColorKey[2]
			{
				new GradientColorKey(Color.white, 0f),
				new GradientColorKey(Color.white, 1f)
			}, new GradientAlphaKey[3]
			{
				new GradientAlphaKey(1f, 0f),
				new GradientAlphaKey(1f, 0.55f),
				new GradientAlphaKey(0f, 1f)
			});
			colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
			ParticleSystemRenderer component = gameObject.GetComponent<ParticleSystemRenderer>();
			component.renderMode = ParticleSystemRenderMode.Billboard;
			Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
			if (shader != null)
			{
				component.material = new Material(shader);
			}
			return _fx;
		}
	}
}

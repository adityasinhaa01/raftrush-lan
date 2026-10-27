using UnityEngine;
using UnityEngine.Rendering;

namespace PuffPirates.Biomes
{
	[CreateAssetMenu(menuName = "Puff Pirates/Biome Theme", fileName = "BiomeTheme")]
	public sealed class BiomeTheme : ScriptableObject
	{
		[Header("Ad")]
		[SerializeField]
		private string _displayNameFallback = "Theme";

		[Header("Su (Uber shader props)")]
		[ColorUsage(true, true)]
		[SerializeField]
		private Color _waterShallow = new Color(0.2f, 0.6f, 0.7f, 1f);

		[ColorUsage(true, true)]
		[SerializeField]
		private Color _waterDeep = new Color(0f, 0.4f, 0.5f, 1f);

		[ColorUsage(true, true)]
		[SerializeField]
		private Color _foamColor = Color.white;

		[SerializeField]
		private float _waterDepth = 1f;

		[SerializeField]
		private float _shaderWaveHeight = 0.1f;

		[SerializeField]
		private bool _shaderWavesOn = true;

		[Header("Ortam isigi")]
		[SerializeField]
		private AmbientMode _ambientMode;

		[SerializeField]
		private Color _ambientColor = new Color(0.5f, 0.5f, 0.5f, 1f);

		[SerializeField]
		private float _ambientIntensity = 1f;

		[Header("Ana isik (Directional)")]
		[ColorUsage(false, true)]
		[SerializeField]
		private Color _sunColor = Color.white;

		[SerializeField]
		private float _sunIntensity = 1f;

		[SerializeField]
		private Vector3 _sunEuler = new Vector3(50f, -30f, 0f);

		[Header("Hava (Faz 2)")]
		[SerializeField]
		private WeatherKind _weather;

		[ColorUsage(true, true)]
		[SerializeField]
		private Color _weatherColor = new Color(1f, 1f, 1f, 0.5f);

		[SerializeField]
		private float _weatherRate = 600f;

		[Tooltip("Ruzgar (dunya-uzay) — hava partikul suruklemesi (Faz 2) + bot itisi (Faz 4).")]
		[SerializeField]
		private Vector3 _wind = Vector3.zero;

		[Tooltip("Opsiyonel GERCEK hava VFX prefabi (paketlerden, or. AllIn1 Air Column/Blue Tornado). ATANIRSA prosedurel yagmur/kar/toz YERINE bu kullanilir (kamerayi takip eder). Bos = prosedurel.")]
		[SerializeField]
		private GameObject _weatherPrefab;

		[Header("Dekor (Faz 3)")]
		[SerializeField]
		private DecorKind _decor;

		[SerializeField]
		private int _decorCount;

		[ColorUsage(false, false)]
		[SerializeField]
		private Color _decorTint = new Color(0.25f, 0.5f, 0.2f, 1f);

		[SerializeField]
		private float _decorInnerRadius = 55f;

		[SerializeField]
		private float _decorOuterRadius = 160f;

		[Header("Oynanis fizigi (Faz 4) — Default NOTR")]
		[Tooltip("Sabit ruzgar KUVVETI (dunya-uzay, ivme) — bot itisi. Host-only. 0 = itis yok.")]
		[SerializeField]
		private Vector3 _windForce = Vector3.zero;

		[Tooltip("Yatay drag CARPANI. 1 = degismez, >1 = yapiskan/yavas (batalik). Host-only.")]
		[SerializeField]
		private float _linearDragMul = 1f;

		public string DisplayName => _displayNameFallback;

		public Color WaterShallow => _waterShallow;

		public Color WaterDeep => _waterDeep;

		public Color FoamColor => _foamColor;

		public float WaterDepth => _waterDepth;

		public float ShaderWaveHeight => _shaderWaveHeight;

		public bool ShaderWavesOn => _shaderWavesOn;

		public AmbientMode AmbientMode => _ambientMode;

		public Color AmbientColor => _ambientColor;

		public float AmbientIntensity => _ambientIntensity;

		public Color SunColor => _sunColor;

		public float SunIntensity => _sunIntensity;

		public Vector3 SunEuler => _sunEuler;

		public WeatherKind Weather => _weather;

		public Color WeatherColor => _weatherColor;

		public float WeatherRate => _weatherRate;

		public Vector3 Wind => _wind;

		public GameObject WeatherPrefab => _weatherPrefab;

		public DecorKind Decor => _decor;

		public int DecorCount => _decorCount;

		public Color DecorTint => _decorTint;

		public float DecorInnerRadius => _decorInnerRadius;

		public float DecorOuterRadius => _decorOuterRadius;

		public Vector3 WindForce => _windForce;

		public float LinearDragMul => _linearDragMul;
	}
}

using System.Collections.Generic;
using PuffPirates.Networking;
using PuffPirates.Track;
using PuffPirates.Water;
using UnityEngine;
using UnityEngine.Rendering;

namespace PuffPirates.Biomes
{
	[DisallowMultipleComponent]
	public sealed class BiomeController : MonoBehaviour
	{
		[Tooltip("index = ThemeId. Eleman 0 = DEFAULT (bugunku gorunum birebir).")]
		[SerializeField]
		private BiomeTheme[] _themes;

		public const int DefaultThemeId = 0;

		[Tooltip("Sahnenin ana Directional Light'i - tema rengini/acisini buna uygular.")]
		[SerializeField]
		private Light _sun;

		private Material _runtimeWaterMat;

		private Material _srcWaterMat;

		private GameObject _weatherGo;

		private GameObject _decorGo;

		private static readonly int ColorShallowId = Shader.PropertyToID("_Color_Shallow");

		private static readonly int ColorDeepId = Shader.PropertyToID("_Color_Deep");

		private static readonly int FoamColorId = Shader.PropertyToID("_SurfFoam_Color");

		private static readonly int WaterDepthId = Shader.PropertyToID("_Water_Depth");

		private static readonly int Wave1HeightId = Shader.PropertyToID("_1st_Wave_Height");

		private static readonly int Wave2HeightId = Shader.PropertyToID("_2nd_Wave_Height");

		private const string EnableWaveProp = "_ENABLEWAVE";

		private bool _retired;

		public static BiomeController Instance { get; private set; }

		public int ThemeCount
		{
			get
			{
				if (_themes == null)
				{
					return 0;
				}
				return _themes.Length;
			}
		}

		public int Current { get; private set; }

		public static Vector3 WindForce { get; private set; }

		public static float LinearDragMul { get; private set; } = 1f;

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Instance.Retire();
			}
			Instance = this;
		}

		private void Retire()
		{
			_retired = true;
			if (_weatherGo != null)
			{
				Object.Destroy(_weatherGo);
				_weatherGo = null;
			}
			if (_decorGo != null)
			{
				Object.Destroy(_decorGo);
				_decorGo = null;
			}
			_runtimeWaterMat = null;
		}

		private void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
			}
			if (!_retired)
			{
				if (_runtimeWaterMat != null)
				{
					Object.Destroy(_runtimeWaterMat);
				}
				if (_weatherGo != null)
				{
					Object.Destroy(_weatherGo);
				}
				if (_decorGo != null)
				{
					Object.Destroy(_decorGo);
				}
			}
		}

		private void Start()
		{
			int themeId = ((RaceState.Instance != null) ? RaceState.Instance.ThemeId : 0);
			Apply(themeId);
		}

		public static int ClampThemeId(int id, int count)
		{
			if (count > 0)
			{
				return Mathf.Clamp(id, 0, count - 1);
			}
			return 0;
		}

		public string ThemeName(int id)
		{
			if (_themes == null || _themes.Length == 0)
			{
				return string.Empty;
			}
			BiomeTheme biomeTheme = _themes[ClampThemeId(id, _themes.Length)];
			if (!(biomeTheme != null))
			{
				return string.Empty;
			}
			return biomeTheme.DisplayName;
		}

		public void Apply(int themeId)
		{
			if (!_retired && _themes != null && _themes.Length != 0)
			{
				int num = ClampThemeId(themeId, _themes.Length);
				BiomeTheme biomeTheme = _themes[num];
				if (!(biomeTheme == null))
				{
					Current = num;
					ApplyEnvironment(biomeTheme);
					ApplyWater(biomeTheme);
					ApplyWeather(biomeTheme);
					ApplyDecor(biomeTheme);
					ApplyGameplay(biomeTheme);
				}
			}
		}

		private void ApplyEnvironment(BiomeTheme t)
		{
			RenderSettings.ambientMode = t.AmbientMode;
			if (t.AmbientMode == AmbientMode.Flat)
			{
				RenderSettings.ambientLight = t.AmbientColor;
			}
			else
			{
				RenderSettings.ambientIntensity = t.AmbientIntensity;
			}
			if (_sun != null)
			{
				_sun.color = t.SunColor;
				_sun.intensity = t.SunIntensity;
				_sun.transform.rotation = Quaternion.Euler(t.SunEuler);
			}
			DynamicGI.UpdateEnvironment();
		}

		private void ApplyWater(BiomeTheme t)
		{
			IReadOnlyList<WaterChannel> channels = WaterSystem.Channels;
			if (channels == null || channels.Count == 0)
			{
				return;
			}
			Material material = null;
			for (int i = 0; i < channels.Count; i++)
			{
				WaterChannel waterChannel = channels[i];
				if (waterChannel != null && waterChannel.WaterSurface != null && waterChannel.WaterSurface.sharedMaterial != null)
				{
					material = waterChannel.WaterSurface.sharedMaterial;
					break;
				}
			}
			if (material == null)
			{
				return;
			}
			if (material != _runtimeWaterMat)
			{
				_srcWaterMat = material;
			}
			if (_srcWaterMat != null)
			{
				material = _srcWaterMat;
			}
			Material material2 = new Material(material);
			if (material2.HasProperty(ColorShallowId))
			{
				material2.SetColor(ColorShallowId, t.WaterShallow);
			}
			if (material2.HasProperty(ColorDeepId))
			{
				material2.SetColor(ColorDeepId, t.WaterDeep);
			}
			if (material2.HasProperty(FoamColorId))
			{
				material2.SetColor(FoamColorId, t.FoamColor);
			}
			if (material2.HasProperty(WaterDepthId))
			{
				material2.SetFloat(WaterDepthId, t.WaterDepth);
			}
			if (material2.HasProperty(Wave1HeightId))
			{
				material2.SetFloat(Wave1HeightId, t.ShaderWaveHeight);
			}
			if (material2.HasProperty(Wave2HeightId))
			{
				material2.SetFloat(Wave2HeightId, t.ShaderWaveHeight);
			}
			if (material2.HasProperty("_ENABLEWAVE"))
			{
				material2.SetFloat("_ENABLEWAVE", t.ShaderWavesOn ? 1f : 0f);
				if (t.ShaderWavesOn)
				{
					material2.EnableKeyword("_ENABLEWAVE");
				}
				else
				{
					material2.DisableKeyword("_ENABLEWAVE");
				}
			}
			for (int j = 0; j < channels.Count; j++)
			{
				WaterChannel waterChannel2 = channels[j];
				if (waterChannel2 != null && waterChannel2.WaterSurface != null)
				{
					waterChannel2.WaterSurface.sharedMaterial = material2;
				}
			}
			if (_runtimeWaterMat != null && _runtimeWaterMat != material2)
			{
				Object.Destroy(_runtimeWaterMat);
			}
			_runtimeWaterMat = material2;
		}

		private void ApplyWeather(BiomeTheme t)
		{
			if (_weatherGo != null)
			{
				Object.Destroy(_weatherGo);
				_weatherGo = null;
			}
			if (t.WeatherPrefab != null)
			{
				_weatherGo = BiomeWeather.BuildPrefab(t.WeatherPrefab, 8f);
			}
			else if (t.Weather != WeatherKind.None)
			{
				_weatherGo = BiomeWeather.Build(t.Weather, t.WeatherColor, t.WeatherRate, t.Wind);
			}
		}

		private void ApplyDecor(BiomeTheme t)
		{
			if (_decorGo != null)
			{
				Object.Destroy(_decorGo);
				_decorGo = null;
			}
			if (t.Decor != DecorKind.None && t.DecorCount > 0)
			{
				int num = ((LevelBuilder.Instance != null) ? LevelBuilder.Instance.CurrentMap : 0);
				int seed = Current * 997 + num * 131 + 12345;
				_decorGo = BiomeDecor.Build(t.Decor, t.DecorCount, t.DecorTint, t.DecorInnerRadius, t.DecorOuterRadius, seed);
			}
		}

		private static void ApplyGameplay(BiomeTheme t)
		{
			WindForce = t.WindForce;
			LinearDragMul = t.LinearDragMul;
		}
	}
}

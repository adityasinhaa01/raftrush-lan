using System;
using System.Collections.Generic;
using GamesMrkt.Localization;
using PuffPirates.Football;
using PuffPirates.Networking;
using PuffPirates.Obstacles;
using PuffPirates.Visuals;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PuffPirates.Track
{
	[RequireComponent(typeof(RaceTrack))]
	[DisallowMultipleComponent]
	public class LevelBuilder : MonoBehaviour
	{
		private readonly struct CollisionMeshKey : IEquatable<CollisionMeshKey>
		{
			public readonly int SourceId;

			public readonly int Kind;

			public readonly float MaxSurfaceY;

			public readonly float FlatY;

			public CollisionMeshKey(int sourceId, int kind, float maxSurfaceY, float flatY)
			{
				SourceId = sourceId;
				Kind = kind;
				MaxSurfaceY = maxSurfaceY;
				FlatY = flatY;
			}

			public bool Equals(CollisionMeshKey o)
			{
				if (SourceId == o.SourceId && Kind == o.Kind && MaxSurfaceY.Equals(o.MaxSurfaceY))
				{
					return FlatY.Equals(o.FlatY);
				}
				return false;
			}

			public override bool Equals(object o)
			{
				if (o is CollisionMeshKey o2)
				{
					return Equals(o2);
				}
				return false;
			}

			public override int GetHashCode()
			{
				return (((((SourceId * 397) ^ Kind) * 397) ^ MaxSurfaceY.GetHashCode()) * 397) ^ FlatY.GetHashCode();
			}
		}

		public const int MapCount = 8;

		public const int DevFieldMap = 3;

		public const int ArenaMap = 4;

		public const int RaceSceneMap = 5;

		public const int Level1Map = 6;

		public const int PlatformMap = 7;

		[Header("Engel Prefabları")]
		[SerializeField]
		private GameObject _wallPrefab;

		[SerializeField]
		private GameObject _boostPrefab;

		[SerializeField]
		private GameObject _spikePrefab;

		[SerializeField]
		private GameObject _whirlpoolPrefab;

		[SerializeField]
		private GameObject _gustPrefab;

		[SerializeField]
		private GameObject _waterCurrentPrefab;

		[SerializeField]
		private GameObject _cavePrefab;

		[SerializeField]
		private GameObject _electricPrefab;

		[SerializeField]
		private GameObject _rotorPrefab;

		[SerializeField]
		private GameObject _pistonPrefab;

		[SerializeField]
		private GameObject _checkpointPrefab;

		[SerializeField]
		private GameObject _finishPrefab;

		[Header("Koridor")]
		[SerializeField]
		private float _wallHeight = 3.5f;

		[SerializeField]
		private float _wallThickness = 1f;

		[Header("Takım")]
		[Tooltip("Başlangıç çizgisinde botlar arası yatay boşluk.")]
		[SerializeField]
		private float _spawnSpacing = 4.5f;

		[Header("FBX Haritalar (kullanıcı modelleri)")]
		[Tooltip("Arena.fbx — FARTBALL sahası.")]
		[SerializeField]
		private GameObject _arenaFbx;

		[Tooltip("Race_Scene.fbx — kapalı devre yarış pisti.")]
		[SerializeField]
		private GameObject _raceSceneFbx;

		[Tooltip("LEVEL1.fbx — tek geçişlik yarış yolu.")]
		[SerializeField]
		private GameObject _level1Fbx;

		[Tooltip("Map.fbx — PARKUR labirenti (harita 0). Boşsa eski prosedürel DAR BOĞAZ kurulur.")]
		[SerializeField]
		private GameObject _parkurFbx;

		[Header("Level Prefablar (ELLE-YAZILMIŞ — prosedürelin yerine geçer)")]
		[Tooltip("Harita id → Level prefab (LevelDefinition kökü). Atanmışsa o harita PROSEDÜREL kurulmaz, prefab instantiate edilir (mesh+checkpoint+spawn+engel elle). Boş slot eski prosedürele düşer.")]
		[SerializeField]
		private GameObject[] _levelPrefabs = new GameObject[8];

		[Header("Parkur Engelleri (op1-4, statik katı)")]
		[Tooltip("op1-4 prefabları — parkur checkpoint hattı boyunca döngüsel yerleşir. Boşsa engel kurulmaz.")]
		[SerializeField]
		private GameObject _op1;

		[SerializeField]
		private GameObject _op2;

		[SerializeField]
		private GameObject _op3;

		[Header("Fart Together EXP Engelleri")]
		[Tooltip("Yeni FBX engel prefabları. En az bir eleman atanırsa op1-4 yerine bu dizi kullanılır; boş bırakılırsa eski parkur engelleri aynen çalışmaya devam eder.")]
		[SerializeField]
		private GameObject[] _importedParkurObstacles;

		[SerializeField]
		private GameObject _op4;

		[Header("Yarış")]
		[Tooltip("Item kutusu prefabı (Task 6). Boşsa kutu kurulmaz.")]
		[SerializeField]
		private GameObject _itemBoxPrefab;

		[Header("Zemin")]
		[Tooltip("Sahnedeki Ground (Plane). DEV SAHA'da 8× ölçeklenir.")]
		[SerializeField]
		private Transform _ground;

		[Tooltip("Su/yol yüzeylerinde kullanılan düşük sürtünmeli fizik materyali.")]
		[SerializeField]
		private PhysicsMaterial _surfaceMaterial;

		[SerializeField]
		private float _normalGroundScale = 10f;

		[SerializeField]
		private float _devFieldGroundScale = 80f;

		[Header("Dev Saha")]
		[SerializeField]
		private int _devCubeCount = 120;

		[SerializeField]
		private float _devFieldHalf = 340f;

		private RaceTrack _track;

		private int _cpIndex;

		private int _builtMap = -1;

		private readonly Dictionary<CollisionMeshKey, Mesh> _collisionMeshCache = new Dictionary<CollisionMeshKey, Mesh>();

		private readonly List<int> _collisionTriBuffer = new List<int>(1024);

		private readonly HashSet<int> _collisionVertSet = new HashSet<int>();

		private const float BarrierClearance = 0.12f;

		private const float BarrierWallHeight = 3f;

		private const int RoadMeshKind = 0;

		private const int BarrierMeshKind = 1;

		public static LevelBuilder Instance { get; private set; }

		public static bool? BuoyancyOverride { get; set; }

		public int CurrentMap => _builtMap;

		private static bool HasBespokeClassifier(int id)
		{
			if (id != 0 && id != 5)
			{
				return id == 6;
			}
			return true;
		}

		private static LevelDefinition AuthoredDefinition(int id)
		{
			if (HasBespokeClassifier(id))
			{
				return null;
			}
			LevelBuilder instance = Instance;
			if (instance == null || instance._levelPrefabs == null || id < 0 || id >= instance._levelPrefabs.Length)
			{
				return null;
			}
			GameObject gameObject = instance._levelPrefabs[id];
			if (!(gameObject != null))
			{
				return null;
			}
			return gameObject.GetComponent<LevelDefinition>();
		}

		public static bool IsLoopingMap(int id)
		{
			LevelDefinition levelDefinition = AuthoredDefinition(id);
			if (!(levelDefinition != null))
			{
				return id == 5;
			}
			return levelDefinition.Looping;
		}

		public static bool IsRaceMap(int id)
		{
			LevelDefinition levelDefinition = AuthoredDefinition(id);
			if (!(levelDefinition != null))
			{
				if (id != 5)
				{
					return id == 6;
				}
				return true;
			}
			return levelDefinition.Looping;
		}

		public static bool HidesGround(int id)
		{
			LevelDefinition levelDefinition = AuthoredDefinition(id);
			if (!(levelDefinition != null))
			{
				if (id != 0 && id != 4 && id != 5)
				{
					return id == 6;
				}
				return true;
			}
			return levelDefinition.HidesGround;
		}

		public static bool UsesBuoyancy(int id)
		{
			if (BuoyancyOverride.HasValue)
			{
				return BuoyancyOverride.Value;
			}
			LevelDefinition levelDefinition = AuthoredDefinition(id);
			if (!(levelDefinition == null))
			{
				return levelDefinition.UseBuoyancy;
			}
			return true;
		}

		public static string MapName(int id)
		{
			LevelDefinition levelDefinition = AuthoredDefinition(id);
			if (levelDefinition != null)
			{
				return levelDefinition.MapName;
			}
			return id switch
			{
				0 => Loc.Get("map.parkur"), 
				1 => Loc.Get("map.whirl_valley"), 
				2 => Loc.Get("map.flat_test"), 
				3 => Loc.Get("map.dev_field"), 
				4 => Loc.Get("map.arena_fartball"), 
				5 => Loc.Get("map.race_scene"), 
				6 => Loc.Get("map.level1"), 
				_ => Loc.Get("map.narrow_straits"), 
			};
		}

		private void Awake()
		{
			Instance = this;
			_track = GetComponent<RaceTrack>();
			Build((RaceState.Instance != null) ? RaceState.Instance.MapId : 7);
		}

		private void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
			}
		}

		public void Build(int mapId)
		{
			mapId = Mathf.Clamp(mapId, 0, 7);
			if (mapId == _builtMap)
			{
				return;
			}
			Clear();
			_builtMap = mapId;
			_cpIndex = 0;
			if (TryBuildLevelPrefab(mapId))
			{
				BuildVoidKill();
				return;
			}
			ShowGround(!HidesGround(mapId));
			ScaleGround((mapId == 3) ? _devFieldGroundScale : _normalGroundScale);
			switch (mapId)
			{
			case 1:
				BuildWhirlValley();
				break;
			case 2:
				BuildFlat();
				break;
			case 3:
				BuildDevField();
				break;
			case 4:
				BuildArena();
				break;
			case 5:
				BuildRaceScene();
				break;
			case 6:
				BuildLevel1();
				break;
			case 0:
				BuildParkurFbx();
				break;
			default:
				BuildNarrowStraits();
				break;
			}
			BuildVoidKill();
		}

		private bool TryBuildLevelPrefab(int mapId)
		{
			if (_levelPrefabs == null || mapId < 0 || mapId >= _levelPrefabs.Length)
			{
				return false;
			}
			GameObject gameObject = _levelPrefabs[mapId];
			if (gameObject == null)
			{
				return false;
			}
			LevelDefinition levelDefinition = FindSceneLevelDefinition(gameObject.name);
			if (levelDefinition != null)
			{
				levelDefinition.gameObject.SetActive(value: true);
				ShowGround(!levelDefinition.HidesGround);
				ScaleGround((mapId == 3) ? _devFieldGroundScale : _normalGroundScale);
				levelDefinition.Apply(_track);
				ConfigurePickupSpawnPoints(levelDefinition.gameObject);
				return true;
			}
			HideSceneLevelInstances(gameObject.name);
			GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject, base.transform);
			gameObject2.transform.localPosition = Vector3.zero;
			gameObject2.transform.localRotation = Quaternion.identity;
			LevelDefinition component = gameObject2.GetComponent<LevelDefinition>();
			if (component == null)
			{
				Debug.LogWarning("[LevelBuilder] Level prefab '" + gameObject.name + "' LevelDefinition taşımıyor → prosedürele düşülüyor.", this);
				if (Application.isPlaying)
				{
					UnityEngine.Object.Destroy(gameObject2);
				}
				else
				{
					UnityEngine.Object.DestroyImmediate(gameObject2);
				}
				return false;
			}
			ShowGround(!component.HidesGround);
			ScaleGround((mapId == 3) ? _devFieldGroundScale : _normalGroundScale);
			component.Apply(_track);
			ConfigurePickupSpawnPoints(gameObject2);
			return true;
		}

		private LevelDefinition FindSceneLevelDefinition(string prefabName)
		{
			Scene scene = base.gameObject.scene;
			LevelDefinition[] array = UnityEngine.Object.FindObjectsByType<LevelDefinition>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			foreach (LevelDefinition levelDefinition in array)
			{
				if (!(levelDefinition == null) && !(levelDefinition.gameObject.scene != scene) && !(levelDefinition.name != prefabName))
				{
					return levelDefinition;
				}
			}
			return null;
		}

		private void HideSceneLevelInstances(string exceptPrefabName)
		{
			Scene scene = base.gameObject.scene;
			LevelDefinition[] array = UnityEngine.Object.FindObjectsByType<LevelDefinition>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			foreach (LevelDefinition levelDefinition in array)
			{
				if (!(levelDefinition == null) && !(levelDefinition.gameObject.scene != scene) && !(levelDefinition.name == exceptPrefabName) && !levelDefinition.name.Contains("(Clone)"))
				{
					levelDefinition.gameObject.SetActive(value: false);
				}
			}
		}

		private static void ConfigurePickupSpawnPoints(GameObject inst)
		{
			PickupSpawnPoint[] componentsInChildren = inst.GetComponentsInChildren<PickupSpawnPoint>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].Configure(i);
			}
			if (componentsInChildren.Length > 32)
			{
				Debug.LogWarning($"[LevelBuilder] {componentsInChildren.Length} PickupSpawnPoint var ama RaceState.MaxPickups={32} → fazlası ağda senkron OLMAZ (kapasiteyi artır).");
			}
		}

		private void Clear()
		{
			_track.ResetRegistry();
			for (int num = base.transform.childCount - 1; num >= 0; num--)
			{
				GameObject obj = base.transform.GetChild(num).gameObject;
				if (Application.isPlaying)
				{
					UnityEngine.Object.Destroy(obj);
				}
				else
				{
					UnityEngine.Object.DestroyImmediate(obj);
				}
			}
		}

		private void BuildNarrowStraits()
		{
			(float, float, float)[] segments = new (float, float, float)[9]
			{
				(-46f, -38f, 9f),
				(-38f, -28f, 8f),
				(-28f, -14f, 9f),
				(-14f, -6f, 4f),
				(-6f, 8f, 12f),
				(8f, 22f, 8f),
				(22f, 34f, 8f),
				(34f, 42f, 6f),
				(42f, 47f, 8f)
			};
			BuildWalls(segments);
			BuildSpawns(-44f);
			Gate(-44f, 9f, isFinish: false);
			for (int i = 0; i < 2; i++)
			{
				Spawn(_boostPrefab, new Vector3(0f, 0.15f, -35f + (float)i * 5f), Quaternion.identity, $"Boost_{i}")?.GetComponent<BoostPad>()?.Configure(2600f);
			}
			for (int j = 0; j < 2; j++)
			{
				Spawn(_rotorPrefab, new Vector3(0f, 1.2f, -24f + (float)j * 6f), Quaternion.identity, $"Rotor_{j}")?.GetComponent<RotorArm>()?.Configure((j == 0) ? 65f : (-80f), (float)j * 1.4f);
			}
			GameObject gameObject = Spawn(_cavePrefab, new Vector3(0f, 0f, -21f), Quaternion.identity, "Cave_Rotor");
			if (gameObject != null)
			{
				gameObject.transform.localScale = new Vector3(1f, 1f, 8f / 15f);
			}
			Gate(-16f, 9f, isFinish: false);
			for (int k = 0; k < 4; k++)
			{
				Spawn(_spikePrefab, new Vector3((k % 2 == 0) ? (-3.2f) : 3.2f, 0.9f, -13f + (float)k * 2.2f), Quaternion.identity, $"Spike_N{k}");
			}
			Gate(-8f, 4f, isFinish: false);
			GameObject obj = Spawn(_whirlpoolPrefab, new Vector3(0f, 1f, 1f), Quaternion.identity, "Whirlpool");
			obj?.GetComponent<Whirlpool>()?.Configure(1100f, 900f);
			ObstacleZoneSizer obstacleZoneSizer = obj?.GetComponent<ObstacleZoneSizer>();
			if (obstacleZoneSizer != null)
			{
				obstacleZoneSizer.Size = new Vector2(10f, 10f);
			}
			Spawn(_waterCurrentPrefab, new Vector3(0f, 1f, 5f), Quaternion.Euler(0f, -90f, 0f), "Current_Wide")?.GetComponent<GustZone>()?.Configure(Vector3.right, 480f);
			Gate(7f, 12f, isFinish: false);
			for (int l = 0; l < 2; l++)
			{
				Spawn(_pistonPrefab, new Vector3((l % 2 == 0) ? (-7f) : 7f, 1.4f, 11f + (float)l * 6f), Quaternion.identity, $"Piston_{l}")?.GetComponent<Piston>()?.Configure(Vector3.right, 6f, 3.2f, (float)l * 0.8f);
			}
			Gate(21f, 8f, isFinish: false);
			Spawn(_gustPrefab, new Vector3(0f, 2f, 28f), Quaternion.identity, "Gust")?.GetComponent<GustZone>()?.Configure(Vector3.right, 950f);
			for (int m = 0; m < 3; m++)
			{
				Spawn(_spikePrefab, new Vector3(6.4f, 0.9f, 24f + (float)m * 3.5f), Quaternion.identity, $"Spike_W{m}");
			}
			Gate(33f, 8f, isFinish: false);
			Spawn(_electricPrefab, new Vector3(0f, 1.6f, 37.5f), Quaternion.identity, "ElectricGate");
			Gate(45f, 8f, isFinish: true);
		}

		private void BuildWhirlValley()
		{
			(float, float, float)[] segments = new (float, float, float)[6]
			{
				(-46f, -34f, 10f),
				(-34f, -18f, 14f),
				(-18f, -2f, 14f),
				(-2f, 14f, 14f),
				(14f, 30f, 10f),
				(30f, 47f, 9f)
			};
			BuildWalls(segments);
			BuildSpawns(-44f);
			Gate(-44f, 10f, isFinish: false);
			Spawn(_boostPrefab, new Vector3(0f, 0.15f, -38f), Quaternion.identity, "Boost_0")?.GetComponent<BoostPad>()?.Configure(3000f);
			Vector3[] array = new Vector3[3]
			{
				new Vector3(-6f, 1f, -26f),
				new Vector3(6f, 1f, -10f),
				new Vector3(-5f, 1f, 6f)
			};
			float[] array2 = new float[3] { 11f, 8f, 5f };
			for (int i = 0; i < array.Length; i++)
			{
				GameObject obj = Spawn(_whirlpoolPrefab, array[i], Quaternion.identity, $"Whirlpool_{i}");
				obj?.GetComponent<Whirlpool>()?.Configure(950f, 1000f);
				ObstacleZoneSizer obstacleZoneSizer = obj?.GetComponent<ObstacleZoneSizer>();
				if (obstacleZoneSizer != null)
				{
					obstacleZoneSizer.Size = new Vector2(array2[i], array2[i]);
				}
			}
			Gate(-18f, 14f, isFinish: false);
			for (int j = 0; j < 3; j++)
			{
				Spawn(_rotorPrefab, new Vector3((j % 2 == 0) ? (-5f) : 5f, 1.2f, -14f + (float)j * 9f), Quaternion.identity, $"Rotor_{j}")?.GetComponent<RotorArm>()?.Configure((j % 2 == 0) ? 55f : (-70f), (float)j * 1.1f);
			}
			GameObject gameObject = Spawn(_cavePrefab, new Vector3(0f, 0f, -5f), Quaternion.identity, "Cave_RotorForest");
			if (gameObject != null)
			{
				gameObject.transform.localScale = new Vector3(1f, 1f, 0.6f);
			}
			Gate(6f, 14f, isFinish: false);
			Spawn(_gustPrefab, new Vector3(0f, 2f, 18f), Quaternion.identity, "Gust_R")?.GetComponent<GustZone>()?.Configure(Vector3.right, 1000f);
			Spawn(_gustPrefab, new Vector3(0f, 2f, 28f), Quaternion.identity, "Gust_L")?.GetComponent<GustZone>()?.Configure(Vector3.left, 1000f);
			for (int k = 0; k < 4; k++)
			{
				Spawn(_spikePrefab, new Vector3((k % 2 == 0) ? (-8.4f) : 8.4f, 0.9f, 17f + (float)k * 4f), Quaternion.identity, $"Spike_V{k}");
			}
			Gate(30f, 9f, isFinish: false);
			Spawn(_electricPrefab, new Vector3(0f, 1.6f, 38f), Quaternion.identity, "ElectricGate");
			Spawn(_waterCurrentPrefab, new Vector3(0f, 1f, 42f), Quaternion.Euler(0f, -90f, 0f), "Current_Finish")?.GetComponent<GustZone>()?.Configure(Vector3.right, 500f);
			Gate(45f, 9f, isFinish: true);
		}

		private void BuildWalls((float z0, float z1, float hw)[] segments)
		{
			for (int i = 0; i < segments.Length; i++)
			{
				(float, float, float) tuple = segments[i];
				float z = tuple.Item2 - tuple.Item1;
				float num = (tuple.Item1 + tuple.Item2) * 0.5f;
				Vector3 localScale = new Vector3(_wallThickness, _wallHeight, z);
				GameObject gameObject = Spawn(_wallPrefab, new Vector3(0f - tuple.Item3, _wallHeight * 0.5f, num), Quaternion.identity, $"Wall_L_{num:0}");
				if (gameObject != null)
				{
					gameObject.transform.localScale = localScale;
				}
				GameObject gameObject2 = Spawn(_wallPrefab, new Vector3(tuple.Item3, _wallHeight * 0.5f, num), Quaternion.identity, $"Wall_R_{num:0}");
				if (gameObject2 != null)
				{
					gameObject2.transform.localScale = localScale;
				}
			}
		}

		private void BuildFlat()
		{
			BuildSpawns(-44f);
			Gate(-44f, 20f, isFinish: false);
			Gate(40f, 20f, isFinish: true);
		}

		private void BuildDevField()
		{
			BuildSpawns(-300f);
			Gate(-300f, 60f, isFinish: false);
			Gate(320f, 60f, isFinish: true);
			System.Random random = new System.Random(20260712);
			Color[] array = new Color[8]
			{
				new Color(0.95f, 0.35f, 0.35f),
				new Color(0.35f, 0.85f, 0.45f),
				new Color(0.35f, 0.6f, 0.95f),
				new Color(0.95f, 0.8f, 0.3f),
				new Color(0.75f, 0.4f, 0.9f),
				new Color(0.3f, 0.85f, 0.85f),
				new Color(0.95f, 0.55f, 0.25f),
				new Color(0.9f, 0.9f, 0.95f)
			};
			for (int i = 0; i < _devCubeCount; i++)
			{
				float num = (float)(random.NextDouble() * 2.0 - 1.0) * _devFieldHalf;
				float num2 = (float)(random.NextDouble() * 2.0 - 1.0) * _devFieldHalf;
				if (Mathf.Abs(num) < 30f && (num2 < -270f || num2 > 290f))
				{
					continue;
				}
				float num3 = 3f + (float)random.NextDouble() * 14f;
				float num4 = num3 * (0.6f + (float)random.NextDouble());
				Color color = array[random.Next(array.Length)];
				GameObject gameObject = Spawn(_wallPrefab, new Vector3(num, num4 * 0.5f, num2), Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f), $"DevCube_{i}");
				if (!(gameObject == null))
				{
					gameObject.transform.localScale = new Vector3(num3, num4, num3);
					Renderer component = gameObject.GetComponent<Renderer>();
					if (component != null)
					{
						RendererColorUtility.SetColor(component, color);
					}
				}
			}
		}

		private void ScaleGround(float scale)
		{
			if (!(_ground == null))
			{
				_ground.localScale = new Vector3(scale, 1f, scale);
				ApplySurfaceMaterial(_ground.GetComponent<Collider>());
			}
		}

		private void ApplySurfaceMaterial(Collider collider)
		{
			if (collider != null && _surfaceMaterial != null)
			{
				collider.sharedMaterial = _surfaceMaterial;
			}
		}

		private void ShowGround(bool visible)
		{
			if (!(_ground == null) && _ground.gameObject.activeSelf != visible)
			{
				_ground.gameObject.SetActive(visible);
			}
		}

		private GameObject SpawnFbx(GameObject fbx, float scale, string name)
		{
			if (fbx == null)
			{
				Debug.LogError("[LevelBuilder] '" + name + "' FBX'i atanmamış — harita kurulamadı.");
				return null;
			}
			GameObject obj = UnityEngine.Object.Instantiate(fbx, Vector3.zero, Quaternion.identity, base.transform);
			obj.name = name;
			obj.transform.localScale = Vector3.one * scale;
			return obj;
		}

		private void BuildRoadCollider(GameObject fbxInstance, float maxSurfaceY, float flatY, bool barrierKills)
		{
			if (fbxInstance == null)
			{
				return;
			}
			MeshFilter componentInChildren = fbxInstance.GetComponentInChildren<MeshFilter>();
			if (componentInChildren == null || componentInChildren.sharedMesh == null)
			{
				return;
			}
			Mesh sharedMesh = componentInChildren.sharedMesh;
			if (!sharedMesh.isReadable)
			{
				Debug.LogWarning("[LevelBuilder] '" + fbxInstance.name + "' mesh'i okunabilir değil — bariyerler collider'da kalacak. Import ayarı: Read/Write Enabled.");
				return;
			}
			Vector3[] verts = null;
			int[] tris = null;
			CollisionMeshKey key = new CollisionMeshKey(sharedMesh.GetInstanceID(), 0, maxSurfaceY, flatY);
			if (!_collisionMeshCache.TryGetValue(key, out var value) || value == null)
			{
				EnsureSourceArrays(sharedMesh, ref verts, ref tris);
				List<int> collisionTriBuffer = _collisionTriBuffer;
				collisionTriBuffer.Clear();
				for (int i = 0; i < tris.Length; i += 3)
				{
					int num = tris[i];
					int num2 = tris[i + 1];
					int num3 = tris[i + 2];
					if (!(verts[num].y > maxSurfaceY) && !(verts[num2].y > maxSurfaceY) && !(verts[num3].y > maxSurfaceY))
					{
						collisionTriBuffer.Add(num);
						collisionTriBuffer.Add(num2);
						collisionTriBuffer.Add(num3);
					}
				}
				if (collisionTriBuffer.Count == 0)
				{
					Debug.LogError($"[LevelBuilder] '{fbxInstance.name}': maxSurfaceY={maxSurfaceY} ile " + "hiç yol üçgeni kalmadı — eşik yanlış.");
					return;
				}
				Vector3[] array = new Vector3[verts.Length];
				for (int j = 0; j < verts.Length; j++)
				{
					array[j] = new Vector3(verts[j].x, flatY, verts[j].z);
				}
				value = TrackCollisionMeshFactory.CreateRoad(fbxInstance.name, sharedMesh, array, collisionTriBuffer);
				_collisionMeshCache[key] = value;
				Debug.Log("[LevelBuilder] '" + fbxInstance.name + "' yol collider'ı: " + $"{tris.Length / 3} → {collisionTriBuffer.Count / 3} üçgen, y={flatY} seviyesine DÜZLEŞTİRİLDİ " + "(çizgi kabartmaları fizikten çıktı).");
			}
			MeshCollider meshCollider = componentInChildren.GetComponent<MeshCollider>();
			if (meshCollider == null)
			{
				meshCollider = componentInChildren.gameObject.AddComponent<MeshCollider>();
			}
			if (meshCollider.sharedMesh != value)
			{
				meshCollider.sharedMesh = value;
			}
			ApplySurfaceMaterial(meshCollider);
			MeshCollider[] componentsInChildren = fbxInstance.GetComponentsInChildren<MeshCollider>();
			foreach (MeshCollider meshCollider2 in componentsInChildren)
			{
				if (meshCollider2 != meshCollider)
				{
					UnityEngine.Object.Destroy(meshCollider2);
				}
			}
			BuildBarrierCollider(fbxInstance, sharedMesh, ref verts, ref tris, maxSurfaceY, flatY, barrierKills);
		}

		private void BuildBarrierCollider(GameObject fbxInstance, Mesh src, ref Vector3[] verts, ref int[] tris, float maxSurfaceY, float flatY, bool killOnTouch)
		{
			CollisionMeshKey key = new CollisionMeshKey(src.GetInstanceID(), 1, maxSurfaceY, flatY);
			if (!_collisionMeshCache.TryGetValue(key, out var value) || value == null)
			{
				EnsureSourceArrays(src, ref verts, ref tris);
				float num = maxSurfaceY + 0.12f;
				List<int> collisionTriBuffer = _collisionTriBuffer;
				collisionTriBuffer.Clear();
				HashSet<int> collisionVertSet = _collisionVertSet;
				collisionVertSet.Clear();
				for (int i = 0; i < tris.Length; i += 3)
				{
					int num2 = tris[i];
					int num3 = tris[i + 1];
					int num4 = tris[i + 2];
					if (!(verts[num2].y < num) || !(verts[num3].y < num) || !(verts[num4].y < num))
					{
						collisionTriBuffer.Add(num2);
						collisionTriBuffer.Add(num3);
						collisionTriBuffer.Add(num4);
						collisionVertSet.Add(num2);
						collisionVertSet.Add(num3);
						collisionVertSet.Add(num4);
					}
				}
				if (collisionTriBuffer.Count == 0)
				{
					Debug.LogWarning("[LevelBuilder] '" + fbxInstance.name + "': bariyer üçgeni bulunamadı " + $"(eşik {num}). Duvar kurulmadı.");
					return;
				}
				float num5 = flatY + 3f;
				Vector3[] array = new Vector3[verts.Length];
				for (int j = 0; j < verts.Length; j++)
				{
					Vector3 vector = verts[j];
					if (!collisionVertSet.Contains(j))
					{
						array[j] = vector;
					}
					else
					{
						array[j] = new Vector3(vector.x, (vector.y >= num) ? num5 : flatY, vector.z);
					}
				}
				value = TrackCollisionMeshFactory.CreateBarrier(fbxInstance.name, array, collisionTriBuffer);
				_collisionMeshCache[key] = value;
				Debug.Log($"[LevelBuilder] '{fbxInstance.name}' bariyer duvarı: {collisionTriBuffer.Count / 3} üçgen, " + $"y={flatY:0.00} → {num5:0.00} yükseltildi " + (killOnTouch ? "(katı duvar + çarpınca BAŞLANGICA dön)." : "(sadece katı duvar, ceza yok)."));
			}
			GameObject gameObject = new GameObject("Barriers");
			gameObject.transform.SetParent(fbxInstance.transform, worldPositionStays: false);
			MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
			meshCollider.sharedMesh = value;
			meshCollider.convex = false;
			ApplySurfaceMaterial(meshCollider);
			if (killOnTouch)
			{
				gameObject.AddComponent<BarrierKill>();
			}
		}

		private static void EnsureSourceArrays(Mesh src, ref Vector3[] verts, ref int[] tris)
		{
			if (verts == null)
			{
				verts = src.vertices;
			}
			if (tris == null)
			{
				tris = src.triangles;
			}
		}

		private void BuildArena()
		{
			GameObject gameObject = SpawnFbx(_arenaFbx, 4f, "Arena");
			if (gameObject != null)
			{
				MeshCollider[] componentsInChildren = gameObject.GetComponentsInChildren<MeshCollider>();
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					UnityEngine.Object.Destroy(componentsInChildren[i]);
				}
			}
			BuildArenaPhysics();
			for (int j = 0; j < 2; j++)
			{
				GameObject obj = new GameObject($"Goal_Team{j}");
				obj.transform.SetParent(base.transform, worldPositionStays: false);
				obj.transform.position = FbxTracks.ArenaGoalCenter(j);
				BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
				boxCollider.isTrigger = true;
				boxCollider.size = FbxTracks.ArenaGoalSize;
				obj.AddComponent<GoalZone>().Configure(j);
			}
			for (int k = 0; k < 4; k++)
			{
				GameObject gameObject2 = new GameObject($"BoatSpawn_{k}");
				gameObject2.transform.SetParent(base.transform, worldPositionStays: false);
				gameObject2.transform.SetPositionAndRotation(FbxTracks.ArenaBoatSpawn(k, 4), Quaternion.identity);
				_track.RegisterTeamSpawn(k, gameObject2.transform);
			}
		}

		private void BuildArenaPhysics()
		{
			float num = 4f;
			float num2 = 19.5f * num;
			float num3 = 11.5f * num;
			float num4 = 6f * num * 0.25f;
			float num5 = 2f * num;
			GameObject gameObject = new GameObject("ArenaPhysics");
			gameObject.transform.SetParent(base.transform, worldPositionStays: false);
			GameObject obj = new GameObject("Floor");
			obj.transform.SetParent(gameObject.transform, worldPositionStays: false);
			obj.transform.localPosition = new Vector3(0f, -2f, 0f);
			BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
			boxCollider.size = new Vector3(num2 * 2f, 4f, num3 * 2f);
			ApplySurfaceMaterial(boxCollider);
			AddWall(gameObject.transform, new Vector3(0f - num2 - num5 * 0.5f, num4 * 0.5f, 0f), new Vector3(num5, num4, num3 * 2f), "Wall_L");
			AddWall(gameObject.transform, new Vector3(num2 + num5 * 0.5f, num4 * 0.5f, 0f), new Vector3(num5, num4, num3 * 2f), "Wall_R");
			AddWall(gameObject.transform, new Vector3(0f, num4 * 0.5f, 0f - num3 - num5 * 0.5f), new Vector3(num2 * 2f + num5 * 2f, num4, num5), "Wall_B");
			AddWall(gameObject.transform, new Vector3(0f, num4 * 0.5f, num3 + num5 * 0.5f), new Vector3(num2 * 2f + num5 * 2f, num4, num5), "Wall_T");
		}

		private void AddWall(Transform parent, Vector3 localPos, Vector3 size, string name)
		{
			GameObject obj = new GameObject(name);
			obj.transform.SetParent(parent, worldPositionStays: false);
			obj.transform.localPosition = localPos;
			BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
			boxCollider.size = size;
			ApplySurfaceMaterial(boxCollider);
		}

		private void BuildRaceScene()
		{
			GameObject fbxInstance = SpawnFbx(_raceSceneFbx, 2.5f, "Race_Scene");
			BuildRoadCollider(fbxInstance, 0.3f, 0.12f, barrierKills: false);
			BuildFbxCheckpoints(FbxTracks.RaceSceneCheckpoints, 2.5f, looping: true, 9f);
			BuildFbxSpawns(FbxTracks.RaceSceneCheckpoints, 2.5f, looping: true);
			BuildFbxItemBoxes(FbxTracks.RaceSceneItemBoxes, 2.5f);
		}

		private void BuildParkurFbx()
		{
			if (_parkurFbx == null)
			{
				ShowGround(visible: true);
				BuildNarrowStraits();
				return;
			}
			GameObject fbxInstance = SpawnFbx(_parkurFbx, 2f, "Parkur");
			BuildRoadCollider(fbxInstance, 0.5f, 0f, barrierKills: true);
			BuildFbxCheckpoints(FbxTracks.ParkurCheckpoints, 2f, looping: false, 8f);
			BuildFbxSpawns(FbxTracks.ParkurCheckpoints, 2f, looping: false);
			BuildParkurObstacles();
		}

		private void BuildParkurObstacles()
		{
			GameObject[] array = ResolveParkurObstaclePrefabs();
			if (array == null || array.Length == 0)
			{
				return;
			}
			Vector2[] parkurCheckpoints = FbxTracks.ParkurCheckpoints;
			float num = 2f;
			int num2 = 0;
			for (int i = 2; i <= parkurCheckpoints.Length - 3; i += 2)
			{
				GameObject gameObject = array[num2 % array.Length];
				num2++;
				if (gameObject == null)
				{
					continue;
				}
				Vector3 pos = FbxTracks.World(parkurCheckpoints[i], num, 0.5f);
				Quaternion rot = FbxTracks.Heading(parkurCheckpoints, i, looping: false);
				GameObject gameObject2 = Spawn(gameObject, pos, rot, $"Obstacle_{i}");
				if (gameObject2 == null)
				{
					continue;
				}
				gameObject2.transform.localScale = Vector3.one * num;
				MeshCollider[] componentsInChildren = gameObject2.GetComponentsInChildren<MeshCollider>();
				foreach (MeshCollider obj in componentsInChildren)
				{
					if (Application.isPlaying)
					{
						UnityEngine.Object.Destroy(obj);
					}
					else
					{
						UnityEngine.Object.DestroyImmediate(obj);
					}
				}
				GroundObstacle(gameObject2, 0f * num);
				if (gameObject2.GetComponentInChildren<BarrierKill>(includeInactive: true) == null && AddObstacleKillTrigger(gameObject2))
				{
					gameObject2.AddComponent<BarrierKill>().Configure(respawnAtStart: false);
				}
			}
		}

		private GameObject[] ResolveParkurObstaclePrefabs()
		{
			if (_importedParkurObstacles != null)
			{
				for (int i = 0; i < _importedParkurObstacles.Length; i++)
				{
					if (_importedParkurObstacles[i] != null)
					{
						return _importedParkurObstacles;
					}
				}
			}
			return new GameObject[4] { _op1, _op2, _op3, _op4 };
		}

		private static bool AddObstacleKillTrigger(GameObject go)
		{
			MeshFilter[] componentsInChildren = go.GetComponentsInChildren<MeshFilter>();
			if (componentsInChildren.Length == 0)
			{
				return false;
			}
			Vector3 lossyScale = go.transform.lossyScale;
			bool flag = false;
			Bounds bounds = default;
			MeshFilter[] array = componentsInChildren;
			foreach (MeshFilter meshFilter in array)
			{
				if (!(meshFilter.sharedMesh == null))
				{
					Bounds bounds2 = meshFilter.sharedMesh.bounds;
					Vector3 center = go.transform.InverseTransformPoint(meshFilter.transform.TransformPoint(bounds2.center));
					Vector3 lossyScale2 = meshFilter.transform.lossyScale;
					Vector3 size = new Vector3(bounds2.size.x * Mathf.Abs(lossyScale2.x) / Mathf.Max(0.0001f, Mathf.Abs(lossyScale.x)), bounds2.size.y * Mathf.Abs(lossyScale2.y) / Mathf.Max(0.0001f, Mathf.Abs(lossyScale.y)), bounds2.size.z * Mathf.Abs(lossyScale2.z) / Mathf.Max(0.0001f, Mathf.Abs(lossyScale.z)));
					Bounds bounds3 = new Bounds(center, size);
					if (!flag)
					{
						bounds = bounds3;
						flag = true;
					}
					else
					{
						bounds.Encapsulate(bounds3);
					}
				}
			}
			if (!flag)
			{
				return false;
			}
			BoxCollider boxCollider = go.AddComponent<BoxCollider>();
			boxCollider.isTrigger = true;
			boxCollider.center = bounds.center;
			boxCollider.size = bounds.size * 0.75f;
			return true;
		}

		private static void GroundObstacle(GameObject go, float roadWorldY)
		{
			Renderer[] componentsInChildren = go.GetComponentsInChildren<Renderer>();
			if (componentsInChildren.Length != 0)
			{
				Bounds bounds = componentsInChildren[0].bounds;
				for (int i = 1; i < componentsInChildren.Length; i++)
				{
					bounds.Encapsulate(componentsInChildren[i].bounds);
				}
				float num = roadWorldY - 0.1f - bounds.min.y;
				go.transform.position += Vector3.up * num;
			}
		}

		private void BuildLevel1()
		{
			GameObject fbxInstance = SpawnFbx(_level1Fbx, 2f, "Level1");
			BuildRoadCollider(fbxInstance, 0.1f, -0.16f, barrierKills: true);
			BuildLevel1Gates();
			BuildFbxSpawns(FbxTracks.Level1Checkpoints, 2f, looping: false);
			BuildFbxItemBoxes(FbxTracks.Level1ItemBoxes, 2f);
		}

		private void BuildLevel1Gates()
		{
			Vector2[] level1Checkpoints = FbxTracks.Level1Checkpoints;
			float scale = 2f;
			GameObject gameObject = Spawn(pos: FbxTracks.World(level1Checkpoints[0], scale, 1f), rot: FbxTracks.Heading(level1Checkpoints, 0, looping: false), prefab: _checkpointPrefab, name: "StartCheckpoint");
			if (gameObject != null)
			{
				BoxCollider component = gameObject.GetComponent<BoxCollider>();
				if (component != null)
				{
					component.size = new Vector3(18f, 8f, 1.2f);
				}
				gameObject.GetComponent<RaceZone>()?.Configure(RaceZone.ZoneKind.Checkpoint, 0);
				_track.RegisterCheckpoint(0, gameObject.transform);
			}
			int num = level1Checkpoints.Length - 1;
			Vector3 pos = FbxTracks.World(level1Checkpoints[num], scale, 1f);
			Quaternion rot = FbxTracks.Heading(level1Checkpoints, num, looping: false);
			GameObject gameObject2 = Spawn(_finishPrefab, pos, rot, "FINISH");
			if (gameObject2 != null)
			{
				BoxCollider component2 = gameObject2.GetComponent<BoxCollider>();
				if (component2 != null)
				{
					component2.size = new Vector3(18f, 8f, 1.2f);
				}
				gameObject2.GetComponent<RaceZone>()?.Configure(RaceZone.ZoneKind.Finish, 1);
				_track.RegisterFinish(gameObject2.transform);
			}
			_cpIndex = 1;
		}

		private void BuildFbxCheckpoints(Vector2[] pts, float scale, bool looping, float gateHalfWidth)
		{
			for (int i = 0; i < pts.Length; i++)
			{
				bool flag = !looping && i == pts.Length - 1;
				Vector3 pos = FbxTracks.World(pts[i], scale, 1f);
				Quaternion rot = FbxTracks.Heading(pts, i, looping);
				GameObject gameObject = Spawn(flag ? _finishPrefab : _checkpointPrefab, pos, rot, flag ? "FINISH" : ((i == 0) ? "StartFinish" : $"Checkpoint_{i}"));
				if (!(gameObject == null))
				{
					BoxCollider component = gameObject.GetComponent<BoxCollider>();
					if (component != null)
					{
						component.size = new Vector3(gateHalfWidth * 2f, 8f, 1.2f);
					}
					gameObject.GetComponent<RaceZone>()?.Configure(flag ? RaceZone.ZoneKind.Finish : RaceZone.ZoneKind.Checkpoint, i);
					_track.RegisterCheckpoint(i, gameObject.transform);
				}
			}
			_cpIndex = pts.Length;
		}

		private void BuildFbxSpawns(Vector2[] pts, float scale, bool looping)
		{
			Vector3 vector = FbxTracks.World(pts[0], scale, 1.5f);
			Quaternion quaternion = FbxTracks.Heading(pts, 0, looping);
			Vector3 vector2 = quaternion * Vector3.back;
			Vector3 vector3 = quaternion * Vector3.right;
			float num = 1.5f;
			for (int i = 0; i < 4; i++)
			{
				Vector3 position = vector + vector3 * (((float)i - num) * 5f) + vector2 * (4f + (float)i * 1.5f);
				GameObject gameObject = new GameObject($"BoatSpawn_{i}");
				gameObject.transform.SetParent(base.transform, worldPositionStays: false);
				gameObject.transform.SetPositionAndRotation(position, quaternion);
				_track.RegisterTeamSpawn(i, gameObject.transform);
			}
		}

		private void BuildFbxItemBoxes(Vector2[] pts, float scale)
		{
			if (!(_itemBoxPrefab == null))
			{
				for (int i = 0; i < pts.Length; i++)
				{
					Spawn(_itemBoxPrefab, FbxTracks.World(pts[i], scale, 2f), Quaternion.identity, $"ItemBox_{i}")?.GetComponent<ItemBox>()?.Configure(i);
				}
				if (pts.Length > 16)
				{
					Debug.LogWarning($"[LevelBuilder] {pts.Length} item kutusu var ama RaceState.MaxItemBoxes=" + $"{16} → fazlası ağda senkron OLMAZ (kapasiteyi artır).");
				}
			}
		}

		private void BuildSpawns(float z)
		{
			float num = 1.5f;
			for (int i = 0; i < 4; i++)
			{
				GameObject gameObject = new GameObject($"TeamSpawn_{i}");
				gameObject.transform.SetParent(base.transform, worldPositionStays: false);
				gameObject.transform.SetPositionAndRotation(new Vector3(((float)i - num) * _spawnSpacing, 1.2f, z), Quaternion.identity);
				_track.RegisterTeamSpawn(i, gameObject.transform);
			}
		}

		private void Gate(float z, float halfWidth, bool isFinish)
		{
			int num = _cpIndex++;
			GameObject gameObject = Spawn(isFinish ? _finishPrefab : _checkpointPrefab, new Vector3(0f, 1f, z), Quaternion.identity, isFinish ? "FINISH" : $"Checkpoint_{num}");
			if (gameObject == null)
			{
				return;
			}
			BoxCollider component = gameObject.GetComponent<BoxCollider>();
			if (component != null)
			{
				component.size = new Vector3(halfWidth * 2f, 6f, 1.2f);
			}
			int num2 = 0;
			foreach (Transform item in gameObject.transform)
			{
				if (!(item.name != "Post"))
				{
					float x = ((num2++ == 0) ? (-1f) : 1f) * (halfWidth - 0.6f);
					item.localPosition = new Vector3(x, 0.8f, 0f);
				}
			}
			gameObject.GetComponent<RaceZone>()?.Configure(isFinish ? RaceZone.ZoneKind.Finish : RaceZone.ZoneKind.Checkpoint, num);
			_track.RegisterCheckpoint(num, gameObject.transform);
		}

		private void BuildVoidKill()
		{
			GameObject obj = new GameObject("VoidKill");
			obj.transform.SetParent(base.transform, worldPositionStays: false);
			obj.transform.position = new Vector3(0f, -1025f, 0f);
			BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
			boxCollider.isTrigger = true;
			boxCollider.size = new Vector3(100000f, 2000f, 100000f);
			obj.AddComponent<RaceZone>().Configure(RaceZone.ZoneKind.Kill, 0);
		}

		private GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, string name)
		{
			if (prefab == null)
			{
				Debug.LogError("[LevelBuilder] '" + name + "' prefab'ı atanmamış — kurulmadı.");
				return null;
			}
			GameObject obj = UnityEngine.Object.Instantiate(prefab, pos, rot, base.transform);
			obj.name = name;
			return obj;
		}
	}
}

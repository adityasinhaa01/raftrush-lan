using System.Collections.Generic;
using PuffPirates.Boats;
using PuffPirates.Networking;
using PuffPirates.Visuals;
using UnityEngine;

namespace PuffPirates.Obstacles
{
	[RequireComponent(typeof(PickupSpawnPoint))]
	public class RandomPickup : FieldObstacle
	{
		[Tooltip("Renklenecek/gizlenecek orb Renderer (boşsa alt Renderer'lar).")]
		[SerializeField]
		private Renderer _orbRenderer;

		[Tooltip("Gizlenecek görsel kök (boşsa alt Renderer'lar tek tek kapatılır).")]
		[SerializeField]
		private GameObject _visualRoot;

		[Header("Toplama VFX (Break_Items — VFX_Coins)")]
		[Tooltip("Toplanınca (görünür→gizli) doğan tek-atım kutlama efekti (ör. VFX_Coins). Boşsa yok.")]
		[SerializeField]
		private GameObject _collectVfxPrefab;

		[Tooltip("Doğan toplama efekti bu süre sonra silinir (sn).")]
		[SerializeField]
		[Min(0.2f)]
		private float _collectVfxLifetime = 2f;

		private PickupSpawnPoint _spawn;

		private Collider _collider;

		private bool _visibleCached = true;

		private bool _collectInit;

		private PickupEffectType _colorCached;

		private static readonly List<RandomPickup> _active = new List<RandomPickup>();

		public static IReadOnlyList<RandomPickup> Active => _active;

		public int SpawnId
		{
			get
			{
				if (!(_spawn != null))
				{
					return -1;
				}
				return _spawn.SpawnId;
			}
		}

		public PickupEffectType[] Allowed
		{
			get
			{
				if (!(_spawn != null))
				{
					return null;
				}
				return _spawn.Allowed;
			}
		}

		private void Awake()
		{
			_spawn = GetComponent<PickupSpawnPoint>();
			_collider = GetComponent<Collider>();
			if (_orbRenderer == null)
			{
				_orbRenderer = GetComponentInChildren<Renderer>(includeInactive: true);
			}
		}

		private void OnEnable()
		{
			if (!_active.Contains(this))
			{
				_active.Add(this);
			}
		}

		private void OnDisable()
		{
			_active.Remove(this);
		}

		private void Update()
		{
			RaceState instance = RaceState.Instance;
			if (instance == null || _spawn == null)
			{
				return;
			}
			int spawnId = _spawn.SpawnId;
			bool flag = spawnId < 0 || instance.IsPickupAvailable(spawnId);
			bool flag2 = !_collectInit;
			if (flag2 || flag != _visibleCached)
			{
				bool visibleCached = _visibleCached;
				_visibleCached = flag;
				_collectInit = true;
				if (_visualRoot != null)
				{
					_visualRoot.SetActive(flag);
				}
				else
				{
					Renderer[] componentsInChildren = GetComponentsInChildren<Renderer>(includeInactive: true);
					for (int i = 0; i < componentsInChildren.Length; i++)
					{
						componentsInChildren[i].enabled = flag;
					}
				}
				if (_collider != null)
				{
					_collider.enabled = flag;
				}
				if ((!flag2 & visibleCached) && !flag && _collectVfxPrefab != null)
				{
					GameObject gameObject = Object.Instantiate(_collectVfxPrefab, base.transform.position, Quaternion.identity);
					if (gameObject != null)
					{
						Object.Destroy(gameObject, _collectVfxLifetime);
					}
				}
			}
			if (flag && _orbRenderer != null)
			{
				PickupEffectType pickupEffectType = ((spawnId < 0) ? PickupEffectType.GasBoost : instance.CurrentEffect(spawnId));
				if (pickupEffectType != _colorCached)
				{
					_colorCached = pickupEffectType;
					RendererColorUtility.SetColor(_orbRenderer, PickupCatalog.Get(pickupEffectType).Color);
				}
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			NetworkBoat networkBoat = FieldObstacle.HostBoat(other);
			if (networkBoat == null)
			{
				return;
			}
			RaceState instance = RaceState.Instance;
			if (instance == null || _spawn == null)
			{
				return;
			}
			int spawnId = _spawn.SpawnId;
			if (spawnId < 0 || instance.IsPickupAvailable(spawnId))
			{
				PickupEffectType type = ((spawnId < 0) ? PickupEffectType.GasBoost : instance.CurrentEffect(spawnId));
				networkBoat.ApplyPickupEffect(type);
				if (spawnId >= 0)
				{
					instance.PickUpPickup(spawnId, _spawn.RespawnSeconds);
				}
			}
		}
	}
}

using System;
using System.Collections.Generic;
using FishNet.Managing;
using FishNet.Object;
using UnityEngine;

namespace PuffPirates.Obstacles.Physics
{
	public sealed class SeesawSpawner : MonoBehaviour
	{
		[Header("Disk Prefab")]
		[Tooltip("Seesaw_Disk.prefab — kökünde NetworkObject ve SeesawDisk bulunmalıdır.")]
		[SerializeField]
		private NetworkObject _diskPrefab;

		private const float BakedDiskRadius = 4f;

		private const float BakedDiskThickness = 0.4f;

		private const float BakedPoleRadius = 0.3f;

		private const float BakedPoleHeight = 2f;

		[Header("Direk (statik görsel, unit scale)")]
		[SerializeField]
		private Transform _poleVisual;

		[SerializeField]
		private MeshFilter _poleMeshFilter;

		[SerializeField]
		private MeshCollider _poleCollider;

		[SerializeField]
		private Mesh _poleBaseMesh;

		[SerializeField]
		[Min(0.1f)]
		private float _poleHeight = 2f;

		[SerializeField]
		[Min(0.05f)]
		private float _poleRadius = 0.3f;

		private Mesh _runtimePoleMesh;

		[Header("Sahne Yerleştirme Önizlemesi")]
		[Tooltip("Editörde gerçek disk görünümünü taşır. Oyun başlarken otomatik kapanır; fizik ve ağ nesnesi değildir.")]
		[SerializeField]
		private Transform _previewRoot;

		[SerializeField]
		private Transform _previewVisual;

		[SerializeField]
		private MeshFilter[] _previewMeshFilters;

		[SerializeField]
		private Mesh[] _previewBaseMeshes;

		private Mesh[] _runtimePreviewMeshes;

		[Header("Disk Yapılandırması")]
		[SerializeField]
		[Min(0.5f)]
		private float _diskRadius = 4f;

		[SerializeField]
		[Min(0.05f)]
		private float _diskThickness = 0.4f;

		[SerializeField]
		[Min(0.1f)]
		private float _mass = 180f;

		[SerializeField]
		[Range(1f, 45f)]
		private float _maxTiltAngleDeg = 18f;

		[SerializeField]
		[Min(0f)]
		private float _tiltSpringForce = 900f;

		[SerializeField]
		[Min(0f)]
		private float _tiltDamping = 350f;

		[SerializeField]
		[Min(1f)]
		private float _returnToCenterForce = 3000f;

		[SerializeField]
		[Min(0f)]
		private float _rotationResistance = 5f;

		private NetworkManager _network;

		private NetworkObject _spawnedDisk;

		private float _nextSpawnAttemptAt;

		private void Awake()
		{
			ApplyPlacementVisuals();
			SetPreviewVisible(visible: false);
		}

		private void OnEnable()
		{
			ApplyPlacementVisuals();
			if (Application.isPlaying)
			{
				SetPreviewVisible(visible: false);
			}
		}

		private void OnDisable()
		{
			if (Application.isPlaying)
			{
				DespawnOwnedDisk();
			}
		}

		private void OnDestroy()
		{
			if (Application.isPlaying)
			{
				DespawnOwnedDisk();
			}
			ReleaseGeneratedMeshes();
		}

		private void Update()
		{
			if (!Application.isPlaying || _diskPrefab == null || Time.unscaledTime < _nextSpawnAttemptAt)
			{
				return;
			}
			IReadOnlyList<NetworkManager> instances = NetworkManager.Instances;
			NetworkManager networkManager = ((instances.Count > 0) ? instances[0] : null);
			if (networkManager != _network)
			{
				DespawnOwnedDisk();
				_network = networkManager;
				_nextSpawnAttemptAt = 0f;
			}
			if (_network == null || !_network.IsServerStarted)
			{
				_spawnedDisk = null;
				_nextSpawnAttemptAt = Time.unscaledTime + 0.25f;
				return;
			}
			if (_spawnedDisk != null)
			{
				if (_spawnedDisk.IsSpawned)
				{
					return;
				}
				_spawnedDisk = null;
			}
			try
			{
				NetworkObject networkObject = UnityEngine.Object.Instantiate(_diskPrefab, base.transform.position, base.transform.rotation);
				_network.ServerManager.Spawn(networkObject, null, base.gameObject.scene);
				if (!networkObject.IsSpawned)
				{
					throw new InvalidOperationException("ServerManager.Spawn failed.");
				}
				_spawnedDisk = networkObject;
				if (!networkObject.TryGetComponent<SeesawDisk>(out var component))
				{
					_network.ServerManager.Despawn(networkObject);
					_spawnedDisk = null;
					throw new MissingComponentException("Spawn edilen NetworkObject üzerinde SeesawDisk yok.");
				}
				component.Configure(_diskRadius, _diskThickness, _mass, _maxTiltAngleDeg, _tiltSpringForce, _tiltDamping, _returnToCenterForce, _rotationResistance);
				if (_poleCollider != null && component.CollisionCollider != null)
				{
					UnityEngine.Physics.IgnoreCollision(component.CollisionCollider, _poleCollider, ignore: true);
				}
			}
			catch (Exception ex)
			{
				_spawnedDisk = null;
				_nextSpawnAttemptAt = Time.unscaledTime + 1f;
				Debug.LogError("[SeesawSpawner] Disk oluşturulamadı; 1 saniye sonra tekrar denenecek. " + ex.Message, this);
			}
		}

		private void DespawnOwnedDisk()
		{
			NetworkObject spawnedDisk = _spawnedDisk;
			_spawnedDisk = null;
			if (spawnedDisk == null || !spawnedDisk.IsSpawned || !spawnedDisk.IsServerInitialized)
			{
				return;
			}
			try
			{
				spawnedDisk.Despawn();
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[SeesawSpawner] Disk temizlenirken hata oluştu: " + ex.Message, this);
			}
		}

		private void ApplyPlacementVisuals()
		{
			float num = Mathf.Max(0.05f, _diskThickness);
			base.transform.localScale = Vector3.one;
			if (_poleVisual != null)
			{
				_poleVisual.localScale = Vector3.one;
				_poleVisual.localRotation = Quaternion.identity;
				_poleVisual.localPosition = new Vector3(0f, 0f - (num * 0.5f + _poleHeight * 0.5f), 0f);
			}
			Mesh mesh = SeesawUnitScaleMesh.Apply(requestedScale: new Vector3(_poleRadius / 0.3f, _poleHeight / 2f, _poleRadius / 0.3f), baseMesh: _poleBaseMesh, generatedMesh: ref _runtimePoleMesh, generatedName: "SeesawPole_Runtime");
			if (_poleMeshFilter != null && mesh != null)
			{
				_poleMeshFilter.sharedMesh = mesh;
			}
			if (_poleCollider != null && mesh != null)
			{
				_poleCollider.sharedMesh = null;
				_poleCollider.sharedMesh = mesh;
			}
			if (_previewRoot != null)
			{
				_previewRoot.localPosition = Vector3.zero;
				_previewRoot.localRotation = Quaternion.identity;
				_previewRoot.localScale = Vector3.one;
			}
			if (_previewVisual != null)
			{
				_previewVisual.localPosition = Vector3.zero;
				_previewVisual.localRotation = Quaternion.identity;
				_previewVisual.localScale = Vector3.one;
			}
			int a = ((_previewMeshFilters != null) ? _previewMeshFilters.Length : 0);
			int b = ((_previewBaseMeshes != null) ? _previewBaseMeshes.Length : 0);
			int num2 = Mathf.Min(a, b);
			if (_runtimePreviewMeshes == null || _runtimePreviewMeshes.Length != num2)
			{
				SeesawUnitScaleMesh.ReleaseAll(_runtimePreviewMeshes);
				_runtimePreviewMeshes = new Mesh[num2];
			}
			Vector3 requestedScale = new Vector3(_diskRadius / 4f, num / 0.4f, _diskRadius / 4f);
			for (int i = 0; i < num2; i++)
			{
				MeshFilter meshFilter = _previewMeshFilters[i];
				Mesh mesh2 = _previewBaseMeshes[i];
				if (!(meshFilter == null) && !(mesh2 == null))
				{
					Mesh mesh3 = SeesawUnitScaleMesh.Apply(mesh2, ref _runtimePreviewMeshes[i], requestedScale, mesh2.name + "_Preview");
					if (mesh3 != null)
					{
						meshFilter.sharedMesh = mesh3;
					}
				}
			}
		}

		private void ReleaseGeneratedMeshes()
		{
			SeesawUnitScaleMesh.Release(ref _runtimePoleMesh);
			SeesawUnitScaleMesh.ReleaseAll(_runtimePreviewMeshes);
			_runtimePreviewMeshes = null;
		}

		private void SetPreviewVisible(bool visible)
		{
			if (_previewRoot != null && _previewRoot.gameObject.activeSelf != visible)
			{
				_previewRoot.gameObject.SetActive(visible);
			}
		}

		private void OnDrawGizmosSelected()
		{
			PhysicsObstacleBase.GizmoDisc(base.transform.position, _diskRadius, Vector3.up, new Color(0.2f, 0.8f, 1f, 0.5f));
			PhysicsObstacleBase.GizmoArrow(base.transform.position, Vector3.down * _poleHeight, Color.yellow);
		}
	}
}

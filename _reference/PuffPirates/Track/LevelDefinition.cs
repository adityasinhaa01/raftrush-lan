using System;
using System.Collections.Generic;
using GamesMrkt.Localization;
using UnityEngine;
using UnityEngine.Rendering;

namespace PuffPirates.Track
{
	[DisallowMultipleComponent]
	public class LevelDefinition : MonoBehaviour
	{
		[Header("Kimlik / Davranış")]
		[SerializeField]
		private string _mapName = "LEVEL";

		[Tooltip("Kapalı devre mi? (tur sayacı yalnız looping haritada çalışır)")]
		[SerializeField]
		private bool _looping;

		[Tooltip("Açık: sahnedeki düz zemin gizlenir (yoldan çıkan bot boşluğa düşer → VoidKill).")]
		[SerializeField]
		private bool _hidesGround = true;

		[Tooltip("Açık: bu haritada botlar HACİM-TABANLI buoyancy (Arşimet, batma/yüzme) ile hareket eder. KAPALI: su kanalı olsa bile ESKİ mekanik (raycast yay + eğime yatma) kullanılır → 'bouncy'siz kuru sürüş. Su OLMAYAN haritalarda değeri önemsiz (zaten buoyancy tetiklenmez).")]
		[SerializeField]
		private bool _useBuoyancy = true;

		[Header("Konteynerler (boşsa 'Checkpoints'/'Spawns' adlı çocuk aranır)")]
		[Tooltip("Doğrudan çocukları SIRAYLA checkpoint (id=0..n). Her biri CheckpointGate (RaceZone) olmalı.")]
		[SerializeField]
		private Transform _checkpointsRoot;

		[Tooltip("Doğrudan çocukları takım spawn'ı (id=0..n). Boşsa ilk checkpoint kullanılır.")]
		[SerializeField]
		private Transform _spawnsRoot;

		[Header("Kenar bariyeri (bake'te collider mesh'i kayboldu → load'da yeniden üretilir)")]
		[Tooltip("Açık: yol mesh'inden duvar collider'ı yeniden üretilir (çarpınca öldürür).")]
		[SerializeField]
		private bool _regenBarrier;

		[Tooltip("Yol yüzeyinin native (ölçeksiz) max Y'si — bunun üstü bariyer sayılır.")]
		[SerializeField]
		private float _roadMaxY;

		[Tooltip("Yol native (ölçeksiz) taban Y'si — duvar tabanı buraya çekilir.")]
		[SerializeField]
		private float _roadY;

		[Tooltip("Açık: duvara çarpan bot başlangıca döner (BarrierKill).")]
		[SerializeField]
		private bool _barrierKills = true;

		[Header("Kenar duvarı KILL KÜPLERİ (mesh dikey yüzlerinden — Y-bağımsız)")]
		[Tooltip("Açık: duvarlara kill-küp trigger'ları yerleştirilir (değince patlar). Yakın-culling'li.")]
		[SerializeField]
		private bool _wallKillCubes;

		[Tooltip("Duvara değen bot başlangıca mı döner (aksi: son checkpoint).")]
		[SerializeField]
		private bool _wallKillAtStart = true;

		[Header("AYRI MESH/MATERYAL COLLIDER MODU (Blender: yol ve duvar AYRI obje/materyal)")]
		[Tooltip("Blender'da yol ve duvar AYRI mesh/materyal ise aç → isim/materyalden ayır, collider kur. AÇIKSA yukarıdaki Y-eşiği (_regenBarrier) yolu ATLANIR — bu YENİ yol onun YERİNE geçer.")]
		[SerializeField]
		private bool _separateMeshColliders;

		[Tooltip("Yol sayılacak isim/materyal anahtar kelimeleri (virgülle ayrılmış, büyük/küçük harf duyarsız).")]
		[SerializeField]
		private string _roadNameTokens = "road,yol";

		[Tooltip("Duvar sayılacak isim/materyal anahtar kelimeleri (virgülle ayrılmış, büyük/küçük harf duyarsız).")]
		[SerializeField]
		private string _wallNameTokens = "wall,duvar,barrier";

		[Tooltip("Açık: duvara çarpan bot öldürülüp respawn edilir (BarrierKill.Configure ile aktarılır).")]
		[SerializeField]
		private bool _wallKills = true;

		private static readonly Dictionary<string, (Mesh road, Mesh barrier)> _regenMeshCache = new Dictionary<string, (Mesh, Mesh)>();

		public string MapName
		{
			get
			{
				string mapName = _mapName;
				if (!(mapName == "STAGE 1 (SU)"))
				{
					if (mapName == "PLATFORM")
					{
						return Loc.Get("map.platform");
					}
					return _mapName;
				}
				return Loc.Get("map.stage1");
			}
		}

		public bool Looping => _looping;

		public bool HidesGround => _hidesGround;

		public bool UseBuoyancy => _useBuoyancy;

		public void Apply(RaceTrack track)
		{
			if (_separateMeshColliders)
			{
				SetupSeparateMeshColliders();
			}
			else
			{
				HealColliders(base.gameObject);
				if (_regenBarrier)
				{
					RegenBarrier();
				}
			}
			if (_wallKillCubes)
			{
				BuildWallKill();
			}
			if (track == null)
			{
				return;
			}
			Transform transform = ((_checkpointsRoot != null) ? _checkpointsRoot : base.transform.Find("Checkpoints"));
			Transform transform2 = ((_spawnsRoot != null) ? _spawnsRoot : base.transform.Find("Spawns"));
			if (transform != null)
			{
				int childCount = transform.childCount;
				for (int i = 0; i < childCount; i++)
				{
					Transform child = transform.GetChild(i);
					RaceZone component = child.GetComponent<RaceZone>();
					if (component != null && component.Kind == RaceZone.ZoneKind.Finish)
					{
						track.RegisterFinish(child);
					}
					if (!(component != null) || component.Kind == RaceZone.ZoneKind.Checkpoint)
					{
						int id = ((component != null) ? component.CheckpointId : i);
						track.RegisterCheckpoint(id, child);
					}
				}
			}
			if (transform2 != null && transform2.childCount > 0)
			{
				for (int j = 0; j < transform2.childCount; j++)
				{
					track.RegisterTeamSpawn(j, transform2.GetChild(j));
				}
			}
			else if (transform != null && transform.childCount > 0)
			{
				Transform child2 = transform.GetChild(0);
				for (int k = 0; k < 8; k++)
				{
					track.RegisterTeamSpawn(k, child2);
				}
			}
		}

		private static void HealColliders(GameObject root)
		{
			MeshCollider[] componentsInChildren = root.GetComponentsInChildren<MeshCollider>(includeInactive: true);
			foreach (MeshCollider meshCollider in componentsInChildren)
			{
				if (!(meshCollider.sharedMesh != null))
				{
					MeshFilter component = meshCollider.GetComponent<MeshFilter>();
					if (component != null && component.sharedMesh != null)
					{
						meshCollider.sharedMesh = component.sharedMesh;
					}
				}
			}
		}

		private void SetupSeparateMeshColliders()
		{
			string[] tokens = SplitTokens(_roadNameTokens);
			string[] tokens2 = SplitTokens(_wallNameTokens);
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			MeshFilter[] componentsInChildren = GetComponentsInChildren<MeshFilter>(includeInactive: true);
			foreach (MeshFilter meshFilter in componentsInChildren)
			{
				if (meshFilter.sharedMesh == null || meshFilter.name.StartsWith("~"))
				{
					continue;
				}
				bool num4 = MatchesTokens(meshFilter, tokens2);
				bool flag = !num4 && MatchesTokens(meshFilter, tokens);
				if (num4)
				{
					MeshCollider meshCollider = meshFilter.GetComponent<MeshCollider>();
					if (meshCollider == null)
					{
						meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
					}
					meshCollider.convex = false;
					meshCollider.isTrigger = false;
					meshCollider.sharedMesh = meshFilter.sharedMesh;
					BarrierKill barrierKill = meshFilter.GetComponent<BarrierKill>();
					if (_wallKills)
					{
						if (barrierKill == null)
						{
							barrierKill = meshFilter.gameObject.AddComponent<BarrierKill>();
						}
						barrierKill.Configure(respawnAtStart: true);
					}
					else if (barrierKill != null)
					{
						UnityEngine.Object.Destroy(barrierKill);
					}
					num2++;
				}
				else if (flag)
				{
					MeshCollider meshCollider2 = meshFilter.GetComponent<MeshCollider>();
					if (meshCollider2 == null)
					{
						meshCollider2 = meshFilter.gameObject.AddComponent<MeshCollider>();
					}
					meshCollider2.convex = false;
					meshCollider2.isTrigger = false;
					meshCollider2.sharedMesh = meshFilter.sharedMesh;
					num++;
				}
				else
				{
					num3++;
				}
			}
			Debug.Log($"[LevelDefinition] Ayrı mesh collider taraması: {num} yol, {num2} duvar, " + $"{num3} atlanan (kozmetik — isim/materyal hiçbir token'a uymadı).");
		}

		private static string[] SplitTokens(string csv)
		{
			if (string.IsNullOrWhiteSpace(csv))
			{
				return Array.Empty<string>();
			}
			string[] array = csv.Split(',');
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = array[i].Trim();
			}
			return array;
		}

		private static bool MatchesTokens(MeshFilter mf, string[] tokens)
		{
			if (tokens.Length == 0)
			{
				return false;
			}
			if (ContainsAny(mf.gameObject.name, tokens))
			{
				return true;
			}
			if (mf.sharedMesh != null && ContainsAny(mf.sharedMesh.name, tokens))
			{
				return true;
			}
			MeshRenderer component = mf.GetComponent<MeshRenderer>();
			if (component != null)
			{
				Material[] sharedMaterials = component.sharedMaterials;
				foreach (Material material in sharedMaterials)
				{
					if (material != null && ContainsAny(material.name, tokens))
					{
						return true;
					}
				}
			}
			return false;
		}

		private static bool ContainsAny(string text, string[] tokens)
		{
			if (string.IsNullOrEmpty(text))
			{
				return false;
			}
			foreach (string value in tokens)
			{
				if (!string.IsNullOrEmpty(value) && text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}
			return false;
		}

		private void RegenBarrier()
		{
			Mesh mesh = null;
			MeshFilter meshFilter = null;
			int num = 0;
			MeshFilter[] componentsInChildren = GetComponentsInChildren<MeshFilter>(includeInactive: true);
			foreach (MeshFilter meshFilter2 in componentsInChildren)
			{
				if (!(meshFilter2.sharedMesh == null) && !meshFilter2.name.StartsWith("~") && meshFilter2.sharedMesh.vertexCount > num)
				{
					num = meshFilter2.sharedMesh.vertexCount;
					mesh = meshFilter2.sharedMesh;
					meshFilter = meshFilter2;
				}
			}
			if (mesh == null)
			{
				return;
			}
			if (!mesh.isReadable)
			{
				Debug.LogWarning("[LevelDefinition] yol mesh'i okunabilir değil (Read/Write Enabled) — bariyer üretilemedi.");
				return;
			}
			string key = mesh.GetInstanceID() + "|" + _roadMaxY.ToString("R") + "|" + _roadY.ToString("R");
			if (!_regenMeshCache.TryGetValue(key, out (Mesh, Mesh) value))
			{
				value = BuildRegenMeshes(mesh);
				_regenMeshCache[key] = value;
			}
			if (value.Item1 != null)
			{
				MeshCollider meshCollider = meshFilter.GetComponent<MeshCollider>();
				if (meshCollider == null)
				{
					meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
				}
				meshCollider.convex = false;
				(meshCollider.sharedMesh, _) = value;
			}
			if (!(value.Item2 != null))
			{
				return;
			}
			Transform transform = null;
			BarrierKill[] componentsInChildren2 = GetComponentsInChildren<BarrierKill>(includeInactive: true);
			foreach (BarrierKill barrierKill in componentsInChildren2)
			{
				if (barrierKill.GetComponent<MeshCollider>() != null)
				{
					transform = barrierKill.transform;
					break;
				}
			}
			if (transform == null)
			{
				GameObject obj = new GameObject("Barriers");
				obj.transform.SetParent(meshFilter.transform, worldPositionStays: false);
				transform = obj.transform;
			}
			MeshCollider meshCollider2 = transform.GetComponent<MeshCollider>();
			if (meshCollider2 == null)
			{
				meshCollider2 = transform.gameObject.AddComponent<MeshCollider>();
			}
			meshCollider2.convex = false;
			meshCollider2.sharedMesh = value.Item2;
			if (_barrierKills && transform.GetComponent<BarrierKill>() == null)
			{
				transform.gameObject.AddComponent<BarrierKill>();
			}
		}

		private (Mesh road, Mesh barrier) BuildRegenMeshes(Mesh road)
		{
			Vector3[] vertices = road.vertices;
			int[] triangles = road.triangles;
			Mesh mesh = null;
			List<int> list = new List<int>(triangles.Length);
			for (int i = 0; i < triangles.Length; i += 3)
			{
				int num = triangles[i];
				int num2 = triangles[i + 1];
				int num3 = triangles[i + 2];
				if (!(vertices[num].y > _roadMaxY) && !(vertices[num2].y > _roadMaxY) && !(vertices[num3].y > _roadMaxY))
				{
					list.Add(num);
					list.Add(num2);
					list.Add(num3);
				}
			}
			if (list.Count > 0)
			{
				Vector3[] array = new Vector3[vertices.Length];
				for (int j = 0; j < vertices.Length; j++)
				{
					array[j] = new Vector3(vertices[j].x, _roadY, vertices[j].z);
				}
				mesh = new Mesh
				{
					name = "Road_Regen",
					indexFormat = road.indexFormat
				};
				mesh.vertices = array;
				mesh.SetTriangles(list, 0);
				mesh.RecalculateBounds();
			}
			float num4 = _roadMaxY + 0.12f;
			float num5 = _roadY + 3f;
			List<int> list2 = new List<int>(triangles.Length / 4);
			HashSet<int> hashSet = new HashSet<int>();
			for (int k = 0; k < triangles.Length; k += 3)
			{
				int num6 = triangles[k];
				int num7 = triangles[k + 1];
				int num8 = triangles[k + 2];
				if (!(vertices[num6].y < num4) || !(vertices[num7].y < num4) || !(vertices[num8].y < num4))
				{
					list2.Add(num6);
					list2.Add(num7);
					list2.Add(num8);
					hashSet.Add(num6);
					hashSet.Add(num7);
					hashSet.Add(num8);
				}
			}
			if (list2.Count == 0)
			{
				Debug.LogWarning($"[LevelDefinition] bariyer üçgeni yok (eşik {num4}). Duvar kurulmadı.");
				return (road: mesh, barrier: null);
			}
			Vector3[] array2 = new Vector3[vertices.Length];
			for (int l = 0; l < vertices.Length; l++)
			{
				Vector3 vector = vertices[l];
				array2[l] = (hashSet.Contains(l) ? new Vector3(vector.x, (vector.y >= num4) ? num5 : _roadY, vector.z) : vector);
			}
			Mesh mesh2 = new Mesh
			{
				name = "Barrier_Regen",
				indexFormat = IndexFormat.UInt32
			};
			mesh2.vertices = array2;
			mesh2.SetTriangles(list2, 0);
			mesh2.RecalculateBounds();
			return (road: mesh, barrier: mesh2);
		}

		private void BuildWallKill()
		{
			MeshFilter meshFilter = null;
			int num = 0;
			MeshFilter[] componentsInChildren = GetComponentsInChildren<MeshFilter>(includeInactive: true);
			foreach (MeshFilter meshFilter2 in componentsInChildren)
			{
				if (meshFilter2.sharedMesh != null && !meshFilter2.name.StartsWith("~") && meshFilter2.sharedMesh.vertexCount > num)
				{
					num = meshFilter2.sharedMesh.vertexCount;
					meshFilter = meshFilter2;
				}
			}
			if (!(meshFilter == null))
			{
				WallKillField wallKillField = base.gameObject.GetComponent<WallKillField>();
				if (wallKillField == null)
				{
					wallKillField = base.gameObject.AddComponent<WallKillField>();
				}
				wallKillField.Build(meshFilter, 0.5f, 8f, 1.5f, 0.3f, _wallKillAtStart);
			}
		}
	}
}

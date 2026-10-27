using System.Collections.Generic;
using PuffPirates.Boats;
using UnityEngine;

namespace PuffPirates.Track
{
	[DisallowMultipleComponent]
	public class WallKillField : MonoBehaviour
	{
		private readonly List<BoxCollider> _cubes = new List<BoxCollider>();

		private Vector3[] _cubeWorldCenters;

		private readonly Vector3[] _boatPos = new Vector3[4];

		private float _nextCull;

		private const float CullRadius = 45f;

		private const float CullInterval = 0.3f;

		public void Build(MeshFilter roadMf, float wallNormalMax, float cellSize, float padNative, float minWallSpan, bool killAtStart)
		{
			if (roadMf == null || roadMf.sharedMesh == null)
			{
				return;
			}
			Mesh sharedMesh = roadMf.sharedMesh;
			if (!sharedMesh.isReadable)
			{
				Debug.LogWarning("[WallKillField] yol mesh'i okunabilir değil (Read/Write Enabled) — kill küpleri kurulmadı.");
				return;
			}
			Vector3[] vertices = sharedMesh.vertices;
			int[] triangles = sharedMesh.triangles;
			Dictionary<long, Bounds> dictionary = new Dictionary<long, Bounds>();
			for (int i = 0; i < triangles.Length; i += 3)
			{
				Vector3 vector = vertices[triangles[i]];
				Vector3 vector2 = vertices[triangles[i + 1]];
				Vector3 vector3 = vertices[triangles[i + 2]];
				Vector3 vector4 = Vector3.Cross(vector2 - vector, vector3 - vector);
				float magnitude = vector4.magnitude;
				if (!(magnitude < 1E-06f) && !(Mathf.Abs(vector4.y / magnitude) >= wallNormalMax))
				{
					Vector3 vector5 = (vector + vector2 + vector3) / 3f;
					int num = Mathf.FloorToInt(vector5.x / cellSize);
					int num2 = Mathf.FloorToInt(vector5.z / cellSize);
					long key = ((long)num << 32) ^ (uint)num2;
					Bounds bounds = new Bounds(vector, Vector3.zero);
					bounds.Encapsulate(vector2);
					bounds.Encapsulate(vector3);
					if (dictionary.TryGetValue(key, out var value))
					{
						value.Encapsulate(bounds);
						dictionary[key] = value;
					}
					else
					{
						dictionary[key] = bounds;
					}
				}
			}
			foreach (KeyValuePair<long, Bounds> item in dictionary)
			{
				Bounds value2 = item.Value;
				if (!(value2.size.y < minWallSpan))
				{
					GameObject obj = new GameObject("KillCube");
					obj.transform.SetParent(roadMf.transform, worldPositionStays: false);
					BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
					boxCollider.isTrigger = true;
					boxCollider.center = value2.center;
					boxCollider.size = value2.size + Vector3.one * padNative;
					boxCollider.enabled = false;
					obj.AddComponent<BarrierKill>().Configure(killAtStart, 2f, DeathCause.Wall);
					_cubes.Add(boxCollider);
				}
			}
			_cubeWorldCenters = new Vector3[_cubes.Count];
			for (int j = 0; j < _cubes.Count; j++)
			{
				_cubeWorldCenters[j] = _cubes[j].transform.TransformPoint(_cubes[j].center);
			}
			Debug.Log($"[WallKillField] {_cubes.Count} kill-küp kuruldu ({dictionary.Count} duvar hücresinden).");
		}

		private void FixedUpdate()
		{
			if (Time.time < _nextCull || _cubes.Count == 0)
			{
				return;
			}
			_nextCull = Time.time + 0.3f;
			List<NetworkBoat> all = NetworkBoat.All;
			float num = 2025f;
			int num2 = Mathf.Min(all.Count, _boatPos.Length);
			int num3 = 0;
			for (int i = 0; i < num2; i++)
			{
				NetworkBoat networkBoat = all[i];
				if (!(networkBoat == null))
				{
					_boatPos[num3++] = networkBoat.transform.position;
				}
			}
			for (int j = 0; j < _cubes.Count; j++)
			{
				BoxCollider boxCollider = _cubes[j];
				if (boxCollider == null)
				{
					continue;
				}
				Vector3 vector = _cubeWorldCenters[j];
				bool flag = false;
				for (int k = 0; k < num3; k++)
				{
					if ((_boatPos[k] - vector).sqrMagnitude < num)
					{
						flag = true;
						break;
					}
				}
				if (boxCollider.enabled != flag)
				{
					boxCollider.enabled = flag;
				}
			}
		}
	}
}

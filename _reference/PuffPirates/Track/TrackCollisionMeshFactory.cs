using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PuffPirates.Track
{
	internal static class TrackCollisionMeshFactory
	{
		public static Mesh CreateRoad(string name, Mesh source, Vector3[] vertices, List<int> triangles)
		{
			Mesh mesh = new Mesh();
			mesh.name = name + "_Road";
			mesh.indexFormat = source.indexFormat;
			mesh.vertices = vertices;
			mesh.SetTriangles(triangles, 0);
			mesh.RecalculateBounds();
			return mesh;
		}

		public static Mesh CreateBarrier(string name, Vector3[] vertices, List<int> triangles)
		{
			Mesh mesh = new Mesh();
			mesh.name = name + "_Barrier";
			mesh.indexFormat = IndexFormat.UInt32;
			mesh.vertices = vertices;
			mesh.SetTriangles(triangles, 0);
			mesh.RecalculateBounds();
			return mesh;
		}
	}
}

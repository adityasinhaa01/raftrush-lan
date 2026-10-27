using UnityEngine;

namespace PuffPirates.Obstacles.Physics
{
	internal static class SeesawUnitScaleMesh
	{
		private const float MinScale = 0.0001f;

		private const float UnitTolerance = 1E-05f;

		public static Mesh Apply(Mesh baseMesh, ref Mesh generatedMesh, Vector3 requestedScale, string generatedName)
		{
			if (baseMesh == null)
			{
				Release(ref generatedMesh);
				return null;
			}
			Vector3 vector = ClampScale(requestedScale);
			if (!Application.isPlaying && ApproximatelyOne(vector))
			{
				Release(ref generatedMesh);
				return baseMesh;
			}
			EnsureGeneratedMesh(baseMesh, ref generatedMesh, generatedName);
			ApplyVertices(baseMesh, generatedMesh, vector);
			ApplyNormals(baseMesh, generatedMesh, vector);
			ApplyTangents(baseMesh, generatedMesh, vector);
			generatedMesh.RecalculateBounds();
			return generatedMesh;
		}

		public static void Release(ref Mesh generatedMesh)
		{
			if (!(generatedMesh == null))
			{
				if (Application.isPlaying)
				{
					Object.Destroy(generatedMesh);
				}
				else
				{
					Object.DestroyImmediate(generatedMesh);
				}
				generatedMesh = null;
			}
		}

		public static void ReleaseAll(Mesh[] generatedMeshes)
		{
			if (generatedMeshes != null)
			{
				for (int i = 0; i < generatedMeshes.Length; i++)
				{
					Release(ref generatedMeshes[i]);
				}
			}
		}

		private static void EnsureGeneratedMesh(Mesh baseMesh, ref Mesh generatedMesh, string generatedName)
		{
			if (!(generatedMesh != null))
			{
				generatedMesh = Object.Instantiate(baseMesh);
				generatedMesh.name = generatedName;
				generatedMesh.hideFlags = HideFlags.HideAndDontSave;
				generatedMesh.MarkDynamic();
			}
		}

		private static void ApplyVertices(Mesh source, Mesh destination, Vector3 scale)
		{
			Vector3[] vertices = source.vertices;
			for (int i = 0; i < vertices.Length; i++)
			{
				vertices[i] = Vector3.Scale(vertices[i], scale);
			}
			destination.vertices = vertices;
		}

		private static void ApplyNormals(Mesh source, Mesh destination, Vector3 scale)
		{
			Vector3[] normals = source.normals;
			if (normals.Length == source.vertexCount)
			{
				Vector3 b = Inverse(scale);
				for (int i = 0; i < normals.Length; i++)
				{
					normals[i] = Vector3.Scale(normals[i], b).normalized;
				}
				destination.normals = normals;
			}
		}

		private static void ApplyTangents(Mesh source, Mesh destination, Vector3 scale)
		{
			Vector4[] tangents = source.tangents;
			if (tangents.Length == source.vertexCount)
			{
				for (int i = 0; i < tangents.Length; i++)
				{
					Vector3 a = new Vector3(tangents[i].x, tangents[i].y, tangents[i].z);
					a = Vector3.Scale(a, scale).normalized;
					tangents[i] = new Vector4(a.x, a.y, a.z, tangents[i].w);
				}
				destination.tangents = tangents;
			}
		}

		private static Vector3 ClampScale(Vector3 value)
		{
			return new Vector3(Mathf.Max(0.0001f, value.x), Mathf.Max(0.0001f, value.y), Mathf.Max(0.0001f, value.z));
		}

		private static Vector3 Inverse(Vector3 value)
		{
			return new Vector3(1f / value.x, 1f / value.y, 1f / value.z);
		}

		private static bool ApproximatelyOne(Vector3 value)
		{
			if (Mathf.Abs(value.x - 1f) <= 1E-05f && Mathf.Abs(value.y - 1f) <= 1E-05f)
			{
				return Mathf.Abs(value.z - 1f) <= 1E-05f;
			}
			return false;
		}
	}
}

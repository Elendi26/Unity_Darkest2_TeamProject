using System.Collections.Generic;
using UnityEngine;

public static class CNodeGroundCoverage
{
	private const float MIN_UP_NORMAL = 0.5f;

	public static bool TryBuild
		(
		Transform source,
		Transform space,
		float sampleSize,
		float groundMaxHeight,
		HashSet<Vector2Int> ground,
		HashSet<Vector2Int> objects,
		int objectPadding = 0
		)
	{
		ground.Clear();
		objects.Clear();

		if (source == null || space == null || sampleSize <= 0f)
		{
			return false;
		}

		MeshFilter[] filters = source.GetComponentsInChildren<MeshFilter>(true);
		List<Vector3> vertices = new List<Vector3>();
		List<int> triangles = new List<int>();
		HashSet<Vector2Int> rawObjects = new HashSet<Vector2Int>();
		bool anyRead = false;

		for (int f = 0; f < filters.Length; f++)
		{
			Mesh mesh = filters[f].sharedMesh;

			// 읽기 꺼진 메시는 건너뛴다 → 하나도 못 읽으면 상자 방식으로 돌아간다
			if (mesh == null || !mesh.isReadable)
			{
				continue;
			}

			anyRead = true;

			Matrix4x4 toSpace = space.worldToLocalMatrix * filters[f].transform.localToWorldMatrix;

			mesh.GetVertices(vertices);

			for (int s = 0; s < mesh.subMeshCount; s++)
			{
				if (mesh.GetTopology(s) != MeshTopology.Triangles)
				{
					continue;
				}

				mesh.GetTriangles(triangles, s);

				for (int t = 0; t + 2 < triangles.Count; t += 3)
				{
					Vector3 a = toSpace.MultiplyPoint3x4(vertices[triangles[t]]);
					Vector3 b = toSpace.MultiplyPoint3x4(vertices[triangles[t + 1]]);
					Vector3 c = toSpace.MultiplyPoint3x4(vertices[triangles[t + 2]]);
					Vector3 normal = Vector3.Cross(b - a, c - a);

					if (normal.sqrMagnitude < 0.000001f)
					{
						continue;
					}

					float top = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
					// 뒤집힌 메시도 있어 위·아래 방향은 따지지 않는다
					bool isFlat = Mathf.Abs(normal.normalized.y) >= MIN_UP_NORMAL;

					if (top <= groundMaxHeight && isFlat)
					{
						MarkTriangle(a, b, c, sampleSize, ground);
					}
					else if (top > groundMaxHeight)
					{
						MarkTriangle(a, b, c, sampleSize, rawObjects);
					}
				}
			}
		}

		// 물체 점을 objectPadding 점만큼 넓힌다 → 크게 넓히면 가로등 하나에도 장식 칸이 통째로 지워지니 기본은 0
		foreach (Vector2Int point in rawObjects)
		{
			for (int dx = -objectPadding; dx <= objectPadding; dx++)
			{
				for (int dy = -objectPadding; dy <= objectPadding; dy++)
				{
					objects.Add(new Vector2Int(point.x + dx, point.y + dy));
				}
			}
		}

		return anyRead;
	}

	// 삼각형의 XZ 투영 안에 든 점을 모두 기록한다
	private static void MarkTriangle(Vector3 a, Vector3 b, Vector3 c, float size, HashSet<Vector2Int> result)
	{
		Vector2 pa = new Vector2(a.x, a.z);
		Vector2 pb = new Vector2(b.x, b.z);
		Vector2 pc = new Vector2(c.x, c.z);
		int minI = Mathf.CeilToInt(Mathf.Min(pa.x, Mathf.Min(pb.x, pc.x)) / size - 0.5f);
		int maxI = Mathf.FloorToInt(Mathf.Max(pa.x, Mathf.Max(pb.x, pc.x)) / size - 0.5f);
		int minJ = Mathf.CeilToInt(Mathf.Min(pa.y, Mathf.Min(pb.y, pc.y)) / size - 0.5f);
		int maxJ = Mathf.FloorToInt(Mathf.Max(pa.y, Mathf.Max(pb.y, pc.y)) / size - 0.5f);

		for (int i = minI; i <= maxI; i++)
		{
			for (int j = minJ; j <= maxJ; j++)
			{
				Vector2 point = new Vector2((i + 0.5f) * size, (j + 0.5f) * size);

				if (IsInTriangle(point, pa, pb, pc))
				{
					result.Add(new Vector2Int(i, j));
				}
			}
		}
	}

	private static bool IsInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
	{
		float d1 = Cross(p, a, b);
		float d2 = Cross(p, b, c);
		float d3 = Cross(p, c, a);
		bool hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
		bool hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;

		return !(hasNegative && hasPositive);
	}

	private static float Cross(Vector2 p, Vector2 a, Vector2 b)
	{
		return (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
	}
}

using System;
using System.Collections.Generic;
using UnityEngine;

// 칸 하나 → 종류 + 다음 행 어느 칸으로 이어지는지 + 그 길의 종류
[Serializable]
public class CFixedMapNode
{
	public CNodeTypeData Type;
	// 다음 행에서 이어질 칸 번호 (비우면 다음 행 모든 칸으로 잇는다)
	public int[] NextLanes;
	// 이 칸에서 나가는 길 (비우면 사건 없는 길)
	public CRouteTypeData Route;
	// 나가는 길의 도로 사건 수
	public int RoadEventCount;
}

// 행 하나 → 왼쪽부터 칸
[Serializable]
public class CFixedMapRow
{
	public CFixedMapNode[] Nodes;
}

// 튜토리얼처럼 매번 같은 지도를 쓸 때 → 행·칸·길을 직접 적어 둔다 (무작위 생성 대신)
[CreateAssetMenu(menuName = "Map/Fixed Map_")]
public class CFixedMapData : ScriptableObject
{
	#region 인스펙터
	// 첫 행은 출발, 마지막 행은 도착 (칸 하나씩 권장)
	[SerializeField] private CFixedMapRow[] _rows;
	// 처음부터 모든 칸·길을 지도에 보여 준다
	[SerializeField] private bool _revealAll = true;
	#endregion

	// 행 목록
	public IReadOnlyList<CFixedMapRow> Rows => _rows;

	/// <summary>
	/// 적어 둔 행·칸·길로 지도 그래프를 만든다 (형식이 잘못되면 null) / 경태
	/// </summary>
	public CMapGraph Build(int seed)
	{
		if (_rows == null || _rows.Length < 2)
		{
			CPrint.Error($"CFixedMapData : {name} 에 행이 2개 이상 필요합니다.");
			return null;
		}

		CMapGraph graph = new CMapGraph(seed);

		for (int row = 0; row < _rows.Length; row++)
		{
			int count = _rows[row] != null && _rows[row].Nodes != null ? _rows[row].Nodes.Length : 0;

			if (count == 0)
			{
				CPrint.Error($"CFixedMapData : {name} 의 {row} 행에 칸이 없습니다.");
				return null;
			}

			IReadOnlyList<CNodeInstance> nodes = graph.AddRow(count);

			for (int lane = 0; lane < count; lane++)
			{
				nodes[lane].TypeData = _rows[row].Nodes[lane].Type;
				nodes[lane].IsRevealed = _revealAll;
			}
		}

		for (int row = 0; row < _rows.Length - 1; row++)
		{
			LinkRow(graph, row);
		}

		return graph;
	}

	/// <summary>
	/// 지도에 쓰인 칸 종류를 모은다 (도착 처리기 등록용) / 경태
	/// </summary>
	public void CollectNodeTypes(List<CNodeTypeData> result)
	{
		if (_rows == null)
		{
			return;
		}

		for (int row = 0; row < _rows.Length; row++)
		{
			if (_rows[row] == null || _rows[row].Nodes == null)
			{
				continue;
			}

			for (int lane = 0; lane < _rows[row].Nodes.Length; lane++)
			{
				CNodeTypeData type = _rows[row].Nodes[lane].Type;

				if (type != null && !result.Contains(type))
				{
					result.Add(type);
				}
			}
		}
	}

	private void LinkRow(CMapGraph graph, int row)
	{
		IReadOnlyList<CNodeInstance> current = graph.GetRow(row);
		IReadOnlyList<CNodeInstance> next = graph.GetRow(row + 1);

		for (int lane = 0; lane < current.Count; lane++)
		{
			CFixedMapNode data = _rows[row].Nodes[lane];
			int[] lanes = data.NextLanes;

			// 비우면 다음 행 모든 칸으로
			if (lanes == null || lanes.Length == 0)
			{
				for (int j = 0; j < next.Count; j++)
				{
					Link(graph, current[lane], next[j], data);
				}

				continue;
			}

			for (int j = 0; j < lanes.Length; j++)
			{
				if (lanes[j] < 0 || lanes[j] >= next.Count)
				{
					CPrint.Warn($"CFixedMapData : {name} {row} 행 {lane} 칸의 다음 칸 번호 {lanes[j]} 가 범위를 벗어나 건너뜁니다.");
					continue;
				}

				Link(graph, current[lane], next[lanes[j]], data);
			}
		}

		// 들어오는 길이 없는 칸은 갈 수 없다 → 알린다
		for (int j = 0; j < next.Count; j++)
		{
			if (next[j].InLinks.Count == 0)
			{
				CPrint.Warn($"CFixedMapData : {name} {row + 1} 행 {j} 칸으로 들어오는 길이 없습니다.");
			}
		}
	}

	private void Link(CMapGraph graph, CNodeInstance from, CNodeInstance to, CFixedMapNode data)
	{
		CNodeLink link = graph.Connect(from, to);

		link.RouteData = data.Route;
		link.RoadEventCount = data.Route != null ? Mathf.Max(0, data.RoadEventCount) : 0;
		link.IsRevealed = _revealAll;
	}
}

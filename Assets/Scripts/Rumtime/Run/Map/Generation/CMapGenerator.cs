using System;
using System.Collections.Generic;
using UnityEngine;

public class CMapGenerator
{
    #region 내부 변수
    private readonly CMapConfig _config;
    private readonly Dictionary<CNodeTypeData, int> _typeCounts = new Dictionary<CNodeTypeData, int>();
    private readonly List<CNodeTypeData> _nodeCandidates = new List<CNodeTypeData>();
    private readonly HashSet<CNodeTypeData> _rowTypes = new HashSet<CNodeTypeData>();
    private System.Random _random;
    #endregion

    public CMapGenerator(CMapConfig config)
    {
        _config = config;
    }

    public CMapGraph Generate(int seed)
    {
        if (_config == null)
        {
            CPrint.Error("맵 설정이 없습니다.");
            return null;
        }

        if (_config.RowCount < 2)
        {
            CPrint.Error("최소 2개 행이 필요합니다.");
            return null;
        }

        if (_config.PreEndNode != null && _config.RowCount < 3)
        {
            CPrint.Warn("CMapGenerator : 행이 3개 미만이라 여관 직전 확정 노드를 넣지 않습니다.");
        }

        if (_config.PreStartNode != null && !IsPreStartRow(1, _config.RowCount - 1))
        {
            CPrint.Warn("CMapGenerator : 행이 모자라 출발 다음 확정 노드(교차로)를 넣지 않습니다.");
        }

        _random = new System.Random(seed);
        _typeCounts.Clear();

        CMapGraph graph = new CMapGraph(seed);

        BuildRows(graph);
        LinkRows(graph);
        AssignNodeTypes(graph);
        AssignRoutes(graph);

        return graph;
    }

    private void BuildRows(CMapGraph graph)
    {
        int lastRow = _config.RowCount - 1;

        for (int row = 0; row <= lastRow; row++)
        {
            // 첫 행·마지막 행·출발 다음/여관 직전 확정 행은 노드 1개
            bool isEdge = row == 0 || row == lastRow || IsPreStartRow(row, lastRow) || IsPreEndRow(row, lastRow);
            int count = isEdge ? 1 : _random.Next(_config.MinNodePerRow, _config.MaxNodePerRow + 1);

            graph.AddRow(count);
        }
    }

    private void LinkRows(CMapGraph graph)
    {
        for (int row = 0; row < graph.RowCount -1; row++)
        {
            IReadOnlyList<CNodeInstance> current = graph.GetRow(row);
            IReadOnlyList<CNodeInstance> next = graph.GetRow(row + 1);
            int i = 0;
            int j = 0;

            graph.Connect(current[0], next[0]);

            while (i < current.Count - 1 || j < next.Count - 1)
            {
                bool canMoveCurrent = i < current.Count - 1;
                bool canMoveNext = j < next.Count - 1;

                if (canMoveCurrent && canMoveNext)
                {
                    double roll = _random.NextDouble();

                    if (roll < _config.BranchChance * 0.5)
                    {
                        i++;
                    }
                    else if (roll < _config.BranchChance)
                    {
                        j++;
                    }
                    else
                    {
                        i++;
                        j++;
                    }
                }
                else if (canMoveCurrent)
                {
                    i++;
                }
                else
                {
                    j++;
                }

                graph.Connect(current[i], next[j]);
            }
        }
    }

    private void AssignNodeTypes(CMapGraph graph)
    {
        int lastRow = graph.RowCount - 1;

        for (int row = 0; row <= lastRow; row++)
        {
            IReadOnlyList<CNodeInstance> nodes = graph.GetRow(row);

            // 행이 바뀌면 같은 행 중복 기록을 비운다
            _rowTypes.Clear();

            for (int i = 0; i < nodes.Count; i++)
            {
                CNodeInstance node = nodes[i];

                if (row == 0)
                {
                    node.TypeData = _config.StarNode;
                    node.IsRevealed = true;
                }
                else if (row == lastRow)
                {
                    node.TypeData = _config.EndNode;
                    node.IsRevealed = true;
                }
                else if (IsPreStartRow(row, lastRow))
                {
                    // 출발하자마자 모든 경로가 모이는 곳이라 처음부터 공개
                    node.TypeData = _config.PreStartNode;
                    node.IsRevealed = true;
                }
                else if (IsPreEndRow(row, lastRow))
                {
                    // 반드시 지나가는 노드라 처음부터 공개
                    node.TypeData = _config.PreEndNode;
                    node.IsRevealed = true;
                }
                else
                {
                    node.TypeData = PickNodeType(node);
                }

                AddTypeCount(node.TypeData);

                if (node.TypeData != null)
                {
                    _rowTypes.Add(node.TypeData);
                }
            }
        }
    }

    private CNodeTypeData PickNodeType(CNodeInstance node)
    {
        CollectCandidates(node);

        if (_nodeCandidates.Count == 0)
        {
            return _config.fallbackNode;
        }

        return PickWeighted(_nodeCandidates, item => item.Weight);
    }

    // 노드 풀에서 지금 노드에 놓을 수 있는 종류만 모은다
    private void CollectCandidates(CNodeInstance node)
    {
        _nodeCandidates.Clear();

        for (int i = 0; i < _config.NodePool.Count; i++)
        {
            CNodeTypeData data = _config.NodePool[i];

            if (data == null)
            {
                continue;
            }

            // 행 중복 금지 종류(상점·병원 등)가 이미 이 행에 있다
            if (data.UniqueInRow && _rowTypes.Contains(data))
            {
                continue;
            }

            if (!CanPlace(data, node))
            {
                continue;
            }

            _nodeCandidates.Add(data);
        }
    }

    // 여관 직전 확정 노드를 넣는 행인지
    private bool IsPreEndRow(int row, int lastRow)
    {
        return _config.PreEndNode != null && lastRow >= 2 && row == lastRow - 1;
    }

    // 출발 바로 다음 확정 노드(교차로)를 넣는 행인지
    //  ㄴ 마지막 행·여관 직전 행과 겹치면 넣지 않는다
    private bool IsPreStartRow(int row, int lastRow)
    {
        return _config.PreStartNode != null && row == 1 && row < lastRow && !IsPreEndRow(row, lastRow);
    }

    private bool CanPlace(CNodeTypeData data, CNodeInstance node)
    {
        // 출발·여관·확정 노드는 정해진 행 전용
        if (data == _config.StarNode || data == _config.EndNode || data == _config.PreStartNode || data == _config.PreEndNode)
        {
            return false;
        }

        if (node.Row < data.MinRow)
        {
            return false;
        }

        if (GetTypeCount(data) >= data.MaxPerBiome)
        {
            return false;
        }

        if (data.AllowConsecutive)
        {
            return true;
        }

        for (int i = 0; i < node.InLinks.Count; i++)
        {
            if (node.InLinks[i].From.TypeData == data)
            {
                return false;
            }
        }

        return true;
    }

    private void AssignRoutes(CMapGraph graph)
    {
        int lastRow = graph.RowCount - 1;

        for (int i = 0; i < graph.Links.Count; i++)
        {
            CNodeLink link = graph.Links[i];

            // 출발→교차로, 성채→여관 길은 고정 길(안전한 길) → 시작과 끝은 위험 없이
            CRouteTypeData route = IsFixedEdgeLink(link, lastRow)
                ? _config.FixedEdgeRoute
                : PickWeighted(_config.RoutePool, item => item.Weight);

            link.RouteData = route;

            if (route == null)
            {
                link.RoadEventCount = 0;
                continue;
            }

            link.RoadEventCount = _random.Next(route.MinRoadEvents, route.MaxRadEvents + 1);
        }
    }

    // 고정 길을 깔아야 하는 연결인지 (출발→교차로 / 성채→여관)
    private bool IsFixedEdgeLink(CNodeLink link, int lastRow)
    {
        if (_config.FixedEdgeRoute == null || link.From == null || link.To == null)
        {
            return false;
        }

        int toRow = link.To.Row;

        if (link.From.Row == 0 && IsPreStartRow(toRow, lastRow))
        {
            return true;
        }

        return toRow == lastRow && IsPreEndRow(link.From.Row, lastRow);
    }

    private T PickWeighted<T>(IReadOnlyList<T> items, Func<T, float> getWeight) where T : class
    {
        if (items == null || items.Count == 0)
        {
            return null;
        }

        float total = 0f;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null)
            {
                continue;
            }

            total += Mathf.Max(0f, getWeight(items[i]));
        }

        if (total <= 0f)
        {
            return items[0];
        }

        double roll = _random.NextDouble() * total;
        float sum = 0f;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null)
            {
                continue;
            }
        
            sum += Mathf.Max(0f, getWeight(items[i]));

            if (roll < sum)
            {
                return items[i];
            }
        }

        return items[items.Count - 1];
    }

    private int GetTypeCount(CNodeTypeData data)
    {
        return _typeCounts.TryGetValue(data, out int count) ? count : 0;
    }

    private void AddTypeCount(CNodeTypeData data)
    {
        if (data == null)
        {
            return;
        }

        _typeCounts[data] = GetTypeCount(data) + 1;
    }
}

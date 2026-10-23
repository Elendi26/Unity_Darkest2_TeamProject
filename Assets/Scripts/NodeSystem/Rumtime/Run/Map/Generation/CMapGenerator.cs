using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CMapGenerator
{
    #region 내부 변수
    private readonly CMapConfig _config;
    private readonly Dictionary<CNodeTypeData, int> _typeCounts = new Dictionary<CNodeTypeData, int>();
    private readonly List<CNodeTypeData> _nodeCandidates = new List<CNodeTypeData>();
    private readonly HashSet<CNodeTypeData> _rowTypes = new HashSet<CNodeTypeData>();	// 지금 행에 이미 놓인 종류
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
            bool isEdge = row == 0 || row == lastRow;
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

            // 행 중복 금지 종류(상점·병원 등)가 이미 이 행에 있다 → 제외. 나머지 종류는 겹쳐도 된다
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

    private bool CanPlace(CNodeTypeData data, CNodeInstance node)
    {
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
        for (int i = 0; i < graph.Links.Count; i++)
        {
            CNodeLink link = graph.Links[i];
            CRouteTypeData route = PickWeighted(_config.RoutePool, item => item.Weight);

            link.RouteData = route;

            if (route == null)
            {
                link.RoadEventCount = 0;
                continue;
            }

            link.RoadEventCount = _random.Next(route.MinRoadEvents, route.MaxRadEvents + 1);
        }
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

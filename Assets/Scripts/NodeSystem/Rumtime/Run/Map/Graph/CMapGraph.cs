using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CMapGraph
{
    #region 내부 변수
    private readonly int _seed;
    private readonly List<List<CNodeInstance>> _rows = new List<List<CNodeInstance>>();
    private readonly List<CNodeInstance> _nodes = new List<CNodeInstance>();
    private readonly List<CNodeLink> _links = new List<CNodeLink>();
    #endregion

    public int Seed => _seed;
    public int RowCount => _rows.Count;
    public IReadOnlyList<CNodeInstance> Nodes => _nodes;
    public IReadOnlyList<CNodeLink> Links => _links;
    public CNodeInstance StartNode => _rows.Count > 0 ? _rows[0][0] : null;

    public CMapGraph(int seed)
    {
        _seed = seed;
    }

    public IReadOnlyList<CNodeInstance> GetRow(int row)
    {
        if (row < 0 || row >= _rows.Count)
        {
            return null;
        }

        return _rows[row];
    }

    public IReadOnlyList<CNodeInstance> AddRow(int count)
    {
        int row = _rows.Count;
        List<CNodeInstance> nodes = new List<CNodeInstance>(count);

        for (int lane = 0; lane < count; lane++)
        {
            CNodeInstance node = new CNodeInstance(_nodes.Count, row, lane);
            nodes.Add(node);
            _nodes.Add(node);
        }

        _rows.Add(nodes);
        return nodes;
    }

    public CNodeLink Connect(CNodeInstance from, CNodeInstance to)
    {
        if (from == null || to == null)
        {
            return null;
        }

        CNodeLink existing = FindLink(from, to);

        if (existing != null)
        {
            return existing;
        }

        CNodeLink link = new CNodeLink(from, to);
        from.AddOutLink(link);
        to.AddInLink(link);
        _links.Add(link);
        return link;
    }

    public CNodeLink FindLink(CNodeInstance from, CNodeInstance to)
    {
        if (from == null)
        {
            return null;
        }

        for (int i = 0; i < from.OutLinks.Count; i++)
        {
            if (from.OutLinks[i].To == to)
            {
                return from.OutLinks[i];
            }
        }

        return null;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CNodeInstance
{
    #region 내부 변수
    private readonly int _id;
    private readonly int _row;
    private readonly int _lane;
    private readonly List<CNodeLink> _outLinks = new List<CNodeLink>();
    private readonly List<CNodeLink> _inLinks = new List<CNodeLink>();
    private CNodeTypeData _typeData;
    private bool _isVisited;
    private bool _isRevealed;
    #endregion

    public int Id => _id;
    public int Row => _row;
    public int Lane => _lane;
    public IReadOnlyList<CNodeLink> OutLinks => _outLinks;
    public IReadOnlyList<CNodeLink> InLinks => _inLinks;
    public CNodeTypeData TypeData { get => _typeData; set => _typeData = value; }
    public bool IsVisited { get => _isVisited; set => _isVisited = value; }
    public bool IsRevealed { get => _isRevealed; set => _isRevealed = value; }
    public bool IsEnd => _outLinks.Count == 0;

    public CNodeInstance(int id, int row, int lane)
    {
        _id = id;
        _row = row;
        _lane = lane;
    }

    public void AddOutLink(CNodeLink link)
    {
        _outLinks.Add(link);
    }

    public void AddInLink(CNodeLink link)
    {
        _inLinks.Add(link);
    }
}

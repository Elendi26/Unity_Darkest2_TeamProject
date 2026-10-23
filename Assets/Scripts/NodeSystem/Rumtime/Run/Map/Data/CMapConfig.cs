using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Map/Map Config_")]
public class CMapConfig : ScriptableObject
{
    #region ÀÎ½ºÆåÅÍ
    [SerializeField] private int _rowCount = 6;
    [SerializeField] private int _minNodePerRow = 1;
    [SerializeField] private int _maxNodePerRow = 3;
    [SerializeField, Range(0f, 1f)] private float _branchChance = 0.4f;

    [SerializeField] private CNodeTypeData _startNode;
    [SerializeField] private CNodeTypeData _endNode;
    [SerializeField] private CNodeTypeData _fallbackNode;

    [SerializeField] private CNodeTypeData[] _nodePool;
    [SerializeField] private CRouteTypeData[] _routePool;
    #endregion

    public int RowCount => _rowCount;
    public int MinNodePerRow => _minNodePerRow;
    public int MaxNodePerRow => _maxNodePerRow;
    public float BranchChance => _branchChance;
    public CNodeTypeData StarNode => _startNode;
    public CNodeTypeData EndNode => _endNode;
    public CNodeTypeData fallbackNode => _fallbackNode;
    public IReadOnlyList<CNodeTypeData> NodePool => _nodePool;
    public IReadOnlyList<CRouteTypeData> RoutePool => _routePool;
}

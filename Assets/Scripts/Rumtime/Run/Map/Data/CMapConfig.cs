using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Map/Map Config_")]
public class CMapConfig : ScriptableObject
{
    #region 인스펙터
    [SerializeField] private int _rowCount = 6;
    [SerializeField] private int _minNodePerRow = 1;
    [SerializeField] private int _maxNodePerRow = 3;
    [SerializeField, Range(0f, 1f)] private float _branchChance = 0.4f;

    [SerializeField] private CNodeTypeData _startNode;
    [SerializeField] private CNodeTypeData _preStartNode;
    [SerializeField] private CNodeTypeData _endNode;
    [SerializeField] private CNodeTypeData _preEndNode;
    [SerializeField] private CNodeTypeData _fallbackNode;

    [SerializeField] private CNodeTypeData[] _nodePool;
    [SerializeField] private CRouteTypeData[] _routePool;
    [SerializeField] private CRouteTypeData _fixedEdgeRoute;

    [Header("정찰 (지역 진입 시 한 번만 판정)")]
    [SerializeField, Range(0f, 1f)] private float _nodeScoutChance = 0.5f;
    [SerializeField, Range(0f, 1f)] private float _routeScoutChance = 0.5f;
    #endregion

    public int RowCount => _rowCount;
    public int MinNodePerRow => _minNodePerRow;
    public int MaxNodePerRow => _maxNodePerRow;
    public float BranchChance => _branchChance;
    public CNodeTypeData StarNode => _startNode;
    public CNodeTypeData PreStartNode => _preStartNode;
    public CNodeTypeData EndNode => _endNode;
    public CNodeTypeData PreEndNode => _preEndNode;
    public CNodeTypeData fallbackNode => _fallbackNode;
    public IReadOnlyList<CNodeTypeData> NodePool => _nodePool;
    public IReadOnlyList<CRouteTypeData> RoutePool => _routePool;
    public CRouteTypeData FixedEdgeRoute => _fixedEdgeRoute;
    public float NodeScoutChance => _nodeScoutChance;
    public float RouteScoutChance => _routeScoutChance;
}

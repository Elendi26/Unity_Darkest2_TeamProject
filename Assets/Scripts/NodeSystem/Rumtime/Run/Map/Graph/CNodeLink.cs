using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CNodeLink
{
    #region 내부 변수
    private readonly CNodeInstance _from;
    private readonly CNodeInstance _to;
    private CRouteTypeData _routeData;
    private int _roadEventCount;
    #endregion

    public CNodeInstance From => _from;
    public CNodeInstance To => _to;
    public CRouteTypeData RouteData { get => _routeData; set => _routeData = value; }
    public int RoadEventCount { get => _roadEventCount; set => _roadEventCount = value; }
    public CNodeLink(CNodeInstance from, CNodeInstance to)
    {
        _from = from;
        _to = to;
    }
}

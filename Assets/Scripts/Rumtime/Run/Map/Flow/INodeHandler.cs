using System;
using System.Collections.Generic;
using UnityEngine;

public interface INodeHandler
{
    ENodeType Type { get; }

    void Enter(CNodeInstance node, Action onComplete);
}

// 노드 종류 여러 개를 한 컴포넌트가 받을 때 (전투 → 전투·괴물 소굴·성채)
public interface INodeHandlerGroup
{
    IReadOnlyList<INodeHandler> ExtraHandlers { get; }
}

public interface IRoadTravel
{
    void Travel(CNodeLink link, Action onArrived);
}

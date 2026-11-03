using System;
using UnityEngine;

public interface INodeHandler
{
    ENodeType Type { get; }

    void Enter(CNodeInstance node, Action onComplete);
}

public interface IRoadTravel
{
    void Travel(CNodeLink link, Action onArrived);
}

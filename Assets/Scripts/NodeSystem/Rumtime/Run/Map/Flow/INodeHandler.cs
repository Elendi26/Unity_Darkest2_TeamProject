using System;
using System.Collections;
using System.Collections.Generic;
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

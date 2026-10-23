using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CPassNodeHandler : INodeHandler
{
    #region 내부 변수
    private readonly ENodeType _type;
    #endregion

    public ENodeType Type => _type;

    public CPassNodeHandler(ENodeType type)
    {
        _type = type;
    }

    public void Enter(CNodeInstance node, Action onComplete)
    {
        CPrint.Log($"{Type} 노드 통과 : ID {node.Id}, 행 {node.Row}");
        onComplete?.Invoke();
    }
}

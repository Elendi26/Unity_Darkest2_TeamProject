using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CMapRunner
{
    #region 이벤트
    public event Action<EMapState, EMapState> OnStateChanged;
    public event Action<CNodeInstance> OnNodeEntered;
    #endregion

    #region 내부 변수
    private readonly CMapGraph _graph;
    private readonly IRoadTravel _travel;
    private readonly Dictionary<ENodeType, INodeHandler> _handlers = new Dictionary<ENodeType, INodeHandler>();
    private EMapState _state = EMapState.None;
    private CNodeInstance _current;
    private bool _logState = true;
    #endregion

    public EMapState State => _state;
    public CNodeInstance Current => _current;
    public CMapGraph Graph => _graph;
    public CMapRunner(CMapGraph graph, IRoadTravel travel)
    {
        _graph = graph;
        _travel = travel;
    }

    public void RegisterHandler(INodeHandler handler)
    {
        if (handler == null)
        {
            return;
        }

        if (_handlers.ContainsKey(handler.Type))
        {
            CPrint.Warn($"CMapRunner : {handler.Type} 핸들러를 덮어씁니다.");
        }

        _handlers[handler.Type] = handler;
    }

    public void Begin()
    {
        if (_graph == null || _graph.StartNode == null)
        {
            CPrint.Warn("맵이 없습니다.");
            return;
        }

        _current = _graph.StartNode;
        _current.IsVisited = true;
        SetState(EMapState.SelectingPath);
    }

    public bool IsSelectable(CNodeLink link)
    {
        if (link == null)
        {
            return false;
        }

        return _state == EMapState.SelectingPath && link.From == _current;
    }

    public bool TrySelectPath(CNodeLink link)
    {
        if (!IsSelectable(link))
        {
            return false;
        }

        SetState(EMapState.Traveling);

        if (_travel == null)
        {
            Arrive(link.To);
            return true;
        }

        _travel.Travel(link, () => Arrive(link.To));
        return true;
    }

    private void Arrive(CNodeInstance node)
    {
        _current = node;
        _current.IsVisited = true;
        _current.IsRevealed = true;

        SetState(EMapState.InNode);
        EnterCurrentNode();
    }

    private void EnterCurrentNode()
    {
        OnNodeEntered?.Invoke(_current);

        ENodeType type = _current.TypeData != null ? _current.TypeData.Type : ENodeType.None;

        if (!_handlers.TryGetValue(type, out INodeHandler handler))
        {
            CPrint.Warn($"{type} 핸들러가 없어 노드를 건너뜁니다.");
            FinishNode();
            return;
        }

        handler.Enter(_current, FinishNode);
    }

    private void FinishNode()
    {
        if (_state != EMapState.InNode)
        {
            return;
        }

        SetState(_current.IsEnd ? EMapState.Completed : EMapState.SelectingPath);
    }

    private void SetState(EMapState next)
    {
        if (_state == next)
        {
            return;
        }

        EMapState prev = _state;
        _state = next;

        if (_logState)
        {
            CPrint.Log($"{prev} → {_state}");
        }

        switch (_state)
        {
            case EMapState.Completed:
                CPrint.Success("바이옴 완료");
                break;
        }

        OnStateChanged?.Invoke(prev, _state);
    }

    public bool HasHandler(ENodeType type)
    {
        return _handlers.ContainsKey(type);
    }
}

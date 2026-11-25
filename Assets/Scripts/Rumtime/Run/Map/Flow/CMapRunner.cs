using System;
using System.Collections.Generic;
using UnityEngine;

public class CMapRunner
{
    #region 이벤트
    public event Action<EMapState, EMapState> OnStateChanged;
    public event Action<CNodeInstance> OnNodeEntered;
    public event Action OnRevealed;
    #endregion

    #region 내부 변수
    private readonly CMapGraph _graph;
    private readonly IRoadTravel _travel;
    private readonly Dictionary<ENodeType, INodeHandler> _handlers = new Dictionary<ENodeType, INodeHandler>();
    private EMapState _state = EMapState.None;
    private CNodeInstance _current;
    private INodeArrivalPresenter _arrivalPresenter;
    private CNodeInstance _enteredNode;
    private bool _logState = true;
    private readonly List<CNodeInstance> _revealFrontier = new List<CNodeInstance>();
    private readonly List<CNodeInstance> _revealNext = new List<CNodeInstance>();
    #endregion

    public EMapState State => _state;
    public CNodeInstance Current => _current;
    public CMapGraph Graph => _graph;
    public CMapRunner(CMapGraph graph, IRoadTravel travel)
    {
        _graph = graph;
        _travel = travel;
    }

    // 도착 연출을 넣는다 (null 이면 바로 노드 이벤트)
    public void SetArrivalPresenter(INodeArrivalPresenter presenter)
    {
        _arrivalPresenter = presenter;
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

        // 직접 달리는 길은 어떤 길인지 알게 된다
        link.IsRevealed = true;
        SetState(EMapState.Traveling);

        if (_travel == null)
        {
            Arrive(link.To);
            return true;
        }

        _travel.Travel(link, () => Arrive(link.To));
        return true;
    }

    public int RevealAhead(CNodeInstance from, int range)
    {
        if (from == null || range <= 0)
        {
            return 0;
        }

        int revealed = 0;

        _revealFrontier.Clear();
        _revealFrontier.Add(from);

        for (int step = 0; step < range && _revealFrontier.Count > 0; step++)
        {
            _revealNext.Clear();

            for (int i = 0; i < _revealFrontier.Count; i++)
            {
                IReadOnlyList<CNodeLink> links = _revealFrontier[i].OutLinks;

                for (int j = 0; j < links.Count; j++)
                {
                    revealed += RevealLink(links[j]);
                    revealed += RevealNode(links[j].To);

                    if (!_revealNext.Contains(links[j].To))
                    {
                        _revealNext.Add(links[j].To);
                    }
                }
            }

            _revealFrontier.Clear();
            _revealFrontier.AddRange(_revealNext);
        }

        if (_logState)
        {
            CPrint.Log($"CMapRunner : 감시 → 앞 {range}칸에서 {revealed}곳을 밝혔습니다.");
        }

        NotifyRevealed();
        return revealed;
    }

    private static int RevealNode(CNodeInstance node)
    {
        if (node == null || node.IsRevealed)
        {
            return 0;
        }

        node.IsRevealed = true;
        return 1;
    }

    private static int RevealLink(CNodeLink link)
    {
        if (link == null || link.IsRevealed)
        {
            return 0;
        }

        link.IsRevealed = true;
        return 1;
    }

    private void NotifyRevealed()
    {
        OnRevealed?.Invoke();
    }

    private void Arrive(CNodeInstance node)
    {
        _current = node;
        _current.IsVisited = true;
        _current.IsRevealed = true;

        // 감시탑처럼 앞을 내다보는 지점이면 앞쪽을 밝힌다
        if (node.TypeData != null && node.TypeData.ScoutRange > 0)
        {
            RevealAhead(node, node.TypeData.ScoutRange);
        }

        SetState(EMapState.InNode);

        // 도착 연출이 있으면 끝난 뒤에 노드 이벤트로
        if (_arrivalPresenter != null)
        {
            _arrivalPresenter.Present(node, () => EnterArrivedNode(node));
            return;
        }

        EnterCurrentNode();
    }

    // 연출이 끝났을 때 아직 그 노드에 있을 때만 들어간다
    private void EnterArrivedNode(CNodeInstance node)
    {
        if (_state != EMapState.InNode || _current != node || _enteredNode == node)
        {
            return;
        }

        EnterCurrentNode();
    }

    private void EnterCurrentNode()
    {
        _enteredNode = _current;
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

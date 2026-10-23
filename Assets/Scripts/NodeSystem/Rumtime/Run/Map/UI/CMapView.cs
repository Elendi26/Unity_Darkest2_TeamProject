using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CMapView : MonoBehaviour
{
    #region 인스펙터
    [SerializeField] private CMapController _controller;
    [SerializeField] private ScrollRect _scroll;
    [SerializeField] private RectTransform _content;
    [SerializeField] private RectTransform _linkLayer;
    [SerializeField] private RectTransform _nodeLayer;
    [SerializeField] private CMapNodeView _nodePrefab;
    [SerializeField] private CMapLinkView _linkPrefab;

    [Header("배치")]
    [SerializeField] private float _rowSpacing = 260f;
    [SerializeField] private float _laneSpacing = 170f;
    [SerializeField] private float _padding = 160f;
    [SerializeField] private float _verticalPadding = 70f;	// 위아래 여백 (노드 반지름 + 여유). 레인 간격이 이 안에 들어가도록 자동으로 줄인다

    [Header("표시")]
    [SerializeField] private Sprite _unknownIcon;
    [SerializeField] private CMapRouteStyle[] _routeStyles;
    [SerializeField] private bool _revealNext = true;		// 지금 갈 수 있는 노드는 종류를 미리 보여준다
    [SerializeField] private bool _revealAll = false;       // 디버그 : 모든 노드 종류 공개
    [SerializeField] private bool _autoScroll = true;       // 현재 노드를 따라 스크롤
    [SerializeField] private bool _logInput = true;         // 디버그 : 클릭 로그
    #endregion

    #region 내부 변수
    private CMapRunner _runner;
    private float _appliedLaneSpacing;	// 패널 높이에 맞춰 실제로 쓰는 레인 간격
    private readonly Dictionary<CNodeInstance, CMapNodeView> _nodeViews = new Dictionary<CNodeInstance, CMapNodeView>();
    private readonly List<CMapLinkView> _linkViews = new List<CMapLinkView>();
    #endregion

    private void OnEnable()
    {
        if (_controller == null)
        {
            CPrint.Warn("CMapView : CMapController 가 연결되지 않았습니다.");
            return;
        }

        _controller.OnRunnerCreated += Bind;

        // 컨트롤러가 먼저 Start 했다면 런너가 이미 있다
        if (_controller.Runner != null)
        {
            Bind(_controller.Runner);
        }
    }

    private void OnDisable()
    {
        if (_controller != null)
        {
            _controller.OnRunnerCreated -= Bind;
        }

        Unbind();
    }

    private void Bind(CMapRunner runner)
    {
        if (runner == null || runner == _runner)
        {
            return;
        }

        Unbind();

        _runner = runner;
        _runner.OnStateChanged += HandleStateChanged;

        Build(_runner.Graph);
        RefreshAll();
    }

    private void Unbind()
    {
        if (_runner != null)
        {
            _runner.OnStateChanged -= HandleStateChanged;
        }

        _runner = null;
    }

    private void HandleStateChanged(EMapState prev, EMapState next)
    {
        RefreshAll();

        if (next == EMapState.SelectingPath)
        {
            ScrollToCurrent();
        }
    }

    // 그래프 → 프리팹 생성·배치 (맵이 바뀔 때 한 번)
    private void Build(CMapGraph graph)
    {
        Clear();

        if (graph == null)
        {
            return;
        }

        float width = _padding * 2f + (graph.RowCount - 1) * _rowSpacing;

        _content.sizeDelta = new Vector2(width, _content.sizeDelta.y);
        _appliedLaneSpacing = CalculateLaneSpacing(graph);

        for (int i = 0; i < graph.Nodes.Count; i++)
        {
            CNodeInstance node = graph.Nodes[i];
            CMapNodeView view = Instantiate(_nodePrefab, _nodeLayer);

            view.Rect.anchoredPosition = GetPosition(graph, node);
            view.Bind(node, HandleNodeClicked);
            _nodeViews.Add(node, view);
        }

        for (int i = 0; i < graph.Links.Count; i++)
        {
            CNodeLink link = graph.Links[i];
            CMapLinkView view = Instantiate(_linkPrefab, _linkLayer);

            view.Bind(link, GetPosition(graph, link.From), GetPosition(graph, link.To));
            _linkViews.Add(view);
        }
    }

    private void Clear()
    {
        foreach (CMapNodeView view in _nodeViews.Values)
        {
            if (view != null)
            {
                Destroy(view.gameObject);
            }
        }

        for (int i = 0; i < _linkViews.Count; i++)
        {
            if (_linkViews[i] == null)
            {
                continue;
            }

            Destroy(_linkViews[i].gameObject);
        }

        _nodeViews.Clear();
        _linkViews.Clear();
    }

    // 행 → x (왼쪽부터), 레인 → y (행 안에서 가운데 정렬, 레인 0 이 위)
    private Vector2 GetPosition(CMapGraph graph, CNodeInstance node)
    {
        int count = graph.GetRow(node.Row).Count;
        float x = _padding + node.Row * _rowSpacing;
        float y = ((count - 1) * 0.5f - node.Lane) * _appliedLaneSpacing;

        return new Vector2(x, y);
    }

    // 한 행의 최대 노드 수가 패널 높이 안에 들어가도록 레인 간격을 줄인다 (늘리지는 않는다)
    //  ㄴ Map Config 의 행당 노드 수를 바꿔도 위아래가 잘리지 않게
    private float CalculateLaneSpacing(CMapGraph graph)
    {
        int maxLanes = 1;

        for (int row = 0; row < graph.RowCount; row++)
        {
            maxLanes = Mathf.Max(maxLanes, graph.GetRow(row).Count);
        }

        if (maxLanes <= 1)
        {
            return _laneSpacing;
        }

        // 첫 프레임에는 Canvas 크기가 아직 계산 전일 수 있다 → 강제로 한 번 계산
        Canvas.ForceUpdateCanvases();

        float available = _content.rect.height - _verticalPadding * 2f;

        // 높이를 아직 모르면 인스펙터 값 그대로
        if (available <= 0f)
        {
            return _laneSpacing;
        }

        return Mathf.Min(_laneSpacing, available / (maxLanes - 1));
    }

    // 상태가 바뀔 때마다 전체 갱신
    private void RefreshAll()
    {
        if (_runner == null)
        {
            return;
        }

        CMapGraph graph = _runner.Graph;
        CNodeInstance current = _runner.Current;
        bool canSelect = _runner.State == EMapState.SelectingPath;

        for (int i = 0; i < graph.Nodes.Count; i++)
        {
            CNodeInstance node = graph.Nodes[i];

            if (!_nodeViews.TryGetValue(node, out CMapNodeView view))
            {
                continue;
            }

            bool isSelectable = canSelect && graph.FindLink(current, node) != null;

            view.Refresh(GetIcon(node, isSelectable), node.IsVisited, node == current, isSelectable);
        }

        for (int i = 0; i < _linkViews.Count; i++)
        {
            CMapLinkView view = _linkViews[i];
            CMapRouteStyle style = GetRouteStyle(view.Link);
            bool isSelectable = canSelect && view.Link.From == current;

            view.Refresh(style != null ? style.Color : Color.white, style != null ? style.Icon : null, isSelectable);
        }
    }

    private Sprite GetIcon(CNodeInstance node, bool isSelectable)
    {
        // 공개 안 된 노드는 ? 아이콘. 단, 지금 갈 수 있는 노드는 보여준다 → 고를 근거가 있어야 하니까
        //  ㄴ 화면 표시만 바꾼다. 노드 데이터(IsRevealed)는 그대로 → 정찰 같은 규칙은 맵 쪽에서
        bool isRevealed = _revealAll || node.IsRevealed || (_revealNext && isSelectable);

        if (!isRevealed)
        {
            return _unknownIcon;
        }

        if (node.TypeData == null || node.TypeData.Icon == null)
        {
            return _unknownIcon;
        }

        return node.TypeData.Icon;
    }

    private CMapRouteStyle GetRouteStyle(CNodeLink link)
    {
        if (_routeStyles == null || link == null || link.RouteData == null)
        {
            return null;
        }

        for (int i = 0; i < _routeStyles.Length; i++)
        {
            if (_routeStyles[i] != null && _routeStyles[i].RouteType == link.RouteData.Type)
            {
                return _routeStyles[i];
            }
        }

        return null;
    }

    private void HandleNodeClicked(CNodeInstance node)
    {
        if (_runner == null || node == null)
        {
            return;
        }

        CNodeLink link = _runner.Graph.FindLink(_runner.Current, node);

        if (link == null)
        {
            if (_logInput)
            {
                CPrint.Log($"CMapView : 노드 {node.Id} 로는 갈 수 없습니다.");
            }

            return;
        }

        if (_logInput)
        {
            CPrint.Log($"CMapView : {link.From.Id} → {node.Id} 선택");
        }

        _runner.TrySelectPath(link);
    }

    // 현재 노드를 화면 왼쪽 1/3 에 둔다
    private void ScrollToCurrent()
    {
        if (!_autoScroll || _scroll == null || _runner == null || _runner.Current == null)
        {
            return;
        }

        if (!_nodeViews.TryGetValue(_runner.Current, out CMapNodeView view))
        {
            return;
        }

        RectTransform viewport = _scroll.viewport != null ? _scroll.viewport : (RectTransform)_scroll.transform;
        float scrollable = _content.rect.width - viewport.rect.width;

        if (scrollable <= 0f)
        {
            return;
        }

        float target = view.Rect.anchoredPosition.x - viewport.rect.width / 3f;

        _scroll.horizontalNormalizedPosition = Mathf.Clamp01(target / scrollable);
    }
}
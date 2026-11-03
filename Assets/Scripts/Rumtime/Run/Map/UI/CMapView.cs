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
    [SerializeField] private EMapLayout _layout = EMapLayout.BottomToTop;
    [SerializeField] private float _rowSpacing = 130f;
    [SerializeField] private float _laneSpacing = 100f;
    [SerializeField] private float _padding = 80f;
    [SerializeField] private float _verticalPadding = 45f;

    [Header("표시")]
    [SerializeField] private Sprite _unknownIcon;
    [SerializeField] private CMapRouteStyle[] _routeStyles;
    [SerializeField] private bool _revealNext = true;
    [SerializeField] private bool _revealAll = false;
    [SerializeField] private bool _autoScroll = true;
    [SerializeField] private float _scrollDuration = 0.35f;
    [SerializeField] private bool _clickToSelect = false;
    [SerializeField] private bool _logInput = true;
    #endregion

    #region 내부 변수
    private CMapRunner _runner;
    private float _appliedLaneSpacing;
    private bool IsVertical => _layout == EMapLayout.BottomToTop;
    private Coroutine _scrollRoutine;
    private bool _isFirstScroll;
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

        // 컴포넌트만 꺼져도 코루틴은 계속 돈다 → 직접 멈춘다
        StopScroll();
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

    // 그래프 → 프리팹 생성·배치
    private void Build(CMapGraph graph)
    {
        Clear();
        _isFirstScroll = true;

        if (graph == null)
        {
            return;
        }

        ApplyContentLayout(graph);
        _appliedLaneSpacing = CalculateLaneSpacing(graph);

        for (int i = 0; i < graph.Nodes.Count; i++)
        {
            CNodeInstance node = graph.Nodes[i];
            CMapNodeView view = Instantiate(_nodePrefab, _nodeLayer);

            ApplyAnchor(view.Rect);
            view.Rect.anchoredPosition = GetPosition(graph, node);
            view.Bind(node, HandleNodeClicked);
            _nodeViews.Add(node, view);
        }

        for (int i = 0; i < graph.Links.Count; i++)
        {
            CNodeLink link = graph.Links[i];
            CMapLinkView view = Instantiate(_linkPrefab, _linkLayer);

            ApplyAnchor((RectTransform)view.transform);
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

    // 진행 방향에 맞춰 Content 의 앵커·크기·스크롤 방향을 정한다
    private void ApplyContentLayout(CMapGraph graph)
    {
        float length = _padding * 2f + (graph.RowCount - 1) * _rowSpacing;

        if (IsVertical)
        {
            // 아래 기준, 가로는 Viewport 에 꽉 차게
            _content.anchorMin = new Vector2(0f, 0f);
            _content.anchorMax = new Vector2(1f, 0f);
            _content.pivot = new Vector2(0.5f, 0f);
            _content.sizeDelta = new Vector2(0f, length);
        }
        else
        {
            // 왼쪽 기준, 세로는 Viewport 에 꽉 차게
            _content.anchorMin = new Vector2(0f, 0f);
            _content.anchorMax = new Vector2(0f, 1f);
            _content.pivot = new Vector2(0f, 0.5f);
            _content.sizeDelta = new Vector2(length, 0f);
        }

        _content.anchoredPosition = Vector2.zero;

        if (_scroll != null)
        {
            _scroll.horizontal = !IsVertical;
            _scroll.vertical = IsVertical;
        }
    }

    // 노드·선 좌표의 기준점 : 세로 진행이면 아래 가운데, 가로 진행이면 왼쪽 가운데
    private void ApplyAnchor(RectTransform rect)
    {
        Vector2 anchor = IsVertical ? new Vector2(0.5f, 0f) : new Vector2(0f, 0.5f);

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
    }

    private Vector2 GetPosition(CMapGraph graph, CNodeInstance node)
    {
        int count = graph.GetRow(node.Row).Count;
        float along = _padding + node.Row * _rowSpacing;
        float across = (node.Lane - (count - 1) * 0.5f) * _appliedLaneSpacing;

        if (IsVertical)
        {
            return new Vector2(across, along);
        }

        return new Vector2(along, -across);
    }

    // 한 행의 최대 노드 수가 패널 안에 들어가도록 레인 간격을 줄인다
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

        // 첫 프레임에는 Canvas 크기가 아직 계산 전일 수 있다
        Canvas.ForceUpdateCanvases();

        // 레인이 펼쳐지는 방향의 크기 : 세로 진행이면 가로 폭, 가로 진행이면 세로 높이
        float crossSize = IsVertical ? _content.rect.width : _content.rect.height;
        float available = crossSize - _verticalPadding * 2f;

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
        // 공개 안 된 노드는 ? 아이콘. 단, 지금 갈 수 있는 노드는 보여준다
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
        if (_runner == null || node == null || !_clickToSelect)
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

    // 현재 노드를 진행 방향 기준 1/3 지점에 둔다
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
        float contentSize = IsVertical ? _content.rect.height : _content.rect.width;
        float viewportSize = IsVertical ? viewport.rect.height : viewport.rect.width;
        float scrollable = contentSize - viewportSize;

        if (scrollable <= 0f)
        {
            return;
        }

        float position = IsVertical ? view.Rect.anchoredPosition.y : view.Rect.anchoredPosition.x;
        float normalized = Mathf.Clamp01((position - viewportSize / 3f) / scrollable);

        StartScroll(normalized);
    }

    // 목표 위치까지 부드럽게 스크롤한다. 이미 움직이는 중이면 새 목표로 바꾼다
    private void StartScroll(float target)
    {
        StopScroll();

        // 처음 그릴 때·시간 0·비활성 상태 → 바로 이동 (비활성이면 코루틴을 시작할 수 없다)
        if (_isFirstScroll || _scrollDuration <= 0f || !isActiveAndEnabled)
        {
            _isFirstScroll = false;
            SetScrollPosition(target);
            return;
        }

        _scrollRoutine = StartCoroutine(CoScroll(target));
    }

    private void StopScroll()
    {
        if (_scrollRoutine == null)
        {
            return;
        }

        StopCoroutine(_scrollRoutine);
        _scrollRoutine = null;
    }

    private IEnumerator CoScroll(float target)
    {
        float start = GetScrollPosition();
        float elapsed = 0f;

        while (elapsed < _scrollDuration)
        {
            // 일시정지(timeScale 0) 중에도 맵 UI 는 움직이게
            elapsed += Time.unscaledDeltaTime;

            // 끝으로 갈수록 천천히 멈춘다
            float t = Mathf.SmoothStep(0f, 1f, elapsed / _scrollDuration);

            // 드래그 관성이 남아 있으면 스크롤과 싸운다 → 매 프레임 지운다
            _scroll.velocity = Vector2.zero;
            SetScrollPosition(Mathf.Lerp(start, target, t));
            yield return null;
        }

        SetScrollPosition(target);
        _scrollRoutine = null;
    }

    private float GetScrollPosition()
    {
        return IsVertical ? _scroll.verticalNormalizedPosition : _scroll.horizontalNormalizedPosition;
    }

    private void SetScrollPosition(float value)
    {
        if (IsVertical)
        {
            _scroll.verticalNormalizedPosition = value;
        }
        else
        {
            _scroll.horizontalNormalizedPosition = value;
        }
    }
}
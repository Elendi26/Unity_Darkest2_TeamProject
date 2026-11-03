using UnityEngine;

public class DebugGizmo : MonoBehaviour
{
    #region 인스펙터
    [SerializeField] private CMapConfig _config;
    [SerializeField] private CMapController _mapController;
    [SerializeField] private int _seed = 1;
    [SerializeField, Range(0f, 0.45f)] private float _laneJitter = 0.25f;	// 레인 방향 흐트러짐 (레인 간격 대비). 0.5 미만이라 좌우 순서는 안 바뀐다
    [SerializeField, Range(0f, 0.4f)] private float _rowJitter = 0.2f;		// 진행 방향 흐트러짐 (행 간격 대비)
    [SerializeField] private EMapLayout _layout = EMapLayout.BottomToTop;	// 진행 방향 (맵 UI 와 맞춘다)
    [SerializeField] private float _rowSpacing = 3f;
    [SerializeField] private float _laneSpacing = 2f;
    [SerializeField] private float _nodeRadius = 0.4f;
    [SerializeField] private bool _showLabels = true;
    [SerializeField] private bool _hideUnrevealed = false;
    #endregion

    #region 내부 변수
    private CMapGraph _previewGraph;
    #endregion

    private void OnValidate()
    {
        _previewGraph = null;
    }

    [ContextMenu("다시 생성")]
    private void Regenerate()
    {
        _previewGraph = null;
        RepaintScene();
    }

    [ContextMenu("다음 시드")]
    private void NextSeed()
    {
        _seed++;
        CPrint.Log($"시드 : {_seed}");
        Regenerate();
    }

    private void OnDrawGizmos()
    {
        CMapGraph graph = GetGraph();

        if (graph == null)
        {
            return;
        }

        CNodeInstance current = GetCurrent();

        DrawLinks(graph, current);
        DrawNodes(graph, current);
    }

    private CMapGraph GetGraph()
    {
        if (Application.isPlaying && _mapController != null && _mapController.Runner != null)
        {
            return _mapController.Runner.Graph;
        }

        if (_config == null)
        {
            return null;
        }

        if (_previewGraph == null)
        {

            _previewGraph = new CMapGenerator(_config).Generate(_seed);
        }

        return _previewGraph;
    }

    private CNodeInstance GetCurrent()
    {
        if (!Application.isPlaying || _mapController == null || _mapController.Runner == null)
        {
            return null;
        }

        return _mapController.Runner.Current;
    }

    private Vector3 GetPosition(CMapGraph graph, CNodeInstance node)
    {
        int count = graph.GetRow(node.Row).Count;
        float along = node.Row * _rowSpacing;
        float across = (node.Lane - (count - 1) * 0.5f) * _laneSpacing;

        // 맵 UI 와 같은 흐트러짐 → 기즈모와 화면 모양이 같다
        Vector2 jitter = CMapLayoutJitter.Get(graph.Seed, node.Id);

        across += jitter.x * _laneJitter * _laneSpacing;
        along += jitter.y * _rowJitter * _rowSpacing;

        // 세로 : 행 0 이 맨 아래, 레인 0 이 왼쪽 / 가로 : 행 0 이 맨 왼쪽, 레인 0 이 위
        Vector3 offset = _layout == EMapLayout.BottomToTop ? new Vector3(across, along, 0f) : new Vector3(along, -across, 0f);

        return transform.position + offset;
    }

    private void DrawLinks(CMapGraph graph, CNodeInstance current)
    {
        for (int i = 0; i < graph.Links.Count; i++)
        {
            CNodeLink link = graph.Links[i];

            // 지금 고를 수 있는 경로는 노란색
            bool isSelectable = current != null && link.From == current;

            Gizmos.color = isSelectable ? Color.yellow : GetRouteColor(link);
            Gizmos.DrawLine(GetPosition(graph, link.From), GetPosition(graph, link.To));
        }
    }

    private void DrawNodes(CMapGraph graph, CNodeInstance current)
    {
        bool isPlaying = current != null;

        for (int i = 0; i < graph.Nodes.Count; i++)
        {
            CNodeInstance node = graph.Nodes[i];
            Vector3 pos = GetPosition(graph, node);
            bool isHidden = isPlaying && _hideUnrevealed && !node.IsRevealed;

            Gizmos.color = isHidden ? Color.gray : GetNodeColor(node);

            // 미리보기 → 전부 채움, 플레이 중 → 방문한 노드만 채움
            if (!isPlaying || node.IsVisited)
            {
                Gizmos.DrawSphere(pos, _nodeRadius);
            }
            else
            {
                Gizmos.DrawWireSphere(pos, _nodeRadius);
            }

            if (node == current)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(pos, _nodeRadius * 1.6f);
            }

            DrawLabel(pos, node, isHidden);
        }
    }

    private void DrawLabel(Vector3 pos, CNodeInstance node, bool isHidden)
    {
#if UNITY_EDITOR
        if (!_showLabels)
        {
            return;
        }

        string type = isHidden || node.TypeData == null ? "?" : node.TypeData.Type.ToString();

        UnityEditor.Handles.Label(pos + Vector3.up * (_nodeRadius + 0.3f), $"{node.Id} {type}");
#endif
    }

    private void RepaintScene()
    {
#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    private static Color GetNodeColor(CNodeInstance node)
    {
        // TypeData 가 비었다 → 설정 누락이 눈에 띄게 자홍색
        if (node.TypeData == null)
        {
            return Color.magenta;
        }

        switch (node.TypeData.Type)
        {
            case ENodeType.Start:
                return Color.white;
            case ENodeType.Inn:
                return Color.green;
            case ENodeType.Combat:
                return Color.red;
            case ENodeType.Store:
                return new Color(1f, 0.85f, 0.2f);
            case ENodeType.Hospital:
                return Color.cyan;
            case ENodeType.Cache:
                return new Color(0.4f, 0.6f, 1f);
            case ENodeType.CreatureDen:
                return new Color(0.6f, 0.3f, 0.9f);
            case ENodeType.Fortress:
                return new Color(0.55f, 0.1f, 0.15f);
        }

        return Color.gray;
    }

    private static Color GetRouteColor(CNodeLink link)
    {
        if (link.RouteData == null)
        {
            return Color.gray;
        }

        switch (link.RouteData.Type)
        {
            case ERouteType.Combat:
                return new Color(1f, 0.3f, 0.3f);
            case ERouteType.RoughPatch:
                return new Color(1f, 0.55f, 0f);
        }

        return Color.white;
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }
}

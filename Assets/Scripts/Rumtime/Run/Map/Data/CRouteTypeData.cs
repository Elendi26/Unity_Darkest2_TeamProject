using UnityEngine;

[CreateAssetMenu(menuName = "Map/Route Type_")]
public class CRouteTypeData : ScriptableObject
{
    #region 인스펙터
    [SerializeField] private ERouteType _type = ERouteType.Safe;
    [SerializeField] private float _weight = 1f;
    [SerializeField] private int _minRoadEvents = 0;
    [SerializeField] private int _maxRadEvents = 1;
    // 이 길을 지나가면 반드시 일어나는 도로 이벤트 (비우면 없음)
    [SerializeField] private CRoadEventData _passEvent;
    [SerializeField, Range(0.05f, 0.95f)] private float _passEventPosition = 0.5f;
    // 켜면 이 길은 처음부터 끝까지 곧은 길만 깐다 (커브 없음)
    [SerializeField] private bool _straightOnly = false;
    #endregion

    public ERouteType Type => _type;
    public float Weight => _weight;
    public int MinRoadEvents => _minRoadEvents;
    public int MaxRadEvents => _maxRadEvents;
    // 지나가면 반드시 일어나는 도로 이벤트
    public CRoadEventData PassEvent => _passEvent;
    // 그 이벤트가 일어나는 위치 (경로 진행도 0~1)
    public float PassEventPosition => _passEventPosition;
    // 처음부터 끝까지 곧은 길만 까는지
    public bool StraightOnly => _straightOnly;
}

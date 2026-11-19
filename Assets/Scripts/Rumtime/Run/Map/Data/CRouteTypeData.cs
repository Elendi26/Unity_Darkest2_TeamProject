using UnityEngine;

[CreateAssetMenu(menuName = "Map/Route Type_")]
public class CRouteTypeData : ScriptableObject
{
    #region 인스펙터
    [SerializeField] private ERouteType _type = ERouteType.Safe;
    // 화면에 띄울 이름 (갈림길 표지판, 비우면 타입 이름)
    [SerializeField] private string _displayName = "";
    [SerializeField] private float _weight = 1f;
    [SerializeField] private int _minRoadEvents = 0;
    [SerializeField] private int _maxRadEvents = 1;
    [SerializeField] private CRoadEventData _passEvent;
    [SerializeField, Range(0.05f, 0.95f)] private float _passEventPosition = 0.5f;
    [SerializeField] private bool _straightOnly = false;
    #endregion

    public ERouteType Type => _type;
    // 화면에 띄울 이름 (비어 있으면 타입 이름)
    public string DisplayName => string.IsNullOrEmpty(_displayName) ? _type.ToString() : _displayName;
    public float Weight => _weight;
    public int MinRoadEvents => _minRoadEvents;
    public int MaxRadEvents => _maxRadEvents;
    public CRoadEventData PassEvent => _passEvent;
    public float PassEventPosition => _passEventPosition;
    public bool StraightOnly => _straightOnly;
}

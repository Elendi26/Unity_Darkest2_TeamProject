using UnityEngine;

[CreateAssetMenu(menuName = "Map/Route Type_")]
public class CRouteTypeData : ScriptableObject
{
    #region 인스펙터
    [SerializeField] private ERouteType _type = ERouteType.Safe;
    [SerializeField] private float _weight = 1f;
    [SerializeField] private int _minRoadEvents = 0;
    [SerializeField] private int _maxRadEvents = 1;
    #endregion

    public ERouteType Type => _type;
    public float Weight => _weight;
    public int MinRoadEvents => _minRoadEvents;
    public int MaxRadEvents => _maxRadEvents;
}

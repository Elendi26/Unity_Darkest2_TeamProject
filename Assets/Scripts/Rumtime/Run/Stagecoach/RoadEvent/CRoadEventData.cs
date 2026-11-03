using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Stagecoach/Road Event_")]
public class CRoadEventData : ScriptableObject
{
    #region 인스펙터
    [SerializeField] private string _id = "roadevent_obstacle_01";
    [SerializeField] private ERoadEventCategory _category = ERoadEventCategory.Obstacle;
    [SerializeField] private float _weight = 1f;
    [SerializeField] private CRoadEventOption[] _options;
    #endregion

    public string Id => _id;
    public ERoadEventCategory Category => _category;
    public float Weight => _weight;
    public IReadOnlyList<CRoadEventOption> Options => _options;
}

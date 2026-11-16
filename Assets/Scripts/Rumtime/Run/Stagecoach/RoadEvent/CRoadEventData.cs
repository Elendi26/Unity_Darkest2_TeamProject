using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Stagecoach/Road Event_")]
public class CRoadEventData : ScriptableObject
{
    #region 인스펙터
    [SerializeField] private string _id = "roadevent_obstacle_01";
    [SerializeField] private ERoadEventCategory _category = ERoadEventCategory.Obstacle;
    [SerializeField] private float _weight = 1f;
    // 팝업 제목·본문 (비우면 ID·종류 문구)
    [SerializeField] private string _title = "";
    [SerializeField, TextArea] private string _body = "";
    [SerializeField] private CRoadEventOption[] _options;
    #endregion

    public string Id => _id;
    public ERoadEventCategory Category => _category;
    public float Weight => _weight;
    // 팝업 제목
    public string Title => _title;
    // 팝업 본문
    public string Body => _body;
    public IReadOnlyList<CRoadEventOption> Options => _options;
}

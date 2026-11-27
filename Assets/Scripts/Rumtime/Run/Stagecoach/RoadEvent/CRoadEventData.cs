using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Stagecoach/Road Event_")]
public class CRoadEventData : ScriptableObject
{
    #region 인스펙터
    [SerializeField] private string _id = "roadevent_obstacle_01";
    [SerializeField] private ERoadEventCategory _category = ERoadEventCategory.Obstacle;
    [SerializeField] private float _weight = 1f;
    [SerializeField] private string _title = "";
    [SerializeField, TextArea] private string _body = "";
    [SerializeField] private CRoadEventOption[] _options;
    [SerializeField] private bool _showPopup = true;
    [SerializeField] private GameObject _roadPropPrefab;
    [SerializeField] private int _bumpCount = 4;
    [SerializeField] private ERunValue _combatWhenEmpty = ERunValue.None;
    [SerializeField] private string _combatId = "";
    [SerializeField] private bool _alwaysCombat = false;
    [SerializeField] private bool _blocksRoad = false;
    [SerializeField] private ERoadHazard _hazard = ERoadHazard.None;
    // 전투로 이어질 때 어떤 전투인지 (None 이면 길을 막는 이벤트는 방책, 아니면 습격)
    [SerializeField] private ECombatKind _combatKind = ECombatKind.None;
    #endregion

    public string Id => _id;
    public ERoadEventCategory Category => _category;
    public float Weight => _weight;
    public string Title => _title;
    public string Body => _body;
    public IReadOnlyList<CRoadEventOption> Options => _options;
    public bool ShowPopup => _showPopup;
    public GameObject RoadPropPrefab => _roadPropPrefab;
    public int BumpCount => Mathf.Max(0, _bumpCount);
    public ERunValue CombatWhenEmpty => _combatWhenEmpty;
    public string CombatId => string.IsNullOrEmpty(_combatId) ? _id : _combatId;
    public bool AlwaysCombat => _alwaysCombat;
    public bool BlocksRoad => _blocksRoad;
    public ERoadHazard Hazard => _hazard;
    // 전투 종류 (방책·습격 등)
    public ECombatKind CombatKind
    {
        get
        {
            if (_combatKind != ECombatKind.None)
            {
                return _combatKind;
            }

            return _blocksRoad ? ECombatKind.Barricade : ECombatKind.Ambush;
        }
    }

    public bool ShouldStartCombat(CRunData runData)
    {
        if (_alwaysCombat)
        {
            return true;
        }

        if (_combatWhenEmpty == ERunValue.None || runData == null)
        {
            return false;
        }

        CRunValue value = runData.Get(_combatWhenEmpty);

        return value != null && value.Current <= 0;
    }
}

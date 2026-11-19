using UnityEngine;

[CreateAssetMenu(menuName = "Map/Node Type_")]
public class CNodeTypeData : ScriptableObject
{
    #region 인스펙터
    // 노드 타입
    [SerializeField] private ENodeType _type = ENodeType.Combat;
    // 화면에 띄울 이름 (갈림길 표지판·이벤트 띠, 비우면 타입 이름)
    [SerializeField] private string _displayName = "";
    // 아이콘
    [SerializeField] private Sprite _icon;
    // 노드 풀 가중치
    [SerializeField] private float _weight = 1f;
    // 바이옴 당 개수
    [SerializeField] private int _maxPerBiome = 99;
    // 이 행보다 앞에는 안나옴
    [SerializeField] private int _minRow = 1;
    // 연속으로 같은 노드가 나와도 되는지 결정
    [SerializeField] private bool _allowConsecutive = true;
    // 같은 행에 이 종류는 하나만
    [SerializeField] private bool _uniqueInRow = false;
    // 도착하면 띄울 이벤트 (비우면 통과)
    [SerializeField] private CNodeEventData _event;
    // 도착하면 열 상점 (이벤트보다 먼저 쓴다)
    [SerializeField] private CShopData _shop;
    // 도착할 때 길 오른쪽에 세울 건축물 (비우면 없음)
    [SerializeField] private GameObject _landmarkPrefab;
    // 정찰과 상관없이 지역에 들어가자마자 지도에 공개 (소굴·성채·여관 등)
    [SerializeField] private bool _alwaysRevealed = false;
    // 도착하면 앞쪽 몇 칸까지 지점·경로를 밝힌다 (감시탑 등, 0 이면 없음)
    [SerializeField] private int _scoutRange = 0;
    #endregion

    public ENodeType Type => _type;
    // 화면에 띄울 이름 (비어 있으면 타입 이름)
    public string DisplayName => string.IsNullOrEmpty(_displayName) ? _type.ToString() : _displayName;
    public Sprite Icon => _icon;
    public float Weight => _weight;
    public int MaxPerBiome => _maxPerBiome;
    public int MinRow => _minRow;
    public bool AllowConsecutive => _allowConsecutive;
    public bool UniqueInRow => _uniqueInRow;
    // 도착하면 띄울 이벤트 (없으면 null)
    public CNodeEventData Event => _event;
    // 도착하면 열 상점 (없으면 null)
    public CShopData Shop => _shop;
    // 도착할 때 길 오른쪽에 세울 건축물 (없으면 null)
    public GameObject LandmarkPrefab => _landmarkPrefab;
    // 정찰과 상관없이 처음부터 공개
    public bool AlwaysRevealed => _alwaysRevealed;
    // 도착하면 밝히는 앞쪽 칸 수 (0 이면 없음)
    public int ScoutRange => Mathf.Max(0, _scoutRange);
}

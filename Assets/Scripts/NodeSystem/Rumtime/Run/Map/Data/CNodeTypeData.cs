using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Map/Node Type_")]
public class CNodeTypeData : ScriptableObject
{
    #region 인스펙터
    // 노드 타입
    [SerializeField] private ENodeType _type = ENodeType.Combat;
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
    // 같은 행에 이 종류는 하나만 (상점·병원처럼 겹치면 선택이 의미 없는 노드)
    [SerializeField] private bool _uniqueInRow = false;
    #endregion

    public ENodeType Type => _type;
    public Sprite Icon => _icon;
    public float Weight => _weight;
    public int MaxPerBiome => _maxPerBiome;
    public int MinRow => _minRow;
    public bool AllowConsecutive => _allowConsecutive;
    // 같은 행 중복 금지 여부
    public bool UniqueInRow => _uniqueInRow;
}

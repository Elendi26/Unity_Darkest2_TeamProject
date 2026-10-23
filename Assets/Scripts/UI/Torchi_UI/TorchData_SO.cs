using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Torch", menuName = "Torch Data")]
public class TorchData_SO : ScriptableObject
{
    #region 인스펙터
    [Header("기준 최소 수치")]
    [SerializeField] private int _minTorchValue;
    [Header("기준 최대 수치")]
    [SerializeField] private int _maxTorchValue;

    [Header("범위 안 상태 이름")]
    [SerializeField] private string _stateName;
    [Header("상태에 맞는 불꽃 이미지")]
    [SerializeField] private Sprite _stateTypeImage;

    [Header("아군 버프/디버프 텍스트")]
    [SerializeField] private string _herosBuffExplain;
    [Header("적군 버프/디버프 텍스트")]
    [SerializeField] private string _enemiesBuffExplain;

    #endregion

    #region 프로퍼티

    public int MinTorchValue => _minTorchValue;
    public int MaxTorchValue => _maxTorchValue;
    public string StateName => _stateName;
    public Sprite StateTypeImage => _stateTypeImage;
    public string HerosBuffExplain => _herosBuffExplain;
    public string EnemiesBuffExplain => _enemiesBuffExplain;

    #endregion
}

using UnityEngine;

[CreateAssetMenu(menuName = "2D3D/SO/EnemyGroupDataSO", fileName = "EnemyGroupData_")]
public class CEnemyGroupDataSO : ScriptableObject
{
    #region 인스펙터

    [Header("적 그룹 구분용")]
    [SerializeField] private string _groupName;
    [SerializeField, Min(0)] private int _index; // 포괄
    [SerializeField, Min(0)] private int _mapIndex; // 맵 기준
    [SerializeField, Min(0)] private int _typeIndex; // 타입 기준

    [Header("적 데이터들")]
    [SerializeField] private CEntityBasicDataSO[] _enemyBasicData = new CEntityBasicDataSO[4];

    #endregion

    #region 프로퍼티

    public string GroupName => _groupName;
    public int Index => _index;
    public CEntityBasicDataSO[] EnemyBasicData => _enemyBasicData;

    #endregion
}

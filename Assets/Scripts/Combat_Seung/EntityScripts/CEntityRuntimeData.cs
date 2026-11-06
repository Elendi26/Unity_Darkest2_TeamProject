using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum EEntityStat
{
    HP,
    MaxHP,
    Stress,
    MaxStress,
    Speed,
    Movement,
    BleedRes,
    BlightRes,
    BurnRes,
    DiseaseRes,
    StunRes,
    MoveRes,
    DebuffRes,
    DeathblowRes
}

public class CEntityRuntimeData : MonoBehaviour, IRankEntity
{
    private CEntityBasicDataSO _heroBasicData;

    private string _name;

    private int _maxHP; // 비율 계산용
    private int _currentHP;
    private int _maxStress;
    private int _currentStress;

    private Dictionary<string, int> _relationships;

    private int _speed;

    private float _bleedRes;
    private float _blightRes;
    private float _burnRes;
    private float _diseaseRes;
    private float _stunRes;
    private float _moveRes;
    private float _debuffRes;
    private float _deathblowRes;

    private ERank _currentRank;
    private int _movement;

    private CSkillDataSO _skill;
    private CTokenStatus _tokenStatus = new CTokenStatus();

    private void Awake()
    {
        InitializeRuntimeData();
    }

    // public 으로 basicData말고 테스트용

    private void InitializeRuntimeData()
    {
        if (_heroBasicData == null)
        {
            Debug.LogError($"[{gameObject.name}] CHeroBasicDataSO가 할당되지 않았습니다!", this);
            return;
        }

        _name = _heroBasicData.Name;

        _currentHP = _heroBasicData.MaxHP;
        _maxHP = _heroBasicData.MaxHP;
        _currentStress = 0;
        _maxStress = 10;

        _speed = _heroBasicData.Speed;

        _bleedRes = _heroBasicData.BleedRes;
        _blightRes = _heroBasicData.BlightRes;
        _burnRes = _heroBasicData.BurnRes;
        _diseaseRes = _heroBasicData.DiseaseRes;
        _stunRes = _heroBasicData.StunRes;
        _moveRes = _heroBasicData.MoveRes;
        _debuffRes = _heroBasicData.DebuffRes;
        _deathblowRes = _heroBasicData.DeathblowRes;

        _movement = _heroBasicData.Movement;

        _relationships = new Dictionary<string, int>();

        Debug.Log($"[{_name}] 런타임 데이터 초기화 완료 (MaxHP: {_currentHP}, Speed: {_speed})");
    }



    /// <summary>
    /// 특정 스탯의 값을 증감시키는 통합 함수
    /// </summary>
    public void ModifyStat(EEntityStat statType, float amount)
    {
        switch (statType)
        {
            case EEntityStat.HP:
                _currentHP += Mathf.RoundToInt(amount);
                _currentHP = Mathf.Clamp(_currentHP, 0, _heroBasicData != null ? _heroBasicData.MaxHP : _currentHP);
                if (_currentHP <= 0) OnDeathblow();
                break;

            case EEntityStat.MaxHP:
                Debug.Log("최대체력값은 변경할 수 없습니다!");
                break;

            case EEntityStat.Stress:
                _currentStress += Mathf.RoundToInt(amount);
                _currentStress = Mathf.Clamp(_currentStress, 0, 10);
                if (_currentStress >= 10) OnMeltdown();
                break;

            case EEntityStat.Speed:
                _speed += Mathf.RoundToInt(amount);
                break;

            case EEntityStat.Movement:
                _movement += Mathf.RoundToInt(amount);
                break;

            case EEntityStat.BleedRes: _bleedRes += amount; break;
            case EEntityStat.BlightRes: _blightRes += amount; break;
            case EEntityStat.BurnRes: _burnRes += amount; break;
            case EEntityStat.DiseaseRes: _diseaseRes += amount; break;
            case EEntityStat.StunRes: _stunRes += amount; break;
            case EEntityStat.MoveRes: _moveRes += amount; break;
            case EEntityStat.DebuffRes: _debuffRes += amount; break;
            case EEntityStat.DeathblowRes: _deathblowRes += amount; break;
        }

        Debug.Log($"[{_name}] 스탯 변경됨 -> {statType} : {amount:+0;-0;0}");
    }


    public float GetStat(EEntityStat statType)
    {
        return statType switch
        {
            EEntityStat.HP => _currentHP,
            EEntityStat.MaxHP => _maxHP,
            EEntityStat.Stress => _currentStress,
            EEntityStat.MaxStress => _maxStress,
            EEntityStat.Speed => _speed,
            EEntityStat.Movement => _movement,
            EEntityStat.BleedRes => _bleedRes,
            EEntityStat.BlightRes => _blightRes,
            EEntityStat.BurnRes => _burnRes,
            EEntityStat.DiseaseRes => _diseaseRes,
            EEntityStat.StunRes => _stunRes,
            EEntityStat.MoveRes => _moveRes,
            EEntityStat.DebuffRes => _debuffRes,
            EEntityStat.DeathblowRes => _deathblowRes,
            _ => 0f,
        };
    }

    public void InitializeRank(ERank rank)
    {
        _currentRank = rank;
    }

    public void ChangeRank(ERank rank)
    {
        _currentRank = rank;
    }

    public ERank GetRank()
    {
        return _currentRank;
    }

    private void OnDeathblow()
    {
        Debug.Log($"{_name}이(가) 쓰러졌습니다!");
    }

    private void OnMeltdown()
    {
        Debug.Log($"{_name}이(가) 붕괴(Meltdown) 상태가 되었습니다!");
    }
}

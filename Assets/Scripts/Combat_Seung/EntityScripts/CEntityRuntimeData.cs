using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public interface ICombatData
{
    public ICombatData ReturnCombatData();
}

public enum EEntityStat
{
    HP,
    MaxHP,
    Stress,
    MaxStress,
    Speed,
    CurrentSpeed,
    Movement,
    BleedRes,
    BlightRes,
    BurnRes,
    DiseaseRes,
    StunRes,
    MoveRes,
    DebuffRes,
    DeathblowRes,
    ExtraCritical,
    Hero
}

public class CEntityRuntimeData : MonoBehaviour, IRankEntity, ICombatData
{
    private CEntityBasicDataSO _heroBasicData;

    private string _name;

    private int _maxHP;
    private int _currentHP;
    private int _maxStress;
    private int _currentStress;

    private Dictionary<string, int> _relationships;

    private int _speed;
    private int _currentSpeed;

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

    public CEntitySkillData _skillData;
    public CTokenStatus _tokenStatus = new CTokenStatus();
    public List<SDamageoverTime> _damageoverTime = new List<SDamageoverTime>();

    private bool _hero = true;
    private float _extraCritical = 0f;

    private void Awake()
    {
        InitializeRuntimeData();
    }

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
        _currentSpeed = 0;

        _bleedRes = _heroBasicData.BleedRes;
        _blightRes = _heroBasicData.BlightRes;
        _burnRes = _heroBasicData.BurnRes;
        _diseaseRes = _heroBasicData.DiseaseRes;
        _stunRes = _heroBasicData.StunRes;
        _moveRes = _heroBasicData.MoveRes;
        _debuffRes = _heroBasicData.DebuffRes;
        _deathblowRes = _heroBasicData.DeathblowRes;

        _movement = _heroBasicData.Movement;

        _hero = _heroBasicData.Hero;
        _extraCritical = 0f;

        _relationships = new Dictionary<string, int>();

        Debug.Log($"[{_name}] 런타임 데이터 초기화 완료 (MaxHP: {_currentHP}, Speed: {_speed})");
    }

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
                Debug.Log("최대체력값은 변경할 수 없습니다");
                break;

            case EEntityStat.Stress:
                _currentStress += Mathf.RoundToInt(amount);
                _currentStress = Mathf.Clamp(_currentStress, 0, 10);
                if (_currentStress >= 10) OnMeltdown();
                break;
            case EEntityStat.MaxStress:
                Debug.Log("최대 스트레스값은 변경할 수 없습니다");
                break;

            case EEntityStat.Speed:
                Debug.Log("기본 속도는 변경할 수 없습니다");
                break;
            case EEntityStat.CurrentSpeed:
                _currentSpeed += Mathf.RoundToInt(amount);
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
            case EEntityStat.ExtraCritical: _extraCritical += amount; break;
            case EEntityStat.Hero:
                Debug.Log("Hero 여부는 변경할 수 없습니다");
                break;
        }

        Debug.Log($"[{_name}] 스탯 변경됨 -> {statType} : {amount}");
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
            EEntityStat.CurrentSpeed => _currentSpeed,
            EEntityStat.Movement => _movement,
            EEntityStat.BleedRes => _bleedRes,
            EEntityStat.BlightRes => _blightRes,
            EEntityStat.BurnRes => _burnRes,
            EEntityStat.DiseaseRes => _diseaseRes,
            EEntityStat.StunRes => _stunRes,
            EEntityStat.MoveRes => _moveRes,
            EEntityStat.DebuffRes => _debuffRes,
            EEntityStat.DeathblowRes => _deathblowRes,
            EEntityStat.ExtraCritical => _extraCritical,
            EEntityStat.Hero => _hero ? 1f : 0f,

            _ => 0f,
        };
    }

    public void ResetStatToDefault(EEntityStat statType)
    {
        if (_heroBasicData == null)
        {
            Debug.LogError($"[{gameObject.name}] CHeroBasicDataSO가 없어 스탯을 초기화할 수 없습니다.", this);
            return;
        }

        switch (statType)
        {
            case EEntityStat.HP:
                _currentHP = _heroBasicData.MaxHP;
                break;
            case EEntityStat.MaxHP:
                _maxHP = _heroBasicData.MaxHP;
                break;

            case EEntityStat.Stress:
                _currentStress = 0;
                break;
            case EEntityStat.MaxStress:
                _maxStress = 10;
                break;

            case EEntityStat.Speed:
                _speed = _heroBasicData.Speed;
                break;
            case EEntityStat.CurrentSpeed:
                _currentSpeed = _speed;
                break;

            case EEntityStat.Movement:
                _movement = _heroBasicData.Movement;
                break;

            case EEntityStat.BleedRes: _bleedRes = _heroBasicData.BleedRes; break;
            case EEntityStat.BlightRes: _blightRes = _heroBasicData.BlightRes; break;
            case EEntityStat.BurnRes: _burnRes = _heroBasicData.BurnRes; break;
            case EEntityStat.DiseaseRes: _diseaseRes = _heroBasicData.DiseaseRes; break;
            case EEntityStat.StunRes: _stunRes = _heroBasicData.StunRes; break;
            case EEntityStat.MoveRes: _moveRes = _heroBasicData.MoveRes; break;
            case EEntityStat.DebuffRes: _debuffRes = _heroBasicData.DebuffRes; break;
            case EEntityStat.DeathblowRes: _deathblowRes = _heroBasicData.DeathblowRes; break;

            case EEntityStat.ExtraCritical:
                _extraCritical = 0f;
                break;
            case EEntityStat.Hero:
                _hero = _heroBasicData.Hero;
                break;
        }

        Debug.Log($"[{_name}] 스탯 초기화됨 -> {statType}이(가) 기본값으로 복구되었습니다.");
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

    public ICombatData ReturnCombatData()
    {
        return this;
    }

    public bool IsHero()
    {
        return _hero;
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
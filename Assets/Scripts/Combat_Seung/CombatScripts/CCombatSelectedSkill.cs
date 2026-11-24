using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CCombatSelectedSkill : MonoBehaviour
{
    [Header("CCombatCurrentTurn 스크립트 참조")]
    [SerializeField] private CCombatCurrentTurn _currentTurn;

    // 원래 전체 8개를 한번에 리스트로 놨었는데, 팀원 / 적으로 나눠서 재구성함.
    [Header("아군 랭크 4개")]
    [SerializeField] private CCombatRank[] _teamRanks;
    [Header("아군 랭크 사이 + 아이콘")]
    [SerializeField] private GameObject[] _teamPlusIcons;

    [Header("적군 랭크 4개")]
    [SerializeField] private CCombatRank[] _enemyRanks;
    [Header("적군 랭크 사이 + 아이콘")]
    [SerializeField] private GameObject[] _enemyPlusIcons;

    private CEntityRuntimeData _entity;
    private CSkillDataSO _skillData;

    private void Awake()
    {
        OffIndicator();

        _currentTurn.StateReset();
    }

    public void InputSelectedSkill(CEntityRuntimeData entity, ICombatData combatData)
    {
        if (combatData is CSkillDataSO skillData)
        {
            _entity = entity;
            _skillData = skillData;
            CheckTarget();
        }

        else
        {
            return;
        }
    }

    private void CheckTarget()
    {
        CPrint.Log("스킬의 대상을 선택하세요");

        int targetBitmask = _skillData.GetTargetBitmask();

        for(int i = 0; i < _teamRanks.Length;  i++)
        {
            if(_teamRanks[i] != null)
            {
                _teamRanks[i].CheckTargetRank(targetBitmask, _skillData.Type);
            }

        }

        for(int j = 0;  j < _enemyRanks.Length; j++)
        {
            if (_enemyRanks[j] != null)
            {
                _enemyRanks[j].CheckTargetRank(targetBitmask, _skillData.Type);
            }

        }

        for(int k = 0; k < 3; k++)
        {
            _teamPlusIcons[k].SetActive(_skillData.AOE && _teamRanks[k].Targetable && _teamRanks[k + 1].Targetable);
            _enemyPlusIcons[k].SetActive(_skillData.AOE && _enemyRanks[k].Targetable && _enemyRanks[k + 1].Targetable);

        }

    }

    public void InputTargetData(ICombatData combatData)
    {
        if (combatData is CEntityRuntimeData target && !target.IsDead())
        {
            for(int i = 0; i< _teamRanks.Length;  i++)
            {
                if (
                    _teamRanks[i] != null && _teamRanks[i].Targetable == true && _teamRanks[i].GetEntityRuntimeData() == target ||
                    _enemyRanks[i] != null && _enemyRanks[i].Targetable == true && _enemyRanks[i].GetEntityRuntimeData() == target
                    )
                {
                    _skillData.UseSKill(_entity, target);

                    OffIndicator();

                    _currentTurn.StateReset();

                    CCombatManager.Instance.TurnOver();

                    return;
                }

            }
        }
    }

    public void InputEveryTargetData()
    {
        List<CEntityRuntimeData> targets = new List<CEntityRuntimeData>();

        for (int i = 0; i < _teamRanks.Length; i++)
        {
            if (_teamRanks[i].Targetable)
            {
                if(!_entity.IsDead())
                {                
                    targets.Add(_teamRanks[i].GetEntityRuntimeData());
                }

            }
        }

        for (int j = 0; j < _enemyRanks.Length; j++)
        {
            if (_enemyRanks[j].Targetable)
            {
                if (!_entity.IsDead())
                {
                    targets.Add(_enemyRanks[j].GetEntityRuntimeData());
                }

            }
        }

        _skillData.UseSKill(_entity, targets);

        OffIndicator();

        _currentTurn.StateReset();

        CCombatManager.Instance.TurnOver();
    }


    private bool CheckHit()
    {
        return true;
    }

    private bool CheckCritical()
    {
        return true;
    }

    private void OffIndicator()
    {
        if(_teamRanks != null)
        {
            for(int i = 0; i < _teamRanks.Length; i++)
            {
                _teamRanks[i].ResetTargetRank();
            }
        }

        if(_enemyRanks != null)
        {
            for(int i = 0; i < _enemyRanks.Length; i++)
            {
                _enemyRanks[i].ResetTargetRank();
            }
        }

        if (_teamPlusIcons != null)
        {
            for (int i = 0; i < _teamPlusIcons.Length; i++)
            {
                _teamPlusIcons[i].SetActive(false);
            }
        }

        if (_enemyPlusIcons != null)
        {
            for (int i = 0; i < _enemyPlusIcons.Length; i++)
            {
                _enemyPlusIcons[i].SetActive(false);
            }
        }

    }

}

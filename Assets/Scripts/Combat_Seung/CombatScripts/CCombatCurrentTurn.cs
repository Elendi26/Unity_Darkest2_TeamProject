using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public enum ECombatSelectType
{
    None,
    Entity,
    Entities,
    Move
}

public class CCombatCurrentTurn : MonoBehaviour
{
    private CEntityRuntimeData _entity;

    [SerializeField] private CCombatSelectedSkill _seletedSkill;
    [SerializeField] private CombatSkillbar_UI _combatSkillbar;
    
    private List<(EUsableSkillState, int, CSkillDataSO)> _skillData;

    private ECombatSelectType _selectType = ECombatSelectType.None;

    #region 프로퍼티
    public ECombatSelectType SelectType => _selectType;
    public CCombatSelectedSkill SelectedSkill => _seletedSkill;

    #endregion

    // 내부 변수
    private bool _isExecutingEnemyTurn;

    // 이벤트

    // 현재 턴이 누구인지 이벤트를 보낸다. (영웅 턴일 때만)
    public event Action<CEntityRuntimeData> ChangedEntityTurn;

    private void OnEnable()
    {
        if(_combatSkillbar != null)
        {
            _combatSkillbar.OnSkillSelected += SkillSelect;
        }
    }

    private void OnDisable()
    {
        if (_combatSkillbar != null)
        {
            _combatSkillbar.OnSkillSelected -= SkillSelect;
        }
    }

    public void InputCurrentTurnEntity(CEntityRuntimeData entity)
    {
        _entity = entity;

        CurrentTurnMechanism();
    }
    
    private void CurrentTurnMechanism()
    {
        if (_entity == null)
        {
            return;
        }

        if (_entity.IsHero())
        {
            SetSkillData();

            _combatSkillbar.HeroTurnSkillDataSet(_skillData);

            ChangedEntityTurn?.Invoke(_entity);
        }

        else
        {
            CPrint.Log($"적 몬스터 턴 진입 {_entity.EntityName}");
            // 임시로 일단 적군 턴은 넘기기
            //CCombatManager.Instance.TurnOver();
            // 적 AI 행동패턴 추가

            ExecuteEnemyTurn();
        }
    }

    public CEntityRuntimeData GetCurrentTurnEntityInfo()
    {
        return _entity;
    }

    public float GetCurrentTurnEntityInfo(EEntityStat status)
    {
        return _entity.GetStat(status);
    }

    private void SetSkillData()
    {
        _skillData = _entity._skillData.OutputSkillDataChecked(_entity.GetRank());
    }

    public List<(EUsableSkillState, int, CSkillDataSO)> GetCurrentTurnSkillData()
    {
        return _skillData;
    }

    public void SkillSelect(CombatSkillSlot_UI slot)
    {
        if( slot != null)
        {
            if(slot.SlotType == ESlotTypes.Skill)
            {
                InputSelectedSkillData(slot.SkillData);
            }
        
            if(slot.SlotType == ESlotTypes.Move)
            {
                InputSelectedMove(_entity);
            }
        }
    }

    public void InputSelectedSkillData(CSkillDataSO skillData)
    {
        if (skillData != null)
        {
            if (skillData.AOE)
            {
                _selectType = ECombatSelectType.Entities;
            }

            else
            {
                _selectType = ECombatSelectType.Entity;
            }

            _seletedSkill.InputSelectedSkill(_entity, skillData);
        }
    
    }

    public void InputSelectedMove(CEntityRuntimeData entity)
    {
        _selectType = ECombatSelectType.Move;

        _seletedSkill.InputMoveSlot(_entity);
    }


    public void StateReset()
    {
        _selectType = ECombatSelectType.None;
    }

    private void ExecuteEnemyTurn()
    {
        if(_isExecutingEnemyTurn)
        {
            return;
        }

        _isExecutingEnemyTurn = true;

        CCombatManager stateManager = CCombatManager.Instance;
        List<CEntityRuntimeData> combatEntities = stateManager != null ? stateManager.OnCombatEntities : null;
        CMonsterBrain brain = _entity != null ? _entity.MonsterBrain : null;

        if (brain == null || combatEntities == null || combatEntities.Count == 0)
        {
            _isExecutingEnemyTurn = false;
            stateManager?.TurnOver();
            return;
        }

        // 여기서부터 ai 반영 
        CMonsterBrainDecision decision = brain.Decide(_entity, combatEntities);

        // 사용 가능 스킬이 없거나 판단 실패 시 Pass -> 즉시 턴 종료
        if (
            decision == null ||
            decision.Decision == EBrainDecisionType.Pass ||
            decision.SelectedSkill == null ||
            decision.TargetUnits.Count == 0
            )
        {
            // Pass 처리 (턴 넘김, 대기 연출 등)
            CPrint.Log("턴 종료");
            stateManager?.TurnOver();
            return;
        }

        // Play -> 광역 여부 어떻게 되는지 잘 모르겠습니다.
        if (decision.SelectedSkill.AOE)
        {
            decision.SelectedSkill.UseSKill(_entity, decision.TargetUnits[0], false, true);
        }
        else
        {
            decision.SelectedSkill.UseSKill(_entity, decision.TargetUnits[0], false, true);
        }

        _isExecutingEnemyTurn = false;
        CPrint.Log("턴 종료");
        stateManager?.TurnOver();

        /*
        //적용은 이렇게 됩니다.
        //선택한 스킬
        CSkillDataSO skill = decision.SelectedSkill;
        // 선택한 타겟
        List<CEntityRuntimeData> targets = decision.TargetUnits;
        */
    }
}

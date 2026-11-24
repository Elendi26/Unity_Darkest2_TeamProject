using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public enum ECombatSelectType
{
    None,
    Entity,
    Entities
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

        CurrentTurnMech();
    }
    
    public void CurrentTurnMech()
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
            CPrint.Log("적 몬스터 턴 진입");
            // 임시로 일단 적군 턴은 넘기기
            CCombatManager.Instance.TurnOver();
            // 적 AI 행동패턴 추가
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

    public void SetSkillData()
    {
        _skillData = _entity._skillData.OutputSkillDataChecked(_entity.GetRank());
    }

    public List<(EUsableSkillState, int, CSkillDataSO)> GetCurrentTurnSkillData()
    {
        return _skillData;
    }

    public void SkillSelect(CombatSkillSlot_UI slot)
    {
        if(slot != null && slot.SlotType != ESlotTypes.TurnChange)
        {
            InputSelectedSkillData(slot.SkillData);
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

    public void StateReset()
    {
        _selectType = ECombatSelectType.None;
    }
}

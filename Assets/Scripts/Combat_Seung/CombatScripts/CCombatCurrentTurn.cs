using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ECombatSelectType
{
    None,
    Skill,
    Entity,
    Entities
}

public class CCombatCurrentTurn : MonoBehaviour
{
    private CEntityRuntimeData _entity;

    private List<(bool, CSkillDataSO)> _skillData;
    private List<CCombatSkillSlot> _skillSlots;

    private CCombatSelectedSkill _seletedSkill;

    private ECombatSelectType _selectType = ECombatSelectType.None;

    public ECombatSelectType SelectType => _selectType;
    public CCombatSelectedSkill SelectedSkill => _seletedSkill;

    public void InputCurrentTurnEntity(CEntityRuntimeData entity)
    {
        _entity = entity;

        CurrentTurnMech();
    }
    
    public void CurrentTurnMech()
    {
        SetSkillData();
        SkillSelect();
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

    public List<(bool, CSkillDataSO)> GetCurrentTurnSkillData()
    {
        return _skillData;
    }

    public void SkillSelect()
    {
        _selectType = ECombatSelectType.Skill;
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


}

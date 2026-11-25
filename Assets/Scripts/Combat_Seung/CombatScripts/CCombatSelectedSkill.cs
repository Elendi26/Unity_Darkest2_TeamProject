using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CCombatSelectedSkill : MonoBehaviour
{
    [SerializeField] private List<CCombatRank> _combatRanks;

    private CEntityRuntimeData _entity;
    private CSkillDataSO _skillData;

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
        int targetBitmask = _skillData.GetTargetBitmask();

        for (int i = 0;  i < _combatRanks.Count; i++)
        {
            _combatRanks[i].CheckTargetRank(targetBitmask, _skillData.Type);
        }
    }

    public void InputTargetData(ICombatData combatData)
    {
        if (combatData is CEntityRuntimeData target)
        {
            SkillExcute(target);
        }
    }

    public void InputEveryTargetData()
    {
        List<CEntityRuntimeData> targets = new List<CEntityRuntimeData>();

        for (int i = 0; i < _combatRanks.Count; i++)
        {
            if (_combatRanks[i].Targetable)
            {
                targets.Add(_combatRanks[i].GetEntityRuntimeData());
            }
        }

        SkillExcute(targets);
    }


    private bool CheckHit(CEntityRuntimeData target)
    {
        float hit = 100f;

        if (_entity._tokenStatus.CheckToken(ETokenBitMask.Blind))
        {
            hit *= 0.5f;
        }

        if (target._tokenStatus.CheckToken(ETokenBitMask.Dodge))
        {
            hit *= 0.5f;
        }

        else if (target._tokenStatus.CheckToken(ETokenBitMask.DodgeE))
        {
            hit *= 0.25f;
        }

        float rand = Random.Range(0, 100f);

        if (rand <= hit)
        {
            return true;
        }

        else
        {
            return false;
        }
    }

    private bool CheckCritical(CEntityRuntimeData target)
    {
        if (_entity._tokenStatus.CheckToken(ETokenBitMask.Crit))
        {
            return true;
        }

        float ciritcal = (_skillData.Critical) + _entity.GetStat(EEntityStat.ExtraCritical);

        float rand = Random.Range(0, 100);

        if (rand <= ciritcal)
        {
            return true;
        }

        else
        {
            return false;
        }
    }

    private void SkillExcute(CEntityRuntimeData target)
    {
        _skillData.UseSKill(_entity, target, CheckCritical(target), CheckHit(target));
    }

    private void SkillExcute(List<CEntityRuntimeData> targets)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            _skillData.UseSKill(_entity, targets[i], CheckHit(targets[i]), CheckCritical(targets[i]));
        }
    }

}

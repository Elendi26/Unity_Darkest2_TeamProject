using System.Collections;
using System.Collections.Generic;
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
            _combatRanks[i].CheckRank(_skillData.Rank, _skillData.Type);
        }
    }

    public void InputTargetData(ICombatData combatData)
    {
        if (combatData is CEntityRuntimeData target)
        {
            _skillData.UseSKill(_entity, target);

            CCombatStateManager.Instance.TurnOver();
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

        _skillData.UseSKill(_entity, targets);

        CCombatStateManager.Instance.TurnOver();
    }


    private bool CheckHit()
    {
        return true;
    }

    private bool CheckCritical()
    {
        return true;
    }



}

using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CEntitySkillData
{
    private int _slotCount;
    private List<CSkillDataSO> _skillData;

    public CEntitySkillData (int slotCount)
    {
        _slotCount = slotCount;
        _skillData = new List<CSkillDataSO>();
    }

    public void InputSkillData(List<(string, int)> skillIndex)
    {
        for (int i = 0; i < skillIndex.Count; i++)
        {
            _skillData.Add(CSkillDataBase.Instance.GetSkillData(skillIndex[i].Item1, skillIndex[i].Item2));
        }
    }

    public List<CSkillDataSO> OutputSkillData()
    {
        return _skillData;
    }

    public List<(EUsableSkillState, int, CSkillDataSO)> OutputSkillDataChecked(ERank entityRank)
    {
        List<(EUsableSkillState, int, CSkillDataSO)> checkedList = new List<(EUsableSkillState, int, CSkillDataSO)>();
        (EUsableSkillState, int, CSkillDataSO) data;
        int rank = (int) entityRank;

        for (int i = 0; 0 < _skillData.Count; i++)
        {
            if ((rank |= _skillData[i].GetRankBitmask()) != 0)
            {
                data.Item1 = EUsableSkillState.Usable;
            }

            else
            {
                data.Item1 = EUsableSkillState.InvalidRank;
            }

            data.Item2 = 0;
            data.Item3 = _skillData[i];

            checkedList[i] = data;
        }

        return checkedList;
    }
}

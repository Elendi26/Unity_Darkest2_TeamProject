using System.Collections;
using System.Collections.Generic;
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
            _skillData[i] = CSkillDataBase.Instance.GetSkillData(skillIndex[i].Item1, skillIndex[i].Item2);
        }
    }

    public List<CSkillDataSO> OutputSkillData()
    {
        return _skillData;
    }

    public List<(bool, CSkillDataSO)> OutputSkillDataChecked(ERank entityRank)
    {
        List<(bool, CSkillDataSO)> checkedList = new List<(bool, CSkillDataSO)>();
        (bool, CSkillDataSO) data;
        int rank = (int) entityRank;

        for (int i = 0; 0 < _skillData.Count; i++)
        {
            data.Item1 = ((rank |= _skillData[i].GetRankBitmask()) != 0);
            data.Item2 = _skillData[i];

            checkedList[i] = data;
        }

        return checkedList;
    }
}

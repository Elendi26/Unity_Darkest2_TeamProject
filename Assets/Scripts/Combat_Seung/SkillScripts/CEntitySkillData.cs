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

    public List<(EUsableSkillState, int, CSkillDataSO)> OutputSkillDataChecked(ERank entityRank)
    {
        List<(EUsableSkillState, int, CSkillDataSO)> checkedList = new List<(EUsableSkillState, int, CSkillDataSO)>();
        (EUsableSkillState, int, CSkillDataSO) data;
        int rank = (int) entityRank;

        for (int i = 0; 0 < _skillData.Count; i++)
        {
            data.Item1 = ((rank |= _skillData[i].GetRankBitmask()) != 0); // if 로 변경하고 true일 때 사용 가능 상태로 전환
            data.Item2 = 0; // 뭘 넣어야 할까요?
            data.Item3 = _skillData[i];

            checkedList[i] = data;
        }

        return checkedList;
    }
}

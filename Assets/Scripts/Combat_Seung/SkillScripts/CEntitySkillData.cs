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
        // 일단 안전하게 먼저 청소
        _skillData.Clear();

        for (int i = 0; i < skillIndex.Count; i++)
        {
            _skillData.Add(CSkillDataBase.Instance.GetSkillData(skillIndex[i].Item1, skillIndex[i].Item2));
        }
    }

    public void InputSkillData(List<CSkillDataSO> skills)
    {
       if(skills == null)
        {
            return;
        }

        _skillData = new List<CSkillDataSO>(skills);
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

        // 0 < _skillData.Count -> i < _skillData.Count 로 수정
        for (int i = 0; i < _skillData.Count; i++)
        {
            // |= 에서 &로 수정
            if ((rank & _skillData[i].GetRankBitmask()) != 0)
            {
                data.Item1 = EUsableSkillState.Usable;
            }

            else
            {
                data.Item1 = EUsableSkillState.InvalidRank;
            }

            data.Item2 = 0;
            data.Item3 = _skillData[i];

            // checkedList[i] = data 에서 수정함
            checkedList.Add(data);
        }

        return checkedList;
    }
}

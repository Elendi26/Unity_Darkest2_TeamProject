using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬을 골라 이름, 인덱스의 리스트로 만들고 다시 데이터로 쏴주는 클래스
/// </summary>
public class CSkillIndexor : MonoBehaviour
{
    private int _slotMaximum;
    private List<(string, int)> _skillIndex;

    public List<(string, int)> SkillIndex => _skillIndex;

    public CSkillIndexor(int slotMaximum = 5)
    {
        _slotMaximum = slotMaximum;
        _skillIndex = new List<(string, int)>();
    }

    public void AddSkillIndex((string, int) skillIndex)
    {
        if (_skillIndex.Count >= _slotMaximum)
        {
            Debug.LogWarning("스킬 인덱스 가득 참");

            return;
        }

        _skillIndex.Add (skillIndex);
    }

    public void RemoveSkillIndex(int slotNumber)
    {
        if ( slotNumber > _skillIndex.Count)
        {
            Debug.LogWarning("제거할 스킬 인덱스가 없습니다");

            return;
        }

        _skillIndex.Remove (_skillIndex[slotNumber]);
    }

    public List<(string, int)> GetSkillIndex()
    {
        if (_skillIndex == null)
        {
            return default;
        }

        return _skillIndex;
    }

    public List<CSkillDataSO> GetSkillData()
    {
        if (_skillIndex == null)
        {
            Debug.LogWarning("스킬 인덱스가 비었습니다");

            return default;
        }

        List<CSkillDataSO> skillDataSO = new List<CSkillDataSO>(); 

        for (int i = 0;  i < _skillIndex.Count; i++)
        {
            skillDataSO.Add(CSkillDataBase.Instance.GetSkillData(_skillIndex[i].Item1, _skillIndex[i].Item2));
        }

        return skillDataSO;
    }
}

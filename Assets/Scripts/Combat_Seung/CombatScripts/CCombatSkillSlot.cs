using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CCombatSkillSlot : MonoBehaviour
{
    [SerializeField] private Sprite _sprite;

    private EUsableSkillState usableSkillState;
    private int _index;
    private CSkillDataSO _skillData;

    public void InputSkillData((EUsableSkillState, int, CSkillDataSO) skillData)
    {
        _active = skillData.Item1;

        _usableSkillState = skillData.Item1;
        _index = skillData.Item2;
        _skillData = skillData.Item3;


        SetSkillSlotUI();
    }

    private void SetSkillSlotUI()
    {
        
    }
}

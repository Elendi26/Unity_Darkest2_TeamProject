using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CCombatSkillSlot : MonoBehaviour
{
    [SerializeField] private Sprite _sprite;

    private bool _active = false;
    private CSkillDataSO _skillData;

    public void InputSkillData((bool, CSkillDataSO) skillData)
    {
        _active = skillData.Item1;
        _skillData = skillData.Item2;

        SetSkillSlotUI();
    }

    private void SetSkillSlotUI()
    {
        if (_active)
        {
            _sprite = _skillData.ActiveImage;
        }

        else
        {
            _sprite = _skillData.InactiveImage;
        }
    }
}

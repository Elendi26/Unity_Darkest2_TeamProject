using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CCombatSkillSlot : MonoBehaviour
{
    [SerializeField] private Image _image;

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
            _image = _skillData.ActiveImage;
        }

        else
        {
            _image = _skillData.InactiveImage;
        }
    }
}

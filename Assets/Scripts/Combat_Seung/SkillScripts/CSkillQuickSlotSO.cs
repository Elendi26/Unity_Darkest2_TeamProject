using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(menuName = "2D3D/SO/SkillQuickSlotSO", fileName = "SkillQuickSlot")]
public class CSkillQuickSlotSO : ScriptableObject
{
    [SerializeField] private List<object> _slots; // 넣고 싶은 것 선택 ex) 오브젝트, 이미지 등등
    [SerializeField] private object _emptyUI; // 빈 UI 용

    /// <summary>
    /// 이 함수를 호출하면 슬롯들에 각 SkillDataSO의 UI 변수에 할당된 것들을 가지고 옴
    /// </summary>
    /// <param name="skillData"></param
    public void InputSkillDataCurrentTurn(List<CSkillDataSO> skillData)
    {
        for (int i = 0; i < skillData.Count; i++)
        {
            if (skillData[i] == null)
            {
                _slots[i] = _emptyUI;

                continue;
            }

            _slots[i] = skillData[i].UI;
        }
    }

    public void MakeEmptySkillQuickSlot()
    {
        for (int i =0; i < _slots.Count; i++)
        {
            _slots[i] = _emptyUI;
        }
    }
}

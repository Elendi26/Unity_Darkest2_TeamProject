using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

public enum SlotTypes
{
    Skill,
    Move,
    TurnChange
}

public class CombatSkillSlot_UI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
{
    #region 인스펙터
    [Header("슬롯 타입 결정")]
    [SerializeField] private SlotTypes _slotType;
    [Header("이미지")]
    [SerializeField] private Image _slotImage;
    [Header("하이라이트 (선택)")]
    [SerializeField] private GameObject _selectHL;
    [Header("턴 패스 게이지 이미지")]
    [SerializeField] private Image _turnPassGauge;

   

    #endregion

    // 이벤트 선언

    // 마우스를 올렸을 때
    public event Action<CombatSkillSlot_UI> OnPointerEntered;

    // 마우스를 뺏을 때
    public event Action<CombatSkillSlot_UI> OnPointerExited;

    // 클릭했을 때
    public event Action<CombatSkillSlot_UI> OnClickedSlot;

    // 누르기 시작했을 때
    public event Action<CombatSkillSlot_UI> OnSlotPointerDown;

    // 누르는 것을 땠을 때
    public event Action<CombatSkillSlot_UI> OnSlotPointerUp;


    #region 프로퍼티
    public SlotTypes SlotType => _slotType;

    #endregion

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnPointerEntered?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnPointerExited?.Invoke(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClickedSlot?.Invoke(this);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnSlotPointerDown?.Invoke(this);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        OnSlotPointerUp?.Invoke(this);
    }
    
    public void GaugeCharge(float value)
    {
        if(_turnPassGauge != null)
        {
            _turnPassGauge.fillAmount = value;
        }
        
    }


    // 선택시 하이라이트 켜고 끄기
    public void SelectOnHighLight(bool isSelected)
    {
        if(_selectHL != null)
        {
            _selectHL.SetActive(isSelected);
        }
    }


}

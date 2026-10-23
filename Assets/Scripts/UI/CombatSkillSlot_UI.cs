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

    [Header("스킬 슬롯 번호")]
    [SerializeField] private int _slotNum;

    [Header("이미지")]
    [SerializeField] private Image _slotImage;

    [Header("하이라이트 (1.선택/2.포인터)")]
    [SerializeField] private GameObject _selectHL;
    [SerializeField] private GameObject _pointerHL;

    [Header("턴 패스 게이지 이미지")]
    [SerializeField] private Image _turnPassGauge;

    [Header("스킬 데이터")]
    [SerializeField] private DummySkillData _skillData;


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

    public int SlotNum => _slotNum;

    public DummySkillData SkillData => _skillData;

    #endregion

    private void Start()
    {
        SelectOnHighLight(false);
        PointerOnHighLight(false);
    }


    /// <summary>
    /// 스킬 아이콘 동기화
    /// </summary>
    /// <param name="data"></param>
    public void SetIconImage(DummySkillData data)
    {
        _skillData = data;

        if(_slotImage != null && _skillData != null)
        {
            _slotImage.sprite = _skillData.SkillIcon;
        }
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        PointerOnHighLight(true);
        OnPointerEntered?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PointerOnHighLight(false);
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


    /// <summary>
    /// 선택시 하이라이트 켜고 끄기
    /// </summary>
    /// <param name="isSelected"></param>
    public void SelectOnHighLight(bool isSelected)
    {
        if(_selectHL != null)
        {
            _selectHL.SetActive(isSelected);
        }
    }

    public void PointerOnHighLight(bool onPointer)
    {
        if(_pointerHL != null)
        {
            _pointerHL.SetActive(onPointer);
        }
    }


}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PopupSkillSlot_UI : MonoBehaviour, IPointerClickHandler
{
    #region 인스펙터
    [Header("스킬 슬롯 번호")]
    [SerializeField] private int _skillIndex;

    [Header("스킬 이미지 들어갈 공간")]
    [SerializeField] private Image _skillImage;

    [Header("선택 하이라이트")]
    [SerializeField] private GameObject _frameHL;

    #endregion

    // 내부 변수

    private bool _isLock;

    // 프로퍼티
    public GameObject FrameHL => _frameHL;

    // 이벤트
    public event Action<int> OnPopupSkillSlotClicked;

    public void OnPointerClick(PointerEventData eventData)
    {
        if(eventData.button == PointerEventData.InputButton.Left)
        {
            OnPopupSkillSlotClicked?.Invoke(_skillIndex);
        }
    }

    public void ClearSlot()
    {
        // 스킬 이미지 끄고
        _skillImage.enabled = false;
        // 하이라이트 끄고
        _frameHL.SetActive(false);

    }

    /// <summary>
    /// 스킬 데이터 받으면 해당 스킬 아이콘 켜는 함수
    /// </summary>
    public void SetSlot(Sprite skillImage)
    {
        _skillImage.sprite = skillImage;
        _skillImage.enabled = true;
    }




}

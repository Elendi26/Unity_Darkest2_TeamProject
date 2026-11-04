using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Profile_popup : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    #region 인스펙터

    [Header("프로필 변경 UI")]
    [SerializeField] private GameObject _popupUI;

    [Header("색변경")]
    [SerializeField] private GameObject _selectHL;

    #endregion

    // 이벤트
    public event Action OnProfileButtonClicked;


    void Start()
    {
        if (_popupUI == null || _selectHL == null)
        {
            CPrint.Warn("UI가 비어있다. 인스펙터 확인");
            return;
        }

        else
        {
            _popupUI.SetActive(false);
            _selectHL.SetActive(false);
        }
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        _popupUI.SetActive(true);
        _selectHL.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _popupUI.SetActive(false);
        _selectHL.SetActive(false);
    }

    /// <summary>
    /// 클릭했다는 이벤트만 전달
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerClick(PointerEventData eventData)
    {
        OnProfileButtonClicked?.Invoke();
    }

}

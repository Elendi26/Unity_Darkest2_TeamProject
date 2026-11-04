using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Popup_basic : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // 인스펙터

    [SerializeField] private GameObject _popupUI;


    void Start()
    {
        if (_popupUI == null)
        {
            CPrint.Warn("UI가 비어있다. 인스펙터 확인");
            return;
        }

        else
        {
            _popupUI.SetActive(false);
        }
    }

    // 마우스를 가져가면 툴팁 UI 활성화 
    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        _popupUI.SetActive(true);
    }

    // 빠지면 비활성화
    public virtual void OnPointerExit(PointerEventData eventData)
    {
        _popupUI.SetActive(false);
    }
}

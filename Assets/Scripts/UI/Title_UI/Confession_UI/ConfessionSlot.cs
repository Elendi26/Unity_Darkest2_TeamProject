using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class ConfessionSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    #region 인스펙터

    [Header("마우스 감지 시 프레임")]
    [SerializeField] private GameObject _onPointerFrame;

    [Header("마우스 감지 시 글씨 하이라이트")]
    [SerializeField] private GameObject _textHL;

    #endregion
    void Start()
    {
        _onPointerFrame.SetActive(false);
        _textHL.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _onPointerFrame.SetActive(true);
        _textHL.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _onPointerFrame.SetActive(false);
        _textHL.SetActive(false);
    }

}

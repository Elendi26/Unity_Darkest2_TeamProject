using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.EventSystems;

public class HighLightOnOff : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // 인스펙터
    [Header("글씨 하이라이트")]
    [SerializeField] private GameObject UITextHighLight;

    [Header("프레임 하이라이트")]
    [SerializeField] private GameObject UIFrameHighLight;

    void Start()
    {
        if (UITextHighLight != null)
        {
            UITextHighLight.SetActive(false);
        }

        else if (UITextHighLight == null)
        {
            return;
        }

        if (UIFrameHighLight != null)
        {
            UIFrameHighLight.SetActive(false);
        }

    }

    // 마우스를 가져가면 툴팁 UI 활성화 
    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        if (UITextHighLight != null)
        {
            UITextHighLight.SetActive(true);
        }

        else if (UITextHighLight == null)
        {
            return;
        }

        if (UIFrameHighLight != null)
        {
            UIFrameHighLight.SetActive(true);
        }

    }

    // 빠지면 비활성화
    public virtual void OnPointerExit(PointerEventData eventData)
    {
        if (UITextHighLight != null)
        {
            UITextHighLight.SetActive(false);
        }

        else if (UITextHighLight == null)
        {
            return;
        }

        if (UIFrameHighLight != null)
        {
            UIFrameHighLight.SetActive(false);
        }

    }

}

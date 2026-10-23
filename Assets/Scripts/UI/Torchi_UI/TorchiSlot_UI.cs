using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TorchiSlot_UI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    #region 인스펙터

    [Header("토치의 불꽃 이미지")]
    [SerializeField] private Image _torchIcon;

    [Header("토치 팝업 UI 참조")]
    [SerializeField] private TorchiPopup_UI _torchPopup;

    [Header("토치 데이터")]
    [SerializeField] private TorchData_SO _data;

    #endregion


    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        // 툴팁에 데이터 넣고
        SetTorcData();
        // 패널 켜고
        _torchPopup.SetPanelOnOff(true);
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        _torchPopup.SetPanelOnOff(false);
    }

    /// <summary>
    /// 툴팁으로 데이터 넣는 함수
    /// </summary>
    public void SetTorcData()
    {
        // 현재 데이터 받고
        int currnetValue = TorchManager.Instance.CurrentTorchValue;
        TorchData_SO currentData = TorchManager.Instance.GetCurrentValue();

        // 툴팁으로 데이터 넘기고
        _torchPopup.SetTorchData(currentData, currnetValue);
    }
}

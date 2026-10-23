using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CombatModifierSlot_UI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    #region 인스펙터
    [Header("아이콘")]
    [SerializeField] private Image _icon;

    [Header("팝업UI")]
    [SerializeField] private Popup_UI _popup;

    [Header("데이터")]
    [SerializeField] private CombatModifier_SO _data;

    #endregion

    /// <summary>
    /// 데이터 넣고 아이콘 갱신
    /// </summary>
    /// <param name="data"></param>
    public void SetData(CombatModifier_SO data)
    {
        CPrint.Log("모디파이어 아이콘이 갱신되었다");
        _data = data;
        _icon.sprite = _data.Icon;
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_data != null && _popup != null)
        {
            _popup.SetText(_data.Title, _data.Explain);
            _popup.SetPanelOnOff(true);
        }

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_popup != null)
        {
            _popup.SetPanelOnOff(false);
        }
    }
}

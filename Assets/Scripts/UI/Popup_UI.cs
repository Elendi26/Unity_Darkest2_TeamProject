using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class Popup_UI : MonoBehaviour
{
    #region 인스펙터
    [SerializeField] private CanvasGroup _explainUI;

    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _explainText;

    #endregion

    #region 프로퍼티

    public TextMeshProUGUI Title => _titleText;
    public TextMeshProUGUI Explain => _explainText;


    #endregion


    void Start()
    {
        if (_explainUI == null)
        {
            CPrint.Warn("UI가 비어있다. 인스펙터 확인");
            return;
        }

        else
        {
            SetPanelOnOff(false);
        }
    }


    /// <summary>
    /// 텍스트 갱신하기
    /// </summary>
    /// <param name="title"></param>
    /// <param name="explain"></param>
   public void SetText(string title, string explain)
    {
        _titleText.text = title;
        _explainText.text = explain;
    }

    public void SetPanelOnOff(bool isTurnOn)
    {
        float panelAlpha = isTurnOn ? 1.0f : 0.0f;
        _explainUI.alpha = panelAlpha;
        
        // 팝업창 클릭 막기
        _explainUI.blocksRaycasts = false;
    }
}

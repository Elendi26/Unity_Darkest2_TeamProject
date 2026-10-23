using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TorchiPopup_UI : MonoBehaviour
{
    #region 인스펙터

    [Header("토치 툴팁패널")]
    [SerializeField] private CanvasGroup _torchPanel;

    [Header("촛불 상태 텍스트")]
    [SerializeField] private TextMeshProUGUI _stateText;

    [Header("촛불 수치 텍스트")]
    [SerializeField] private TextMeshProUGUI _valueText;

    [Header("아군 영향 텍스트")]
    [SerializeField] private TextMeshProUGUI _herosIfText;

    [Header("적군 영향 텍스트")]
    [SerializeField] private TextMeshProUGUI _enemiesIfText;

    #endregion

    void Start()
    {
        SetPanelOnOff(false);
    }

    /// <summary>
    /// 토치 데이터 입력 및 갱신
    /// </summary>
    /// <param name="data"></param>
    /// <param name="currentValue"></param>
    public void SetTorchData(TorchData_SO data, int currentValue)
    {
        _stateText.text = data.StateName;
        _valueText.text = $"{currentValue}";
        _herosIfText.text = data.HerosBuffExplain;
        _enemiesIfText.text = data.EnemiesBuffExplain;

    }

    /// <summary>
    /// 패널 알파값으로 켜고 끄기
    /// </summary>
    /// <param name="isTurnOn"></param>
    public void SetPanelOnOff(bool isTurnOn)
    {
        float panelAlpha = isTurnOn ? 1.0f : 0.0f;
        _torchPanel.alpha = panelAlpha;

        // 팝업창 클릭 막기
        _torchPanel.blocksRaycasts = false;
    }
}

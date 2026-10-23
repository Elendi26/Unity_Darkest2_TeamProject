using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillUITooltipManager : MonoBehaviour
{
    // 싱글톤 : 툴팁 내용 전달
    public static SkillUITooltipManager Instance { get; private set; }

    #region 인스펙터
    [Header("스킬 툴팁 캔버스 그룹")]
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("스킬 설명 텍스트")]
    [SerializeField] private TextMeshProUGUI _explainText;

    [Header("스킬 타입 텍스트")]
    [SerializeField] private TextMeshProUGUI _typeText;

    [Header("스킬 이름 텍스트")]
    [SerializeField] private TextMeshProUGUI _nameText;

    [Header("스킬 디테일 텍스트(피해, 치명타율, 조건 등)")]
    [SerializeField] private TextMeshProUGUI _detailText;

    [Header("스킬 타입 이미지")]
    [SerializeField] private Image _typeImage;

    [Header("스킬 범위 이미지")]
    [SerializeField] private Image _rangeImage;

    #endregion

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        else
        {
            Destroy(gameObject);
        }

        HideTooltip();
    }

    /// <summary>
    /// 스킬 툴팁 숨기기
    /// </summary>
    public void HideTooltip()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
        }
    }

    /// <summary>
    /// 스킬 데이터 SO 받아서 정리
    /// </summary>
    public void SetSkillData(DummySkillData data)
    {
        if (data == null)
        {
            return;
        }

        _nameText.text = data.SkillName;
        _explainText.text = data.SkillExplain;
        _typeText.text = data.SkillType;
        _detailText.text = data.SkillDetail;
        _typeImage.sprite = data.SkillTypeIcon;
        _rangeImage.sprite = data.SkillRangeImage;
    }
}

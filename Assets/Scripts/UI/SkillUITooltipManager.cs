using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillUITooltipManager : MonoBehaviour
{
    #region 인스펙터
    [Header("스킬 툴팁 캔버스 그룹")]
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("스킬 설명 텍스트")]
    [SerializeField] private TextMeshPro _explainText;

    [Header("스킬 타입 텍스트")]
    [SerializeField] private TextMeshPro _typeText;

    [Header("스킬 이름 텍스트")]
    [SerializeField] private TextMeshPro _nameText;

    [Header("스킬 타입 이미지")]
    [SerializeField] private Image _typeImage;

    [Header("스킬 범위 이미지")]
    [SerializeField] private Image _rangeImage;
    
    #endregion

    // 스킬 데이터 받기
    public void SetSkillData()
    {

    }
}

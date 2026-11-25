using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatEnemyInfo_UI : MonoBehaviour
{
    #region 인스펙터
    [Header("인풋핸들러 스크립트 참조")]
    [SerializeField] private CCombatInputHandler _inputHandler;

    [Header("info 캔버스그룹")]
    [SerializeField] private CanvasGroup _infoCanvasG;

    [Header("이름 출력용 텍스트")]
    [SerializeField] private TextMeshProUGUI _nameText;

    [Header("종족 출력용 텍스트")]
    [SerializeField] private TextMeshProUGUI _raceText;

    [Header("속도 수치 출력 텍스트")]
    [SerializeField] private TextMeshProUGUI _speedText;

    [Header("현재체력 및 최대체력 출력 텍스트")]
    [SerializeField] private TextMeshProUGUI _hpText;

    [Header("체력 게이지용 이미지")]
    [SerializeField] private Image _hpBar;

    [Header("출혈 수치 텍스트")]
    [SerializeField] private TextMeshProUGUI _bleedText;

    [Header("중독 수치 텍스트")]
    [SerializeField] private TextMeshProUGUI _blightText;

    [Header("화상 수치 텍스트")]
    [SerializeField] private TextMeshProUGUI _burnText;

    [Header("기절 수치 텍스트")]
    [SerializeField] private TextMeshProUGUI _stunText;

    [Header("이동 수치 텍스트")]
    [SerializeField] private TextMeshProUGUI _moveText;

    [Header("약화 수치 텍스트")]
    [SerializeField] private TextMeshProUGUI _debuffText;

    #endregion

    // 내부 변수

    
    // 현재 띄워놓은 정보가 누구의 정보인지 저장할 변수
    private CEntityRuntimeData _currentTarget;



    void Awake()
    {
        if (_infoCanvasG != null)
        {
            _infoCanvasG.alpha = 0f;
            _infoCanvasG.interactable = false;
            _infoCanvasG.blocksRaycasts = false;
        }
    }

    private void OnEnable()
    {
        if(_inputHandler != null)
        {
            _inputHandler.OnEnemyPointer += UpdateEnemyInfo;
        }
    }

    private void OnDisable()
    {
        if (_inputHandler != null)
        {
            _inputHandler.OnEnemyPointer -= UpdateEnemyInfo;
        }
    }

    public void UpdateEnemyInfo(CEntityRuntimeData data)
    {
        // 먼저 데이터 없거나 영웅이면 함수 진입 금지
        if(data == null || data.IsHero())
        {
            HideEnemyInfo();
            return;
        }

        // 현재 데이터에 저장
        _currentTarget = data;

        string currentHP = data.GetStat(EEntityStat.HP).ToString();
        string maxHP = data.GetStat(EEntityStat.MaxHP).ToString();

        // 텍스트에 데이터 연동
        _nameText.text = data.EntityName;
        _hpText.text = $"{currentHP}/{maxHP}";
        _raceText.text = data.Race;
        _speedText.text = data.GetStat(EEntityStat.Speed).ToString();
        if(data.GetStat(EEntityStat.HP) > 0)
        {
            _hpBar.fillAmount = data.GetStat(EEntityStat.HP) / data.GetStat(EEntityStat.MaxHP);
        }
        _bleedText.text = data.GetStat(EEntityStat.BleedRes).ToString();
        _blightText.text = data.GetStat(EEntityStat.BlightRes).ToString();
        _burnText.text = data.GetStat(EEntityStat.BurnRes).ToString();
        _stunText.text = data.GetStat(EEntityStat.StunRes).ToString();
        _moveText.text = data.GetStat(EEntityStat.MoveRes).ToString();
        _debuffText.text = data.GetStat(EEntityStat.DebuffRes).ToString();

        // 캔버스 띄우기
        if (_infoCanvasG != null)
        {
            _infoCanvasG.alpha = 1f;
        }
    }

    public void HideEnemyInfo()
    {
        _currentTarget = null;

        if (_infoCanvasG != null)
        {
            _infoCanvasG.alpha = 0f;
        }
    }

 
}

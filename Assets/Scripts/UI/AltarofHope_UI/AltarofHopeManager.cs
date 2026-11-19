using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum AltarMenu
{
    BoldCoast,
    LivingCity,
    DiligentFarm,
    Rumination
}

// 메뉴 4개를 한번에 정리하기 위한 구조체
[System.Serializable]
public struct AltarMenuView
{
    public AltarMenu menuType;
    public CanvasGroup menuCanvasG;
    public string menuTitle;
}


public class AltarofHopeManager : MonoBehaviour
{
    // 싱글톤
    public static AltarofHopeManager Instance;

    #region 인스펙터
    [Header("구조체 메뉴 4개")]
    [SerializeField] private AltarMenuView[] _menuViews;

    [Header("타이틀 텍스트")]
    [SerializeField] private TextMeshProUGUI _titleText;

    [Header("뒤로가기 버튼")]
    [SerializeField] private Button _backButton;

    [Header("출정 버튼")]
    [SerializeField] private GameObject _depatureButton;

    [Header("메인 UI 스크립트 참조")]
    [SerializeField] private AltarofHopeMain_UI _mainUI;
    #endregion

    // 내부변수
    
    // 구조체 쉽게 찾을 수 있게 딕셔너리화
    Dictionary<AltarMenu, AltarMenuView> _menuViewsDict = new Dictionary<AltarMenu, AltarMenuView>();



    // 인스턴스


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

        // 딕셔너리 등록
        for(int i = 0;  i < _menuViews.Length; i++)
        {
            _menuViewsDict.Add(_menuViews[i].menuType, _menuViews[i]);
        }
    }


    void Start()
    {
        ClearMenu();
    }

    private void OnEnable()
    {
        if(_mainUI != null)
        {
            _mainUI.TargetClicked += SwitchMenu;
        }
    }

    private void OnDisable()
    {
        if( _mainUI != null )
        {
            _mainUI.TargetClicked -= SwitchMenu;
        }
    }

    public void SwitchMenu(AltarMenu menu)
    {

    }

    /// <summary>
    /// 모든 창을 닫고 메인 화면만 보여준다.
    /// </summary>
    public void ClearMenu()
    {
        // 뒤로가기 버튼 끄고
        if (_backButton != null)
        {
            _backButton.gameObject.SetActive(false);
        }

        // 출정버튼 켜고
        if (_depatureButton != null)
        {
            _depatureButton.SetActive(true);
        }

        // 모든 팝업창 닫기
        foreach (var view in _menuViews)
        {
            view.menuCanvasG.alpha = 0f;
            view.menuCanvasG.interactable = false;
            view.menuCanvasG.blocksRaycasts = false;
        }
    }

}

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum PopupTab
{
    Relationship,
    CombatSkills,
    Status
}


public class HeroPopup_UI : MonoBehaviour
{
    // 이 팝업창의 경우 역마차, 전투, 갈림길 등 어느 곳에서나 켜지기 때문에 싱글톤으로 어디서나 부를 수 있게 한다.
    public static HeroPopup_UI instance;

    #region 인스펙터
    [Header("영웅 목록")]
    [SerializeField] private List<HeroDataSO> _heroes = new List<HeroDataSO>();

    [Header("직업 이름이 들어갈 텍스트")]
    [SerializeField] private TextMeshProUGUI _jopTitle;

    [Header("영웅 이름이 들어갈 텍스트")]
    [SerializeField] private TextMeshProUGUI _heroName;
    [SerializeField] private TextMeshProUGUI _heroName2;

    [Header("캐릭터 모델 띄울 이미지 공간")]
    [SerializeField] private RawImage _heroModelSpace;

    [Header("전투 기술 설정 용 캔버스그룹")]
    [SerializeField] private CanvasGroup _skillCanvas;

    [Header("관계 캔버스그룹")]
    [SerializeField] private CanvasGroup _rlsCanvas;

    [Header("상태 캔버스 그룹")]
    [SerializeField] private CanvasGroup _stsCanvas;

    [Header("전투 기술이 들어갈 이미지 공간")]
    [SerializeField] private GameObject[] _combatSkills;

    [Header("3D 모델 생성 위치")]
    [SerializeField] private Transform _modelSpawnPoint;

    [Header("모델을 출력할 출력용 카메라")]
    [SerializeField] private Camera _modelCamera;

    // 저항력 수치 들어갈 텍스트
    [Header("출혈")]
    [SerializeField] private TextMeshProUGUI _bleedingValue;
    [Header("중독")]
    [SerializeField] private TextMeshProUGUI _poisoningValue;
    [Header("화상")]
    [SerializeField] private TextMeshProUGUI _burnValue;
    [Header("기절")]
    [SerializeField] private TextMeshProUGUI _stunValue;
    [Header("이동")]
    [SerializeField] private TextMeshProUGUI _moveValue;
    [Header("약화")]
    [SerializeField] private TextMeshProUGUI _weakeningValue;
    [Header("질병")]
    [SerializeField] private TextMeshProUGUI _diseaseValue;
    [Header("죽격")]
    [SerializeField] private TextMeshProUGUI _deathblowValue;
    [Header("스트")]
    [SerializeField] private TextMeshProUGUI _stressValue;
    [Header("체력")]
    [SerializeField] private TextMeshProUGUI _heroHP;
    [Header("속도")]
    [SerializeField] private TextMeshProUGUI _speed;

    // TODO : 장신구 들어갈 배열 or 리스트 변수

    #endregion

    // 내부 변수

    private int _currentHP;

    private int _currentHeroIndex;

    private GameObject _currentModel;

    void Start()
    {
        if (instance == null)
        {
            instance = this;
        }

        else
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(this);

        if(_modelCamera != null)
        {
            _modelCamera.enabled = true;
        }

    }

    public void OpenPopup(List<HeroDataSO> list, int startIndex)
    {
        _currentHeroIndex = startIndex;
        list = _heroes;

        HeroDataUpdate(list[_currentHeroIndex]);
    }

    public void HeroDataUpdate(HeroDataSO data)
    {
        if(data == null)
        {
            return;
        }

        // 기존의 모델 존재하면 파괴 후 비우기
        if (_currentModel != null)
        {
            Destroy(_currentModel);
            _currentModel = null;
        }

        // 기본으로 출력할 탭은 전투 기술 탭
        SelectTab(PopupTab.CombatSkills);

        // 새로운 모델 생성한 후, 캐싱하기
        _currentModel = Instantiate(data.HeroModel, _modelSpawnPoint);
        
        // 위치, 로테이션 값 초기화
        _currentModel.transform.localPosition = Vector3.zero;
        _currentModel.transform.localRotation = Quaternion.Euler(0, 180f, 0);
        _currentModel.transform.localScale = Vector3.one;

        foreach (Transform child in _currentModel.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = _modelSpawnPoint.gameObject.layer;
        }

        // 카메라 렌더링 켜기 
        if (_modelCamera != null)
        {
            _modelCamera.enabled = true;
        }

        _jopTitle.text = data.JobTitle;
        _heroName.text = data.HeroName;
        _heroName2.text = data.HeroName;
        _bleedingValue.text = data.ResBleeding.ToString();
        _poisoningValue.text = data.ResPoisoning.ToString();
        _burnValue.text = data.ResBurn.ToString();
        _stunValue.text = data.ResStun.ToString();
        _moveValue.text = data.ResMove.ToString();
        _weakeningValue.text = data.ResWeakening.ToString();
        _diseaseValue.text = data.ResDisease.ToString();
        _deathblowValue.text = data.ResDeathBlow.ToString();
        _stressValue.text = data.HeroStressValue.ToString();
        _heroHP.text = data.HeroHP.ToString();
        _speed.text = data.HeroSpeed.ToString();

        // 둘 중 더 작은 값을 기준으로 반복문 돌릴 수 있게
        int numA = Mathf.Min(data.HeroSkills.Length, _combatSkills.Length);

        for (int i = 0; i < numA; i++)
        {
            _combatSkills[i].GetComponent<Image>().sprite = data.HeroSkills[i];
        }

    }

    private void CanvasGroupState(CanvasGroup targetCanvas, bool OnCanvas)
    {
        if (targetCanvas == null)
        {
            return;
        }

        targetCanvas.alpha = OnCanvas ? 1f : 0f;
        targetCanvas.interactable = OnCanvas;
        targetCanvas.blocksRaycasts = OnCanvas;


    }

    private void SelectTab(PopupTab targetTab)
    {
        CanvasGroupState(_rlsCanvas, targetTab == PopupTab.Relationship);
        CanvasGroupState(_skillCanvas, targetTab == PopupTab.CombatSkills);
        CanvasGroupState(_stsCanvas, targetTab == PopupTab.Status);
    }

    public void OnClickRelationTab()
    {
        SelectTab(PopupTab.Relationship);
    }

    public void OnClickCombatSkillsTab()
    {
        SelectTab(PopupTab.CombatSkills);
    }

    public void OnClickStatusTab()
    {
        SelectTab(PopupTab.Status);
    }

    public void OnClickNextHero()
    {
        _currentHeroIndex = (_currentHeroIndex + 1) % _heroes.Count;

        HeroDataUpdate(_heroes[_currentHeroIndex]);
    }

    public void OnClickpreviousHero()
    {
        _currentHeroIndex = (_currentHeroIndex - 1 + _heroes.Count) % _heroes.Count;

        HeroDataUpdate(_heroes[_currentHeroIndex]);
    }


    /// <summary>
    /// 팝업창이 꺼질 때, 카메라도 함께 사용을 중단해야 하므로 호출할 함수
    /// </summary>
    public void OffModelCamera()
    {
        if(_modelCamera != null)
        {
            _modelCamera.enabled = false;
        }

    }

}

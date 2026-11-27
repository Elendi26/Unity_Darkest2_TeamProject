using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;
using static UserProfileData;

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
    [SerializeField] private List<CEntityBasicDataSO> _heroes = new List<CEntityBasicDataSO>();

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

    [Header("팝업 스킬 슬롯 참조")]
    [SerializeField] private PopupSkillSlot_UI[] _skillSlots;

    [Header("모델링 뷰어 참조")]
    [SerializeField] private ModelingViewer_UI _modelingViewer;

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

    private int _currentHeroIndex;

    private bool _isTheCrossRoadScene;

    private bool _isCombatScene;

    private List<CEntityRuntimeData> _combatHeroes = new List<CEntityRuntimeData>();

    private CEntityRuntimeData _currentEntity;

    private HeroSaveData _currentHeroSaveData;

    private UserProfileData _currentProfileData;

    private CEntityBasicDataSO _currentHeroDataSO;

    // 프로퍼티

    public CEntityBasicDataSO CurrentHeroDataSO => _currentHeroDataSO;

    public bool IsCombatScene => _isCombatScene;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;

            DontDestroyOnLoad(gameObject);

        }

        else
        {
            Destroy(gameObject);
        }

    }

    private void OnEnable()
    {
        for(int i = 0; i < _skillSlots.Length; i++)
        {
            _skillSlots[i].OnPopupSkillSlotClicked += SetCombatSkills;
        }

        if(_currentEntity != null)
        {
            _currentEntity.ChangedHP += RefreshHp;
            _currentEntity.ChangedStress += RefreshStress;
        }
    }


    private void OnDisable()
    {
        for (int i = 0; i < _skillSlots.Length; i++)
        {
            _skillSlots[i].OnPopupSkillSlotClicked -= SetCombatSkills;
        }

        if (_currentEntity != null)
        {
            _currentEntity.ChangedHP -= RefreshHp;
            _currentEntity.ChangedStress -= RefreshStress;
        }
    }

    public void OpenPopupinTheCrossRoad(List<CEntityBasicDataSO> list, int startIndex)
    {
        _isTheCrossRoadScene = true;

        if (_currentEntity != null)
        {
            _currentEntity.ChangedHP -= RefreshHp;
            _currentEntity.ChangedStress -= RefreshStress;
        }

        _currentEntity = null;
        _currentHeroIndex = startIndex;
        _heroes = list;
        HeroDataUpdate(list[_currentHeroIndex]);
    }

    /// <summary>
    /// 특정 영웅의 정보 불러오기
    /// </summary>
    /// <param name="data"></param>
    public void HeroDataUpdate(CEntityBasicDataSO data)
    {
        if(data == null)
        {
            return;
        }

        _currentHeroSaveData = null;

        _currentHeroDataSO = data;

        // 기본으로 출력할 탭은 전투 기술 탭
        SelectTab(PopupTab.CombatSkills);

        if(_modelingViewer != null)
        {
            _modelingViewer.ShowModeling(data.HeroModel);
        }

        _jopTitle.text = data.JobTitle;
        _heroName.text = data.Name;
        _heroName2.text = data.Name;
        _bleedingValue.text = data.BleedRes.ToString();
        _poisoningValue.text = data.BlightRes.ToString();
        _burnValue.text = data.BurnRes.ToString();
        _stunValue.text = data.StunRes.ToString();
        _moveValue.text = data.MoveRes.ToString();
        _weakeningValue.text = data.DebuffRes.ToString();
        _diseaseValue.text = data.DiseaseRes.ToString();
        _deathblowValue.text = data.DeathblowRes.ToString();
        _stressValue.text = data.CurrentStress.ToString();
        _heroHP.text = data.MaxHP.ToString();
        _speed.text = data.Speed.ToString();

        // 세이브 파일 불러오고
        if(DataSaveManager.Instance != null)
        {
            _currentProfileData = DataSaveManager.Instance.LoadProfile(DataSaveManager.Instance.CurrentSlotIndex);
        }

        if(_currentProfileData == null)
        {
            _currentProfileData = new UserProfileData();
        }

        // 불러온 정보의 영웅과 내부 파티원 리스트에서 동일한 영웅 데이터를 가진 데이터를 찾아서 현재 팝업창에 켜둔 영웅 데이터와 연결한다
        if(_currentProfileData != null && _currentProfileData.PartyHeroes != null)
        {
            for (int i = 0; i < _currentProfileData.PartyHeroes.Count; i++)
            {
                if(_currentProfileData.PartyHeroes[i].HeroID == data.HeroID)
                {
                    _currentHeroSaveData = _currentProfileData.PartyHeroes[i];
                    break;
                }
            }
        }

        // 스킬 탭 새로고침
        RefreshSkillUI();
    }

    /// <summary>
    /// 교차로 씬이 아닌 곳에서 영웅 팝업UI를 켤경우
    /// </summary>
    /// <param name="data"></param>
    public void OpenPopupExceptTheCrossRoad(List<CEntityRuntimeData> list, int startIndex)
    {
        _isTheCrossRoadScene = false;

        if (_currentEntity != null)
        {
            _currentEntity.ChangedHP -= RefreshHp;
            _currentEntity.ChangedStress -= RefreshStress;
        }

        _currentHeroIndex = startIndex;
        _combatHeroes = list;
        CombatHeroDataUpdate(list[_currentHeroIndex]);

    }

    public void CombatHeroDataUpdate(CEntityRuntimeData data)
    {
        if (data == null)
        {
            return;
        }

        _currentHeroSaveData = null;
        _currentHeroDataSO = data.HeroBasicData;

        _currentEntity = data;

        // 기존 영웅 구독 해제하고
        if (_currentEntity != null)
        {
            _currentEntity.ChangedHP -= RefreshHp;
            _currentEntity.ChangedStress -= RefreshStress;
        }

        // 영웅 데이터 갱신
        _currentEntity = data;

        // 데이터 갱신 됐으면 구독
        if (_currentEntity != null)
        {
            _currentEntity.ChangedHP += RefreshHp;
            _currentEntity.ChangedStress += RefreshStress;
        }

        // 기본으로 출력할 탭은 전투 기술 탭
        SelectTab(PopupTab.CombatSkills);

        // 모델링 갱신
        if (_modelingViewer != null)
        {
            _modelingViewer.ShowModeling(data.HeroBasicData.HeroModel);
        }

        _bleedingValue.text = data.GetStat(EEntityStat.BleedRes).ToString();
        _poisoningValue.text = data.GetStat(EEntityStat.BlightRes).ToString();
        _burnValue.text = data.GetStat(EEntityStat.BurnRes).ToString();
        _stunValue.text = data.GetStat(EEntityStat.StunRes).ToString();
        _moveValue.text = data.GetStat(EEntityStat.MoveRes).ToString();
        _weakeningValue.text = data.GetStat(EEntityStat.DebuffRes).ToString();
        _diseaseValue.text = data.GetStat(EEntityStat.DiseaseRes).ToString();
        _deathblowValue.text = data.GetStat(EEntityStat.DeathblowRes).ToString();
        _stressValue.text = $"{data.GetStat(EEntityStat.Stress)} / {data.GetStat(EEntityStat.MaxStress)}";
        _heroHP.text = $"{data.GetStat(EEntityStat.HP)} / {data.GetStat(EEntityStat.MaxHP)}";
        _speed.text = data.GetStat(EEntityStat.Speed).ToString();

        RefreshSkillUI();

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
        if(!_isTheCrossRoadScene)
        {
            _currentHeroIndex = (_currentHeroIndex + 1) % _combatHeroes.Count;
            CombatHeroDataUpdate(_combatHeroes[_currentHeroIndex]);
        }

        else
        {
            _currentHeroIndex = (_currentHeroIndex + 1) % _heroes.Count;
            HeroDataUpdate(_heroes[_currentHeroIndex]);
        }

    }

    public void OnClickpreviousHero()
    {
        if(!_isTheCrossRoadScene)
        {
            _currentHeroIndex = (_currentHeroIndex - 1 + _combatHeroes.Count) % _combatHeroes.Count;
            CombatHeroDataUpdate(_combatHeroes[_currentHeroIndex]);
        }

        else
        {            
            _currentHeroIndex = (_currentHeroIndex - 1 + _heroes.Count) % _heroes.Count;
            HeroDataUpdate(_heroes[_currentHeroIndex]);
        }

    }

    /// <summary>
    /// 장착되어 있는 스킬을 클릭했을 경우 발동하는 함수.
    /// </summary>
    /// <param name="skillIndex"></param>
    public void SetCombatSkills(int skillIndex)
    {
        if (_currentHeroSaveData == null || _currentHeroDataSO == null)
        {
            return;
        }

        // 현재 전투 씬일 경우 해당 함수는 사용 불가
        if (_isCombatScene)
        {
            return;
        }

        // 아직 스킬 데이터 SO를 받기 전이기 떄문에 이미지 파일명 프로퍼티를 임시로 받아서 id로 넣는다.
        // TODO : 스킬 데이터 SO 받으면 SkillID로 변경해야한다.
        string unSkillID = _currentHeroDataSO.HeroSkills[skillIndex].SkillID;

        if (_currentHeroSaveData.HeroSkillIDs.Contains(unSkillID) == true)
        {
            // 기본적으로 스킬이 들어가있는 상태이기 때문에 스킬을 클릭할 경우 장착이 해제되어 장착 스킬 리스트에서 없앤다.
            _currentHeroSaveData.HeroSkillIDs.Remove(unSkillID);
        }

        // 장착되어있지 않은 보유 스킬을 누르게 되면 장착 스킬 리스트 마지막에 넣는다.
        else if (_currentHeroSaveData.HeroSkillIDs.Contains(unSkillID) == false && _currentHeroSaveData.HeroSkillIDs.Count < 5)
        {
            _currentHeroSaveData.HeroSkillIDs.Add(unSkillID);
        }

        RefreshSkillUI();

        // 프로필 내부의 파티원 목록 리스트 순회하여 현재 영웅과 일치하는 고유 ID 찾기
        if (_currentProfileData != null && _currentProfileData.PartyHeroes != null)
        {
            for (int i = 0; i < _currentProfileData.PartyHeroes.Count; i++)
            {
                if (_currentProfileData.PartyHeroes[i].HeroID == _currentHeroSaveData.HeroID)
                {
                    _currentProfileData.PartyHeroes[i] = _currentHeroSaveData;
                    break;
                }
            }
        }

        // 매니저가 존재하고, 세이브 데이터가 존재할 때 디스크에 덮어쓸 수 있게 널참조 오류 방지
        if(DataSaveManager.Instance != null && _currentHeroSaveData != null)
        {
            DataSaveManager.Instance.SaveProfile(_currentProfileData, DataSaveManager.Instance.CurrentSlotIndex);
        }


    }

    /// <summary>
    /// 스킬 슬롯의 하이라이트를 켜고 끄는 함수
    /// </summary>
    public void RefreshSkillUI()
    {
        // 팝업창 모든 기술 슬롯 순회
        for (int i = 0; i < _skillSlots.Length;  i++)
        {
            Sprite skillSprite = _currentHeroDataSO.HeroSkills[i].SkillIcon;
            _skillSlots[i].SetSlot(skillSprite);

            // 현재 세이브 데이터에 기술이 들어갔는지 검사하고
            string skillID = _currentHeroDataSO.HeroSkills[i].SkillID;
            bool isEquipped = _currentHeroSaveData != null && _currentHeroSaveData.HeroSkillIDs != null && _currentHeroSaveData.HeroSkillIDs.Contains(skillID);

            // 장착 여부에 따라 하이라이트 켜거나 끄기
            if (_skillSlots[i].FrameHL != null)
            {
                _skillSlots[i].FrameHL.SetActive(isEquipped);
            }

        }
      
    }

    public void RefreshHp(float currentHP, float maxHP)
    {
        _heroHP.text = $"{currentHP.ToString()} / {maxHP.ToString()}";
    }

    public void RefreshStress(float currentStress, float maxStress)
    {
        _stressValue.text = $"{currentStress.ToString()} / {maxStress.ToString()}";
    }


}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.Image;
using static UserProfileData;

public class TheCrossroadManager : MonoBehaviour
{
    public static TheCrossroadManager instance;

    // 출정 완료 알림 → 주행 쪽(교차로 노드 처리기)이 받아서 교차로 씬을 닫고 다음 길로 간다 / 경태
    public static event System.Action OnDeparted;

    #region 인스펙터
    [Header("파티 슬롯")]
    [SerializeField] private PartySlot_UI[] _partySlots;

    [Header("영웅 로스터")]
    [SerializeField] private HeroRoster_UI[] _heroRoster;

    [Header("영웅 데이터 SO 참조")]
    [SerializeField] private CEntityBasicDataSO[] _heroDatas;

    [Header("영웅 정보 UI 참조")]
    [SerializeField] private HeroStatus_UI _heroStatus;

    [Header("하이라이트 리셋용 스크립트 참조")]
    [SerializeField] private ClickHandler _clickHandler;

    [Header("영웅 Status 캔버스 그룹 참조")]
    [SerializeField] private CanvasGroup _statusCanvas;

    [Header("영웅 팝업 UI 캔버스 그룹 참조")]
    [SerializeField] private CanvasGroup _heroPopupCanvas;

    [Header("영웅 팝업창 스킬 슬롯 참조")]
    [SerializeField] private PopupSkillSlot_UI[] skillSlots;

    [Header("파티 출정 버튼 캔버스그룹")]
    [SerializeField] private CanvasGroup _departureButton;

    [Header("파티 출정 버튼 홀딩 스크립트")]
    [SerializeField] private HoldButton _holdButton;

    [Header("모델링 뷰어 참조")]
    [SerializeField] private ModelingViewer_UI _modelingViewer;



    #endregion

    // 내부 변수
    private bool _onPopup;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        // 구독
        for (int i = 0; i < _partySlots.Length; i++)
        {
            _partySlots[i].HeroDropped += DropHerotoParty;
            _partySlots[i].PartySlotRightClicked += HeroPortraitRightClicked;
            _partySlots[i].PartySlotLeftClicked += PartySlotClicked;
            _partySlots[i].PartySlotDoubleClicked += PartySlotDoubleClicked;
        }

        for(int i = 0; i < _heroRoster.Length; i++)
        {
            _heroRoster[i].HeroRosterRightClicked += HeroPortraitRightClicked;
            _heroRoster[i].HeroRosterLeftClicked += HeroRosterClicked;
            _heroRoster[i].HeroRosterDoubleClicked += HeroRosterDoubleClicked;
        }

        _clickHandler.ClickEmptySpace += HighlightReset;

        _holdButton.HoldingFullGauge += OnFullHoldingDepartureButton;

        // 팝업창 시작할 때는 꺼놓기
        if (_heroPopupCanvas != null)
        {
            _onPopup = false;
            _heroPopupCanvas.alpha = 0f;
            _heroPopupCanvas.blocksRaycasts = false;
            _heroPopupCanvas.interactable = false;
        }

        // 영웅 데이터들 먼저 생성시키고
        if(DataSaveManager.Instance != null)
        {
            InitializeHeroSaveData();
        }

        // 기본 시작할 때 영웅 정보 띄워놓기
        if (_heroRoster[3].HeroData != null)
        {
            CEntityBasicDataSO data = _heroRoster[3].HeroData;
            _heroStatus.UpdateStatus(data);
            HeroPopup_UI.instance.HeroDataUpdate(data);
        }

        // 출정버튼 꺼놓기
        if(_departureButton != null)
        {
            _departureButton.alpha = 0f;
            _departureButton.interactable = false;
            _departureButton.blocksRaycasts = false;
        }

        _heroDatas = new CEntityBasicDataSO[_partySlots.Length];
    }

    private void Update()
    {
        // esc로도 팝업창 닫을 수 있게
        if(Input.GetKeyDown(KeyCode.Escape) && _onPopup == true)
        {
            ExitPopup();
        }
    }

    /// <summary>
    /// 이벤트 구독 해제
    /// </summary>
    private void OnDisable()
    {
        for (int i = 0; i < _partySlots.Length; i++)
        {
            _partySlots[i].HeroDropped -= DropHerotoParty;
            _partySlots[i].PartySlotRightClicked -= HeroPortraitRightClicked;
            _partySlots[i].PartySlotLeftClicked -= PartySlotClicked;
            _partySlots[i].PartySlotDoubleClicked -= PartySlotDoubleClicked;
        }

        for (int i = 0; i < _heroRoster.Length; i++)
        {
            _heroRoster[i].HeroRosterRightClicked -= HeroPortraitRightClicked;
            _heroRoster[i].HeroRosterLeftClicked -= HeroRosterClicked;
            _heroRoster[i].HeroRosterDoubleClicked -= HeroRosterDoubleClicked;
        }

        _clickHandler.ClickEmptySpace -= HighlightReset;

        _holdButton.HoldingFullGauge -= OnFullHoldingDepartureButton;

        _modelingViewer.OffModelCamera();

    }

    /// <summary>
    /// 초상화를 드래그 앤 드롭으로 파티 슬롯에 놓았을 경우 
    /// </summary>
    /// <param name="partySlotIndex"></param>
    /// <param name="data"></param>
    private void DropHerotoParty(int partySlotIndex, CEntityBasicDataSO data)
    {
        // 기존 슬롯에 들어있는 데이터를 일단 변수로 저장 데이터 없으면 null
        CEntityBasicDataSO inSlotData = _heroDatas[partySlotIndex];

        // 파티 데이터를 동기화한다.
        _heroDatas[partySlotIndex] = data;

        if(inSlotData != null && inSlotData != data)
        {
            for(int i = 0; i < _heroRoster.Length; i++)
            {
                if (_heroRoster[i].HeroData == inSlotData)
                {
                    _heroRoster[i].inParty = false;
                    _heroRoster[i].RosterStateUpdate();
                }
            }
        }

        for(int i = 0; i < _partySlots.Length; i++)
        {
            // 드랍했는데, 중복된 데이터가 다른 슬롯에 있다면 다른 슬롯의 데이터 삭제
            if (_partySlots[i].HeroData == data && i != partySlotIndex)
            {
                _partySlots[i].SlotClear();
                _heroDatas[i] = null;
            }

        }
        
        // 영웅 로스터 확인해서 파티에 들어왔다면 색 변경
        for(int j = 0;  j < _heroRoster.Length; j++)
        {
            if(_heroRoster[j].HeroData == data)
            {
                // 파티에 들어온 상태로 바꾸고
                _heroRoster[j].inParty = true;

                // 로스터 초상화의 색을 바꾼다.
                _heroRoster[j].RosterStateUpdate();
            }
        }

        ActiveDepartureButton();
    }

    /// <summary>
    /// 파티 슬롯을 더블클릭 했을 경우
    /// </summary>
    /// <param name="data"></param>
    private void PartySlotDoubleClicked(CEntityBasicDataSO data)
    {
        if(data != null)
        {
            for(int i = 0; i < _heroDatas.Length; i++)
            {
                if (_partySlots[i].HeroData == data)
                {
                    _partySlots[i].SlotClear();
                    _heroDatas[i] = null;
                }
            }

            ActiveDepartureButton();

        }

        // 영웅 로스터 확인해서 파티에 빠졌다면 색 변경
        for (int j = 0; j < _heroRoster.Length; j++)
        {
            if (_heroRoster[j].HeroData == data)
            {
                // 파티에 들어온 상태로 바꾸고
                _heroRoster[j].inParty = false;

                // 로스터 초상화의 색을 바꾼다.
                _heroRoster[j].RosterStateUpdate();

            }
        }

    }

    /// <summary>
    /// 영웅 로스터의 영웅 초상화를 더블클릭 했을 경우
    /// </summary>
    /// <param name="data"></param>
    private void HeroRosterDoubleClicked(CEntityBasicDataSO data)
    {
        int targetindex = -1;

        for(int i = 0; i < _partySlots.Length; i++)
        {
            if (_partySlots[i].HeroData == data)
            {
                targetindex = i;
                break;
            }
        
        }

        // 파티에 이미 참여한 경우
        if (targetindex != -1)
        {
            for (int i = 0; i < _heroRoster.Length; i++)
            {
                if (_heroRoster[i].HeroData == data)
                {
                    _heroRoster[i].inParty = false;
                    _heroRoster[i].RosterStateUpdate();
                }
            }

            _partySlots[targetindex].SlotClear();
        }

        else
        {
            for(int i = 0; i < _partySlots.Length; i++)
            {
                if (_partySlots[i].HeroData == null)
                {
                    _partySlots[i].SlotDataAdd(data);
                    _heroDatas[i] = data;

                    for(int j = 0; j < _heroRoster.Length; j++)
                    {
                        if (_heroRoster[j].HeroData == data)
                        {
                            _heroRoster[j].inParty = true;
                            _heroRoster[j].RosterStateUpdate();
                            break;
                        }

                        
                    }

                    break;
                }

            }
        }

        ActiveDepartureButton();

    }

    /// <summary>
    /// 영웅 초상화 우클릭했을 경우 팝업 ui 켜기
    /// </summary>
    /// <param name="data"></param>
    private void HeroPortraitRightClicked(CEntityBasicDataSO data)
    {
        HeroPopup_UI.instance.HeroDataUpdate(data);

        List<CEntityBasicDataSO> heroes = new List<CEntityBasicDataSO>();

        for (int i = 0; i < _heroRoster.Length; i++)
        {
            if(_heroRoster[i].HeroData != null)
            {
                heroes.Add(_heroRoster[i].HeroData);
            }
        }

        int currentIndex = heroes.IndexOf(data);

        HeroPopup_UI.instance.OpenPopupinTheCrossRoad(heroes, currentIndex);

        if (_heroPopupCanvas != null)
        {
            _onPopup = true;
            _heroPopupCanvas.alpha = 1f;
            _heroPopupCanvas.blocksRaycasts = true;
            _heroPopupCanvas.interactable = true;
        }
    }
    
    /// <summary>
    /// 파티슬롯 단일 클릭시 하이라이트 켜고 끄기
    /// </summary>
    /// <param name="slotIndex"></param>
    private void PartySlotClicked(int slotIndex, CEntityBasicDataSO data)
    {
        for(int i = 0; i < _partySlots.Length; i++)
        {
            if (i == slotIndex)
            {
                _partySlots[i].SlotHighlight(true);
            }

            else
            {
                _partySlots[i].SlotHighlight(false);
            }
        }

        if(data != null)
        {
            HeroInfoUpdate(data);
        }
    }

    /// <summary>
    /// 영웅 로스터 슬롯 단일 클릭시 하이라이트 켜고 끄기
    /// </summary>
    /// <param name="data"></param>
    private void HeroRosterClicked(int slotIndex, CEntityBasicDataSO data)
    {
        HeroInfoUpdate(data);

        for (int i = 0; i < _heroRoster.Length; i++)
        {
            if (i == slotIndex)
            {
                _heroRoster[i].SlotHighlight(true);
            }

            else
            {
                _heroRoster[i].SlotHighlight(false);
            }
        }
    }

    /// <summary>
    /// 초상화 클릭하면 화면 정보 업데이트하는 함수
    /// </summary>
    /// <param name="data"></param>
    public void HeroInfoUpdate(CEntityBasicDataSO data)
    {
        if (data != null)
        {
            _heroStatus.UpdateStatus(data);
            HeroPopup_UI.instance.HeroDataUpdate(data);
        }
    }



    /// <summary>
    /// 출정버튼이 활성화 되는 함수
    /// </summary>
    private void ActiveDepartureButton()
    {
        
        bool _fullParty = System.Array.TrueForAll(_partySlots, slot => slot.HeroData != null);

        if ( _fullParty )
        {
            _departureButton.alpha = 1f;
            _departureButton.interactable = true;
            _departureButton.blocksRaycasts = true;
        }

        else
        {
            _departureButton.alpha = 0f;
            _departureButton.interactable = false;
            _departureButton.blocksRaycasts = false;
        }



    }

    /// <summary>
    /// 출정버튼을 풀홀딩해서 출정을 시작했을 때
    /// </summary>
    private void OnFullHoldingDepartureButton()
    {
        // 현재 슬롯 번호 가져오고
        int currentSlot = DataSaveManager.Instance.CurrentSlotIndex;

        // 타이틀에서 만든 프로필 불러오기
        UserProfileData currentRunData = DataSaveManager.Instance.LoadProfile(currentSlot);

        if ( currentRunData == null )
        {
            CPrint.Error("프로필 데이터를 찾을 수 없습니다.");
            return;
        }

        // 파티 순서 저장하는 방식
        // 1. 일단 파티에 들어간 모든 슬롯의 파티번호를 -1로 초기화한다.
        for(int i = 0; i < currentRunData.PartyHeroes.Count; i++)
        {
            currentRunData.PartyHeroes[i].PartySlotIndex = -1;
        }

        for(int i = 0; i < 4;  i++)
        {
            // 각 영웅의 런타임 데이터 생성
            HeroSaveData targetHero = null;

            // 원본 SO 데이터 참조
            CEntityBasicDataSO originData = _partySlots[i].HeroData;

            for(int j = 0; j < currentRunData.PartyHeroes.Count; j++)
            {
                if (currentRunData.PartyHeroes[j].HeroID == originData.HeroID)
                {
                    targetHero = currentRunData.PartyHeroes[j];
                }
            }

            if (targetHero != null)
            {
                // 변화하는 수치, 고정된 수치의 초기값 불러오기
                targetHero.HeroID = originData.HeroID;
                targetHero.CurrentHP = originData.MaxHP;
                targetHero.CurrentStress = 0;
                targetHero.PartySlotIndex = i;

            }
        }
        
        // 영웅 4명의 데이터 프로필 세이브 데이터에 저장
        DataSaveManager.Instance.SaveProfile(currentRunData, currentSlot);

        CPrint.Log("파티 결정 및 데이터 저장 완료");

        // 역마차 씬 복귀는 교차로 노드 처리기가 맡는다 → 출정만 알린다 / 경태
        OnDeparted?.Invoke();


    }

    /// <summary>
    /// 기본 영웅 4명의 데이터를 생성하여 저장하는 함수
    /// </summary>
    private void InitializeHeroSaveData()
    {

        if (_heroRoster == null || _heroRoster.Length == 0) return;

        // 현재 프로필 슬롯 번호 가져오고
        int currentSlot = DataSaveManager.Instance.CurrentSlotIndex;

        // 프로필 불러오고
        UserProfileData currentRunData = DataSaveManager.Instance.LoadProfile(currentSlot);

        if(currentRunData == null)
        {
            currentRunData = new UserProfileData();
        }

        if (currentRunData.PartyHeroes == null)
        {
            currentRunData.PartyHeroes = new List<HeroSaveData>();
        }

        for (int i = 0; i < _heroRoster.Length; i++)
        {

            // 원본 SO 데이터 참조
            CEntityBasicDataSO originData = _heroRoster[i].HeroData;

            if(originData == null)
            {
                continue;
            }

            bool isExist = false;

            foreach(var heroSave in currentRunData.PartyHeroes)
            {
                if (heroSave.HeroID == originData.HeroID)
                {
                    // 영웅을 찾았는데, 스킬을 들고 있지 않을경우
                    if(heroSave.HeroSkillIDs == null || heroSave.HeroSkillIDs.Count == 0)
                    {
                        heroSave.HeroSkillIDs = new List<string>();

                        for (int j = 0; j < originData.HeroSkills.Count; j++)
                        {
                            heroSave.HeroSkillIDs.Add(originData.HeroSkills[j].SkillID);
                        }
                    }

                    isExist = true;
                    break;
                }
            }


            if(!isExist)
            {
                // 영웅 데이터 처음 생성
                UserProfileData.HeroSaveData newHeroData = new UserProfileData.HeroSaveData();
                
                // 파티 슬롯 번호, 아이디, 스킬 데이터 넣기
                newHeroData.PartySlotIndex = -1;
                newHeroData.HeroID = originData.HeroID;
                newHeroData.HeroSkillIDs = new List<string>();

                // 반복문 돌려서 스킬 리스트에 넣기
                for(int j = 0; j < originData.HeroSkills.Count; j++)
                {
                    newHeroData.HeroSkillIDs.Add(originData.HeroSkills[j].SkillID);
                }

                currentRunData.PartyHeroes.Add(newHeroData);

            }

        }
        // 영웅 4명의 데이터 프로필 세이브 데이터에 저장
        DataSaveManager.Instance.SaveProfile(currentRunData, currentSlot);

    }


    /// <summary>
    /// 팝업 창 닫고 닫기 전 마지막으로 띄워놓은 영웅 데이터로 리셋
    /// </summary>
    public void ExitPopup()
    {
        if (_heroPopupCanvas != null)
        {
            _onPopup = false;
            _heroPopupCanvas.alpha = 0f;
            _heroPopupCanvas.blocksRaycasts = false;
            _heroPopupCanvas.interactable = false;
        }

        CEntityBasicDataSO currentHero = HeroPopup_UI.instance.CurrentHeroDataSO;

        if(currentHero != null)
        {
            _heroStatus.UpdateStatus(currentHero);
        }
    }

    /// <summary>
    /// 빈 공간 클릭하면 기존 하이라이트 모두 꺼지게하는 함수
    /// </summary>
    public void HighlightReset()
    {
        for(int i = 0; i < _heroRoster.Length; i++)
        {
            _heroRoster[i].SlotHighlight(false);
        }

        for(int j = 0; j < _partySlots.Length; j++)
        {
            _partySlots[j].SlotHighlight(false);
        }
    }

}

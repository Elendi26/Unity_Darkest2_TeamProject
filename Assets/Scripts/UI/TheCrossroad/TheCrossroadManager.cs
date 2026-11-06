using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TheCrossroadManager : MonoBehaviour
{
    public TheCrossroadManager instance;

    #region 인스펙터
    [Header("파티 슬롯")]
    [SerializeField] private PartySlot_UI[] _partySlots;

    [Header("영웅 로스터")]
    [SerializeField] private HeroRoster_UI[] _heroRoster;

    [Header("영웅 데이터 SO 참조")]
    [SerializeField] private HeroDataSO[] _heroDatas;

    [Header("영웅 정보 UI 참조")]
    [SerializeField] private HeroStatus_UI _heroStatus;

    [Header("하이라이트 리셋용 스크립트 참조")]
    [SerializeField] private ClickHandler _clickHandler;

    [Header("영웅 Status 캔버스 그룹 참조")]
    [SerializeField] private CanvasGroup _statusCanvas;

    [Header("영웅 팝업 UI 캔버스 그룹 참조")]
    [SerializeField] private CanvasGroup _heroPopupCanvas;

    [Header("파티 출정 버튼")]
    [SerializeField] private CanvasGroup _departureButton;



    #endregion

    // 내부 변수
    private bool _onPopup;
   

    void Start()
    {
        // 구독
        for(int i = 0;  i < _partySlots.Length; i++)
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

        // 팝업창 시작할 때는 꺼놓기
        if (_heroPopupCanvas != null)
        {
            _onPopup = false;
            _heroPopupCanvas.alpha = 0f;
            _heroPopupCanvas.blocksRaycasts = false;
            _heroPopupCanvas.interactable = false;
        }

        // 기본 시작할 때 영웅 정보 띄워놓기
        if (_heroRoster[3].HeroData != null)
        {
            HeroDataSO data = _heroRoster[3].HeroData;
            _heroStatus.UpdateStatus(data);
            HeroPopup_UI.instance.HeroDataUpdate(data);
        }

        if(_departureButton != null)
        {
            _departureButton.alpha = 0f;
            _departureButton.interactable = false;
            _departureButton.blocksRaycasts = false;
        }

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
    }

    /// <summary>
    /// 초상화를 드래그 앤 드롭으로 파티 슬롯에 놓았을 경우 
    /// </summary>
    /// <param name="partySlotIndex"></param>
    /// <param name="data"></param>
    private void DropHerotoParty(int partySlotIndex, HeroDataSO data)
    {
        // 기존 슬롯에 들어있는 데이터를 일단 변수로 저장 데이터 없으면 null
        HeroDataSO inSlotData = _heroDatas[partySlotIndex];

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
    private void PartySlotDoubleClicked(HeroDataSO data)
    {
        if(data != null)
        {
            for(int i = 0; i < _heroDatas.Length; i++)
            {
                if (_partySlots[i].HeroData == data)
                {
                    _partySlots[i].SlotClear();

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
    private void HeroRosterDoubleClicked(HeroDataSO data)
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
    private void HeroPortraitRightClicked(HeroDataSO data)
    {
        HeroPopup_UI.instance.HeroDataUpdate(data);

        List<HeroDataSO> heroes = new List<HeroDataSO>();

        for (int i = 0; i < _heroRoster.Length; i++)
        {
            if(_heroRoster[i].HeroData != null)
            {
                heroes.Add(_heroRoster[i].HeroData);
            }
        }

        int currentIndex = heroes.IndexOf(data);

        HeroPopup_UI.instance.OpenPopup(heroes, currentIndex);

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
    private void PartySlotClicked(int slotIndex)
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
    }

    /// <summary>
    /// 영웅 로스터 슬롯 단일 클릭시 하이라이트 켜고 끄기
    /// </summary>
    /// <param name="data"></param>
    private void HeroRosterClicked(int slotIndex, HeroDataSO data)
    {
        if(data != null)
        {
            _heroStatus.UpdateStatus(data);
            HeroPopup_UI.instance.HeroDataUpdate(data);
        }

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
    /// 팝업 창 닫기
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

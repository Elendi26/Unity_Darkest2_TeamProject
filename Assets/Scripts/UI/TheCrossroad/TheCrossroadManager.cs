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

    [Header("영웅 Status 캔버스 그룹 참조")]
    [SerializeField] private CanvasGroup _statusCanvas;

    [Header("영웅 팝업 UI 캔버스 그룹 참조")]
    [SerializeField] private CanvasGroup _heroPopupCanvas;



    #endregion

    void Start()
    {
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

        if(_heroPopupCanvas != null)
        {
            _heroPopupCanvas.alpha = 0f;
            _heroPopupCanvas.blocksRaycasts = false;
            _heroPopupCanvas.interactable = false;
        }

    }

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
    }

    /// <summary>
    /// 초상화를 드래그 앤 드롭으로 파티 슬롯에 놓았을 경우 
    /// </summary>
    /// <param name="partySlotIndex"></param>
    /// <param name="data"></param>
    private void DropHerotoParty(int partySlotIndex, HeroDataSO data)
    {
        for(int i = 0; i < _partySlots.Length; i++)
        {
            // 드랍했는데, 중복된 데이터가 다른 슬롯에 있다면 다른 슬롯의 데이터 삭제
            if (_partySlots[i].HeroData == data && i != partySlotIndex)
            {
                _partySlots[i].SlotClear();
            }

        }
        
        // 영웅 로스터 확인해서 파티에 들어왔다면 색 변경
        for(int j = 0;  j < _heroRoster.Length; j++)
        {
            if(_heroRoster[j].HeroData == data)
            {
                // 파티에 들어온 상태로 바꾸고
                _heroRoster[j].inParty = true;

                // 로스터 초상화의 색을 바꾸고
                _heroRoster[j].RosterStateUpdate();

                // 파티 데이터를 동기화한다.
                _heroDatas[partySlotIndex] = data;
            }
        }
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
                if (_heroDatas[i] == data)
                {
                    _heroDatas[i] = null;
                }
            }
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


    }

    /// <summary>
    /// 영웅 초상화 우클릭했을 경우 팝업 ui 켜기
    /// </summary>
    /// <param name="data"></param>
    private void HeroPortraitRightClicked(HeroDataSO data)
    {
        HeroPopup_UI.instance.HeroDataUpdate(data);

        if (_heroPopupCanvas != null)
        {
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
    private void HeroRosterClicked(HeroDataSO data)
    {
        if(data != null)
        {
            _heroStatus.UpdateStatus(data);
        }
    }

}

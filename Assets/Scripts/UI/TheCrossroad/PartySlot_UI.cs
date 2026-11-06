using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using UnityEngine.UI;

public class PartySlot_UI : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    #region 인스펙터
    [Header("슬롯에 있는 영웅의 데이터 참조 SO")]
    [SerializeField] private HeroDataSO _heroData;

    [Header("슬롯의 번호")]
    [SerializeField] private int _slotIndex;

    [Header("초상화 띄울 이미지")]
    [SerializeField] private GameObject _portrait;

    [Header("빈슬롯 기본 이미지")]
    [SerializeField] private GameObject _image;

    [Header("클릭시 하이라이트")]
    [SerializeField] private GameObject _slotHL;

    [Header("참조할 매니저")]
    [SerializeField] private TheCrossroadManager _manager;

    #endregion

    // 이벤트 (몇번 슬롯에 어떤 영웅 데이터인지)
    public event Action<int, HeroDataSO> HeroDropped;

    public event Action<HeroDataSO> PartySlotRightClicked;
    public event Action<int> PartySlotLeftClicked;
    public event Action<HeroDataSO> PartySlotDoubleClicked;

    // 내부 변수
    private float _lastClick = -999f;
    private float _doubleClickTime = 0.5f;
    private bool inParty = false;

    // 프로퍼티
    public HeroDataSO HeroData => _heroData;

    private void Start()
    {
        _portrait.SetActive(false);
        _image.SetActive(true);
        _slotHL.SetActive(false);
    }

    public void OnDrop(PointerEventData eventData)
    {
        /*
        1. 드래그 드롭 했을 때, 해당 슬롯에 드롭한 영웅의 데이터를 대입한다 (슬롯이 이미 찼든 안찼든)
        2. 만약 다른 슬롯에 같은 영웅의 데이터가 있었다면 해당 슬롯의 데이터는 비우고 지금 슬롯의 데이터만 남긴다.
        3. 매니저에게 몇번슬롯에 무슨 영웅이 배치되었는지 보고하고, 매니저가 파티 데이터를 갱신하다록 한다.
        */

        // 드래그 중인 데이터가 있는지 없는지 먼저 확인하고
        if(eventData.pointerDrag != null)
        {
            // 스크립트 접근해서 데이터 불러오기
             var data = eventData.pointerDrag.GetComponent<DragDropHandler>();

            if (data != null)
            {
                _heroData = data.HeroData;
                _image.SetActive(false);
                _portrait.SetActive(true);
                _portrait.GetComponent<Image>().sprite = _heroData.HeroPortrait;
                HeroDropped?.Invoke(_slotIndex, _heroData);
            }
        }
       
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        /*
        1. 우클릭 했을 경우 / 슬롯에 영웅 데이터가 있으면 -> 영웅 데이터를 기반으로 팝업을 띄운다.
        2. 좌클릭했을 경우
        2 - 1 한 번 클릭 시 : 매니저에 클릭 이벤트를 전달한다. 매니저에서 할 것 : 모든 슬롯의 하이라이트를 끄고, 클릭된 슬롯만 하이라이트를 켠다.
        2 - 2 더블 클릭 시 : 더블 클릭 시간 측정하여 정해진 시간 안에 클릭이 한번 더 감지될 경우 더블 클릭으로 감지하여 해당 슬롯에 영웅의 데이터가 존재하면, 파티 슬롯에서 영웅 데이터를
        없애고 비운다.
        */

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (_heroData != null)
            {
                CPrint.Log("영웅의 초상화 우클릭 감지");
                PartySlotRightClicked?.Invoke(_heroData);
            }
        }

        else if (eventData.button == PointerEventData.InputButton.Left)
        {

            float clickInterval = Time.time - _lastClick;

            // 더블 클릭 검사
            if (clickInterval <= _doubleClickTime && _lastClick >= 0)
            {

                CPrint.Log("더블클릭 감지");
                _lastClick = -999f;
                if(_heroData != null)
                {
                    // 무슨 영웅의 데이터가 있는 상태에서 더블클릭 되었는지 이벤트를 보내고
                    PartySlotDoubleClicked?.Invoke(_heroData);

                    // 파티에서 해체된다.
                    SlotClear();
                }

            }

            else if (clickInterval > _doubleClickTime)
            {
                PartySlotLeftClicked?.Invoke(_slotIndex);
                
                _lastClick = Time.time;
            }
        }
    }


    /// <summary>
    /// 슬롯과 상관없이 로스터 더블클릭으로 슬롯이 채워지는 경우 호출할 함수
    /// </summary>
    /// <param name="data"></param>
    public void SlotDataAdd(HeroDataSO data)
    {
        // 데이터 넣고
        _heroData = data;

        // 이미지 켜고
        _image.SetActive(false);
        _portrait.SetActive(true);

        // 이미지에 데이터 초상화 넣고
        _portrait.GetComponent<Image>().sprite = data.HeroPortrait;
    }

    public void SlotClear()
    {
        // 슬롯의 영웅 데이터 지우고 이미지 없애기
        inParty = false;
        _heroData = null;
        _image.SetActive(true);
        _portrait.SetActive(false);

    }

    public void SlotHighlight(bool isClicked)
    {
        // 하이라이트 활성화 비활성화 작업
        if(_slotHL != null)
        {
            _slotHL.SetActive(isClicked);
        }
    }
}

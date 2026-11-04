using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HeroRoster_UI : MonoBehaviour, IPointerClickHandler
{
    #region 인스펙터
    [Header("영웅 데이터 SO 참조")]
    [SerializeField] private HeroDataSO _heroData;

    [Header("영웅 초상화가 들어갈 슬롯 위치 이미지")]
    [SerializeField] private GameObject _portrait;

    [Header("영웅 미선택용 회색 초상화 이미지")]
    [SerializeField] private GameObject _grayPortrait;

    [Header("좌클릭시 테두리 하이라이트")]
    [SerializeField] private GameObject _frameHL;

    [Header("매니저 참조")]
    [SerializeField] private TheCrossroadManager _manager;


    #endregion

    // 이벤트

    public event Action<HeroDataSO> HeroRosterRightClicked;
    public event Action<HeroDataSO> HeroRosterLeftClicked;
    public event Action<HeroDataSO> HeroRosterDoubleClicked;


    // 내부 변수

    private float _lastClick = -999f;
    private float _doubleClickTime = 0.5f;
    public bool inParty = false;

    // 프로퍼티
    public HeroDataSO HeroData => _heroData;
    public GameObject Portrait => _portrait;
   


    void Start()
    {
        // 시작할 때는 기본적으로 회색 초상화 가져와서, 로스터용 초상화 이미지에 넣는다.
        if(_portrait != null)
        {
            _portrait.SetActive(false);
        }

        if( _grayPortrait != null)
        {
            _grayPortrait.SetActive(true);
        }

    }

    public void OnPointerClick(PointerEventData eventData)
    {
        /*
        1. 우클릭인지 좌클릭인지 검사하기
        2. 우클릭이면 해당 영웅의 데이터가 들어간 팝업창을 킨다. (매니저가 켤 수 있도록 이벤트만 보낸다)
        3. 좌클릭은 더블클릭 또는 드래그앤 드롭만 받는데, 여기서는 더블 클릭을 인식할 수 있는 장치를 만든다.
        4. 더블 클릭했다면 이미 파티 구성원인지 확인하고, 파티 구성원이면 파티 해제, 파티 구성원이 아니라면 가장 오른쪽 빈슬롯부터 채운다.
        5. 마지막으로 클릭한 시간 초기화.
        */

        // 우클릭했다면
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            CPrint.Log("영웅의 초상화 우클릭 감지");
            HeroRosterRightClicked?.Invoke(_heroData);
        }

        else if (eventData.button == PointerEventData.InputButton.Left)
        {

            float clickInterval = Time.time - _lastClick;

            // 더블 클릭 검사
            if (clickInterval <= _doubleClickTime && _lastClick >= 0)
            {
                CPrint.Log("더블클릭 감지");
                HeroRosterDoubleClicked?.Invoke(_heroData);
                _lastClick = -999f;

                if(inParty == false)
                {
                    RosterStateUpdate();
                }

                else
                {
                    RosterStateUpdate();
                }
                
            }

            else if(clickInterval > _doubleClickTime)
            {
                HeroRosterLeftClicked?.Invoke(_heroData);
                _lastClick = Time.time;
            }
        }

    }

    /// <summary>
    /// 파티에 들어가있는지 판정 후, 회색 or 원본 초상화를 슬롯 이미지에 띄우는 함수
    /// </summary>
    public void RosterStateUpdate()
    {
        if(!inParty)
        {
            if (_portrait != null)
            {
                _portrait.SetActive(false);
            }

            if (_grayPortrait != null)
            {
                _grayPortrait.SetActive(true);
            }
        }

        else
        {
            if (_portrait != null)
            {
                _portrait.SetActive(true);
            }

            if (_grayPortrait != null)
            {
                _grayPortrait.SetActive(false);
            }
        }
    }
}

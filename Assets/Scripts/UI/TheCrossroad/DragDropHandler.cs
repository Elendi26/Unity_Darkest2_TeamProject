using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DragDropHandler : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    #region 인스펙터
    [Header("포인터를 따라나닐 이미지")]
    [SerializeField] private RectTransform _pointerImage;

    // 캔버스 그룹을 받아 blockRaycasts를 꺼야 따라오는 이미지 뒤에있는 UI에 상호작용이 가능하다.
    [Header("임시 이미지 캔버스그룹")]
    [SerializeField] private CanvasGroup _ImageCanvas;

    [Header("캔버스 스케일 보정")]
    [SerializeField] private Canvas _scaleFactor;

    [Header("영웅 데이터 SO 참조")]
    [SerializeField] private HeroDataSO _heroData;

    [Header("로스터 UI 참조")]
    [SerializeField] private HeroRoster_UI _heroRoster;


    #endregion

    // 프로퍼티 
    public HeroDataSO HeroData => _heroData;

    public void OnBeginDrag(PointerEventData eventData)
    {
        /*
        1. HeroRoster_UI에서 영웅의 초상화 이미지와 데이터를 받아와서 넣고
        2. 블록레이케스트를 false로 끈다.
        */

        var roster = GetComponent<HeroRoster_UI>();

        _heroData = roster.HeroData;

        _pointerImage.GetComponent<Image>().sprite = roster.HeroData.HeroPortrait;

        _pointerImage.gameObject.SetActive(true);

        _ImageCanvas.blocksRaycasts = false;

    }

    public void OnEndDrag(PointerEventData eventData) 
    {
        /*
        1. 마우스 따라다니던 이미지 끄고
        2. 블록레이케스트 다시 키고
        3. 이미지랑 데이터 초기화
        */

        _pointerImage.gameObject.SetActive(false);

        _ImageCanvas.blocksRaycasts = true;

        _heroData = null;



    }

    public void OnDrag(PointerEventData eventData)
    {
        /*
        1. 마우스 포인터의 스크린 좌표를 캔버스 기준 로컬 좌표로 변환하고
        2. 계산된 좌표를 따라다니는 이미지의 위치값으로 업데이트하여 이미지가 따라다니게 한다.
        */

        _pointerImage.position = eventData.position;

    }
}

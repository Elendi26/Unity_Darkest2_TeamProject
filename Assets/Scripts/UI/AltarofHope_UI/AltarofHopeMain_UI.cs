using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;


public class AltarofHopeMain_UI : MonoBehaviour, IPointerClickHandler
{
    // 메뉴 4개를 한번에 정리하기 위한 구조체
    [System.Serializable]
    public struct TargetMenuView
    {        
        public GameObject targetObject;
        public AltarMenu targetMenu;
    }

    #region 인스펙터

    [Header("구조체 빌딩 4개 연결")]
    [SerializeField] private TargetMenuView[] _menuObjects;

    #endregion

    // 내부 변수
    Dictionary<GameObject, AltarMenu> _targetDict = new Dictionary<GameObject, AltarMenu>();

    // 이벤트

    public event Action<AltarMenu> TargetClicked;


    void Awake()
    {
        for (int i = 0; i < _menuObjects.Length; i++)
        {
            _targetDict.Add(_menuObjects[i].targetObject, _menuObjects[i].targetMenu);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(_targetDict.TryGetValue(eventData.pointerCurrentRaycast.gameObject, out AltarMenu menu))
        {
            TargetClicked?.Invoke(menu);
        }
    }

}

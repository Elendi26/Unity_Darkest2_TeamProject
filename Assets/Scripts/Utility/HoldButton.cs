using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    #region 인스펙터
    [Header("홀드 시간")]
    [SerializeField] private float _maxHoldTime = 2f;

    [Header("게이지 이미지")]
    [SerializeField] private Image _gauge;

    #endregion

    // 내부 변수

    private float _currentHoldTime = 0f;

    private bool _isHolding;

    // 이벤트

    public event Action HoldingFullGauge;

     void Start()
     {
        _gauge.fillAmount = 0;
     }

    void Update()
    {
        if (_isHolding)
        {
            _currentHoldTime += Time.deltaTime;
            _gauge.fillAmount = _currentHoldTime / _maxHoldTime;

            if (_currentHoldTime >= _maxHoldTime)
            {
                _isHolding = false;
                HoldingFullGauge?.Invoke();
            }
        }
    }


    public void OnPointerUp(PointerEventData eventData)
    {
        _isHolding = false;
        _gauge.fillAmount = 0;
        _currentHoldTime = 0f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isHolding = true;
        _currentHoldTime = 0f;

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHolding = false;
        _gauge.fillAmount = 0;
        _currentHoldTime = 0f;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TorchManager : MonoBehaviour
{
    // 싱글톤
    public static TorchManager Instance { get; private set; }

    #region 인스펙터
    [SerializeField] private int _currentTorchValue;

    [SerializeField] private List<TorchData_SO> _torchData = new List<TorchData_SO>();


    #endregion

    // 프로퍼티
    public int CurrentTorchValue => _currentTorchValue;


    // 이벤트

    // 촛불 수치 변화시 전달할 이벤트
    // 전투아이템 사용 / 전투에 따라 촛불의 수치가 감소하거나 증가할 수 있기 때문에
    // public event Action<int> OnTorchValueChanged;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }

        else
        {
            Destroy(gameObject);
        }
    }


    void Start()
    {
        // 테스트로 일단 시작시 무작위 수치 부여
        // 역마차 시스템 촛불 연동시 해당 구역 삭제
        SetTorchValue(UnityEngine.Random.Range(0, 101));


        //TODO : 역마차 시스템 도입시 촛불 수치 관련 함수 호출



    }

    /// <summary>
    /// 토치의 불꽃 수치 일정 범위 안으로 보간
    /// </summary>
    /// <param name="value"></param>
    public void SetTorchValue(int value)
    {
        _currentTorchValue = Math.Clamp(value, 0, 100);

        // 수치 변화 기능 (전투아이템, 전투 등으로 변화하는 함수)이 들어온다면 변화를 알리는 기능을 살려야한다.
        // OnTorchValueChanged?.Invoke(_currentTorchValue);
    }

    /// <summary>
    /// 부여된 촛불 수치를 가지고 어떤 데이터를 불러올건지 SO파일을 검사하여 데이터를 불러온다
    /// </summary>
    /// <returns></returns>
    public TorchData_SO GetCurrentValue()
    {
        foreach(TorchData_SO data in _torchData)
        {
            if(_currentTorchValue >= data.MinTorchValue && _currentTorchValue <= data.MaxTorchValue)
            {
                return data;
            }
        }

        return null;


    }

}

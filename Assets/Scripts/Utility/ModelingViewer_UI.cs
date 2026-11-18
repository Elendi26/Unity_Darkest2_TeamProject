using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelingViewer_UI : MonoBehaviour
{
    #region 인스턴스

    [Header("3D 모델 생성 위치")]
    [SerializeField] private Transform _modelSpawnPoint;

    [Header("모델을 출력할 출력용 카메라")]
    [SerializeField] private Camera _modelCamera;

    #endregion

    // 내부 변수

    private GameObject _currentModel;

    void Awake()
    {
        if (_modelCamera != null)
        {
            _modelCamera.enabled = false;
        }

        DontDestroyOnLoad(gameObject);
    }
    
    public void ShowModeling(GameObject modeling)
    {
        if(modeling == null)
        {
            return;
        }

        // 기존의 모델 존재하면 파괴 후 비우기
        if (_currentModel != null)
        {
            Destroy(_currentModel);
            _currentModel = null;
        }

        // 새로운 모델 생성한 후, 캐싱하기
        _currentModel = Instantiate(modeling, _modelSpawnPoint);

        // 위치, 로테이션 값 초기화
        _currentModel.transform.localPosition = Vector3.zero;
        _currentModel.transform.localRotation = Quaternion.Euler(0, 180f, 0);

        foreach (Transform child in _currentModel.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = _modelSpawnPoint.gameObject.layer;
        }

        // 카메라 렌더링 켜기 
        if (_modelCamera != null)
        {
            _modelCamera.enabled = true;
        }

    }

    /// <summary>
    /// 팝업창이 꺼질 때, 카메라도 함께 사용을 중단해야 하므로 호출할 함수
    /// </summary>
    public void OffModelCamera()
    {
        if (_modelCamera != null)
        {
            _modelCamera.enabled = false;
        }
    }
}

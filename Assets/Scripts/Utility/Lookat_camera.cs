using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lookat_camera : MonoBehaviour
{
    #region 인스펙터
    [Header("메인 카메라")]
    [SerializeField] private Camera _mainCam;


    #endregion
    void Start()
    {
        if(_mainCam == null )
        {
            _mainCam = Camera.main;
        }
    }

    void LateUpdate()
    {
        if( _mainCam != null)
        {
            transform.rotation = _mainCam.transform.rotation;
        }
    }
}

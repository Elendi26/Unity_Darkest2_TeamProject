using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Streetlight : MonoBehaviour
{
    #region 인스펙터

    [Header("가로등 프리팹")]
    [SerializeField] private GameObject _streetlight;

    [Header("가로등 스폰 위치")]
    [SerializeField] private Transform _spawnPoint;

    [Header("가로등 전용 카메라")]
    [SerializeField] private Camera _camera;

    [Header("실제로 띄울 로우이미지")]
    [SerializeField] private RawImage _rawImage;

    #endregion

    void Start()
    {
        _camera.enabled = true;

        GameObject lamp = Instantiate(_streetlight, _spawnPoint);

        _streetlight.transform.localRotation = Quaternion.Euler(0, 200f, 2.2f);
    }

}

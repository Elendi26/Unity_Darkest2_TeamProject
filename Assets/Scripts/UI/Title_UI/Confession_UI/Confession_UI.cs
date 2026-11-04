using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Confession_UI : MonoBehaviour
{
    #region 인스펙터
    [Header("고해 기본 버튼")]
    [SerializeField] private GameObject _cofessionBT;

    [Header("고해 계속하기 버튼")]
    [SerializeField] private GameObject _continueBt;

    [Header("새 고해 버튼")]
    [SerializeField] private GameObject _newConfBt;

    [Header("뒤로가기 버튼")]
    [SerializeField] private GameObject _backBt;


    #endregion

    // 내부 변수

    // 현재 선택된 프로필 데이터 불러올 변수
    private UserProfileData _currentProfile;

    // 이벤트

    // 생성된 프로필 없이 고해 버튼을 눌러 게임에 입장하려고 할 때, 프로필 선택창을 띄워서 프로필을 만들도록 유도해야한다. 따라서 이벤트를 보내야한다.
    public event Action NullProfileOnPopup;

    private void Start()
    {
        if(_continueBt != null && _newConfBt != null && _backBt != null)
        {
            _continueBt.SetActive(false);
            _newConfBt.SetActive(false);
            _backBt.SetActive(false);
        }
    }

    public void MenuUpdate(UserProfileData userProfileData)
    {
        // 전달받은 프로필 저장
        _currentProfile = userProfileData;
    }

    public void OnConfessionButtonClick()
    {
        // 프로필이 없다면 이벤트 전달
        if (_currentProfile == null)
        {
            if (NullProfileOnPopup != null)
            {
                NullProfileOnPopup?.Invoke();
            }

            return;
        }

        // 여기로 넘어왓다면 프로필이 있는 것, 하지만 세이브파일이 있는지 한번 더 검사해야한다.
        bool haveSaveData = CheckSaveFileExist(_currentProfile);

        _cofessionBT.SetActive(false);  

        // 세이브파일이 있다면
        if (haveSaveData)
        {
            _continueBt.SetActive(true);
            _newConfBt.SetActive(true);
            _backBt.SetActive(true);
        }

        else
        {
            _newConfBt.SetActive(true);
            _backBt.SetActive(true);
        }

    }

    public void OnClickBackButton()
    {
        _cofessionBT.SetActive(true);
        _continueBt.SetActive(false);
        _newConfBt.SetActive(false);
        _backBt.SetActive(false);
    }

    public bool CheckSaveFileExist(UserProfileData data)
    {
        //TODO : 나중에 기본값인지 아닌지 검사하는 방식으로 채울 것.

        // 기본은 현재 데이터 없음
        return false;
    }
    
}

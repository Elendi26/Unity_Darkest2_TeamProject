using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

public class ProfileSlot_UI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    #region 인스펙터

    [Header("프로필 이름")]
    [SerializeField] private TextMeshProUGUI _profileName;

    [Header("프로필 닉네임 게임오브젝트")]
    [SerializeField] private GameObject _pfNicName;
    
    [Header("프로필 이름 수정 필드")]
    [SerializeField] private TMP_InputField _modifierField;

    [Header("프로필 이름 수정 게임 오브젝트")]
    [SerializeField] private GameObject _mfField;



    [Header("선택 활성화 프레임 ")]
    [SerializeField] private GameObject _onSelectedFrame;

    [Header("선택 활성화 체크 이미지")]
    [SerializeField] private GameObject _onSelectedCheck;

    [Header("프로필 존재시 배경")]
    [SerializeField] private GameObject _isProfileBG;

    [Header("마우스 감지 시 프레임")]
    [SerializeField] private GameObject _onPointerFrame;

    [Header("프로필 이름 수정 / 프로필 삭제 버튼")]
    [SerializeField] private GameObject _nameModifyButton;
    [SerializeField] private GameObject _profileDelete;

    

    #endregion

    // 내부 변수

    // 부모 팝업 스크립트 참조용
    private ProfileSelectPopup_UI _onoffRef;

    // 이 스크립트를 들고있는 슬롯에 저장된 데이터
    private UserProfileData _profileData;

    // 이 스크립트를 들고 있는 슬롯의 번호
    private int _slotIndex;

    // 이벤트

    // 현재 슬롯 번호와 함께 이름 수정 버튼이 눌렸다는 이벤트를 보내기 위함
    public event Action<int, string> ClickedModifier;

    // 현재 슬롯 번호와 함께 프로필 삭제 버튼이 눌렸다는 이벤트를 보내기 위함
    public event Action<int> ClickedDelete;

    private void Awake()
    {
        // 인풋필드의 종료 이벤트 감지 함수 등록
        _modifierField.onEndEdit.AddListener(ModifierEnd);
        _modifierField.characterLimit = 15;

    }


    private void Start()
    {
        if(_onSelectedFrame == null || _onSelectedCheck == null || _isProfileBG == null || _onPointerFrame == null || _nameModifyButton == null || _profileDelete == null)
        {
            CPrint.Warn("프로필UI 인스펙터가 비어있다. 인스펙터 확인");
        }

        if (_onSelectedFrame != null && _onSelectedCheck != null && _isProfileBG != null && _onPointerFrame != null && _nameModifyButton != null && _profileDelete != null)
        {
            _onPointerFrame.SetActive(false);
            _onSelectedFrame.SetActive(false);
            _onSelectedCheck.SetActive(false);
            _isProfileBG.SetActive(false);
            _nameModifyButton.SetActive(false);
            _profileDelete.SetActive(false);
        }

        if(_mfField != null)
        {
            _mfField.SetActive(false);
        }
    }

    /// <summary>
    /// 닉네임 수정버튼 눌렀을 때
    /// </summary>
    public void ClickedModifierButton()
    {
        _pfNicName.SetActive(false);
        _mfField.SetActive(true);

        _modifierField.text = _profileName.text;

        _modifierField.ActivateInputField();
    }

    /// <summary>
    /// 수정을 마치고 엔터나 다른 공간을 클릭했을 때
    /// </summary>
    /// <param name="inputName"></param>
    private void ModifierEnd(string inputName)
    {
        CPrint.Log("닉네임 변경 버튼 클릭");
        if (string.IsNullOrEmpty(_modifierField.text))
        {
            _modifierField.text = "Darkest";
        }

        string modifierName = _modifierField.text;

        _profileName.text = modifierName;

        _pfNicName.SetActive(true);
        _mfField.SetActive(false);

        ClickedModifier?.Invoke(_slotIndex, modifierName);
    }

    public void ClickedDeleteButton()
    {
        CPrint.Log("데이터 삭제 버튼 클릭");
        ClickedDelete?.Invoke(_slotIndex);
    }

    public void InputSlotData(ProfileSelectPopup_UI refer, UserProfileData data, int slotIndex)
    {
        _onoffRef = refer;
        _profileData = data;
        _slotIndex = slotIndex;

        // 데이터가 있는지 없는지 검사 후, 슬롯에 출력해야 한다.
        if(_profileData != null)
        {
            _profileName.text = _profileData.ProfileName;
            _isProfileBG.SetActive(true);
            _nameModifyButton.SetActive(true);
            _profileDelete.SetActive(true);
        }

        else
        {
            _profileName.text = "신규 생성";
            _isProfileBG.SetActive(false);
            _nameModifyButton.SetActive(false);
            _profileDelete.SetActive(false);
        }

        // 선택 안된 상태로 돌리기
        SetSelectedState(false);

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _onPointerFrame.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _onPointerFrame.SetActive(false);
    }

    /// <summary>
    /// 클릭된 것을 부모 팝업에게 보고한다.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerClick(PointerEventData eventData)
    {
        if(_onoffRef != null)
        {
            _onoffRef.OnSlotSelected(this);
        }
    }

    /// <summary>
    /// 선택된 슬롯은 필요한 UI를 켠다.
    /// </summary>
    /// <param name="isSelected"></param>
    public void SetSelectedState(bool isSelected)
    {
        _onSelectedCheck.SetActive(isSelected);
        _onSelectedFrame.SetActive(isSelected);
 
    }

    /// <summary>
    /// 비어있는 슬롯인지 아닌지 확인하는 함수
    /// </summary>
    /// <returns></returns>
    public bool IsEmptySlot()
    {
        return _profileData == null;
    }

    /// <summary>
    /// 슬롯의 번호가 몇번인지 알려주는 함수
    /// </summary>
    /// <returns></returns>
    public int GetSlotIndex() 
    {
        return _slotIndex;
    } 

}

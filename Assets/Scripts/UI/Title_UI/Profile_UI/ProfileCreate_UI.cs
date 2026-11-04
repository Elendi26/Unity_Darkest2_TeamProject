using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ProfileCreate_UI : MonoBehaviour
{
	#region 인스펙터
	[Header("팝업 캔버스 그룹")]
	[SerializeField] private CanvasGroup _createUI;

	[Header("플레이어 닉네임 입력 창")]
	[SerializeField] private TMP_InputField _userInput;

	[Header("확인/취소 버튼")]
	[SerializeField] private Button _createButton;
	[SerializeField] private Button _cancelButton;

	[Header("프로필 선택창 참조")]
	[SerializeField] private ProfileSelectPopup_UI _profileSP;
    #endregion

    // 내부 변수

    // 유저가 선택한 슬롯의 번호
    private int _selectSlotIndex;

	// 이벤트
	// 창이 닫혔다는 이벤트를 선언한다.
	public event Action _closeCreatePU;

    private void Awake()
    {
		_userInput.characterLimit = 15;
    }
    private void Start()
    {
		CloseCreateUI();
    }

    /// <summary>
    /// 신규 생성 슬롯을 눌렀을 때, 뜨게 하는 팝업창을 킬 함수
    /// </summary>
    /// <param name="selectSlotIndex"></param>
    public void OpenCreateUI(int selectSlotIndex)
	{
		// 슬롯 번호 넣고
		_selectSlotIndex = selectSlotIndex;

		// 입력칸 기본적으로 Darkest로 세팅
		if(_userInput != null)
		{
			_userInput.text = "Darkest";
		}

		// 팝업창 켜기
        _createUI.alpha = 1f;
        _createUI.interactable = true;
        _createUI.blocksRaycasts = true;
    }

	/// <summary>
	/// 신규 생성 팝업에서 확인버튼을 눌렀을 때 실행되는 함수
	/// </summary>
	public void OnClickCreateButton()
	{
		// 아무것도 적지 않고 확인버튼 눌렀다면 닉네임 Darkest로 고정
		if(string.IsNullOrEmpty(_userInput.text))
		{
			_userInput.text = "Darkest";
		}

		UserProfileData data = new UserProfileData();

		data.ProfileName = _userInput.text;

		// 세이브파일 만들 함수 호출
		DataSaveManager.Instance.SaveProfile(data, _selectSlotIndex);

		// 슬롯 리프레쉬
        _profileSP.RefreshSlots();

        // 팝업창 닫기
        CloseCreateUI();


    }

	/// <summary>
	/// 취소 버튼 눌렀을 때 실행되는 함수
	/// </summary>
	public void OnClickCancelButton()
	{
		CloseCreateUI();
        _closeCreatePU.Invoke();
    }

	/// <summary>
	/// 팝업창 닫는 함수
	/// </summary>
    public void CloseCreateUI()
    {
        _createUI.alpha = 0f;
        _createUI.interactable = false;
        _createUI.blocksRaycasts = false;
    }

}

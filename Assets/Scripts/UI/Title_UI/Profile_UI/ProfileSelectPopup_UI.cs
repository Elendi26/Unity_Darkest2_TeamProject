using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProfileSelectPopup_UI : MonoBehaviour
{
    #region 인스펙터
    [Header("클릭 이벤트 구독 참조")]
    [SerializeField] private Profile_popup _profileChangeButton;

    [Header("프로필 생성 팝업 참조")]
    [SerializeField] private ProfileCreate_UI _profileCreateUI;

    [Header("프로필 선택 캔버스 그룹")]
    [SerializeField] private CanvasGroup _profileCanvas;

    [Header("프로필 슬롯")]
    [SerializeField] private ProfileSlot_UI[] _slots = new ProfileSlot_UI[5];

    #endregion

    // 이벤트

    // 몇번 슬롯이 선택되었는지 보내는 이벤트
    public event Action<int> OnValidProfileSelect;

    // 취소 버튼이 눌렸다면 보내는 이벤트
    public event Action OnClickedBackButton;

    private void Start()
    {
        // 클릭하면 이벤트 구독
        if(_profileChangeButton != null)
        {
            _profileChangeButton.OnProfileButtonClicked += OpenUI;
        }

        if(_profileCanvas  != null)
        {
            ProfileUIOnOff(false);
        }
    }

    /// <summary>
    /// 클릭했다는 이벤트를 받았으니 팝업창 On
    /// </summary>
    private void OpenUI()
    {
        ProfileUIOnOff(true);
    }

    public void OnClickBackButton()
    {
        OnClickedBackButton?.Invoke();
        ProfileUIOnOff(false);
    }
 

    public void ProfileUIOnOff(bool onClick)
    {
        _profileCanvas.alpha = onClick ? 1f : 0f;
        _profileCanvas.interactable = onClick;
        _profileCanvas.blocksRaycasts = onClick;

        if(onClick)
        {
            RefreshSlots();
        }
    }

    public void RefreshSlots()
    {
        
        for (int i = 0; i < _slots.Length; i++)
        {
            // TODO : 세이브 시스템 완성시, 몇번째에 세이브 데이터가 들어가있는 지 확인하는 작업

            // 지금은 세이브 파일이 없으므로 null처리
            UserProfileData data1 = DataSaveManager.Instance.LoadProfile(i);

            // InputSlotData 함수도 호출
            _slots[i].InputSlotData(this, data1, i);
        }
        
    }


    /// <summary>
    /// 클릭된 슬롯만 하이라이트 켜기
    /// </summary>
    /// <param name="Clickedslot"></param>
    public void OnSlotSelected(ProfileSlot_UI Clickedslot)
    {
        // 일단 슬롯 모두 꺼놓고
        foreach (var slot in _slots)
        {
            slot.SetSelectedState(false);
        }

        // 클릭된 슬롯만 킨다.
        Clickedslot.SetSelectedState(true);

        // 슬롯 번호 가져오고
        int index = Clickedslot.GetSlotIndex();

        // 슬롯이 비어있다면
        if (Clickedslot.IsEmptySlot() == true)
        {
            
            // 창 열고
            _profileCreateUI.OpenCreateUI(index);
        }

        // 비어있지 않다면
        else
        {
            DataSaveManager.Instance.SetCurrentSlot(index);
            OnValidProfileSelect.Invoke(index);
        }
    }




    /// <summary>
    /// 창이 닫힐 때 구독도 해제
    /// </summary>
    private void OnDestroy()
    {
        if(_profileChangeButton != null)
        {
            _profileChangeButton.OnProfileButtonClicked -= OpenUI;
        }
    }

    

}

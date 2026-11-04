using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TitleUIManager : MonoBehaviour
{
    #region 인스펙터
    [Header("프로필 선택 팝업창")]
    [SerializeField] private ProfileSelectPopup_UI _profileSP;

    [Header("고해 선택 UI")]
    [SerializeField] private Confession_UI _confessionUI;

    [Header("고해 버튼 캔버스그룹")]
    [SerializeField] private CanvasGroup _confessionBT;

    [Header("프로필 생성 창 참조")]
    [SerializeField] private ProfileCreate_UI _profileCUI;

    [Header("슬롯 참조")]
    [SerializeField] private ProfileSlot_UI[] _slots;

    #endregion

    void Start()
    {
        if(_confessionUI != null)
        {
            _confessionUI.NullProfileOnPopup += OpenProfileSelectPopup;
        }

        if(_profileCUI != null)
        {
            _profileCUI._closeCreatePU += CloseCreatePopup;
            
        }
        
        if(_profileSP != null)
        {
            _profileSP.OnValidProfileSelect += ApplyProfile;
            _profileSP.OnClickedBackButton += CloseCreatePopup;

        }

        if(_slots != null)
        {
            foreach(var slot in _slots)
            {
                slot.ClickedModifier += ModifierNickName;
                slot.ClickedDelete += DeleteProfile;
            }

        }

        SetStartData();


    }

    /// <summary>
    /// 프로필이 생성되지 않은 상태에서 고해 버튼을 눌러 시작할 경우 켜지는 함수 
    /// </summary>
    private void OpenProfileSelectPopup()
    {
        _profileSP.ProfileUIOnOff(true);
    }

    private void CloseCreatePopup()
    {
        _profileCUI.CloseCreateUI();
    }

    private void ApplyProfile(int slotIndex)
    {
        // 디스크에서 데이터 가져오기
        UserProfileData data = DataSaveManager.Instance.LoadProfile(slotIndex);

        // 고해 버튼 데이터 갱신
        _confessionUI.MenuUpdate(data);

        // 화면 끄고
        _profileSP.ProfileUIOnOff(false);
    }

    private void ModifierNickName(int slotIndex, string nickName)
    {
        // 기존 슬롯의 데이터 불러오고 
        UserProfileData data = DataSaveManager.Instance.LoadProfile(slotIndex);

        // 프로필 이름 갱신하고
        data.ProfileName = nickName;

        // 갱신된 이름 저장하고
        DataSaveManager.Instance.SaveProfile(data, slotIndex);

        // 고해 UI 에도 데이터 전달
        if(slotIndex == DataSaveManager.Instance.CurrentSlotIndex)
        {
            _confessionUI.MenuUpdate(data);
        }


    }

    private void DeleteProfile(int slotIndex)
    {
        // 디스크에 저장된 데이터 삭제하고
        DataSaveManager.Instance.DeleteSaveData(slotIndex);

        // 슬롯의 데이터를 null로 변경하고
        _slots[slotIndex].InputSlotData(_profileSP, null, slotIndex);

        // 선택되어있던 슬롯이라면 초기화 후 고해 UI에도 전달
        if (slotIndex == DataSaveManager.Instance.CurrentSlotIndex)
        {
            DataSaveManager.Instance.CurrentSlotIndex = -1;

            _confessionUI.MenuUpdate(null);
        }
        
    }

    /// <summary>
    /// 게임을 시작할 때, 프로필이 있는지 확인하고 UI 동기화 시키기.
    /// </summary>
    private void SetStartData()
    {
        if(DataSaveManager.Instance != null)
        {
            int lastSlot = DataSaveManager.Instance.CurrentSlotIndex;

            // 데이터가 존재한다면
            if(lastSlot != -1)
            {
                // 데이터 불러오고
                UserProfileData data = DataSaveManager.Instance.LoadProfile(lastSlot);

                // 고해 UI 동기화
                _confessionUI.MenuUpdate(data);
            }

            else
            {
                _confessionUI.MenuUpdate(null);
            }


        }
    }

    /// <summary>
    /// 게임 종료 함수
    /// </summary>
    public void OnClickGameQuit()
    {
        Application.Quit();
    }

    private void OnDestroy()
    {
        if (_confessionUI != null)
        {
            _confessionUI.NullProfileOnPopup -= OpenProfileSelectPopup;
        }

        if (_profileCUI != null)
        {
            _profileCUI._closeCreatePU -= CloseCreatePopup;

        }

        if (_profileSP != null)
        {
            _profileSP.OnValidProfileSelect -= ApplyProfile;
            _profileSP.OnClickedBackButton -= CloseCreatePopup;

        }

        if (_slots != null)
        {
            foreach (var slot in _slots)
            {
                slot.ClickedModifier -= ModifierNickName;
                slot.ClickedDelete -= DeleteProfile;
            }

        }

    }
}

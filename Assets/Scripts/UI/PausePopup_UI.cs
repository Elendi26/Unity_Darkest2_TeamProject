using Newtonsoft.Json.Bson;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class PausePopup_UI : MonoBehaviour
{
    // 인스펙터
    [Header("퍼즈 전용 캔버스그룹")]
    [SerializeField] private CanvasGroup _pauseCanvasG;


    // 내부변수
    bool _paused = false;

    void Start()
    {
        HideUI();
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape) && _paused == false)
        {
            Time.timeScale = 0;
            ShowUI();
        }

        else if(_paused == true &&  Input.GetKeyDown(KeyCode.Escape) && Time.timeScale == 0)
        {
            Time.timeScale = 1;
            HideUI();
        }
    }

    public void ClickResumeGame()
    {
        Time.timeScale = 1;
        HideUI();
    }

    public void ClickGiveupButton()
    {
        //TODO : 씬전환하여 원정포기 후, 정산으로 넘어가기
    }

    public void ClickQuitGame()
    {
        Application.Quit();
    }

    /// <summary>
    /// 현재 진행상황을 저장하는 함수
    /// </summary>
    public void SaveCurrentProgress()
    {
        // 현재 씬 이름을 저장하고
        string currentScene = SceneManager.GetActiveScene().name;

        // 타이틀 씬이나 교차로 씬에서는 데이터 저장하지 않는다. + TODO : 로딩씬 등 저장 필요없는 씬
        if (currentScene == "Title" || currentScene == "TheCrossRoad")
        {
            return;
        }

        if (DataSaveManager.Instance != null)
        {
            if(DataSaveManager.Instance.CurrentSlotIndex >= 0)
            {
                UserProfileData data = DataSaveManager.Instance.LoadProfile(DataSaveManager.Instance.CurrentSlotIndex);
            }

            else
            {
                CPrint.Warn("세이브된 프로필이 없다.");
                return;
            }
        }

        // TODO : 영웅데이터 CEntityRuntimeData 에서 각 HeroID가 일치하는 영웅들의 세이브 데이터를 찾아서 스탯 등 수치를 꺼내서 현재 변화된 상황에 대한 데이터를 덮어씌운다. (체력, 스트레스, 스킬 등)
    }


    public void ShowUI()
    {
        _paused = true;

        SaveCurrentProgress();


        if (_pauseCanvasG != null)
        {
            _pauseCanvasG.alpha = 1f;
            _pauseCanvasG.interactable = true;
            _pauseCanvasG.blocksRaycasts = true;
        }
    }

    public void HideUI()
    {
        _paused = false;

        if (_pauseCanvasG != null)
        {
            _pauseCanvasG.alpha = 0f;
            _pauseCanvasG.interactable = false;
            _pauseCanvasG.blocksRaycasts = false;
        }
    }
}

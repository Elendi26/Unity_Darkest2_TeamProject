using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public class DataSaveManager : MonoBehaviour
{
    public static DataSaveManager Instance { get; private set; }

    // 내부 변수
    // 게임 시작할 때 자동으로 마지막으로 플레이한 프로필을 저장하기 위한 변수
    private int _currentSlotIndex = -1;

    // 프로퍼티
    public int CurrentSlotIndex { get; set; } = -1;

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

        DontDestroyOnLoad(this.gameObject);
    }

    /// <summary>
    /// 현재 활성화 된 슬롯 저장 함수
    /// </summary>
    /// <param name="slotIndex"></param>
    public void SetCurrentSlot(int slotIndex)
    {
        CurrentSlotIndex = slotIndex;
    }

    /// <summary>
    /// 번호표 받으면, 파일 이름과 지정 경로에 세이브 파일을 만들어서 문자열로 반환한다.
    /// </summary>
    /// <param name="slotIndex"></param>
    /// <returns></returns>
    private string SaveDataPath(int slotIndex)
    {
        // 파일 이름 정하고
        string fileName = $"Profile_{slotIndex}.json";

        // 경로 정하고 반환
        string pathName = Path.Combine(Application.persistentDataPath, fileName);
        return pathName;
    }

    /// <summary>
    /// 세이브 파일이 존재하는지 여부를 파악하고 있다면 true, 없다면 false로 반환한다.
    /// </summary>
    /// <param name="slotIndex"></param>
    /// <returns></returns>
    public bool HaveSaveData(int slotIndex)
    {
        return System.IO.File.Exists(SaveDataPath(slotIndex));
    }

    /// <summary>
    /// 프로파일을 최종적으로 생성하는 함수.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="slotIndex"></param>
    public void SaveProfile(UserProfileData data, int slotIndex)
    {
        // 저장할 경로 확보하고
        string savePath = SaveDataPath(slotIndex);

        
        // 기존 포장하는 방식
        // string json = JsonUtility.ToJson(data, true);
        

        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            // 자동 줄바꿈 및 띄어쓰기
            // TODO : 최종 빌드때는 용량을 줄이기 위해 None으로 변경해야한다.
            Formatting = Formatting.Indented,
            // 빈 데이터는 저장을 무시하여 용량 세이브하기
            NullValueHandling = NullValueHandling.Ignore,
            // 상속 받은 클래스의 원래 타입을 기억하기
            TypeNameHandling = TypeNameHandling.Auto

        };

        // 새로운 포장 방식
        string json = JsonConvert.SerializeObject(data, settings);
        // 디스크에 저장하고
        System.IO.File.WriteAllText(savePath, json);
        // 가장 최신 슬롯으로 저장
        SetCurrentSlot(slotIndex);

    }

    /// <summary>
    /// 데이터 불러오는 함수 
    /// </summary>
    /// <param name="slotIndex"></param>
    /// <returns></returns>
    public UserProfileData LoadProfile(int slotIndex)
    {
        // 데이터가 있는지 확인하고
        if(HaveSaveData(slotIndex) == false)
        {
            return null;
        }


        // 데이터 읽어올 경로 만들고
        string path = SaveDataPath(slotIndex);

        // 디스크에서 읽은 후
        string loadJson = System.IO.File.ReadAllText(path);

        // 불러오기 (기존 구형 방식)
        //return JsonUtility.FromJson<UserProfileData>(loadJson);

        // Newtonsoft 방식 불러오기
        JsonSerializerSettings settings = new JsonSerializerSettings
        { 
            TypeNameHandling = TypeNameHandling.Auto
        };

        return JsonConvert.DeserializeObject<UserProfileData>(loadJson, settings);

    }
    
    public void DeleteSaveData(int slotIndex)
    {
        if(slotIndex < 0 || slotIndex > 4)
        {
            return;
        }

        // 경로 생성하고
        string filePath = SaveDataPath(slotIndex);

        // 파일 삭제
        try
        {
            // 존재하는지 확인하고
            if (HaveSaveData(slotIndex) != false)
            {
                // 삭제
                System.IO.File.Delete(filePath);

                if (CurrentSlotIndex == slotIndex)
                {
                    CurrentSlotIndex = -1;
                }
            }
        }
        catch (System.Exception Error)
        {
            CPrint.Error("데이터 삭제 중 문제가 발생하였습니다.");
        }
            
    }

    public void SaveCombatState(UserProfileData.CombatSaveData combatData)
    {
        if(CurrentSlotIndex < 0)
        {
            CPrint.Warn("활성화된 프로필이 없다. 세이브 불가능");
            return;
        }

        UserProfileData profile = LoadProfile(CurrentSlotIndex);
        if (profile != null)
        {
            CPrint.Warn("프로필 데이터를 불러오지 못했다. 저장 불가");
        }

        // 전투 순간 저장 및 저장 시각 갱신하기
        profile.CurrentCombat = combatData;
        profile.LastSaveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        SaveProfile(profile, CurrentSlotIndex);

    }

    /// <summary>
    /// 전투 상황 종료 시 상황을 저장하여 고해 이어하기를 눌렀을 경우 전투로 다시 돌아가지 않도록 하는 함수.
    /// </summary>
    public void CombatStageClear()
    {
        if(_currentSlotIndex < 0)
        {
            return;
        }

        // 프로필 데이터 불러오기
        UserProfileData profileData = LoadProfile(CurrentSlotIndex);

        if(profileData == null)
        {
            return;
        }

        profileData.CurrentCombat = null;
        profileData.LastSaveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        SaveProfile(profileData, CurrentSlotIndex);

    }
    /// <summary>
    /// 이어하기 버튼 클릭시 전투 중 인지 판단하여 전투로 들어갈지 역마차로 이어갈지 판단하는 함수
    /// </summary>
    public bool InCombatJudgement(int slotIndex)
    {
        // 프로필 데이터 로드하고
        UserProfileData profileData = LoadProfile(slotIndex);

        // 밑 조건이면 전투씬으로 들어가는 것
        if(profileData != null && profileData.CurrentCombat != null && profileData.CurrentCombat.IsIncombat)
        {
            return true;
        }

        return false;

    }
}

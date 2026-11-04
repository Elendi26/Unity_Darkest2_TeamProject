using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DataSaveManager : MonoBehaviour
{
    public static DataSaveManager Instance { get; private set; }

    // 내부 변수
    // 게임 시작할 때 자동으로 마지막으로 플레이한 프로필을 저장하기 위한  변수
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
        string savepath = SaveDataPath(slotIndex);
        // 포장하고
        string json = JsonUtility.ToJson(data, true);
        // 디스크에 저장하고
        System.IO.File.WriteAllText(savepath, json);
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

        // 불러오기
        return JsonUtility.FromJson<UserProfileData>(loadJson);

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
            CPrint.Warn("데이터 삭제 중 문제가 발생하였습니다.");
        }
            
    }
}

using System.Collections.Generic;
using UnityEngine;

public class CEntityDataBase : MonoBehaviour
{
    public static CEntityDataBase Instance { get; private set; }

    // 구조: Dictionary<엔티티이름, CEntityBasicDataSO>
    private Dictionary<string, CEntityBasicDataSO> _entityDatabase = new Dictionary<string, CEntityBasicDataSO>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAndBuildEntityDatabase();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadAndBuildEntityDatabase()
    {
        // "EntityBasicDataSO" 폴더 경로 안의 모든 CEntityBasicDataSO를 로드합니다.
        CEntityBasicDataSO[] allEntities = Resources.LoadAll<CEntityBasicDataSO>("EntityBasicDataSO");

        foreach (var entityData in allEntities)
        {
            if (entityData == null) continue;

            string entityName = entityData.Name;

            // 중복 이름 체크
            if (_entityDatabase.ContainsKey(entityName))
            {
                Debug.LogWarning($"[EntityDataBase] 중복된 엔티티 이름 발견! 이름: {entityName}");
                continue;
            }

            // 딕셔너리에 이름과 엔티티 데이터 매핑
            _entityDatabase[entityName] = entityData;
        }

        Debug.Log($"[EntityDataBase] 총 {_entityDatabase.Count}개의 엔티티 기본 데이터 로드 완료.");
    }

    /// <summary>
    /// 엔티티 이름으로 기본 데이터(CEntityBasicDataSO)를 검색합니다.
    /// </summary>
    public CEntityBasicDataSO GetEntityData(string entityName)
    {
        if (_entityDatabase.TryGetValue(entityName, out var entityData))
        {
            return entityData;
        }

        Debug.LogWarning($"[EntityDataBase] 등록되지 않은 엔티티 이름입니다: '{entityName}'");
        return null;
    }
}

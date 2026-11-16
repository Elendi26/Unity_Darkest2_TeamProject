using System.Collections.Generic;
using UnityEngine;

public class CSkillDataBase : MonoBehaviour
{
    public static CSkillDataBase Instance { get; private set; }

    private Dictionary<string, Dictionary<int, CSkillDataSO>> _skillDatabase = new Dictionary<string, Dictionary<int, CSkillDataSO>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAndBuildSkillDatabase();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadAndBuildSkillDatabase()
    {
        CSkillDataSO[] allSkills = Resources.LoadAll<CSkillDataSO>("SkillDataSO");

        foreach (var skillData in allSkills)
        {
            if (skillData == null) continue;

            string heroName = skillData.HeroName;
            int skillIndex = skillData.Index;

            // 이름이 없으면 새로 만들기
            if (!_skillDatabase.ContainsKey(heroName))
            {
                _skillDatabase[heroName] = new Dictionary<int, CSkillDataSO>();
            }

            // 중복 인덱스 체크
            if (_skillDatabase[heroName].ContainsKey(skillIndex))
            {
                Debug.LogWarning($"[SkillDatabase] 중복된 스킬 인덱스 // 영웅: {heroName}, 인덱스: {skillIndex} ({skillData.HeroName})");
                continue;
            }

            // 2차 딕셔너리에 인덱스와 스킬 데이터 매핑
            _skillDatabase[heroName][skillIndex] = skillData;
        }

        Debug.Log($"[SkillDatabase] 총 {_skillDatabase.Count}명의 영웅 스킬 데이터 로드");
    }

    public CSkillDataSO GetSkillData(string heroName, int skillIndex)
    {
        // 영웅 이름으로 스킬 그룹 찾기
        if (_skillDatabase.TryGetValue(heroName, out var skillGroup))
        {
            // 인덱스로 특정 스킬 데이터 찾기
            if (skillGroup.TryGetValue(skillIndex, out var skillData))
            {
                return skillData;
            }
            else
            {
                Debug.LogWarning($"[SkillDatabase] 영웅 '{heroName}'에게 {skillIndex}번 인덱스의 스킬이 없습니다.");
            }
        }
        else
        {
            Debug.LogWarning($"[SkillDatabase] 등록되지 않은 영웅 이름입니다: '{heroName}'");
        }

        return null;
    }
}

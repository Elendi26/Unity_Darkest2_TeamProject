using System.Collections.Generic;
using UnityEngine;


public enum ESkillDesireType
{
    SelfHpWeighted,
    Random
}

/// <summary>
/// 사용 가능 스킬과 해당 스킬에 유효한 타겟 후보 목록을 함께 묶은 구조체
/// </summary>
public readonly struct UsableSkillEntity
{
    public readonly CSkillDataSO Skill;
    public readonly IReadOnlyList<CEntityRuntimeData> ValidTargets;

    public UsableSkillEntity(CSkillDataSO skill, IReadOnlyList<CEntityRuntimeData> validTargets)
    {
        Skill = skill;
        ValidTargets = validTargets;
    }
}

/// <summary>
/// 스킬 선택 욕구의 추상 부모 클래스
/// </summary>
public abstract class CSkillSelectionDesire
{
    /// <summary>
    /// SelfHpWeighted / Random
    /// </summary>
    public ESkillDesireType Type { get; set; }

    /// <summary>
    /// 사용 가능 스킬이 있는 경우 decision에 스킬과 후보 대상을 정리
    /// </summary>
    public abstract bool SelectSkill
        (
        CEntityRuntimeData performer,
        IReadOnlyList<UsableSkillEntity> usableSkills,
        CMonsterBrainDecision decision
        );


    /// <summary>
    /// 현재 랭크에서 사용 가능하고 유효 대상이 1명 이상인 스킬과 타겟 후보 정리
    /// </summary>
    public static List<UsableSkillEntity> CollectUsableSkillEntities
        (
        CEntityRuntimeData performer,
        List<CSkillDataSO> ownedSkills,
        List<CEntityRuntimeData> aliveEntities
        )
    {
        List<UsableSkillEntity> results = new List<UsableSkillEntity>();
        if (performer == null || performer.IsDead() || ownedSkills == null || aliveEntities == null)
        {
            return results;
        }
        // 스킬의 시전 가능 위치 확인
        int performerRankMask = (int)performer.GetRank();
        for (int i = 0; i < ownedSkills.Count; i++)
        {
            CSkillDataSO skill = ownedSkills[i];
            if (skill == null)
            {
                continue;
            }

            if ((performerRankMask & skill.GetRankBitmask()) == 0)
            {
                continue;
            }

            List<CEntityRuntimeData> validTargets = FilterValidTargetsForSkill(performer, skill, aliveEntities);
            if (validTargets.Count > 0)
            {
                results.Add(new UsableSkillEntity(skill, validTargets));
            }
        }

        return results;
    }

    /// <summary>
    /// 빈칸 및 사망자를 제외하고 스킬 진영 및 대상 랭크 조건을 만족하는 대상 반환
    /// </summary>
    public static List<CEntityRuntimeData> FilterValidTargetsForSkill
        (
        CEntityRuntimeData performer,
        CSkillDataSO skill,
        IReadOnlyList<CEntityRuntimeData> entities
        )
    {
        List<CEntityRuntimeData> validTargets = new List<CEntityRuntimeData>();
        if (performer == null || skill == null || entities == null)
        {
            return validTargets;
        }

        bool isAllySkill = skill.Type == ESkillType.Buff || skill.Type == ESkillType.Heal;
        bool performerIsHero = performer.IsHero();
        int targetRankMask = skill.GetTargetBitmask();

        for (int i = 0; i < entities.Count; i++)
        {
            CEntityRuntimeData unit = entities[i];
            if (unit == null || unit.IsDead())
            {
                continue;
            }

            // 시전자와 대상이 같은 진영인지 확인
            bool sameSide = unit.IsHero() == performerIsHero;
            if (isAllySkill && !sameSide)
            {
                continue;
            }

            if (!isAllySkill && sameSide)
            {
                continue;
            }

            int unitRankMask = (int)unit.GetRank();
            if (targetRankMask != 0 && (targetRankMask & unitRankMask) == 0)
            {
                continue;
            }

            validTargets.Add(unit);
        }

        return validTargets;
    }

    /// <summary>
    /// 가중치 목록을 기준으로 추첨 가능 여부 판단 후 인덱스 추첨
    /// </summary>
    protected int PickWeightedIndex(List<float> weights)
    {
        if (weights == null || weights.Count == 0)
        {
            return -1;
        }

        float totalWeight = 0f;
        for (int i = 0; i < weights.Count; i++)
        {
            if (weights[i] > 0f)
            {
                totalWeight += weights[i];
            }
        }

        if (totalWeight <= 0f)
        {
            return -1;
        }

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < weights.Count; i++)
        {
            float w = weights[i];
            if (w <= 0f)
            {
                continue;
            }

            cumulative += w;
            if (roll < cumulative)
            {
                return i;
            }
        }

        for (int i = weights.Count - 1; i >= 0; i--)
        {
            if (weights[i] > 0f)
            {
                return i;
            }
        }

        return -1;
    }
}

/// <summary>
/// 본인 체력 비율 기준에 따라 각 스킬 SO의 LowHpWeight 또는 BaseWeight에 따라서
/// 스킬에 가중치를 부여하여 추첨
/// </summary>
public class CSkillSelectionSelfHpWeighted : CSkillSelectionDesire
{
    /// <summary>
    /// 체력 판정 기준 비율 상수 ( 0.3f = 30%)
    /// </summary>
    private const float HpRatioThreshold = 0.3f;

    public CSkillSelectionSelfHpWeighted()
    {
        Type = ESkillDesireType.SelfHpWeighted;
    }

    /// <summary>
    /// 본인 체력 비율과 스킬 SO의 가중치를 기준으로 스킬을 선택
    /// </summary>
    public override bool SelectSkill
        (
        CEntityRuntimeData performer,
        IReadOnlyList<UsableSkillEntity> usableSkills,
        CMonsterBrainDecision decision
        )
    {
        if (performer == null || performer.IsDead() || usableSkills == null || usableSkills.Count == 0 || decision == null)
        {
            return false;
        }

        float maxHp = performer.GetStat(EEntityStat.MaxHP);
        float hpRatio = maxHp > 0f ? performer.GetStat(EEntityStat.HP) / maxHp : 1f;
        bool isLowHp = hpRatio <= HpRatioThreshold;

        List<float> weights = new List<float>(usableSkills.Count);
        for (int i = 0; i < usableSkills.Count; i++)
        {
            CSkillDataSO skill = usableSkills[i].Skill;
            float weight = skill != null
                ? Mathf.Max(0f, isLowHp ? skill.LowHpWeight : skill.BaseWeight)
                : 0f;
            weights.Add(weight);
        }

        int chosenIndex = PickWeightedIndex(weights);
        if (chosenIndex < 0 || chosenIndex >= usableSkills.Count)
        {
            return false;
        }

        UsableSkillEntity chosen = usableSkills[chosenIndex];
        if (chosen.Skill == null || chosen.ValidTargets == null || chosen.ValidTargets.Count == 0)
        {
            return false;
        }

        decision.SelectedSkill = chosen.Skill;
        decision.SetTargets(chosen.ValidTargets);
        return true;
    }

}

/// <summary>
/// 사용 가능 스킬 중 균등 확률로 무작위 스킬을 선택하는 방식
/// </summary>
public class CSkillSelectionRandom : CSkillSelectionDesire
{
    public CSkillSelectionRandom()
    {
        Type = ESkillDesireType.Random;
    }


    public override bool SelectSkill
        (
        CEntityRuntimeData performer,
        IReadOnlyList<UsableSkillEntity> usableSkills,
        CMonsterBrainDecision decision
        )
    {
        if (performer == null || performer.IsDead() || usableSkills == null || usableSkills.Count == 0 || decision == null)
        {
            return false;
        }

        List<UsableSkillEntity> pool = new List<UsableSkillEntity>(usableSkills);
        while (pool.Count > 0)
        {
            int index = Random.Range(0, pool.Count);
            UsableSkillEntity candidate = pool[index];
            pool.RemoveAt(index);

            if (candidate.Skill != null && candidate.ValidTargets != null && candidate.ValidTargets.Count > 0)
            {
                decision.SelectedSkill = candidate.Skill;
                decision.SetTargets(candidate.ValidTargets);
                return true;
            }
        }

        return false;
    }
}
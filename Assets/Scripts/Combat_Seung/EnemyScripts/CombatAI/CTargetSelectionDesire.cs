using System.Collections.Generic;
using UnityEngine;

public enum ETargetDesireType
{
    HpRatioWeighted,
    Random
}

/// <summary>
/// 타겟 선택 욕구의 추상 부모 클래스
/// </summary>
public abstract class CTargetSelectionDesire
{
    /// <summary>
    /// HpRatioWeighted / Random
    /// </summary>
    public ETargetDesireType Type { get; set; }

    /// <summary>
    /// 선택된 스킬과 대상을 바탕으로 최종 타겟 후보를 결정하는 매서드
    /// </summary>
    public abstract bool SelectTarget
        (
        CEntityRuntimeData performer,
        CMonsterBrainDecision decision,
        IReadOnlyList<CEntityRuntimeData> entityTargets
        );

    /// <summary>
    /// 가중치 목록을 기준으로 인덱스 하나를 무작위로 추첨하는 매서드
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
/// 단일 대상은 잃은 체력 비율을 반영(최대 60% 우선 선택 + 가중치 추첨)해 고르고,
/// 광역(AOE) 스킬은 스킬 대상 열/진영 구성에 맞춰 유효 후보 전체를 선정하는 타겟 욕구입니다.
/// </summary>
public class CTargetSelectionHpRatioWeighted : CTargetSelectionDesire
{

    /// <summary>
    /// 최소 기본 가중치
    /// </summary>
    private const float BaseWeight = 10f;
    /// <summary>
    /// 잃은 체력 비율(1 - HP/MaxHP)에 곱해지는 가중치 배율
    /// </summary>
    private const float MissingHpRatioMultiplier = 90f;
    /// <summary>
    /// 잃은 체력 비율이 가장 높은 대상을 우선 선택할 확률 (0.6f = 60%)
    /// </summary>
    private const float PriorityTargetChance = 0.6f;

    public CTargetSelectionHpRatioWeighted()
    {
        Type = ETargetDesireType.HpRatioWeighted;
    }

    /// <summary>
    /// 광역이면 스킬 구성에 따라 정해진 유효 대상 전체를 적용하고,
    /// 단일 대상이면 잃은 체력 비율을 반영해 최종 대상을 결정합니다.
    /// </summary>
    public override bool SelectTarget
        (
        CEntityRuntimeData performer,
        CMonsterBrainDecision decision,
        IReadOnlyList<CEntityRuntimeData> entityTargets
        )
    {
        if (
            performer == null || 
            performer.IsDead() || 
            decision == null ||
            decision.SelectedSkill == null ||
            entityTargets == null ||
            entityTargets.Count == 0
            )
        {
            return false;
        }

        // 광역은 스킬에 해당하는 대상을 적용
        if (decision.SelectedSkill.AOE)
        {
            decision.SetTargets(entityTargets);
            return decision.TargetUnits.Count > 0;
        }

        int topPriorityIndex = -1;
        float highestMissingRatio = -1f;
        List<float> weights = new List<float>(entityTargets.Count);

        for (int i = 0; i < entityTargets.Count; i++)
        {
            CEntityRuntimeData unit = entityTargets[i];
            if (unit == null || unit.IsDead())
            {
                weights.Add(0f);
                continue;
            }

            float maxHp = unit.GetStat(EEntityStat.MaxHP);
            float hpRatio = maxHp > 0f ? Mathf.Clamp01(unit.GetStat(EEntityStat.HP) / maxHp) : 1f;
            float missingRatio = 1f - hpRatio;

            if (missingRatio > highestMissingRatio)
            {
                highestMissingRatio = missingRatio;
                topPriorityIndex = i;
            }

            weights.Add(Mathf.Max(0.01f, BaseWeight + missingRatio * MissingHpRatioMultiplier));
        }

        if (topPriorityIndex < 0)
        {
            return false;
        }

        int chosenIndex;
        // 잃은 체력 비율이 가장 높은 대상이 있으면 최대 60% 확률로 우선 선택
        if (highestMissingRatio > 0f && Random.value < PriorityTargetChance)
        {
            chosenIndex = topPriorityIndex;
        }
        else
        {
            if (entityTargets.Count > 1 && highestMissingRatio > 0f)
            {
                weights[topPriorityIndex] = 0f;
            }

            chosenIndex = PickWeightedIndex(weights);
            if (chosenIndex < 0)
            {
                chosenIndex = topPriorityIndex;
            }
        }

        decision.ClearTargets();
        decision.TargetUnits.Add(entityTargets[chosenIndex]);
        return true;
    }
}

public class CTargetSelectionRandom : CTargetSelectionDesire
{
    public CTargetSelectionRandom()
    {
        Type = ETargetDesireType.Random;
    }

    public override bool SelectTarget(
        CEntityRuntimeData performer,
        CMonsterBrainDecision decision,
        IReadOnlyList<CEntityRuntimeData> entityTargets)
    {
        if (performer == null || performer.IsDead() || decision == null || decision.SelectedSkill == null ||
            entityTargets == null || entityTargets.Count == 0)
        {
            return false;
        }

        if (decision.SelectedSkill.AOE)
        {
            decision.SetTargets(entityTargets);
            return decision.TargetUnits.Count > 0;
        }

        int chosenIndex = Random.Range(0, entityTargets.Count);
        CEntityRuntimeData chosenUnit = entityTargets[chosenIndex];
        if (chosenUnit == null || chosenUnit.IsDead())
        {
            return false;
        }

        decision.ClearTargets();
        decision.TargetUnits.Add(chosenUnit);
        return true;
    }
}
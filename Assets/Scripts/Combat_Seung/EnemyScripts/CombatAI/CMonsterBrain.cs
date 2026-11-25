using System.Collections.Generic;

/// <summary>
/// 사용 가능 스킬 검증 후 스킬 욕구와 타겟 욕구를 순차 실행하여 판단으로 넘김
/// </summary>
public class CMonsterBrain
{
    #region 필드
    private List<CSkillSelectionDesire> _skillDesireSet;
    private List<CTargetSelectionDesire> _targetDesireSet;
    #endregion

    #region 프로퍼티
    public List<CSkillSelectionDesire> SkillDesireSet
    {
        get => _skillDesireSet;
        set => _skillDesireSet = value;
    }

    public List<CTargetSelectionDesire> TargetDesireSet
    {
        get => _targetDesireSet;
        set => _targetDesireSet = value;
    }
    #endregion

    public CMonsterBrain()
    {
        _skillDesireSet = new List<CSkillSelectionDesire>();
        _targetDesireSet = new List<CTargetSelectionDesire>();
    }

    public CMonsterBrainDecision Decide(CEntityRuntimeData performer, List<CEntityRuntimeData> entities)
    {
        CMonsterBrainDecision decision = new CMonsterBrainDecision(EBrainDecisionType.Pass);

        if (performer == null || performer.IsDead() || performer._skillData == null || entities == null || entities.Count == 0)
        {
            return decision;
        }

        // 빈칸/사망자를 제외한 유효 후보 목록을 1회만 추출
        List<CEntityRuntimeData> aliveEntities = CollectAliveEntity(entities);
        if (aliveEntities.Count == 0)
        {
            return decision;
        }

        List<CSkillDataSO> ownedSkills = performer._skillData.OutputSkillData();
        if (ownedSkills == null || ownedSkills.Count == 0)
        {
            return decision;
        }

        // 1. 사용 가능 스킬 및 유효 타겟 후보 선검증
        List<UsableSkillEntity> usableSkills = CSkillSelectionDesire.CollectUsableSkillEntities(
            performer, ownedSkills, aliveEntities);
        if (usableSkills.Count == 0)
        {
            return decision;
        }

        // 2. 스킬 욕구 평가
        bool skillSelected = false;
        if (_skillDesireSet != null)
        {
            for (int i = 0; i < _skillDesireSet.Count; i++)
            {
                CSkillSelectionDesire skillDesire = _skillDesireSet[i];
                if (skillDesire == null)
                {
                    continue;
                }

                decision.SetPass();
                if (skillDesire.SelectSkill(performer, usableSkills, decision) && decision.SelectedSkill != null)
                {
                    skillSelected = true;
                    break;
                }
            }
        }

        if (!skillSelected || decision.SelectedSkill == null)
        {
            decision.SetPass();
            return decision;
        }

        // 스킬 선택 시 가능한 타겟 목록에 따라서 타겟 욕구 평가 전 초기화
        List<CEntityRuntimeData> entitiesTargets = new List<CEntityRuntimeData>(decision.TargetUnits);

        // 3. 타겟 욕구 평가
        bool targetSelected = false;
        if (_targetDesireSet != null)
        {
            for (int i = 0; i < _targetDesireSet.Count; i++)
            {
                CTargetSelectionDesire targetDesire = _targetDesireSet[i];
                if (targetDesire == null)
                {
                    continue;
                }

                decision.ClearTargets();
                if (targetDesire.SelectTarget(performer, decision, entitiesTargets) && decision.TargetUnits.Count > 0)
                {
                    targetSelected = true;
                    break;
                }
            }
        }

        if (!targetSelected || decision.TargetUnits.Count == 0)
        {
            decision.SetPass();
            return decision;
        }

        decision.Decision = EBrainDecisionType.Play;
        return decision;
    }

    private static List<CEntityRuntimeData> CollectAliveEntity(List<CEntityRuntimeData> entity)
    {
        List<CEntityRuntimeData> alive = new List<CEntityRuntimeData>();
        for (int i = 0; i < entity.Count; i++)
        {
            CEntityRuntimeData unit = entity[i];
            if (unit != null && !unit.IsDead())
            {
                alive.Add(unit);
            }
        }

        return alive;
    }
}
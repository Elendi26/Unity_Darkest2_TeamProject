using System.Collections.Generic;
using UnityEngine;

public enum EBrainDecisionType
{
    Pass,
    Play
}

/// <summary>
/// 몬스터 브레인이 결정한 판단을 보관하는 곳
/// </summary>
public class CMonsterBrainDecision
{
    #region 필드
    private EBrainDecisionType _decision;
    private CSkillDataSO _selectedSkill;
    private readonly List<CEntityRuntimeData> _targetUnits = new List<CEntityRuntimeData>();
    #endregion

    #region 프로퍼티
    /// <summary>
    /// Pass/play
    /// </summary>
    public EBrainDecisionType Decision
    {
        get => _decision;
        set => _decision = value;
    }

    public CSkillDataSO SelectedSkill
    {
        get => _selectedSkill;
        set => _selectedSkill = value;
    }

    public List<CEntityRuntimeData> TargetUnits => _targetUnits;
    #endregion

    public CMonsterBrainDecision(EBrainDecisionType decision = EBrainDecisionType.Pass)
    {
        _decision = decision;
        _selectedSkill = null;
    }

    /// <summary>
    /// 결정 상태, 선택 스킬, 대상 목록을 지정해 인스턴스를 생성
    /// </summary>
    public CMonsterBrainDecision
        (
        EBrainDecisionType decision,
        CSkillDataSO selectedSkill,
        IEnumerable<CEntityRuntimeData> targetUnits
        )
    {
        _decision = decision;
        _selectedSkill = selectedSkill;
        SetTargets(targetUnits);
    }

    public void ClearTargets()
    {
        _targetUnits.Clear();
    }

    public void SetTargets(IEnumerable<CEntityRuntimeData> targets)
    {
        _targetUnits.Clear();
        if (targets == null)
        {
            return;
        }

        foreach (CEntityRuntimeData unit in targets)
        {
            if (unit != null && !unit.IsDead())
            {
                _targetUnits.Add(unit);
            }
        }
    }

    public void SetPass()
    {
        _decision = EBrainDecisionType.Pass;
        _selectedSkill = null;
        _targetUnits.Clear();
    }

}
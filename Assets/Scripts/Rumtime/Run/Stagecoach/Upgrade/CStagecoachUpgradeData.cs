using System.Collections.Generic;
using UnityEngine;

// 마차에 다는 업그레이드 → 짐칸에 들고 다니다가 여관 정비공에서 단다
[CreateAssetMenu(menuName = "Stagecoach/Upgrade_")]
public class CStagecoachUpgradeData : CItemDataBase
{
    #region 인스펙터
    [SerializeField] private EStagecoachSlot _slot = EStagecoachSlot.General;
    [SerializeField] private CRunStatModifier[] _modifiers;
    [SerializeReference] private List<IRunEffect> _beforeNodeEffects = new List<IRunEffect>();
    #endregion

    public EStagecoachSlot Slot => _slot;
    public IReadOnlyList<CRunStatModifier> Modifiers => _modifiers;
    public IReadOnlyList<IRunEffect> BeforeNodeEffects => _beforeNodeEffects;
    // 마차 업그레이드는 겹치지 않는다
    public override int MaxStack => 1;
    // 마차 아이템은 패물로 산다
    public override ERunValue BuyCurrency => ERunValue.Faction;

    public override string GetDetailNote(CRunData runData)
    {
        return "(여관 마차 정비공에서 마차에 단다)";
    }
}

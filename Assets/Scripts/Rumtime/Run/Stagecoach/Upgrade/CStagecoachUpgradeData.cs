using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Stagecoach/Upgrade_")]
public class CStagecoachUpgradeData : ScriptableObject
{
    #region 인스펙터
    [SerializeField] private string _id = "storage_trunk";
    [SerializeField] private EStagecoachSlot _slot = EStagecoachSlot.General;
    [SerializeField] private ERunValue _costCurrency = ERunValue.Faction;
    [SerializeField] private int _buyCost = 25;
    [SerializeField] private CRunStatModifier[] _modifiers;
    [SerializeReference] private List<IRunEffect> _beforeNodeEffects = new List<IRunEffect>();
    #endregion

    public string Id => _id;
    public EStagecoachSlot Slot => _slot;
    public ERunValue CostCurrency => _costCurrency;
    public int BuyCost => _buyCost;
    public IReadOnlyList<CRunStatModifier> Modifiers => _modifiers;
    public IReadOnlyList<IRunEffect> BeforeNodeEffects => _beforeNodeEffects;
}

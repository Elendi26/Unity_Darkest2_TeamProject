using System;
using System.Collections.Generic;
using UnityEngine;

public class CStagecoach
{
    #region 이벤트
    public event Action<CStagecoachUpgradeData> OnEquipped;
    public event Action<CStagecoachUpgradeData> OnUnequipped;
    #endregion

    #region 내부 변수
    private readonly CStagecoachConfig _config;
    private readonly CRunData _runData;
    private readonly List<CStagecoachUpgradeData> _equipped = new List<CStagecoachUpgradeData>();
    private readonly Dictionary<EStagecoachSlot, int> _unlockedSlots = new Dictionary<EStagecoachSlot, int>();
    #endregion

    public CRunValue Wheels => _runData.Get(ERunValue.Wheels);
    public CRunValue Armor => _runData.Get(ERunValue.Armor);
    public IReadOnlyList<CStagecoachUpgradeData> Equipped => _equipped;
    public bool IsWheelBroken => Wheels == null || Wheels.IsEmpty;
    public CStagecoach(CStagecoachConfig config, CRunData runData)
    {
        _config = config;
        _runData = runData;

        _runData.Register(ERunValue.Wheels, _config.MaxWheels, _config.MaxWheels);
        _runData.Register(ERunValue.Armor, _config.MaxArmor, _config.MaxArmor);
    }

    public int GetSlotCount(EStagecoachSlot slot)
    {
        int unlocked = _unlockedSlots.TryGetValue(slot, out int count) ? count : 0;

        return _config.GetBaseSlotCount(slot) + unlocked;
    }

    public void UnlockSlot(EStagecoachSlot slot, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        int unlocked = _unlockedSlots.TryGetValue(slot, out int count) ? count : 0;

        _unlockedSlots[slot] = unlocked + amount;
    }

    public int GetUsedSlots(EStagecoachSlot slot)
    {
        int used = 0;

        for (int i = 0; i < _equipped.Count; i++)
        {
            if (_equipped[i].Slot == slot)
            {
                used++;
            }
        }

        return used;
    }

    public bool TryEquip(CStagecoachUpgradeData upgrade)
    {
        if (upgrade == null)
        {
            return false;
        }

        if (GetUsedSlots(upgrade.Slot) >= GetSlotCount(upgrade.Slot))
        {
            CPrint.Log("슬롯이 가득참");
            return false;
        }

        _equipped.Add(upgrade);
        OnEquipped?.Invoke(upgrade);
        return true;
    }

    public bool Unequip(CStagecoachUpgradeData upgrade)
    {
        if (!_equipped.Remove(upgrade))
        {
            return false;
        }

        OnUnequipped?.Invoke(upgrade);
        return true;
    }

    public float GetStat(ERunStat stat)
    {
        float total = 0f;

        for (int i = 0; i < _equipped.Count; i++)
        {
            IReadOnlyList<CRunStatModifier> modifiers = _equipped[i].Modifiers;

            if (modifiers == null)
            {
                continue;
            }

            for (int j = 0; j < modifiers.Count; j++)
            {
                if (modifiers[j] == null || modifiers[j].Stat != stat)
                {
                    continue;
                }

                total += modifiers[j].Value;
            }
        }

        return total;
    }

    public void RunBeforeNodeEffects(CRunContext ctx)
    {
        for (int i = 0; i < _equipped.Count; ++i)
        {
            IReadOnlyList<IRunEffect> effects = _equipped[i].BeforeNodeEffects;

            for (int j = 0; j < effects.Count; j++)
            {
                if (effects[j] == null)
                {
                    continue;
                }

                effects[j].Apply(ctx);
            }
        }
    }

    public bool TryRepair(ERunValue part, int amount, ERunValue currency, int costPerPoint)
    {
        if (part != ERunValue.Wheels && part != ERunValue.Armor)
        {
            return false;
        }

        CRunValue target = _runData.Get(part);

        if (target == null || amount <= 0)
        {
            return false;
        }

        int repair = Mathf.Min(amount, target.Max - target.Current);

        if (repair <= 0)
        {
            return false;
        }

        int cost = repair * costPerPoint;

        if (!_runData.Has(currency, cost))
        {
            return false;
        }

        _runData.Add(currency, -cost);
        _runData.Add(part, repair);
        return true;
    }
}

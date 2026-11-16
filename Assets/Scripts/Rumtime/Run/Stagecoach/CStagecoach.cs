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
    private readonly CInventory _inventory;
    #endregion

    // 마차 짐칸 (소모품)
    public CInventory Inventory => _inventory;
    public CRunValue Wheels => _runData.Get(ERunValue.Wheels);
    public CRunValue Armor => _runData.Get(ERunValue.Armor);
    public IReadOnlyList<CStagecoachUpgradeData> Equipped => _equipped;
    public bool IsWheelBroken => Wheels == null || Wheels.IsEmpty;
    public CStagecoach(CStagecoachConfig config, CRunData runData)
    {
        _config = config;
        _runData = runData;

        // 시작은 기본 최대치 → 업그레이드로 상한까지 올린다
        _runData.Register(ERunValue.Wheels, _config.BaseWheels, _config.BaseWheels);
        _runData.Register(ERunValue.Armor, _config.BaseArmor, _config.BaseArmor);

        _inventory = new CInventory(GetInventorySlotCount);
    }

    // 짐칸 수 → 기본 칸 + 업그레이드(InventorySlots)
    private int GetInventorySlotCount()
    {
        return _config.InventorySlots + Mathf.RoundToInt(GetStat(ERunStat.InventorySlots));
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

    // 슬롯 None 은 슬롯 제한이 없다 (바퀴·장갑 보강 등)
    //  ㄴ 내구도 최대치를 올리는 업그레이드는 이미 상한이면 못 단다
    public bool CanEquip(CStagecoachUpgradeData upgrade)
    {
        if (upgrade == null)
        {
            return false;
        }

        if (upgrade.Slot != EStagecoachSlot.None && GetUsedSlots(upgrade.Slot) >= GetSlotCount(upgrade.Slot))
        {
            return false;
        }

        if (RaisesStat(upgrade, ERunStat.MaxWheels) && Wheels.Max >= _config.MaxWheels)
        {
            return false;
        }

        if (RaisesStat(upgrade, ERunStat.MaxArmor) && Armor.Max >= _config.MaxArmor)
        {
            return false;
        }

        return true;
    }

    public bool TryEquip(CStagecoachUpgradeData upgrade)
    {
        if (!CanEquip(upgrade))
        {
            CPrint.Log($"업그레이드 {(upgrade != null ? upgrade.Id : "null")} 를 달 수 없음 (슬롯이 가득 찼거나 상한)");
            return false;
        }

        _equipped.Add(upgrade);
        RefreshDurability();
        _inventory.NotifySlotsChanged();
        OnEquipped?.Invoke(upgrade);
        return true;
    }

    public bool Unequip(CStagecoachUpgradeData upgrade)
    {
        if (!_equipped.Remove(upgrade))
        {
            return false;
        }

        RefreshDurability();
        _inventory.NotifySlotsChanged();
        OnUnequipped?.Invoke(upgrade);
        return true;
    }

    private bool RaisesStat(CStagecoachUpgradeData upgrade, ERunStat stat)
    {
        IReadOnlyList<CRunStatModifier> modifiers = upgrade.Modifiers;

        if (modifiers == null)
        {
            return false;
        }

        for (int i = 0; i < modifiers.Count; i++)
        {
            if (modifiers[i] != null && modifiers[i].Stat == stat && modifiers[i].Value > 0f)
            {
                return true;
            }
        }

        return false;
    }

    // 장착한 업그레이드로 바퀴·장갑 최대치를 다시 정한다
    private void RefreshDurability()
    {
        SetDurabilityMax(Wheels, _config.BaseWheels + Mathf.RoundToInt(GetStat(ERunStat.MaxWheels)), _config.MaxWheels);
        SetDurabilityMax(Armor, _config.BaseArmor + Mathf.RoundToInt(GetStat(ERunStat.MaxArmor)), _config.MaxArmor);
    }

    private static void SetDurabilityMax(CRunValue value, int max, int cap)
    {
        if (value == null)
        {
            return;
        }

        int next = Mathf.Clamp(max, 0, cap);
        int added = next - value.Max;

        value.Max = next;

        // 늘어난 칸은 채워서 준다
        if (added > 0)
        {
            value.Add(added);
        }
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

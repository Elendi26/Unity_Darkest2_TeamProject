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
    private readonly CRelicInventorySync _relicSync;
    private readonly CRelicInventorySync _factionSync;
    #endregion

    // 마차 짐칸 (소모품)
    public CInventory Inventory => _inventory;
    public CRunValue Wheels => _runData.Get(ERunValue.Wheels);
    public CRunValue Armor => _runData.Get(ERunValue.Armor);
    public IReadOnlyList<CStagecoachUpgradeData> Equipped => _equipped;
    // 업그레이드로 더 연 슬롯 수 (씬을 넘길 때 기록용)
    public IReadOnlyDictionary<EStagecoachSlot, int> UnlockedSlots => _unlockedSlots;
    // 짐칸을 차지하는 유물 아이템 (없으면 null)
    public CItemDataBase RelicItem => _config.RelicItem;
    // 짐칸을 차지하는 패물 아이템 (없으면 null)
    public CItemDataBase FactionItem => _config.FactionItem;
    public bool IsWheelBroken => Wheels == null || Wheels.IsEmpty;
    public CStagecoach(CStagecoachConfig config, CRunData runData)
    {
        _config = config;
        _runData = runData;

        // 시작은 기본 최대치 → 업그레이드로 상한까지 올린다
        _runData.Register(ERunValue.Wheels, _config.BaseWheels, _config.BaseWheels);
        _runData.Register(ERunValue.Armor, _config.BaseArmor, _config.BaseArmor);

        _inventory = new CInventory(GetInventorySlotCount);

        // 유물 아이템이 있으면 유물 값과 짐칸을 맞춘다 (유물 값은 역마차보다 먼저 등록돼 있어야 한다)
        if (_config.RelicItem != null)
        {
            _relicSync = new CRelicInventorySync(_runData.Get(ERunValue.Gold), _inventory, _config.RelicItem);
        }

        // 패물도 유물처럼 짐칸에 겹쳐 넣는다
        if (_config.FactionItem != null)
        {
            _factionSync = new CRelicInventorySync(_runData.Get(ERunValue.Faction), _inventory, _config.FactionItem);
        }
    }

    // 짐칸 수 → 기본 칸 + 업그레이드(InventorySlots)
    private int GetInventorySlotCount()
    {
        return _config.InventorySlots + Mathf.RoundToInt(GetStat(ERunStat.InventorySlots));
    }

    // 마차 칸 수 → 종류 구분 없이 함께 쓰는 칸 + 업그레이드로 더 연 칸
    public int GetSlotCount()
    {
        int unlocked = 0;

        foreach (KeyValuePair<EStagecoachSlot, int> pair in _unlockedSlots)
        {
            unlocked += pair.Value;
        }

        return _config.CoachSlots + unlocked;
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

    // 칸을 차지한 아이템 수 (슬롯 None 은 칸을 쓰지 않는다)
    public int GetUsedSlots()
    {
        int used = 0;

        for (int i = 0; i < _equipped.Count; i++)
        {
            if (_equipped[i].Slot != EStagecoachSlot.None)
            {
                used++;
            }
        }

        return used;
    }

    // 슬롯 None 은 슬롯 제한이 없다 (바퀴·장갑 보강 등)
    public bool CanEquip(CStagecoachUpgradeData upgrade)
    {
        if (upgrade == null)
        {
            return false;
        }

        if (upgrade.Slot != EStagecoachSlot.None && GetUsedSlots() >= GetSlotCount())
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

    // 짐칸 칸에 든 마차 아이템 (아니면 null)
    private CStagecoachUpgradeData GetInventoryUpgrade(int index)
    {
        if (index < 0 || index >= _inventory.Stacks.Count)
        {
            return null;
        }

        CItemStack stack = _inventory.Stacks[index];

        return stack != null ? stack.Item as CStagecoachUpgradeData : null;
    }

    public bool CanEquipFromInventory(int index)
    {
        return CanEquip(GetInventoryUpgrade(index));
    }

    // 짐칸의 마차 아이템을 단다 → 짐칸에서 1개 빠진다
    public bool TryEquipFromInventory(int index)
    {
        CStagecoachUpgradeData upgrade = GetInventoryUpgrade(index);

        if (!CanEquip(upgrade))
        {
            return false;
        }

        _inventory.Remove(index, 1);
        return TryEquip(upgrade);
    }

    // 떼면 짐칸으로 돌아간다 → 짐칸 수를 늘려 주던 것이면 줄어든 칸에도 짐이 다 들어가야 뗄 수 있다
    public bool CanUnequipToInventory(CStagecoachUpgradeData upgrade)
    {
        if (upgrade == null || !_equipped.Contains(upgrade))
        {
            return false;
        }

        int lostSlots = 0;
        IReadOnlyList<CRunStatModifier> modifiers = upgrade.Modifiers;

        if (modifiers != null)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i] != null && modifiers[i].Stat == ERunStat.InventorySlots)
                {
                    lostSlots += Mathf.RoundToInt(modifiers[i].Value);
                }
            }
        }

        return _inventory.UsedSlots + 1 <= _inventory.SlotCount - lostSlots;
    }

    public bool TryUnequipToInventory(CStagecoachUpgradeData upgrade)
    {
        if (!CanUnequipToInventory(upgrade))
        {
            return false;
        }

        Unequip(upgrade);
        _inventory.Add(upgrade, 1);
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

    // 새 지역(주행 씬)에 들어설 때 → 단 업그레이드의 지역 진입 효과 (철테 바퀴 +1 등), 적용한 개수를 돌려준다
    public int RunRegionEnterEffects(CRunContext ctx)
    {
        int applied = 0;

        for (int i = 0; i < _equipped.Count; i++)
        {
            IReadOnlyList<IRunEffect> effects = _equipped[i].RegionEnterEffects;

            if (effects == null)
            {
                continue;
            }

            for (int j = 0; j < effects.Count; j++)
            {
                if (effects[j] == null)
                {
                    continue;
                }

                effects[j].Apply(ctx);
                applied++;
            }
        }

        return applied;
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

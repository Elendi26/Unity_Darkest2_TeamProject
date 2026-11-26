using System;
using System.Collections.Generic;
using UnityEngine;

// 씬을 넘어가도 런을 잇는다 → 넘어가기 전에 기록하고, 다음 씬 역마차가 시작할 때 꺼내 쓴다
public static class CRunCarry
{
	#region 이벤트
	// 런을 기록했을 때 (지도 완료·여관 저장하고 나가기·일시정지 메인 메뉴로) → 파일 저장은 CRunProfileSave 가 구독한다
	public static event Action<CRunSaveData> OnRunSaved;
	#endregion

	#region 내부 변수
	private static CRunSnapshot _pending;
	#endregion

	// 이번 런에서 교차로 출정으로 파티를 골랐는지 → 세이브에 함께 넣고 이어하기 때 되살린다
	public static bool PartyChosen { get; private set; }

	// 에디터에서 도메인 리로드를 꺼 둬도 지난 재생의 기록이 남지 않게 비운다
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetOnPlay()
	{
		_pending = null;
		PartyChosen = false;
	}

	public static void Save(CRunData runData, CStagecoach stagecoach, string nextScenePath = null)
	{
		if (runData == null || stagecoach == null)
		{
			CPrint.Warn("CRunCarry : 런 데이터나 역마차가 없어 기록하지 않습니다.");
			return;
		}

		_pending = CRunSnapshot.Capture(runData, stagecoach);

		// 파일 저장은 구독한 쪽에 맡긴다 → 런 쪽은 세이브 형식을 몰라도 된다
		CRunSaveData save = _pending.ToSave(nextScenePath);

		save.PartyChosen = PartyChosen;
		save.HeroTrinkets = CHeroTrinkets.ToSave();
		OnRunSaved?.Invoke(save);
	}

	public static bool Restore(CRunSaveData save)
	{
		if (save == null)
		{
			CPrint.Warn("CRunCarry : 불러올 런 기록이 없습니다.");
			return false;
		}

		PartyChosen = save.PartyChosen;
		CHeroTrinkets.Restore(save.HeroTrinkets);
		_pending = CRunSnapshot.FromSave(save);
		return _pending != null;
	}

	// 교차로에서 출정했을 때
	public static void MarkPartyChosen()
	{
		PartyChosen = true;
	}

	// 새 모험을 시작할 때 → 지난 런의 파티는 아직 고르지 않은 것으로
	public static void ClearPartyChosen()
	{
		PartyChosen = false;
	}

	public static CRunSnapshot Take()
	{
		CRunSnapshot snapshot = _pending;

		_pending = null;
		return snapshot;
	}
}

// 한 시점의 런 상태
public class CRunSnapshot
{
	// 값 하나 (지금 / 최대)
	public struct SValue
	{
		public int Current;
		public int Max;
	}

	// 짐칸 한 칸
	public struct SStack
	{
		public CItemDataBase Item;
		public int Count;
	}

	#region 내부 변수
	private readonly Dictionary<ERunValue, SValue> _values = new Dictionary<ERunValue, SValue>();
	private readonly List<SStack> _stacks = new List<SStack>();
	private readonly List<CStagecoachUpgradeData> _equipped = new List<CStagecoachUpgradeData>();
	private readonly Dictionary<EStagecoachSlot, int> _unlockedSlots = new Dictionary<EStagecoachSlot, int>();
	#endregion

	public static CRunSnapshot Capture(CRunData runData, CStagecoach stagecoach)
	{
		CRunSnapshot snapshot = new CRunSnapshot();
		ERunValue[] types = (ERunValue[])Enum.GetValues(typeof(ERunValue));

		for (int i = 0; i < types.Length; i++)
		{
			CRunValue value = runData.Get(types[i]);

			if (value != null)
			{
				snapshot._values[types[i]] = new SValue { Current = value.Current, Max = value.Max };
			}
		}

		IReadOnlyList<CItemStack> stacks = stagecoach.Inventory.Stacks;

		for (int i = 0; i < stacks.Count; i++)
		{
			if (stacks[i] != null && stacks[i].Item != null && stacks[i].Count > 0)
			{
				snapshot._stacks.Add(new SStack { Item = stacks[i].Item, Count = stacks[i].Count });
			}
		}

		snapshot._equipped.AddRange(stagecoach.Equipped);

		foreach (KeyValuePair<EStagecoachSlot, int> pair in stagecoach.UnlockedSlots)
		{
			snapshot._unlockedSlots[pair.Key] = pair.Value;
		}

		return snapshot;
	}

	public void RegisterValues(CRunData runData)
	{
		foreach (KeyValuePair<ERunValue, SValue> pair in _values)
		{
			// 바퀴·장갑은 역마차가 직접 등록한다
			if (pair.Key == ERunValue.Wheels || pair.Key == ERunValue.Armor)
			{
				continue;
			}

			// 유물·패물은 짐칸과 맞춰야 해서 0 으로 등록하고 마지막에 더한다
			if (IsInventoryValue(pair.Key))
			{
				runData.Register(pair.Key, 0, int.MaxValue);
				continue;
			}

			runData.Register(pair.Key, pair.Value.Current, pair.Value.Max);
		}

		// 기록에 없던 유물·패물 값도 역마차가 찾을 수 있게 등록해 둔다
		if (runData.Get(ERunValue.Gold) == null)
		{
			runData.Register(ERunValue.Gold, 0, int.MaxValue);
		}

		if (runData.Get(ERunValue.Faction) == null)
		{
			runData.Register(ERunValue.Faction, 0, int.MaxValue);
		}
	}

	// 짐칸 아이템으로도 들고 다니는 값 (유물·패물)
	private static bool IsInventoryValue(ERunValue type)
	{
		return type == ERunValue.Gold || type == ERunValue.Faction;
	}

	public void ApplyTo(CStagecoach stagecoach, CRunData runData)
	{
		foreach (KeyValuePair<EStagecoachSlot, int> pair in _unlockedSlots)
		{
			stagecoach.UnlockSlot(pair.Key, pair.Value);
		}

		// 업그레이드를 먼저 달아야 짐칸 수·바퀴 최대치가 맞는다
		for (int i = 0; i < _equipped.Count; i++)
		{
			stagecoach.TryEquip(_equipped[i]);
		}

		CItemDataBase relic = stagecoach.RelicItem;
		CItemDataBase faction = stagecoach.FactionItem;

		for (int i = 0; i < _stacks.Count; i++)
		{
			// 유물·패물 칸은 값을 더할 때 다시 생긴다
			if ((relic != null && _stacks[i].Item == relic) || (faction != null && _stacks[i].Item == faction))
			{
				continue;
			}

			stagecoach.Inventory.Add(_stacks[i].Item, _stacks[i].Count);
		}

		RestoreValue(runData.Get(ERunValue.Wheels), ERunValue.Wheels);
		RestoreValue(runData.Get(ERunValue.Armor), ERunValue.Armor);

		if (_values.TryGetValue(ERunValue.Gold, out SValue gold))
		{
			runData.Add(ERunValue.Gold, gold.Current);
		}

		if (_values.TryGetValue(ERunValue.Faction, out SValue factionValue))
		{
			runData.Add(ERunValue.Faction, factionValue.Current);
		}
	}

	public CRunSaveData ToSave(string scenePath)
	{
		CRunSaveData save = new CRunSaveData { ScenePath = scenePath };

		foreach (KeyValuePair<ERunValue, SValue> pair in _values)
		{
			save.Values.Add(new CRunSaveData.SValueSave { Type = pair.Key.ToString(), Current = pair.Value.Current, Max = pair.Value.Max });
		}

		for (int i = 0; i < _stacks.Count; i++)
		{
			save.Stacks.Add(new CRunSaveData.SStackSave { ItemId = _stacks[i].Item.Id, Count = _stacks[i].Count });
		}

		for (int i = 0; i < _equipped.Count; i++)
		{
			if (_equipped[i] == null)
			{
				continue;
			}

			save.EquippedIds.Add(_equipped[i].Id);
		}

		foreach (KeyValuePair<EStagecoachSlot, int> pair in _unlockedSlots)
		{
			save.UnlockedSlots.Add(new CRunSaveData.SSlotSave { Slot = pair.Key.ToString(), Count = pair.Value });
		}

		return save;
	}

	public static CRunSnapshot FromSave(CRunSaveData save)
	{
		if (save == null)
		{
			return null;
		}

		CRunSnapshot snapshot = new CRunSnapshot();
		CRunSaveRegistry registry = CRunSaveRegistry.Instance;

		for (int i = 0; i < save.Values.Count; i++)
		{
			CRunSaveData.SValueSave value = save.Values[i];

			if (!Enum.TryParse(value.Type, out ERunValue type))
			{
				CPrint.Warn($"CRunSnapshot : 모르는 자원 {value.Type} 은 건너뜁니다.");
				continue;
			}

			snapshot._values[type] = new SValue { Current = value.Current, Max = value.Max };
		}

		for (int i = 0; i < save.Stacks.Count; i++)
		{
			CRunSaveData.SStackSave stack = save.Stacks[i];

			if (registry == null || !registry.TryGetItem(stack.ItemId, out CItemDataBase item))
			{
				CPrint.Warn($"CRunSnapshot : 아이템 {stack.ItemId} 을 찾지 못해 건너뜁니다.");
				continue;
			}

			snapshot._stacks.Add(new SStack { Item = item, Count = stack.Count });
		}

		for (int i = 0; i < save.EquippedIds.Count; i++)
		{
			if (registry == null || !registry.TryGetUpgrade(save.EquippedIds[i], out CStagecoachUpgradeData upgrade))
			{
				CPrint.Warn($"CRunSnapshot : 업그레이드 {save.EquippedIds[i]} 를 찾지 못해 건너뜁니다.");
				continue;
			}

			snapshot._equipped.Add(upgrade);
		}

		for (int i = 0; i < save.UnlockedSlots.Count; i++)
		{
			CRunSaveData.SSlotSave slot = save.UnlockedSlots[i];

			if (!Enum.TryParse(slot.Slot, out EStagecoachSlot type))
			{
				CPrint.Warn($"CRunSnapshot : 모르는 슬롯 {slot.Slot} 은 건너뜁니다.");
				continue;
			}

			snapshot._unlockedSlots[type] = slot.Count;
		}

		return snapshot;
	}

	private void RestoreValue(CRunValue value, ERunValue type)
	{
		if (value == null || !_values.TryGetValue(type, out SValue saved))
		{
			return;
		}

		value.Max = saved.Max;
		value.Add(saved.Current - value.Current);
	}
}

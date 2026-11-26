using System;
using System.Collections.Generic;
using UnityEngine;

// 영웅이 끼운 장신구 (영웅마다 2칸) → 런 동안 들고 있다가 런 세이브에 함께 넣는다
//  ㄴ 영웅 세이브(팀 데이터)는 건드리지 않고 런 쪽에 둔다 → 장신구도 짐칸처럼 런 아이템이라서
//  ㄴ 능력치 반영(전투 쪽)은 아직 연결하지 않았다 → GetEquipped 로 꺼내 쓰면 된다
public static class CHeroTrinkets
{
	#region 상수
	public const int SLOT_COUNT = 2;
	#endregion

	#region 이벤트
	// 어떤 영웅의 장신구가 바뀌었을 때 (영웅 ID)
	public static event Action<string> OnChanged;
	#endregion

	#region 내부 변수
	private static readonly Dictionary<string, CTrinketItemData[]> _equipped = new Dictionary<string, CTrinketItemData[]>();
	#endregion

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetOnPlay()
	{
		_equipped.Clear();
		OnChanged = null;
	}

	public static CTrinketItemData Get(string heroId, int slot)
	{
		if (string.IsNullOrEmpty(heroId) || slot < 0 || slot >= SLOT_COUNT)
		{
			return null;
		}

		return _equipped.TryGetValue(heroId, out CTrinketItemData[] slots) ? slots[slot] : null;
	}

	// 이 영웅이 끼운 장신구 전부 (빈칸 제외)
	public static List<CTrinketItemData> GetEquipped(string heroId)
	{
		List<CTrinketItemData> result = new List<CTrinketItemData>();

		for (int i = 0; i < SLOT_COUNT; i++)
		{
			CTrinketItemData trinket = Get(heroId, i);

			if (trinket != null)
			{
				result.Add(trinket);
			}
		}

		return result;
	}

	// 같은 장신구를 다른 칸에 이미 끼웠으면 막는다 (중복 허용 장신구 제외)
	public static bool CanEquip(string heroId, int slot, CTrinketItemData trinket)
	{
		if (trinket == null || string.IsNullOrEmpty(heroId) || slot < 0 || slot >= SLOT_COUNT)
		{
			return false;
		}

		if (trinket.AllowDuplicate)
		{
			return true;
		}

		for (int i = 0; i < SLOT_COUNT; i++)
		{
			if (i != slot && Get(heroId, i) == trinket)
			{
				return false;
			}
		}

		return true;
	}

	// 짐칸의 장신구를 끼운다 → 칸에 이미 있던 것은 짐칸으로 돌아간다 (빠진 자리에 들어가므로 자리는 늘 있다)
	public static bool TryEquipFromInventory(string heroId, int slot, CInventory inventory, int index)
	{
		if (inventory == null || index < 0 || index >= inventory.Stacks.Count)
		{
			return false;
		}

		CTrinketItemData trinket = inventory.Stacks[index].Item as CTrinketItemData;

		if (!CanEquip(heroId, slot, trinket))
		{
			return false;
		}

		CTrinketItemData previous = Get(heroId, slot);

		inventory.Remove(index, 1);
		Set(heroId, slot, trinket);

		if (previous != null)
		{
			inventory.Add(previous, 1);
		}

		return true;
	}

	public static bool CanUnequipToInventory(string heroId, int slot, CInventory inventory)
	{
		CTrinketItemData trinket = Get(heroId, slot);

		return trinket != null && inventory != null && inventory.CanAdd(trinket, 1);
	}

	// 떼어 짐칸으로
	public static bool TryUnequipToInventory(string heroId, int slot, CInventory inventory)
	{
		if (!CanUnequipToInventory(heroId, slot, inventory))
		{
			return false;
		}

		CTrinketItemData trinket = Get(heroId, slot);

		Set(heroId, slot, null);
		inventory.Add(trinket, 1);
		return true;
	}

	// 같은 영웅의 두 칸을 맞바꾼다
	public static bool Swap(string heroId, int from, int to)
	{
		if (from == to || Get(heroId, from) == null || to < 0 || to >= SLOT_COUNT)
		{
			return false;
		}

		CTrinketItemData moving = Get(heroId, from);
		CTrinketItemData other = Get(heroId, to);

		Set(heroId, to, moving);
		Set(heroId, from, other);
		return true;
	}

	// 새 모험 → 모두 비운다
	public static void Clear()
	{
		_equipped.Clear();
		OnChanged?.Invoke(null);
	}

	public static List<CRunSaveData.SHeroTrinketSave> ToSave()
	{
		List<CRunSaveData.SHeroTrinketSave> result = new List<CRunSaveData.SHeroTrinketSave>();

		foreach (KeyValuePair<string, CTrinketItemData[]> pair in _equipped)
		{
			for (int i = 0; i < SLOT_COUNT; i++)
			{
				if (pair.Value[i] != null)
				{
					result.Add(new CRunSaveData.SHeroTrinketSave { HeroId = pair.Key, Slot = i, ItemId = pair.Value[i].Id });
				}
			}
		}

		return result;
	}

	// 이어하기 → 키로 장신구를 찾아 되살린다
	public static void Restore(List<CRunSaveData.SHeroTrinketSave> saves)
	{
		_equipped.Clear();

		if (saves != null && CRunSaveRegistry.Instance != null)
		{
			for (int i = 0; i < saves.Count; i++)
			{
				CRunSaveData.SHeroTrinketSave save = saves[i];

				if (CRunSaveRegistry.Instance.TryGetItem(save.ItemId, out CItemDataBase item) && item is CTrinketItemData trinket)
				{
					Set(save.HeroId, save.Slot, trinket, false);
				}
				else
				{
					CPrint.Warn($"CHeroTrinkets : 장신구 '{save.ItemId}' 를 찾지 못해 {save.HeroId} 에게 되살리지 않습니다.");
				}
			}
		}

		OnChanged?.Invoke(null);
	}

	private static void Set(string heroId, int slot, CTrinketItemData trinket, bool notify = true)
	{
		if (string.IsNullOrEmpty(heroId) || slot < 0 || slot >= SLOT_COUNT)
		{
			return;
		}

		if (!_equipped.TryGetValue(heroId, out CTrinketItemData[] slots))
		{
			slots = new CTrinketItemData[SLOT_COUNT];
			_equipped.Add(heroId, slots);
		}

		slots[slot] = trinket;

		if (notify)
		{
			OnChanged?.Invoke(heroId);
		}
	}
}

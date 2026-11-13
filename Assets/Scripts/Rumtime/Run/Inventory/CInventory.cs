using System;
using System.Collections.Generic;

public class CInventory
{
	#region 이벤트
	public event Action OnChanged;
	#endregion

	#region 내부 변수
	private readonly List<CItemStack> _stacks = new List<CItemStack>();
	private readonly Func<int> _getSlotCount;
	#endregion

	public IReadOnlyList<CItemStack> Stacks => _stacks;
	public int SlotCount => _getSlotCount != null ? Math.Max(0, _getSlotCount()) : 0;
	public int UsedSlots => _stacks.Count;

	public CInventory(Func<int> getSlotCount)
	{
		_getSlotCount = getSlotCount;
	}

	public int GetAddableCount(CItemData item)
	{
		if (item == null)
		{
			return 0;
		}

		int space = 0;

		for (int i = 0; i < _stacks.Count; i++)
		{
			if (_stacks[i].Item == item)
			{
				space += Math.Max(0, item.MaxStack - _stacks[i].Count);
			}
		}

		int freeSlots = SlotCount - _stacks.Count;

		if (freeSlots > 0)
		{
			space += freeSlots * item.MaxStack;
		}

		return space;
	}

	public bool CanAdd(CItemData item, int count)
	{
		return count > 0 && GetAddableCount(item) >= count;
	}

	public int Add(CItemData item, int count)
	{
		if (item == null || count <= 0)
		{
			return 0;
		}

		int left = count;

		for (int i = 0; i < _stacks.Count && left > 0; i++)
		{
			CItemStack stack = _stacks[i];

			if (stack.Item != item)
			{
				continue;
			}

			int put = Math.Min(left, item.MaxStack - stack.Count);

			if (put <= 0)
			{
				continue;
			}

			stack.Count += put;
			left -= put;
		}

		while (left > 0 && _stacks.Count < SlotCount)
		{
			int put = Math.Min(left, item.MaxStack);

			_stacks.Add(new CItemStack { Item = item, Count = put });
			left -= put;
		}

		int added = count - left;

		if (added > 0)
		{
			OnChanged?.Invoke();
		}

		if (left > 0)
		{
			CPrint.Warn($"CInventory : 자리가 없어 {item.DisplayName} {left}개를 못 넣었습니다.");
		}

		return added;
	}

	public bool Remove(int index, int count)
	{
		if (!IsValid(index) || count <= 0)
		{
			return false;
		}

		CItemStack stack = _stacks[index];

		stack.Count -= count;

		if (stack.Count <= 0)
		{
			_stacks.RemoveAt(index);
		}

		OnChanged?.Invoke();
		return true;
	}

	public bool CanUse(int index, CRunData runData)
	{
		if (!IsValid(index))
		{
			return false;
		}

		CItemData item = _stacks[index].Item;

		if (item == null || !item.IsUsable)
		{
			return false;
		}

		return !IsBlockedByFull(item, runData);
	}

	public bool TryUse(int index, CRunContext ctx)
	{
		if (ctx == null || !CanUse(index, ctx.RunData))
		{
			return false;
		}

		CItemData item = _stacks[index].Item;

		CRunEffectUtil.ApplyAll(item.UseEffects, ctx);
		Remove(index, 1);

		CPrint.Log($"CInventory : {item.DisplayName} 사용");
		return true;
	}

	public static bool IsBlockedByFull(CItemData item, CRunData runData)
	{
		if (item == null || item.BlockWhenFull == ERunValue.None || runData == null)
		{
			return false;
		}

		CRunValue value = runData.Get(item.BlockWhenFull);

		return value != null && value.Max > 0 && value.Current >= value.Max;
	}

	public void NotifySlotsChanged()
	{
		OnChanged?.Invoke();
	}

	private bool IsValid(int index)
	{
		return index >= 0 && index < _stacks.Count;
	}
}

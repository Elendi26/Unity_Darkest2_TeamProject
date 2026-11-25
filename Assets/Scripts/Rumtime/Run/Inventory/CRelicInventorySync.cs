using System;

// 유물·패물 값 ↔ 짐칸의 같은 아이템을 같은 수로 맞춘다 (유물 = Gold, 패물 = Faction)
public class CRelicInventorySync
{
	#region 내부 변수
	private readonly CRunValue _value;
	private readonly CInventory _inventory;
	private readonly CItemDataBase _relic;
	private bool _isSyncing;
	#endregion

	public CRelicInventorySync(CRunValue value, CInventory inventory, CItemDataBase relic)
	{
		_value = value;
		_inventory = inventory;
		_relic = relic;

		if (_value == null || _inventory == null || _relic == null)
		{
			CPrint.Warn("CRelicInventorySync : 값·짐칸·아이템 중 빠진 것이 있어 짐칸과 맞추지 않습니다.");
			return;
		}

		PutStartValue();

		_value.OnChanged += HandleValueChanged;
		_inventory.OnChanged += HandleInventoryChanged;
	}

	public void UnBind()
	{
		if (_value != null)
		{
			_value.OnChanged -= HandleValueChanged;
		}

		if (_inventory != null)
		{
			_inventory.OnChanged -= HandleInventoryChanged;
		}
	}

	// 시작 값을 짐칸에 넣는다 → 못 넣은 만큼은 값에서도 뺀다
	private void PutStartValue()
	{
		Run(() =>
		{
			int start = _value.Current;
			int added = _inventory.Add(_relic, start);

			if (added < start)
			{
				CPrint.Warn($"CRelicInventorySync : 짐칸이 모자라 시작 {_relic.DisplayName} {start - added}개를 버립니다.");
			}

			_value.Max = int.MaxValue;
			_value.Add(added - start);
			RefreshMax();
		});
	}

	// 값이 바뀌었다 (상점 결제·전리품 등) → 짐칸을 따라 바꾼다
	private void HandleValueChanged(ERunValue type, int prev, int current)
	{
		if (_isSyncing)
		{
			return;
		}

		Run(() =>
		{
			int delta = current - prev;

			if (delta > 0)
			{
				int added = _inventory.Add(_relic, delta);

				// 최대치로 막고 있어 보통은 다 들어간다 → 혹시 남으면 값을 되돌린다
				if (added < delta)
				{
					_value.Add(added - delta);
				}
			}
			else if (delta < 0)
			{
				RemoveRelics(-delta);
			}

			RefreshMax();
		});
	}

	// 짐칸이 바뀌었다 (유물 버리기·다른 아이템이 자리 차지·칸 수 변화) → 값을 짐칸에 맞춘다
	private void HandleInventoryChanged()
	{
		if (_isSyncing)
		{
			return;
		}

		Run(() =>
		{
			int count = CountRelics();

			_value.Max = Math.Max(_value.Max, count);
			_value.Add(count - _value.Current);
			RefreshMax();
		});
	}

	// 작은 더미부터 꺼낸다 → 큰 더미(상자 아이콘)가 오래 남는다
	private void RemoveRelics(int count)
	{
		int left = count;

		while (left > 0)
		{
			int index = FindSmallestStack();

			if (index < 0)
			{
				CPrint.Warn($"CRelicInventorySync : 짐칸에 {_relic.DisplayName} 이 모자라 {left}개를 못 뺐습니다.");
				return;
			}

			int take = Math.Min(left, _inventory.Stacks[index].Count);

			_inventory.Remove(index, take);
			left -= take;
		}
	}

	private int FindSmallestStack()
	{
		int found = -1;

		for (int i = 0; i < _inventory.Stacks.Count; i++)
		{
			CItemStack stack = _inventory.Stacks[i];

			if (stack.Item != _relic)
			{
				continue;
			}

			if (found < 0 || stack.Count < _inventory.Stacks[found].Count)
			{
				found = i;
			}
		}

		return found;
	}

	private int CountRelics()
	{
		int count = 0;

		for (int i = 0; i < _inventory.Stacks.Count; i++)
		{
			if (_inventory.Stacks[i].Item == _relic)
			{
				count += _inventory.Stacks[i].Count;
			}
		}

		return count;
	}

	// 최대치 = 지금 개수 + 짐칸에 더 들어갈 수 있는 수
	private void RefreshMax()
	{
		_value.Max = CountRelics() + _inventory.GetAddableCount(_relic);
	}

	// 서로 부르는 알림이 다시 돌아오지 않게 막고 실행한다
	private void Run(Action action)
	{
		_isSyncing = true;

		try
		{
			action();
		}
		finally
		{
			_isSyncing = false;
		}
	}
}

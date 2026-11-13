using System;
using System.Collections.Generic;

// 상점 노드에 도착하면 상품을 진열하고, 떠나기를 누를 때까지 사고팔게 한다
public class CShopNodeHandler : INodeHandler
{
	#region 내부 변수
	private readonly ENodeType _type;
	private readonly CShopData _shop;
	private readonly CShopView _view;
	private readonly CRunContext _ctx;
	private readonly CStagecoach _stagecoach;
	private readonly List<CShopItemData> _items = new List<CShopItemData>();
	private readonly List<int> _left = new List<int>();
	private readonly List<CShopCardInfo> _cards = new List<CShopCardInfo>();
	private readonly List<CShopItemData> _candidates = new List<CShopItemData>();
	private Action _onComplete;
	#endregion

	public ENodeType Type => _type;

	public CShopNodeHandler(ENodeType type, CShopData shop, CShopView view, CRunContext ctx, CStagecoach stagecoach)
	{
		_type = type;
		_shop = shop;
		_view = view;
		_ctx = ctx;
		_stagecoach = stagecoach;
	}

	public void Enter(CNodeInstance node, Action onComplete)
	{
		_onComplete = onComplete;

		RollItems();
		CPrint.Log($"{_type} 상점 : {_shop.Title} 상품 {_items.Count}개 (ID {node.Id}, 행 {node.Row})");

		// 상점에 있는 동안 짐칸을 쓰거나 버리면 '가득 참' 상태가 바뀐다 → 다시 그린다
		if (_stagecoach != null)
		{
			_stagecoach.Inventory.OnChanged += Refresh;
		}

		_view.Open(_shop.Title, _shop.Body, Leave);
		Refresh();
	}

	// 고정 상품 + 풀에서 가중치로 중복 없이 뽑는다
	private void RollItems()
	{
		_items.Clear();
		_left.Clear();

		IReadOnlyList<CShopItemData> fixedItems = _shop.FixedItems;

		if (fixedItems != null)
		{
			for (int i = 0; i < fixedItems.Count; i++)
			{
				if (fixedItems[i] == null)
				{
					continue;
				}

				AddItem(fixedItems[i]);
			}
		}

		_candidates.Clear();

		IReadOnlyList<CShopItemData> pool = _shop.Pool;

		if (pool != null)
		{
			for (int i = 0; i < pool.Count; i++)
			{
				if (pool[i] == null || pool[i].Weight <= 0f || _items.Contains(pool[i]))
				{
					continue;
				}

				_candidates.Add(pool[i]);
			}
		}

		System.Random random = _ctx.Random ?? new System.Random();

		for (int n = 0; n < _shop.RandomCount && _candidates.Count > 0; n++)
		{
			int picked = PickWeighted(random);

			AddItem(_candidates[picked]);
			_candidates.RemoveAt(picked);
		}
	}

	private int PickWeighted(System.Random random)
	{
		float total = 0f;

		for (int i = 0; i < _candidates.Count; i++)
		{
			total += _candidates[i].Weight;
		}

		float roll = (float)random.NextDouble() * total;

		for (int i = 0; i < _candidates.Count; i++)
		{
			roll -= _candidates[i].Weight;

			if (roll < 0f)
			{
				return i;
			}
		}

		return _candidates.Count - 1;
	}

	private void AddItem(CShopItemData item)
	{
		_items.Add(item);
		_left.Add(item.Stock);
	}

	private void Refresh()
	{
		_cards.Clear();

		for (int i = 0; i < _items.Count; i++)
		{
			CShopItemData item = _items[i];

			_cards.Add(new CShopCardInfo
			{
				Name = item.DisplayName,
				Description = item.Description,
				Icon = item.Icon,
				Price = $"{GetValueName(item.CostType)} {item.Cost}",
				Left = _left[i],
				State = GetState(i),
			});
		}

		_view.SetWallet(GetWalletText());
		_view.SetCards(_cards, Buy);
	}

	private void Buy(int index)
	{
		if (index < 0 || index >= _items.Count || GetState(index) != EShopCardState.Available)
		{
			return;
		}

		CShopItemData item = _items[index];

		if (item.Cost > 0)
		{
			_ctx.RunData.Add(item.CostType, -item.Cost);
		}

		if (item.Upgrade != null)
		{
			_stagecoach.TryEquip(item.Upgrade);
		}

		CRunEffectUtil.ApplyAll(item.Effects, _ctx);
		_left[index]--;

		// 짐칸에 넣으면 OnChanged → Refresh 가 한 번 더 불린다 (재고를 먼저 줄여 둔다)
		if (item.Item != null)
		{
			_stagecoach.Inventory.Add(item.Item, item.ItemCount);
		}

		CPrint.Log($"상점 구매 → {item.DisplayName} (남은 재고 {_left[index]})");
		Refresh();
	}

	private void Leave()
	{
		if (_stagecoach != null)
		{
			_stagecoach.Inventory.OnChanged -= Refresh;
		}

		_view.Close();

		// 콜백 전에 비운다
		Action onComplete = _onComplete;

		_onComplete = null;
		onComplete?.Invoke();
	}

	// 품절 → 상한 → 가득 참 → 돈 부족 순으로 본다
	private EShopCardState GetState(int index)
	{
		CShopItemData item = _items[index];

		if (_left[index] <= 0)
		{
			return EShopCardState.SoldOut;
		}

		if (item.Upgrade != null && (_stagecoach == null || !_stagecoach.CanEquip(item.Upgrade)))
		{
			return EShopCardState.Maxed;
		}

		if (item.Item != null && (_stagecoach == null || !_stagecoach.Inventory.CanAdd(item.Item, item.ItemCount)))
		{
			return EShopCardState.Full;
		}

		if (item.BlockWhenFull != ERunValue.None)
		{
			CRunValue value = _ctx.RunData.Get(item.BlockWhenFull);

			if (value != null && value.Max > 0 && value.Current >= value.Max)
			{
				return EShopCardState.Full;
			}
		}

		if (item.Cost > 0 && !_ctx.RunData.Has(item.CostType, item.Cost))
		{
			return EShopCardState.TooExpensive;
		}

		return EShopCardState.Available;
	}

	private string GetWalletText()
	{
		CRunData runData = _ctx.RunData;

		return $"골드 {runData.GetCurrent(ERunValue.Gold)}    "
			+ $"바퀴 {GetPair(runData.Get(ERunValue.Wheels))}    "
			+ $"장갑 {GetPair(runData.Get(ERunValue.Armor))}    "
			+ $"횃불 {GetPair(runData.Get(ERunValue.Torch))}"
			+ GetInventoryText();
	}

	private string GetInventoryText()
	{
		if (_stagecoach == null)
		{
			return string.Empty;
		}

		CInventory inventory = _stagecoach.Inventory;

		return $"    짐칸 {inventory.UsedSlots}/{inventory.SlotCount}";
	}

	private static string GetPair(CRunValue value)
	{
		return value != null ? $"{value.Current}/{value.Max}" : "-";
	}

	private static string GetValueName(ERunValue type)
	{
		switch (type)
		{
			case ERunValue.Gold:
				return "골드";
			case ERunValue.Faction:
				return "문장";
			case ERunValue.Torch:
				return "횃불";
			case ERunValue.Wheels:
				return "바퀴";
			case ERunValue.Armor:
				return "장갑";
		}

		return type.ToString();
	}
}

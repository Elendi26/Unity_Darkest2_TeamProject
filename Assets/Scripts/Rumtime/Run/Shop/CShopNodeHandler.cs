using System;
using System.Collections.Generic;

// 상점 노드에 도착하면 상품을 진열하고, 떠나기를 누를 때까지 사고팔게 한다
public class CShopNodeHandler : INodeHandler
{
	// 진열 칸 하나 → 서비스(수리·업그레이드 등) 또는 짐칸 아이템 중 하나
	private class CShopEntry
	{
		public CShopItemData Service;
		public CItemDataBase Item;
		public int Left;

		public string Name => Item != null ? Item.DisplayName : Service.DisplayName;
		public string Description => Item != null ? Item.Description : Service.Description;
		public UnityEngine.Sprite Icon => Item != null ? Item.Icon : Service.Icon;
		// 아이템은 아이템이 정한 재화로 산다 (기본 유물, 장신구·마차는 패물)
		public ERunValue CostType => Item != null ? Item.BuyCurrency : Service.CostType;
		public int Cost => Item != null ? Item.BuyPrice : Service.Cost;
		// 분류 → 서비스는 횃불을 채우면 횃불, 나머지(수리·보강·교환)는 마차
		public EItemCategory Category => Item != null ? Item.Category : (Service.BlockWhenFull == ERunValue.Torch ? EItemCategory.Torch : EItemCategory.Coach);
		public EItemRarity Rarity => Item != null ? Item.Rarity : EItemRarity.Common;
	}

	#region 내부 변수
	private readonly ENodeType _type;
	private readonly CShopData _shop;
	private readonly IShopView _view;
	private readonly CRunContext _ctx;
	private readonly CStagecoach _stagecoach;
	private readonly List<CShopEntry> _entries = new List<CShopEntry>();
	private readonly List<CShopCardInfo> _cards = new List<CShopCardInfo>();
	private readonly List<CShopItemData> _serviceCandidates = new List<CShopItemData>();
	private readonly List<CItemDataBase> _itemCandidates = new List<CItemDataBase>();
	private readonly List<float> _weights = new List<float>();
	private Action _onComplete;
	#endregion

	public ENodeType Type => _type;

	public CShopNodeHandler(ENodeType type, CShopData shop, IShopView view, CRunContext ctx, CStagecoach stagecoach)
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

		RollEntries();
		// 여관 씬 단독 테스트에서는 노드가 없다
		string where = node != null ? $"ID {node.Id}, 행 {node.Row}" : "노드 없음";

		CPrint.Log($"{_type} 상점 : {_shop.Title} 상품 {_entries.Count}개 ({where})");

		// 상점에 있는 동안 짐칸을 쓰거나 버리면 '가득 참' 상태가 바뀐다 → 다시 그린다
		if (_stagecoach != null)
		{
			_stagecoach.Inventory.OnChanged += Refresh;
		}

		_view.SetLook(_shop.Portrait, _shop.Crest, _shop.Banner);
		_view.Open(_shop.Title, _shop.Body, Leave);
		Refresh();
	}

	/// <summary>
	/// 진열을 다시 뽑지 않고 상점 화면만 다시 연다 (여관처럼 한 번 들른 곳에서 여러 번 드나들 때) / 경태
	/// </summary>
	public void Reopen(Action onComplete)
	{
		_onComplete = onComplete;

		if (_stagecoach != null)
		{
			_stagecoach.Inventory.OnChanged += Refresh;
		}

		_view.SetLook(_shop.Portrait, _shop.Crest, _shop.Banner);
		_view.Open(_shop.Title, _shop.Body, Leave);
		Refresh();
	}

	// 고정 서비스 → 서비스 풀 → 아이템 풀 순으로 진열한다
	private void RollEntries()
	{
		_entries.Clear();

		System.Random random = _ctx.Random ?? new System.Random();

		AddFixedServices();
		RollServices(random);
		RollItems(random);
	}

	private void AddFixedServices()
	{
		IReadOnlyList<CShopItemData> fixedItems = _shop.FixedItems;

		if (fixedItems == null)
		{
			return;
		}

		for (int i = 0; i < fixedItems.Count; i++)
		{
			if (fixedItems[i] == null)
			{
				continue;
			}

			_entries.Add(new CShopEntry { Service = fixedItems[i], Left = fixedItems[i].Stock });
		}
	}

	// 서비스 풀에서 가중치로 중복 없이 뽑는다
	private void RollServices(System.Random random)
	{
		_serviceCandidates.Clear();
		_weights.Clear();

		IReadOnlyList<CShopItemData> pool = _shop.Pool;

		if (pool != null)
		{
			for (int i = 0; i < pool.Count; i++)
			{
				if (pool[i] == null || pool[i].Weight <= 0f || HasService(pool[i]) || _serviceCandidates.Contains(pool[i]))
				{
					continue;
				}

				_serviceCandidates.Add(pool[i]);
				_weights.Add(pool[i].Weight);
			}
		}

		for (int n = 0; n < _shop.RandomCount && _serviceCandidates.Count > 0; n++)
		{
			int picked = PickWeighted(random);
			CShopItemData service = _serviceCandidates[picked];

			_entries.Add(new CShopEntry { Service = service, Left = service.Stock });
			_serviceCandidates.RemoveAt(picked);
			_weights.RemoveAt(picked);
		}
	}

	// 아이템 풀에서 희귀도 비중으로 중복 없이 뽑는다 (살 값이 0 이면 상점에 안 나온다)
	private void RollItems(System.Random random)
	{
		_itemCandidates.Clear();
		_weights.Clear();

		CItemRegistry itemPool = _shop.ItemPool;

		if (itemPool == null)
		{
			return;
		}

		IReadOnlyList<CItemDataBase> items = itemPool.Items;

		for (int i = 0; i < items.Count; i++)
		{
			CItemDataBase item = items[i];

			if (item == null || item.BuyPrice <= 0 || _itemCandidates.Contains(item) || _shop.IsExcluded(item.Category))
			{
				continue;
			}

			float weight = _shop.GetRarityWeight(item.Rarity);

			if (weight <= 0f)
			{
				continue;
			}

			_itemCandidates.Add(item);
			_weights.Add(weight);
		}

		for (int n = 0; n < _shop.ItemRandomCount && _itemCandidates.Count > 0; n++)
		{
			int picked = PickWeighted(random);

			_entries.Add(new CShopEntry { Item = _itemCandidates[picked], Left = _shop.ItemStock });
			_itemCandidates.RemoveAt(picked);
			_weights.RemoveAt(picked);
		}
	}

	private bool HasService(CShopItemData service)
	{
		for (int i = 0; i < _entries.Count; i++)
		{
			if (_entries[i].Service == service)
			{
				return true;
			}
		}

		return false;
	}

	// _weights 에서 하나를 고른다
	private int PickWeighted(System.Random random)
	{
		float total = 0f;

		for (int i = 0; i < _weights.Count; i++)
		{
			total += _weights[i];
		}

		float roll = (float)random.NextDouble() * total;

		for (int i = 0; i < _weights.Count; i++)
		{
			roll -= _weights[i];

			if (roll < 0f)
			{
				return i;
			}
		}

		return _weights.Count - 1;
	}

	private void Refresh()
	{
		_cards.Clear();

		for (int i = 0; i < _entries.Count; i++)
		{
			CShopEntry entry = _entries[i];

			_cards.Add(new CShopCardInfo
			{
				Name = entry.Name,
				Description = entry.Description,
				Icon = entry.Icon,
				Price = $"{CRunValueTable.GetName(entry.CostType)} {entry.Cost}",
				Cost = entry.Cost,
				CostType = entry.CostType,
				Left = entry.Left,
				State = GetState(entry),
				Category = entry.Category,
				Rarity = entry.Rarity,
			});
		}

		_view.SetWallet(GetWalletText());
		_view.SetCards(_cards, Buy);
	}

	private void Buy(int index)
	{
		if (index < 0 || index >= _entries.Count)
		{
			return;
		}

		CShopEntry entry = _entries[index];

		if (GetState(entry) != EShopCardState.Available)
		{
			return;
		}

		if (entry.Cost > 0)
		{
			_ctx.RunData.Add(entry.CostType, -entry.Cost);
		}

		entry.Left--;

		if (entry.Service != null)
		{
			CRunEffectUtil.ApplyAll(entry.Service.Effects, _ctx);
		}

		// 짐칸에 넣으면 OnChanged → Refresh 가 한 번 더 불린다 (재고를 먼저 줄여 둔다)
		if (entry.Item != null)
		{
			_stagecoach.Inventory.Add(entry.Item, 1);
		}

		CPrint.Log($"상점 구매 → {entry.Name} (남은 재고 {entry.Left})");
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
	private EShopCardState GetState(CShopEntry entry)
	{
		if (entry.Left <= 0)
		{
			return EShopCardState.SoldOut;
		}

		if (entry.Item != null && (_stagecoach == null || !_stagecoach.Inventory.CanAdd(entry.Item, 1)))
		{
			return EShopCardState.Full;
		}

		if (entry.Service != null)
		{
			CShopItemData service = entry.Service;

			if (service.BlockWhenFull != ERunValue.None)
			{
				CRunValue value = _ctx.RunData.Get(service.BlockWhenFull);

				if (value != null && value.Max > 0 && value.Current >= value.Max)
				{
					return EShopCardState.Full;
				}
			}
		}

		if (entry.Cost > 0 && !_ctx.RunData.Has(entry.CostType, entry.Cost))
		{
			return EShopCardState.TooExpensive;
		}

		return EShopCardState.Available;
	}

	private string GetWalletText()
	{
		CRunData runData = _ctx.RunData;

		return $"{CRunValueTable.GetName(ERunValue.Gold)} {runData.GetCurrent(ERunValue.Gold)}    "
			+ $"{CRunValueTable.GetName(ERunValue.Faction)} {runData.GetCurrent(ERunValue.Faction)}    "
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

}

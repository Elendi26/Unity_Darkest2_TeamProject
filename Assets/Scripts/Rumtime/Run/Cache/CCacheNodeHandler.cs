using System;
using System.Collections.Generic;
using System.Text;

// 은닉처 노드 → 도착하면 뒤질지 묻고, 뒤지면 재화·아이템을 무작위로 얻은 뒤 얻은 목록을 보여 준다
public class CCacheNodeHandler : INodeHandler
{
	#region 내부 변수
	private readonly ENodeType _type;
	private readonly CCacheData _data;
	private readonly CRoadEventPopup _popup;
	private readonly CRunContext _ctx;
	private readonly CStagecoach _stagecoach;
	private readonly List<string> _labels = new List<string>();
	private readonly List<CItemDataBase> _picks = new List<CItemDataBase>();
	private readonly StringBuilder _result = new StringBuilder();
	private System.Random _random;
	private Action _onComplete;
	// 이미 뒤졌으면 다음 버튼은 닫기
	private bool _isLooted;
	#endregion

	public ENodeType Type => _type;

	public CCacheNodeHandler(ENodeType type, CCacheData data, CRoadEventPopup popup, CRunContext ctx, CStagecoach stagecoach)
	{
		_type = type;
		_data = data;
		_popup = popup;
		_ctx = ctx;
		_stagecoach = stagecoach;
	}

	public void Enter(CNodeInstance node, Action onComplete)
	{
		_onComplete = onComplete;
		_isLooted = false;

		CPrint.Log($"{_type} 노드 : {_data.Title} (ID {node.Id}, 행 {node.Row})");

		_labels.Clear();
		_labels.Add(_data.SearchLabel);
		_labels.Add(_data.LeaveLabel);
		_popup.Show(_data.Title, _data.Body, _labels, null, HandleChoose);
	}

	private void HandleChoose(int index)
	{
		// 뒤진 뒤의 버튼, 또는 그냥 지나가기
		if (_isLooted || index != 0)
		{
			Finish();
			return;
		}

		_isLooted = true;

		string result = Loot();

		CPrint.Log($"{_type} 노드 → {_data.Title} 을 뒤짐\n{result}");

		// 같은 콜백으로 다시 띄운다 → 팝업이 다른 팝업으로 바뀌었다고 경고하지 않게
		_labels.Clear();
		_labels.Add(_data.DoneLabel);
		_popup.Show(_data.Title, result, _labels, null, HandleChoose);
	}

	#region 뒤지기
	private string Loot()
	{
		_random = _ctx != null && _ctx.Random != null ? _ctx.Random : new System.Random();
		_result.Clear();

		LootCurrencies();
		LootItems();

		if (_result.Length == 0)
		{
			return "은닉처는 텅 비어 있었다.";
		}

		return _result.ToString().TrimEnd();
	}

	private void LootCurrencies()
	{
		IReadOnlyList<CCacheData.CCurrencyDrop> currencies = _data.Currencies;

		if (currencies == null || _ctx == null || _ctx.RunData == null)
		{
			return;
		}

		for (int i = 0; i < currencies.Count; i++)
		{
			CCacheData.CCurrencyDrop drop = currencies[i];

			if (drop == null || drop.Type == ERunValue.None)
			{
				continue;
			}

			int amount = Roll(drop.Min, drop.Max);

			if (amount <= 0)
			{
				continue;
			}

			// 유물·패물은 짐칸에 들어간 만큼만 늘어난다 → 실제로 늘어난 양을 보여 준다
			CRunValue value = _ctx.RunData.Get(drop.Type);
			int before = value != null ? value.Current : 0;

			_ctx.RunData.Add(drop.Type, amount);

			int gained = value != null ? value.Current - before : amount;

			_result.Append($"{CRunValueTable.GetName(drop.Type)} +{gained}");

			if (gained < amount)
			{
				_result.Append($" <color=#d9604a>(짐칸이 모자라 {amount - gained}개는 두고 간다)</color>");
			}

			_result.AppendLine();
		}
	}

	private void LootItems()
	{
		IReadOnlyList<CCacheData.CLootPool> pools = _data.Pools;

		if (pools == null)
		{
			return;
		}

		// 아이템은 마차 짐칸에 넣는다 → 마차가 없으면 재화만
		if (_stagecoach == null)
		{
			CPrint.Warn("CCacheNodeHandler : 역마차가 없어 아이템은 건너뜁니다.");
			return;
		}

		for (int i = 0; i < pools.Count; i++)
		{
			CCacheData.CLootPool pool = pools[i];

			if (pool == null)
			{
				continue;
			}

			// 범주마다 먼저 나올지 굴린다 → 아이템이 하나도 안 나올 수도 있다
			if (_random.NextDouble() >= GetPoolChance(pool))
			{
				continue;
			}

			PickKinds(pool, Roll(pool.MinKinds, pool.MaxKinds));

			for (int p = 0; p < _picks.Count; p++)
			{
				AddItem(_picks[p], Roll(pool.MinCount, pool.MaxCount));
			}
		}
	}

	// 장신구가 든 범주는 마차 업그레이드(장신구 정리)만큼 더 잘 나온다 (확률 × (1 + 보정))
	private float GetPoolChance(CCacheData.CLootPool pool)
	{
		if (_stagecoach == null || pool.Items == null)
		{
			return pool.Chance;
		}

		float bonus = _stagecoach.GetStat(ERunStat.TrinketLootPercent);

		if (bonus <= 0f)
		{
			return pool.Chance;
		}

		for (int i = 0; i < pool.Items.Length; i++)
		{
			if (pool.Items[i] is CTrinketItemData)
			{
				return UnityEngine.Mathf.Clamp01(pool.Chance * (1f + bonus));
			}
		}

		return pool.Chance;
	}

	private void AddItem(CItemDataBase item, int count)
	{
		if (count <= 0)
		{
			return;
		}

		int added = _stagecoach.Inventory.Add(item, count);
		int left = count - added;

		if (added > 0)
		{
			_result.Append($"{item.DisplayName} x{added}");
		}
		else
		{
			_result.Append(item.DisplayName);
		}

		// 짐칸이 모자라면 남은 건 두고 간다
		if (left > 0)
		{
			_result.Append($" <color=#d9604a>(짐칸이 모자라 {left}개는 두고 간다)</color>");
		}

		_result.AppendLine();
	}

	// 목록에서 겹치지 않게 kinds 종류를 고른다
	private void PickKinds(CCacheData.CLootPool pool, int kinds)
	{
		_picks.Clear();

		if (pool.Items == null || kinds <= 0)
		{
			return;
		}

		for (int i = 0; i < pool.Items.Length; i++)
		{
			if (pool.Items[i] != null)
			{
				_picks.Add(pool.Items[i]);
			}
		}

		// 앞에서부터 kinds 칸만 섞는다
		int take = Math.Min(kinds, _picks.Count);

		for (int i = 0; i < take; i++)
		{
			int j = _random.Next(i, _picks.Count);
			CItemDataBase temp = _picks[i];

			_picks[i] = _picks[j];
			_picks[j] = temp;
		}

		_picks.RemoveRange(take, _picks.Count - take);
	}

	// min ~ max 사이 (max 포함)
	private int Roll(int min, int max)
	{
		if (max < min)
		{
			int temp = min;

			min = max;
			max = temp;
		}

		return _random.Next(min, max + 1);
	}
	#endregion

	private void Finish()
	{
		_popup.Close();

		// 콜백 전에 비운다
		Action onComplete = _onComplete;

		_onComplete = null;
		onComplete?.Invoke();
	}
}

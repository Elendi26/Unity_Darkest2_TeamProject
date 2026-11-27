using System;
using System.Collections.Generic;
using UnityEngine;

// 전투 승리 보상 → 유물·횃불 조금, 장신구는 확률로 (10% 로 1개, 5% 로 2개)
//  ㄴ 장신구 후보는 세이브 표(RunSaveRegistry)의 장신구 전체, 희귀도 비중으로 뽑는다
[Serializable]
public class CCombatRewardTable
{
	#region 인스펙터
	[SerializeField] private int _goldMin = 5;
	[SerializeField] private int _goldMax = 15;
	[SerializeField] private int _torchMin = 5;
	[SerializeField] private int _torchMax = 10;
	// 장신구 1개 나올 확률 (%)
	[SerializeField, Range(0, 100)] private int _oneTrinketChance = 10;
	// 장신구 2개 나올 확률 (%) → 1개 확률과 겹치지 않는다 (합쳐 15% 면 둘 중 하나)
	[SerializeField, Range(0, 100)] private int _twoTrinketChance = 5;
	// 희귀도별 뽑힐 비중 (흔함·드묾·희귀·전설 순)
	[SerializeField] private float[] _rarityWeights = { 4f, 2f, 1f, 0.5f };
	#endregion

	// 보상을 주고, 결과창에 띄울 줄을 lines 에 더한다
	public void Give(CRunData runData, CInventory inventory, System.Random random, List<string> lines)
	{
		if (runData == null || random == null)
		{
			return;
		}

		GiveValue(runData, ERunValue.Gold, "유물", RollRange(random, _goldMin, _goldMax), lines);
		GiveValue(runData, ERunValue.Torch, "횃불", RollRange(random, _torchMin, _torchMax), lines);

		int trinketCount = RollTrinketCount(random);

		if (trinketCount <= 0)
		{
			return;
		}

		List<CTrinketItemData> candidates = CollectTrinkets();

		for (int i = 0; i < trinketCount && candidates.Count > 0; i++)
		{
			CTrinketItemData trinket = PickWeighted(candidates, random);

			// 같은 전투에서 같은 장신구는 두 번 나오지 않는다
			candidates.Remove(trinket);

			int added = inventory != null ? inventory.Add(trinket, 1) : 0;

			lines?.Add(added > 0 ? $"장신구 {trinket.DisplayName}" : $"장신구 {trinket.DisplayName} (짐칸이 가득 차 버림)");
		}
	}

	private static void GiveValue(CRunData runData, ERunValue type, string label, int amount, List<string> lines)
	{
		if (amount <= 0 || runData.Get(type) == null)
		{
			return;
		}

		int added = runData.Add(type, amount);

		// 최대치에 막혀 다 못 받았으면 받은 만큼만 보인다
		if (added > 0)
		{
			lines?.Add($"{label} +{added}");
		}
	}

	private static int RollRange(System.Random random, int min, int max)
	{
		if (max < min)
		{
			max = min;
		}

		return random.Next(min, max + 1);
	}

	// 0~99 를 굴려 → 앞 _two% 는 2개, 그다음 _one% 는 1개
	private int RollTrinketCount(System.Random random)
	{
		int roll = random.Next(0, 100);

		if (roll < _twoTrinketChance)
		{
			return 2;
		}

		if (roll < _twoTrinketChance + _oneTrinketChance)
		{
			return 1;
		}

		return 0;
	}

	private static List<CTrinketItemData> CollectTrinkets()
	{
		List<CTrinketItemData> list = new List<CTrinketItemData>();
		CRunSaveRegistry registry = CRunSaveRegistry.Instance;

		if (registry == null)
		{
			return list;
		}

		IReadOnlyList<CItemDataBase> items = registry.Items;

		for (int i = 0; i < items.Count; i++)
		{
			if (items[i] is CTrinketItemData trinket && !list.Contains(trinket))
			{
				list.Add(trinket);
			}
		}

		return list;
	}

	private CTrinketItemData PickWeighted(List<CTrinketItemData> candidates, System.Random random)
	{
		float total = 0f;

		for (int i = 0; i < candidates.Count; i++)
		{
			total += GetWeight(candidates[i].Rarity);
		}

		if (total <= 0f)
		{
			return candidates[random.Next(0, candidates.Count)];
		}

		float roll = (float)random.NextDouble() * total;

		for (int i = 0; i < candidates.Count; i++)
		{
			roll -= GetWeight(candidates[i].Rarity);

			if (roll < 0f)
			{
				return candidates[i];
			}
		}

		return candidates[candidates.Count - 1];
	}

	private float GetWeight(EItemRarity rarity)
	{
		int index = (int)rarity;

		if (_rarityWeights == null || index < 0 || index >= _rarityWeights.Length)
		{
			return 0f;
		}

		return Mathf.Max(0f, _rarityWeights[index]);
	}
}

using System.Collections.Generic;
using UnityEngine;

// 끼운 장신구의 능력치를 전투 영웅에게 더한다 (전투 씬에서 영웅이 세워진 뒤 한 번)

public static class CHeroTrinketEffects
{
	#region 상수
	// 전투 쪽 CTokenStatus.LoadTokenData 가 읽는 경로 → 여기서 못 읽으면 토큰 판정에서 전투 쪽이 오류를 낸다
	private const string TOKEN_DATA_PATH = "Scripts/Combat_Data/TokenDataSO";
	#endregion

	#region 토큰
	// 전투 쪽 토큰 데이터가 읽히는지 (안 읽히면 토큰을 주지 않는다)
	public static bool IsTokenDataReady()
	{
		return Resources.Load<CTokenStructDataSO>(TOKEN_DATA_PATH) != null;
	}

	// 영웅 한 명에게 이 때(전투 시작·라운드 시작)의 장신구 토큰을 주고, 준 개수를 돌려준다
	public static int GrantTokens(CEntityRuntimeData hero, ETrinketTokenTiming timing, System.Random random, bool log)
	{
		if (hero == null || hero.HeroBasicData == null || hero.IsDead() || hero._tokenStatus == null)
		{
			return 0;
		}

		string heroId = hero.HeroBasicData.HeroID;
		List<CTrinketItemData> trinkets = CHeroTrinkets.GetEquipped(heroId);
		int granted = 0;

		for (int i = 0; i < trinkets.Count; i++)
		{
			IReadOnlyList<CTrinketTokenGrant> grants = trinkets[i].TokenGrants;

			if (grants == null)
			{
				continue;
			}

			for (int j = 0; j < grants.Count; j++)
			{
				CTrinketTokenGrant grant = grants[j];

				if (grant == null || grant.Timing != timing || grant.Token == ETokenBitMask.None || grant.Count <= 0)
				{
					continue;
				}

				if (random.Next(100) >= grant.Chance)
				{
					continue;
				}

				hero._tokenStatus.AddToken(grant.Token, grant.Count);
				granted += grant.Count;

				if (log)
				{
					CPrint.Log($"CHeroTrinketEffects : {heroId} ← {trinkets[i].DisplayName} {timing} {grant.Token} +{grant.Count} ({grant.Chance}%)");
				}
			}
		}

		return granted;
	}
	#endregion

	// 영웅 한 명에게 끼운 장신구 능력치를 더하고, 더한 줄 수를 돌려준다
	public static int Apply(CEntityRuntimeData hero, bool log)
	{
		if (hero == null || hero.HeroBasicData == null)
		{
			return 0;
		}

		string heroId = hero.HeroBasicData.HeroID;
		List<CTrinketItemData> trinkets = CHeroTrinkets.GetEquipped(heroId);
		int applied = 0;

		for (int i = 0; i < trinkets.Count; i++)
		{
			IReadOnlyList<CItemStatModifier> modifiers = trinkets[i].StatModifiers;

			if (modifiers == null)
			{
				continue;
			}

			for (int j = 0; j < modifiers.Count; j++)
			{
				CItemStatModifier modifier = modifiers[j];

				if (modifier == null || modifier.Value == 0)
				{
					continue;
				}

				// 피해·치유량은 전투 쪽 계산식에 아직 끼울 곳이 없다 → GetExtraPercent 로 꺼내 쓰게 남겨 둔다
				if (modifier.Extra != ETrinketExtraStat.None)
				{
					CPrint.Once
						(
						$"TrinketExtra_{modifier.Extra}",
						$"CHeroTrinketEffects : {modifier.Extra} 은(는) 전투 계산식에 아직 연결되지 않아 반영하지 않습니다. ({trinkets[i].DisplayName})"
						);
					continue;
				}

				if (!IsSupported(modifier.Stat))
				{
					CPrint.Once
						(
						$"TrinketStat_{modifier.Stat}",
						$"CHeroTrinketEffects : {modifier.Stat} 은(는) 전투 쪽에서 바꿀 수 없어 장신구 효과를 반영하지 않습니다. ({trinkets[i].DisplayName})"
						);
					continue;
				}

				float amount = modifier.IsPercent ? hero.GetStat(modifier.Stat) * modifier.Value / 100f : modifier.Value;

				hero.ModifyStat(modifier.Stat, amount);
				applied++;

				if (log)
				{
					CPrint.Log($"CHeroTrinketEffects : {heroId} ← {trinkets[i].DisplayName} {modifier.Stat} {FormatValue(modifier)} → {hero.GetStat(modifier.Stat)}");
				}
			}
		}

		return applied;
	}

	// 이 영웅이 끼운 장신구의 피해·치유량 보정 합 (%) → 전투 계산식에서 곱해 쓰면 된다 (예: 피해 × (1 + 값/100))
	public static int GetExtraPercent(string heroId, ETrinketExtraStat extra)
	{
		List<CTrinketItemData> trinkets = CHeroTrinkets.GetEquipped(heroId);
		int sum = 0;

		for (int i = 0; i < trinkets.Count; i++)
		{
			IReadOnlyList<CItemStatModifier> modifiers = trinkets[i].StatModifiers;

			if (modifiers == null)
			{
				continue;
			}

			for (int j = 0; j < modifiers.Count; j++)
			{
				if (modifiers[j] != null && modifiers[j].Extra == extra)
				{
					sum += modifiers[j].Value;
				}
			}
		}

		return sum;
	}

	// 전투 쪽 ModifyStat 이 실제로 더해 주는 능력치만
	public static bool IsSupported(EEntityStat stat)
	{
		switch (stat)
		{
			case EEntityStat.Movement:
			case EEntityStat.BleedRes:
			case EEntityStat.BlightRes:
			case EEntityStat.BurnRes:
			case EEntityStat.DiseaseRes:
			case EEntityStat.StunRes:
			case EEntityStat.MoveRes:
			case EEntityStat.DebuffRes:
			case EEntityStat.DeathblowRes:
			case EEntityStat.ExtraCritical:
				return true;
		}

		return false;
	}

	private static string FormatValue(CItemStatModifier modifier)
	{
		string sign = modifier.Value > 0 ? "+" : string.Empty;

		return modifier.IsPercent ? $"{sign}{modifier.Value}%" : $"{sign}{modifier.Value}";
	}
}

using System.Collections.Generic;
using System.Text;
using UnityEngine;

// 영웅에게 끼우는 장신구 → 끼우고 있는 동안 능력치를 바꾼다
[CreateAssetMenu(menuName = "Item/Trinket_")]
public class CTrinketItemData : CItemDataBase
{
	#region 인스펙터
	// 끼우면 바뀌는 능력치
	[SerializeField] private List<CItemStatModifier> _statModifiers = new List<CItemStatModifier>();
	// 전투 중 주는 토큰 (전투 시작·라운드 시작)
	[SerializeField] private List<CTrinketTokenGrant> _tokenGrants = new List<CTrinketTokenGrant>();
	// 같은 장신구를 한 영웅이 여러 개 끼울 수 있는지
	[SerializeField] private bool _allowDuplicate = false;
	#endregion

	// 끼우면 바뀌는 능력치
	public IReadOnlyList<CItemStatModifier> StatModifiers => _statModifiers;
	// 전투 중 주는 토큰
	public IReadOnlyList<CTrinketTokenGrant> TokenGrants => _tokenGrants;
	// 같은 장신구 중복 장착 허용
	public bool AllowDuplicate => _allowDuplicate;
	// 장신구는 겹치지 않는다
	public override int MaxStack => 1;
	// 장신구는 패물로 산다
	public override ERunValue BuyCurrency => ERunValue.Faction;

	// TODO : 조건부 효과 (체력 50% 이하일 때 등) 추가

	public override string GetDetailNote(CRunData runData)
	{
		return "(영웅에게 끼우는 장신구)";
	}

	#region 효과 줄
	private const string GOOD_COLOR = "#8fbf6a";	// 오르는 능력치·이로운 토큰
	private const string BAD_COLOR = "#d9604a";		// 내리는 능력치·해로운 토큰
	private const string NOTE_COLOR = "#8a8a8a";	// 보조 설명

	// 능력치 줄 + 토큰 줄 → 데이터에서 만든다 (설명 문구를 따로 고치지 않아도 맞는다)
	public override string GetEffectText()
	{
		StringBuilder sb = new StringBuilder();

		for (int i = 0; i < _statModifiers.Count; i++)
		{
			AppendStatLine(sb, _statModifiers[i]);
		}

		for (int i = 0; i < _tokenGrants.Count; i++)
		{
			AppendTokenLine(sb, _tokenGrants[i]);
		}

		return sb.Length > 0 ? sb.ToString() : null;
	}

	private static void AppendStatLine(StringBuilder sb, CItemStatModifier modifier)
	{
		if (modifier == null || modifier.Value == 0)
		{
			return;
		}

		bool isExtra = modifier.Extra != ETrinketExtraStat.None;
		string name = isExtra ? GetExtraName(modifier.Extra) : GetStatName(modifier.Stat);
		bool isPercent = isExtra || modifier.IsPercent || IsPercentStat(modifier.Stat);
		string value = $"{(modifier.Value > 0 ? "+" : "")}{modifier.Value}{(isPercent ? "%" : "")}";

		// 스트레스는 오를수록 나쁘다 → 색을 뒤집는다
		bool isGood = modifier.Value > 0;

		if (!isExtra && (modifier.Stat == EEntityStat.Stress || modifier.Stat == EEntityStat.MaxStress))
		{
			isGood = !isGood;
		}

		string updown = modifier.Value > 0 ? "증가" : "감소";

		AppendLine(sb, $"<color={(isGood ? GOOD_COLOR : BAD_COLOR)}>{name} {value} {updown}</color>");

		// 전투 쪽에서 아직 못 바꾸는 능력치는 알려 둔다
		if (isExtra || !CHeroTrinketEffects.IsSupported(modifier.Stat))
		{
			sb.Append($" <color={NOTE_COLOR}>(전투 미적용)</color>");
		}
	}

	private static void AppendTokenLine(StringBuilder sb, CTrinketTokenGrant grant)
	{
		if (grant == null || grant.Token == ETokenBitMask.None || grant.Count <= 0 || grant.Chance <= 0)
		{
			return;
		}

		string timing = grant.Timing == ETrinketTokenTiming.RoundStart ? "라운드 시작 시" : "전투 시작 시";
		string chance = grant.Chance < 100 ? $" {grant.Chance}% 확률로" : "";
		string color = IsGoodToken(grant.Token) ? GOOD_COLOR : BAD_COLOR;
		string effect = GetTokenEffect(grant.Token);

		AppendLine(sb, $"{timing}{chance} <color={color}>{GetTokenName(grant.Token)} 토큰 {grant.Count}개</color>");

		if (!string.IsNullOrEmpty(effect))
		{
			sb.Append($" <color={NOTE_COLOR}>({effect})</color>");
		}
	}

	private static void AppendLine(StringBuilder sb, string line)
	{
		if (sb.Length > 0)
		{
			sb.Append('\n');
		}

		sb.Append(line);
	}

	// 저항·치명타는 % 로 보인다
	private static bool IsPercentStat(EEntityStat stat)
	{
		switch (stat)
		{
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

	private static string GetStatName(EEntityStat stat)
	{
		switch (stat)
		{
			case EEntityStat.HP:
				return "체력";
			case EEntityStat.MaxHP:
				return "최대 체력";
			case EEntityStat.Stress:
				return "스트레스";
			case EEntityStat.MaxStress:
				return "최대 스트레스";
			case EEntityStat.Speed:
			case EEntityStat.CurrentSpeed:
				return "속도";
			case EEntityStat.Movement:
				return "이동";
			case EEntityStat.BleedRes:
				return "출혈 저항";
			case EEntityStat.BlightRes:
				return "역병 저항";
			case EEntityStat.BurnRes:
				return "화상 저항";
			case EEntityStat.DiseaseRes:
				return "질병 저항";
			case EEntityStat.StunRes:
				return "기절 저항";
			case EEntityStat.MoveRes:
				return "이동 저항";
			case EEntityStat.DebuffRes:
				return "약화 저항";
			case EEntityStat.DeathblowRes:
				return "죽음의 일격 저항";
			case EEntityStat.ExtraCritical:
				return "치명타";
		}

		return stat.ToString();
	}

	private static string GetExtraName(ETrinketExtraStat extra)
	{
		switch (extra)
		{
			case ETrinketExtraStat.Damage:
				return "피해";
			case ETrinketExtraStat.HealDealt:
				return "주는 치유량";
			case ETrinketExtraStat.HealReceived:
				return "받는 치유량";
		}

		return extra.ToString();
	}

	private static bool IsGoodToken(ETokenBitMask token)
	{
		switch (token)
		{
			case ETokenBitMask.Daze:
			case ETokenBitMask.Stun:
			case ETokenBitMask.Blind:
			case ETokenBitMask.Weak:
			case ETokenBitMask.Vulnerable:
			case ETokenBitMask.Taunt:
			case ETokenBitMask.Immobilize:
			case ETokenBitMask.Combo:
				return false;
		}

		return true;
	}

	private static string GetTokenName(ETokenBitMask token)
	{
		switch (token)
		{
			case ETokenBitMask.Crit:
				return "치명타";
			case ETokenBitMask.Riposte:
				return "반격";
			case ETokenBitMask.Dodge:
				return "회피";
			case ETokenBitMask.DodgeE:
				return "강화 회피";
			case ETokenBitMask.Block:
				return "방호";
			case ETokenBitMask.BlockE:
				return "강화 방호";
			case ETokenBitMask.Guard:
				return "보호";
			case ETokenBitMask.Strength:
				return "힘";
			case ETokenBitMask.Speed:
				return "속도";
			case ETokenBitMask.Stealth:
				return "은신";
			case ETokenBitMask.Daze:
				return "현기증";
			case ETokenBitMask.Stun:
				return "기절";
			case ETokenBitMask.Blind:
				return "실명";
			case ETokenBitMask.Weak:
				return "약화";
			case ETokenBitMask.Vulnerable:
				return "취약";
			case ETokenBitMask.Taunt:
				return "도발";
			case ETokenBitMask.Immobilize:
				return "속박";
			case ETokenBitMask.Combo:
				return "연계";
		}

		return token.ToString();
	}

	// 토큰이 하는 일 한 줄
	private static string GetTokenEffect(ETokenBitMask token)
	{
		switch (token)
		{
			case ETokenBitMask.Crit:
				return "다음 행동 피해·치유 1.5배";
			case ETokenBitMask.Block:
				return "받는 피해 -50%";
			case ETokenBitMask.BlockE:
				return "받는 피해 -75%";
			case ETokenBitMask.Dodge:
				return "공격 50% 회피";
			case ETokenBitMask.DodgeE:
				return "공격 75% 회피";
			case ETokenBitMask.Strength:
				return "다음 공격 피해 +50%";
			case ETokenBitMask.Speed:
				return "라운드에서 먼저 행동";
			case ETokenBitMask.Daze:
				return "라운드에서 마지막에 행동";
			case ETokenBitMask.Weak:
				return "다음 공격 피해 -50%";
			case ETokenBitMask.Vulnerable:
				return "받는 피해 +50%";
			case ETokenBitMask.Blind:
				return "다음 공격 50% 빗나감";
		}

		return null;
	}
	#endregion
}

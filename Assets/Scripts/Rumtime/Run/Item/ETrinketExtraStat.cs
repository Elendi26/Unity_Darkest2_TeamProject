// 전투 능력치(EEntityStat)에 없는 장신구 효과 → 고르면 Stat 대신 이것을 쓴다 (값은 % )
public enum ETrinketExtraStat
{
	None = 0,		// 없음 (Stat 을 쓴다)
	Damage,			// 주는 피해
	HealDealt,		// 기술로 제공하는 치유량
	HealReceived,	// 기술로 받는 치유량
}

using System;
using UnityEngine;

[Serializable]
public class CTrinketTokenGrant
{
	public ETrinketTokenTiming Timing = ETrinketTokenTiming.CombatStart;
	public ETokenBitMask Token = ETokenBitMask.None;
	public int Count = 1;
	// 줄 확률 (%)
	[Range(0, 100)] public int Chance = 100;
}

// 장신구 토큰을 주는 때
public enum ETrinketTokenTiming
{
	None = 0,
	CombatStart,	// 전투 시작 시 한 번
	RoundStart,		// 라운드가 시작될 때마다
}

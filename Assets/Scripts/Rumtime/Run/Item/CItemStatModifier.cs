using System;

// 장신구 능력치 보정 한 줄 (예: 속도 +2, 출혈 저항 -10)
[Serializable]
public class CItemStatModifier
{
	public EEntityStat Stat;
	public int Value;
	// 켜면 Value 를 % 로 본다 (최대 체력 +10% 등)
	public bool IsPercent;
}

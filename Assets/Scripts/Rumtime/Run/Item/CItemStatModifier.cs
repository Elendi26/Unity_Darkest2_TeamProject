using System;

// 장신구 능력치 보정 한 줄 (예: 속도 +2, 출혈 저항 -10)
[Serializable]
public class CItemStatModifier
{
	public EEntityStat Stat;
	public int Value;
	// 켜면 Value 를 % 로 본다 (최대 체력 +10% 등)
	public bool IsPercent;
	// 전투 능력치에 없는 효과 (피해·치유량) → None 이 아니면 Stat 대신 이것 (Value 는 %)
	public ETrinketExtraStat Extra;
}

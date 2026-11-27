using UnityEngine;

public enum ERunStat
{
    None = 0,
    InventorySlots,
    TorchDrainPercent,
    ScoutChance,
    MaxWheels,
    MaxArmor,
    ShopDiscountPercent,	// 상점 할인 (0.2 = 20%)
    TrinketLootPercent,		// 장신구 노획 확률 증가 (0.5 = 50% 더)
    HealPercent,			// 여정 중 치유량 증가 (0.2 = 20%)
}

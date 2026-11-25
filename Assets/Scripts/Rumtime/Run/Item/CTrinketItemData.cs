using System.Collections.Generic;
using UnityEngine;

// 영웅에게 끼우는 장신구 → 끼우고 있는 동안 능력치를 바꾼다
[CreateAssetMenu(menuName = "Item/Trinket_")]
public class CTrinketItemData : CItemDataBase
{
	#region 인스펙터
	// 끼우면 바뀌는 능력치
	[SerializeField] private List<CItemStatModifier> _statModifiers = new List<CItemStatModifier>();
	// 같은 장신구를 한 영웅이 여러 개 끼울 수 있는지
	[SerializeField] private bool _allowDuplicate = false;
	#endregion

	// 끼우면 바뀌는 능력치
	public IReadOnlyList<CItemStatModifier> StatModifiers => _statModifiers;
	// 같은 장신구 중복 장착 허용
	public bool AllowDuplicate => _allowDuplicate;
	// 장신구는 겹치지 않는다
	public override int MaxStack => 1;
	// 장신구는 패물로 산다
	public override ERunValue BuyCurrency => ERunValue.Faction;

	// TODO : 영웅 장착 칸·장착/해제 흐름 연결 (영웅 데이터 쪽과 맞춰서)
	// TODO : 조건부 효과 (체력 50% 이하일 때 등) 추가

	public override string GetDetailNote(CRunData runData)
	{
		return "(영웅에게 끼우는 장신구)";
	}
}

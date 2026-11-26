using System.Collections.Generic;
using UnityEngine;

// 짐칸에서 바로 쓰는 소모품
[CreateAssetMenu(menuName = "Item/Consumable_")]
public class CConsumableItemData : CItemDataBase
{
	#region 인스펙터
	// 이 값이 가득 차 있으면 못 쓴다 (기름·수리 도구 등). None 이면 검사 안 함
	[SerializeField] private ERunValue _blockWhenFull = ERunValue.None;
	// 쓰면 적용하는 효과 (비우면 보관만 가능)
	[SerializeReference] private List<IRunEffect> _useEffects = new List<IRunEffect>();
	#endregion

	// 가득 차면 못 쓰게 막을 값
	public ERunValue BlockWhenFull => _blockWhenFull;
	// 쓰면 적용하는 효과
	public IReadOnlyList<IRunEffect> UseEffects => _useEffects;
	// 쓸 수 있는 아이템인지 (효과가 하나라도 있다)
	public override bool IsUsable => _useEffects != null && _useEffects.Count > 0;

	public override bool IsBlocked(CRunData runData)
	{
		if (_blockWhenFull == ERunValue.None || runData == null)
		{
			return false;
		}

		CRunValue value = runData.Get(_blockWhenFull);

		return value != null && value.Max > 0 && value.Current >= value.Max;
	}

	public override void Use(CRunContext ctx)
	{
		CRunEffectUtil.ApplyAll(_useEffects, ctx);
	}
}

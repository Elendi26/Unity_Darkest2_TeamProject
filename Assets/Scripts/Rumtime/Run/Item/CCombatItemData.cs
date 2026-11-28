using System.Collections.Generic;
using System.Text;
using UnityEngine;

// 전투 중 영웅이 쓰는 아이템 (붕대·해독제·폭탄 등)
[CreateAssetMenu(menuName = "Item/Combat_")]
public class CCombatItemData : CItemDataBase
{
	#region 인스펙터
	// 누구에게 쓰는지
	[SerializeField] private ECombatItemTarget _target = ECombatItemTarget.Self;
	// 여관에서 짐칸으로 쓰면 적용하는 효과 (비우면 여관에서도 못 쓴다)
	[SerializeReference] private List<IRunEffect> _innUseEffects = new List<IRunEffect>();
	#endregion

	// 누구에게 쓰는지
	public ECombatItemTarget Target => _target;
	// 여관에서 쓸 수 있는 아이템인지
	public override bool IsUsable => _innUseEffects != null && _innUseEffects.Count > 0;

	// 여관 밖이거나 받을 영웅이 없으면 못 쓴다
	public override bool IsBlocked(CRunData runData)
	{
		return !CInnNodeHandler.IsInside || !HasTarget();
	}

	public override void Use(CRunContext ctx)
	{
		CRunEffectUtil.ApplyAll(_innUseEffects, ctx);
	}

	public override string GetEffectText()
	{
		if (!IsUsable)
		{
			return null;
		}

		StringBuilder sb = new StringBuilder();

		for (int i = 0; i < _innUseEffects.Count; i++)
		{
			if (!(_innUseEffects[i] is IRunEffectText text))
			{
				continue;
			}

			if (sb.Length > 0)
			{
				sb.Append('\n');
			}

			sb.Append($"여관에서 사용 : {text.GetText()}");
		}

		return sb.Length > 0 ? sb.ToString() : null;
	}

	public override string GetDetailNote(CRunData runData)
	{
		if (!IsUsable)
		{
			return "(전투 중에 쓰는 아이템)";
		}

		if (!CInnNodeHandler.IsInside)
		{
			return "(여관에서 쓸 수 있다)";
		}

		return HasTarget() ? null : "(지금은 회복할 영웅이 없다)";
	}

	// 치료 효과가 있으면 받을 영웅이 있어야 한다 (다른 효과는 늘 받는다고 본다)
	private bool HasTarget()
	{
		for (int i = 0; i < _innUseEffects.Count; i++)
		{
			if (!(_innUseEffects[i] is CPartyHealEffect heal) || heal.HasTarget())
			{
				return true;
			}
		}

		return false;
	}
}

using UnityEngine;

// 전투 중 영웅이 쓰는 아이템 (붕대·해독제·폭탄 등)
[CreateAssetMenu(menuName = "Item/Combat_")]
public class CCombatItemData : CItemDataBase
{
	#region 인스펙터
	// 누구에게 쓰는지
	[SerializeField] private ECombatItemTarget _target = ECombatItemTarget.Self;
	// 전투 밖(이동 중)에도 짐칸에서 쓸 수 있는지
	[SerializeField] private bool _usableOutOfCombat = false;
	#endregion

	// 누구에게 쓰는지
	public ECombatItemTarget Target => _target;
	// 전투 밖에서도 쓸 수 있는지
	public bool UsableOutOfCombat => _usableOutOfCombat;

	// TODO : 전투 효과 (회복·피해·상태 이상 제거 등) 를 전투 시스템과 연결
	// TODO : 전투 중 사용 흐름 (대상 고르기 → 효과 → 개수 차감)

	public override string GetDetailNote(CRunData runData)
	{
		return "(전투 중에 쓰는 아이템)";
	}
}

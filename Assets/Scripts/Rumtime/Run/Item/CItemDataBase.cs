using UnityEngine;

public abstract class CItemDataBase : ScriptableObject
{
	#region 인스펙터
	// 저장·불러오기용 고유 키 (비우면 에셋 이름)
	[SerializeField] private string _id;
	[SerializeField] private string _displayName = "아이템";
	[SerializeField, TextArea] private string _description = "";
	[SerializeField] private Sprite _icon;
	// 짐칸 분류 탭 (전투·횃불·장신구·마차)
	[SerializeField] private EItemCategory _category = EItemCategory.Supply;
	[SerializeField] private EItemRarity _rarity = EItemRarity.Common;
	// 한 칸에 겹칠 수 있는 개수
	[SerializeField] private int _maxStack = 4;
	// 상점에서 살 때 값 (0 이면 상점에 안 나온다)
	[SerializeField] private int _buyPrice = 10;
	// 팔 때 받는 금화 (0 이면 못 판다)
	[SerializeField] private int _sellPrice = 0;
	#endregion

	// 저장·불러오기용 고유 키
	public string Id => string.IsNullOrEmpty(_id) ? name : _id;
	// 짐칸에 보이는 이름
	public string DisplayName => _displayName;
	// 짐칸에 보이는 설명
	public string Description => _description;
	// 칸 아이콘
	public Sprite Icon => _icon;
	// 짐칸 분류
	public EItemCategory Category => _category;
	// 희귀도
	public EItemRarity Rarity => _rarity;
	// 한 칸에 겹칠 수 있는 개수 (장신구처럼 겹치지 않는 종류는 자식에서 1 로 막는다)
	public virtual int MaxStack => Mathf.Max(1, _maxStack);
	// 상점에서 살 때 값 (0 이면 상점에 안 나온다)
	public int BuyPrice => Mathf.Max(0, _buyPrice);
	// 팔 때 받는 금화
	public int SellPrice => Mathf.Max(0, _sellPrice);
	// 상점에서 살 때 내는 재화 (장신구·마차 아이템은 패물)
	public virtual ERunValue BuyCurrency => ERunValue.Gold;
	// 짐칸에서 '사용' 버튼으로 쓸 수 있는 종류인지
	public virtual bool IsUsable => false;

	// 지금 상태에서 못 쓰게 막혀 있는지 (예: 횃불이 가득 차 기름을 못 쓴다)
	public virtual bool IsBlocked(CRunData runData)
	{
		return false;
	}

	// 칸에 띄울 아이콘 → 겹친 개수에 따라 바뀌는 종류(유물)만 덮어쓴다
	public virtual Sprite GetIcon(int count)
	{
		return Icon;
	}

	// 짐칸에서 쓸 수 있는지
	public bool CanUse(CRunData runData)
	{
		return IsUsable && !IsBlocked(runData);
	}

	// 짐칸에서 썼을 때 → 쓸 수 있는 종류만 덮어쓴다
	public virtual void Use(CRunContext ctx)
	{
		// Do Nothing
	}

	// 짐칸 설명 칸 아래에 덧붙일 한 줄 (종류별 안내)
	public virtual string GetDetailNote(CRunData runData)
	{
		if (!IsUsable)
		{
			return "(보관만 하는 아이템)";
		}

		return IsBlocked(runData) ? "(가득 차 있어 지금은 쓸 수 없다)" : null;
	}
}

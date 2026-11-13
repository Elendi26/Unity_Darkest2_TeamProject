using System.Collections.Generic;
using UnityEngine;

// 상점에 진열되는 상품 하나
[CreateAssetMenu(menuName = "Shop/Shop Item_")]
public class CShopItemData : ScriptableObject
{
	#region 인스펙터
	[SerializeField] private string _displayName = "바퀴 수리";
	[SerializeField, TextArea] private string _description = "";
	[SerializeField] private Sprite _icon;
	[SerializeField] private ERunValue _costType = ERunValue.Gold;
	[SerializeField] private int _cost = 15;
	// 한 번 들를 때 살 수 있는 개수
	[SerializeField] private int _stock = 1;
	// 랜덤 진열 가중치
	[SerializeField] private float _weight = 1f;
	// 이 값이 가득 차 있으면 못 산다 (수리·기름 등). None 이면 검사 안 함
	[SerializeField] private ERunValue _blockWhenFull = ERunValue.None;
	// 사면 역마차에 다는 업그레이드 (비우면 없음)
	[SerializeField] private CStagecoachUpgradeData _upgrade;
	// 사면 짐칸에 넣는 아이템 (비우면 없음). 짐칸에 자리가 없으면 못 산다
	[SerializeField] private CItemData _item;
	[SerializeField] private int _itemCount = 1;
	[SerializeReference] private List<IRunEffect> _effects = new List<IRunEffect>();
	#endregion

	// 카드에 보이는 이름
	public string DisplayName => _displayName;
	// 카드에 보이는 설명
	public string Description => _description;
	// 카드 아이콘 (없으면 숨김)
	public Sprite Icon => _icon;
	// 지불 자원
	public ERunValue CostType => _costType;
	// 가격
	public int Cost => _cost;
	// 방문당 재고
	public int Stock => Mathf.Max(1, _stock);
	// 랜덤 진열 가중치
	public float Weight => _weight;
	// 가득 차면 못 사게 막을 값
	public ERunValue BlockWhenFull => _blockWhenFull;
	// 사면 다는 업그레이드
	public CStagecoachUpgradeData Upgrade => _upgrade;
	// 사면 짐칸에 넣는 아이템
	public CItemData Item => _item;
	// 한 번 살 때 넣는 개수
	public int ItemCount => Mathf.Max(1, _itemCount);
	// 사면 적용하는 효과
	public IReadOnlyList<IRunEffect> Effects => _effects;
}

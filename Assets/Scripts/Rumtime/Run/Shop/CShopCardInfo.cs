using UnityEngine;

// 상품 칸 하나에 보여 줄 내용
public struct CShopCardInfo
{
	public string Name;
	public string Description;
	public Sprite Icon;
	// 가격 글 (재화 이름 + 숫자, 설명 줄에 쓴다)
	public string Price;
	// 가격 숫자 (칸 아래에 쓴다)
	public int Cost;
	// 지불 재화
	public ERunValue CostType;
	// 남은 개수
	public int Left;
	public EShopCardState State;
	// 분류 탭·테두리 색에 쓴다
	public EItemCategory Category;
	// 희귀도 → 희귀 이상은 화려한 테두리
	public EItemRarity Rarity;
}

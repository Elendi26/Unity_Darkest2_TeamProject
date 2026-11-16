using UnityEngine;

// 카드 하나에 보여 줄 내용 (핸들러가 채우고 뷰가 그린다)
public struct CShopCardInfo
{
	public string Name;
	public string Description;
	public Sprite Icon;
	public string Price;
	public int Left;
	public EShopCardState State;
}

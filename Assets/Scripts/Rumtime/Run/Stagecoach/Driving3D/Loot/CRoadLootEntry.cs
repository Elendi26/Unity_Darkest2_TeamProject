using System;
using UnityEngine;

// 길가 수풀에서 나올 수 있는 보상 한 줄
[Serializable]
public class CRoadLootEntry
{
	// 인스펙터 목록에 보일 이름
	public string Label = "유물";
	// 뽑힐 비중 (0 이면 안 나온다)
	public int Weight = 1;
	// 줄 금화 범위 x ~ y (둘 다 0 이면 금화 없음)
	public Vector2Int Gold = new Vector2Int(3, 8);
	// 줄 패물 범위 x ~ y (둘 다 0 이면 패물 없음)
	public Vector2Int Faction = Vector2Int.zero;
	// 줄 아이템 (비우면 금화만)
	public CItemDataBase Item;
	// 채우면 Item 대신 이 목록에서 희귀도 비중으로 하나를 뽑는다
	public CItemRegistry ItemPool;
	// 아이템 개수
	public int ItemCount = 1;
	// 짐칸이 꽉 차 아이템을 못 넣으면 대신 줄 금화
	public int FullGold = 5;
}

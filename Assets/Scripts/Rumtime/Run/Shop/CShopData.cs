using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Shop_")]
public class CShopData : ScriptableObject
{
	#region 인스펙터
	[SerializeField] private string _title = "상점";
	[SerializeField, TextArea] private string _body = "떠돌이 상인이 물건을 펼쳐 놓았다.";
	// 항상 진열하는 서비스 (수리 등)
	[SerializeField] private CShopItemData[] _fixedItems;
	// 랜덤 진열 서비스 후보 (업그레이드·교환 등)
	[SerializeField] private CShopItemData[] _pool;
	// 서비스 풀에서 뽑는 개수 (중복 없음)
	[SerializeField] private int _randomCount = 1;
	// 짐칸 아이템을 뽑는 풀 → 살 값(BuyPrice)이 0 인 아이템은 빠진다
	[SerializeField] private CItemRegistry _itemPool;
	// 아이템 풀에서 뽑는 개수 (중복 없음)
	[SerializeField] private int _itemRandomCount = 3;
	// 아이템 하나당 방문 재고
	[SerializeField] private int _itemStock = 1;
	// 희귀도별 뽑힐 비중 (흔함·드묾·희귀·전설 순)
	[SerializeField] private float[] _rarityWeights = { 4f, 2f, 1f, 0.5f };
	#endregion

	// 상점 제목
	public string Title => _title;
	// 상점 설명
	public string Body => _body;
	// 항상 진열하는 서비스
	public IReadOnlyList<CShopItemData> FixedItems => _fixedItems;
	// 랜덤 진열 서비스 후보
	public IReadOnlyList<CShopItemData> Pool => _pool;
	// 서비스 풀에서 뽑는 개수
	public int RandomCount => _randomCount;
	// 짐칸 아이템 풀
	public CItemRegistry ItemPool => _itemPool;
	// 아이템 풀에서 뽑는 개수
	public int ItemRandomCount => _itemRandomCount;
	// 아이템 하나당 방문 재고
	public int ItemStock => Mathf.Max(1, _itemStock);

	public float GetRarityWeight(EItemRarity rarity)
	{
		int index = (int)rarity;

		if (_rarityWeights == null || index < 0 || index >= _rarityWeights.Length)
		{
			return 0f;
		}

		return Mathf.Max(0f, _rarityWeights[index]);
	}
}

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Shop_")]
public class CShopData : ScriptableObject
{
	#region 인스펙터
	[SerializeField] private string _title = "상점";
	[SerializeField, TextArea] private string _body = "떠돌이 상인이 물건을 펼쳐 놓았다.";
	// 왼쪽 상인 그림 (비우면 상점 화면 기본 그림)
	[SerializeField] private Sprite _portrait;
	// 제목 옆 문장 (비우면 없음)
	[SerializeField] private Sprite _crest;
	// 오른쪽 위 실루엣 장식 (비우면 없음)
	[SerializeField] private Sprite _banner;
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
	// 아이템 풀에서 이 분류는 진열하지 않는다 (보급상에서 마차 아이템 빼기 등)
	[SerializeField] private EItemCategory[] _excludeCategories;
	// 희귀도별 뽑힐 비중 (흔함·드묾·희귀·전설 순)
	[SerializeField] private float[] _rarityWeights = { 4f, 2f, 1f, 0.5f };
	#endregion

	// 상점 제목
	public string Title => _title;
	// 상점 설명
	public string Body => _body;
	// 왼쪽 상인 그림 (없으면 null)
	public Sprite Portrait => _portrait;
	// 제목 옆 문장 (없으면 null)
	public Sprite Crest => _crest;
	// 오른쪽 위 실루엣 장식 (없으면 null)
	public Sprite Banner => _banner;
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

	/// <summary>
	/// 이 분류의 아이템을 진열하지 않는지 / 경태
	/// </summary>
	public bool IsExcluded(EItemCategory category)
	{
		if (_excludeCategories == null)
		{
			return false;
		}

		for (int i = 0; i < _excludeCategories.Length; i++)
		{
			if (_excludeCategories[i] == category)
			{
				return true;
			}
		}

		return false;
	}

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

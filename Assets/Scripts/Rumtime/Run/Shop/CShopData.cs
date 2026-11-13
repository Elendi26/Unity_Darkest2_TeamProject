using System.Collections.Generic;
using UnityEngine;

// 상점 한 종류 → 고정 상품 + 상품 풀에서 랜덤으로 몇 개를 진열한다
[CreateAssetMenu(menuName = "Shop/Shop_")]
public class CShopData : ScriptableObject
{
	#region 인스펙터
	[SerializeField] private string _title = "상점";
	[SerializeField, TextArea] private string _body = "떠돌이 상인이 물건을 펼쳐 놓았다.";
	// 항상 진열 (수리 등)
	[SerializeField] private CShopItemData[] _fixedItems;
	// 랜덤 진열 후보
	[SerializeField] private CShopItemData[] _pool;
	// 풀에서 뽑는 개수 (중복 없음)
	[SerializeField] private int _randomCount = 3;
	#endregion

	// 상점 제목
	public string Title => _title;
	// 상점 설명
	public string Body => _body;
	// 항상 진열하는 상품
	public IReadOnlyList<CShopItemData> FixedItems => _fixedItems;
	// 랜덤 진열 후보
	public IReadOnlyList<CShopItemData> Pool => _pool;
	// 풀에서 뽑는 개수
	public int RandomCount => _randomCount;
}

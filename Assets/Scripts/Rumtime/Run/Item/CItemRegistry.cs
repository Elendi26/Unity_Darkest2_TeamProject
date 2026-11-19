using System.Collections.Generic;
using UnityEngine;

// 게임에 있는 모든 아이템 목록 → 저장한 아이템 키(Id)로 다시 찾을 때 쓴다
[CreateAssetMenu(menuName = "Item/Item Registry")]
public class CItemRegistry : ScriptableObject
{
	#region 인스펙터
	[SerializeField] private List<CItemDataBase> _items = new List<CItemDataBase>();
	#endregion

	#region 내부 변수
	private Dictionary<string, CItemDataBase> _byId;
	#endregion

	// 등록된 모든 아이템
	public IReadOnlyList<CItemDataBase> Items => _items;

	public bool TryGet(string id, out CItemDataBase item)
	{
		item = null;

		if (string.IsNullOrEmpty(id))
		{
			return false;
		}

		BuildLookup();

		return _byId.TryGetValue(id, out item);
	}

	// 원하는 종류만 모은다
	public void GetAll<T>(List<T> result) where T : CItemDataBase
	{
		result.Clear();

		for (int i = 0; i < _items.Count; i++)
		{
			if (_items[i] is T item)
			{
				result.Add(item);
			}
		}
	}

	// TODO : 희귀도별 무작위 뽑기 (보상·상점 진열용)

	private void BuildLookup()
	{
		if (_byId != null)
		{
			return;
		}

		_byId = new Dictionary<string, CItemDataBase>();

		for (int i = 0; i < _items.Count; i++)
		{
			CItemDataBase item = _items[i];

			if (item == null)
			{
				continue;
			}

			if (_byId.ContainsKey(item.Id))
			{
				CPrint.Warn($"CItemRegistry : 아이템 키 {item.Id} 가 겹칩니다. 뒤의 것은 무시합니다.");
				continue;
			}

			_byId.Add(item.Id, item);
		}
	}

	private void OnValidate()
	{
		// 인스펙터에서 목록을 바꾸면 다시 만든다
		_byId = null;
	}
}

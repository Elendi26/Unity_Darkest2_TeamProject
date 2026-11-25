using System.Collections.Generic;
using UnityEngine;

// 저장한 키(Id)로 아이템·마차 업그레이드 SO 를 다시 찾는 표
[CreateAssetMenu(menuName = "Run/Run Save Registry")]
public class CRunSaveRegistry : ScriptableObject
{
	private const string RESOURCE_PATH = "RunData/RunSaveRegistry";

	#region 인스펙터
	// 짐칸에 들어갈 수 있는 모든 아이템
	[SerializeField] private List<CItemDataBase> _items = new List<CItemDataBase>();
	// 마차에 달 수 있는 모든 업그레이드
	[SerializeField] private List<CStagecoachUpgradeData> _upgrades = new List<CStagecoachUpgradeData>();
	#endregion

	#region 내부 변수
	private static CRunSaveRegistry _instance;
	private static bool _isLoaded;
	private Dictionary<string, CItemDataBase> _itemById;
	private Dictionary<string, CStagecoachUpgradeData> _upgradeById;
	#endregion

	// Resources 에서 한 번만 불러온 표 (없으면 null)
	public static CRunSaveRegistry Instance
	{
		get
		{
			if (!_isLoaded)
			{
				_isLoaded = true;
				_instance = Resources.Load<CRunSaveRegistry>(RESOURCE_PATH);

				if (_instance == null)
				{
					CPrint.Warn($"CRunSaveRegistry : Resources/{RESOURCE_PATH} 가 없어 저장한 아이템·업그레이드를 되살릴 수 없습니다.");
				}
			}

			return _instance;
		}
	}

	public bool TryGetItem(string id, out CItemDataBase item)
	{
		item = null;

		if (string.IsNullOrEmpty(id))
		{
			return false;
		}

		BuildLookup();

		return _itemById.TryGetValue(id, out item);
	}

	public bool TryGetUpgrade(string id, out CStagecoachUpgradeData upgrade)
	{
		upgrade = null;

		if (string.IsNullOrEmpty(id))
		{
			return false;
		}

		BuildLookup();

		return _upgradeById.TryGetValue(id, out upgrade);
	}

	private void BuildLookup()
	{
		if (_itemById != null && _upgradeById != null)
		{
			return;
		}

		_itemById = new Dictionary<string, CItemDataBase>();
		_upgradeById = new Dictionary<string, CStagecoachUpgradeData>();

		for (int i = 0; i < _items.Count; i++)
		{
			CItemDataBase item = _items[i];

			if (item == null)
			{
				continue;
			}

			if (_itemById.ContainsKey(item.Id))
			{
				CPrint.Warn($"CRunSaveRegistry : 아이템 키 {item.Id} 가 겹칩니다. 뒤의 것은 무시합니다.");
				continue;
			}

			_itemById.Add(item.Id, item);
		}

		for (int i = 0; i < _upgrades.Count; i++)
		{
			CStagecoachUpgradeData upgrade = _upgrades[i];

			if (upgrade == null || string.IsNullOrEmpty(upgrade.Id))
			{
				continue;
			}

			if (_upgradeById.ContainsKey(upgrade.Id))
			{
				CPrint.Warn($"CRunSaveRegistry : 업그레이드 키 {upgrade.Id} 가 겹칩니다. 뒤의 것은 무시합니다.");
				continue;
			}

			_upgradeById.Add(upgrade.Id, upgrade);

			// 마차 업그레이드도 짐칸에 들어가는 아이템이다
			if (!_itemById.ContainsKey(upgrade.Id))
			{
				_itemById.Add(upgrade.Id, upgrade);
			}
		}
	}

	private void OnValidate()
	{
		// 인스펙터에서 목록을 바꾸면 다시 만든다
		_itemById = null;
		_upgradeById = null;
	}
}

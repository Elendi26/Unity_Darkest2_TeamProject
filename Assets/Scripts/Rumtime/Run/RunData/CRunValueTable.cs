using System;
using UnityEngine;

// 자원(골드·횃불·바퀴 등)을 화면에 띄울 이름·아이콘 표
[CreateAssetMenu(menuName = "Run/Run Value Table")]
public class CRunValueTable : ScriptableObject
{
	[Serializable]
	public class CEntry
	{
		public ERunValue Type;
		public string DisplayName;
		public Sprite Icon;
	}

	private const string RESOURCE_PATH = "RunData/RunValueTable";

	#region 인스펙터
	[SerializeField] private CEntry[] _entries;
	#endregion

	#region 내부 변수
	private static CRunValueTable _instance;
	private static bool _isLoaded;
	#endregion

	// Resources 에서 한 번만 불러온 표 (없으면 null)
	public static CRunValueTable Instance
	{
		get
		{
			if (!_isLoaded)
			{
				_isLoaded = true;
				_instance = Resources.Load<CRunValueTable>(RESOURCE_PATH);

				if (_instance == null)
				{
					CPrint.Warn($"CRunValueTable : Resources/{RESOURCE_PATH} 가 없어 자원 이름을 타입 이름으로 띄웁니다.");
				}
			}

			return _instance;
		}
	}

	public static string GetName(ERunValue type)
	{
		CEntry entry = Instance != null ? Instance.Find(type) : null;

		return entry != null && !string.IsNullOrEmpty(entry.DisplayName) ? entry.DisplayName : type.ToString();
	}

	public static Sprite GetIcon(ERunValue type)
	{
		CEntry entry = Instance != null ? Instance.Find(type) : null;

		return entry != null ? entry.Icon : null;
	}

	private CEntry Find(ERunValue type)
	{
		if (_entries == null)
		{
			return null;
		}

		for (int i = 0; i < _entries.Length; i++)
		{
			if (_entries[i] != null && _entries[i].Type == type)
			{
				return _entries[i];
			}
		}

		return null;
	}
}

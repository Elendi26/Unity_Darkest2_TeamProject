using System;
using UnityEngine;

// 자원(골드·횃불·바퀴 등)을 화면에 띄울 이름·아이콘 표
//  ㄴ 자원 이름이 여러 화면의 switch 에 복사돼 있던 것을 에셋 하나로 모았다 → 새 자원은 표에 한 줄만 추가
//  ㄴ Resources/RunData/RunValueTable 에 두면 어디서든 CRunValueTable.GetName() 으로 읽는다
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

	/// <summary>
	/// 자원을 화면에 띄울 이름 (표에 없으면 타입 이름) / 경태
	/// </summary>
	public static string GetName(ERunValue type)
	{
		CEntry entry = Instance != null ? Instance.Find(type) : null;

		return entry != null && !string.IsNullOrEmpty(entry.DisplayName) ? entry.DisplayName : type.ToString();
	}

	/// <summary>
	/// 자원 아이콘 (표에 없으면 null) / 경태
	/// </summary>
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

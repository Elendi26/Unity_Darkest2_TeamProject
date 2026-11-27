using System;
using System.Collections.Generic;
using UnityEngine;

// 은닉처 노드 데이터 → 뒤지면 재화와 아이템 범주마다 정한 만큼 무작위로 떨어진다
[CreateAssetMenu(menuName = "Map/Cache_")]
public class CCacheData : ScriptableObject
{
	// 재화 하나 → 범위 안에서 무작위 양
	[Serializable]
	public class CCurrencyDrop
	{
		public ERunValue Type = ERunValue.Gold;
		public int Min = 12;
		public int Max = 16;
	}

	// 아이템 범주 하나 → 목록에서 몇 종류를 골라 각각 몇 개씩 (목록이 비면 건너뛴다)
	[Serializable]
	public class CLootPool
	{
		public string Name = "전투 아이템";
		// 이 범주가 나올 확률 (0~1) → 못 뽑으면 이 범주는 아무것도 안 나온다
		[Range(0f, 1f)] public float Chance = 1f;
		public CItemDataBase[] Items;
		public int MinKinds = 1;
		public int MaxKinds = 1;
		public int MinCount = 1;
		public int MaxCount = 1;
	}

	#region 인스펙터
	[SerializeField] private string _title = "교수의 은닉처";
	[SerializeField, TextArea] private string _body = "";
	[SerializeField] private string _searchLabel = "은닉처를 뒤진다";
	[SerializeField] private string _leaveLabel = "그냥 지나간다";
	[SerializeField] private string _doneLabel = "챙겨서 떠난다";
	[SerializeField] private CCurrencyDrop[] _currencies;
	[SerializeField] private CLootPool[] _pools;
	#endregion

	// 팝업 제목
	public string Title => _title;
	// 도착했을 때 본문
	public string Body => _body;
	// 뒤지는 버튼 글자
	public string SearchLabel => _searchLabel;
	// 뒤지지 않고 지나가는 버튼 글자
	public string LeaveLabel => _leaveLabel;
	// 얻은 목록을 보여 준 뒤 닫는 버튼 글자
	public string DoneLabel => _doneLabel;
	// 떨어지는 재화 목록
	public IReadOnlyList<CCurrencyDrop> Currencies => _currencies;
	// 떨어지는 아이템 범주 목록
	public IReadOnlyList<CLootPool> Pools => _pools;
}

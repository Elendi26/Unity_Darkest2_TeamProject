using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Map/Node Event_")]
public class CNodeEventData : ScriptableObject
{
	#region 인스펙터
	[SerializeField] private string _title = "상점";
	[SerializeField, TextArea] private string _body = "";
	[SerializeField] private string _leaveLabel = "떠나기";
	[SerializeField] private CNodeEventOption[] _options;
	#endregion

	// 팝업 제목
	public string Title => _title;
	// 팝업 본문
	public string Body => _body;
	// 맨 아래에 붙는 나가기 버튼 글자
	public string LeaveLabel => _leaveLabel;
	// 선택지 목록
	public IReadOnlyList<CNodeEventOption> Options => _options;
}

[Serializable]
public class CNodeEventOption
{
	public string Label = "바퀴 수리";
	public ERunValue CostType = ERunValue.Gold;
	public int Cost = 0;
	// 고른 뒤 팝업을 닫는다 → 상점처럼 여러 번 사게 하려면 끈다
	public bool CloseAfter = true;
	// 고르면 역마차에 다는 업그레이드 (비우면 없음). 달 수 없으면 버튼이 꺼진다
	public CStagecoachUpgradeData Upgrade;
	[SerializeReference] public List<IRunEffect> Effects = new List<IRunEffect>();
}

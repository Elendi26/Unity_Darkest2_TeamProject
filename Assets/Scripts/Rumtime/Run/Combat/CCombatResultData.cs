using System.Collections.Generic;
using UnityEngine;

// 전투가 어떻게 끝났는지
public enum ECombatResult
{
	None = 0,
	Victory,	// 적 전멸
	Defeat,		// 파티 전멸
	Skipped,	// 건너뛰기 키
	Abandoned,	// 일시정지 → 전투 포기
}

// 전투 결과창에 띄울 내용 → 전투 씬을 내리기 전에 모아 둔다 (씬이 내려가면 영웅 데이터가 사라진다)
public class CCombatResultData
{
	// 영웅 한 줄 (전투 전 → 전투 후)
	public class CHeroRow
	{
		public string Name;
		public Sprite Portrait;
		public int HpBefore;
		public int HpAfter;
		public int MaxHp;
		public int StressBefore;
		public int StressAfter;
		public int MaxStress;
		public bool IsDead;
	}

	public ECombatResult Result;
	public ECombatKind Kind;
	// 노드 이름·도로 이벤트 제목
	public string PlaceName;
	public readonly List<CHeroRow> Heroes = new List<CHeroRow>();
	// 받은 보상 한 줄씩 (예: 유물 +8, 장신구 모호한 수호부)
	public readonly List<string> Rewards = new List<string>();
}

using UnityEngine;

// 어떤 길로 들어온 전투인지 → 전투 쪽이 적 구성 등을 다르게 할 때 쓴다 (CCombatContext.Kind)
public enum ECombatKind
{
	None = 0,
	Node,			// 지도의 전투 노드
	Barricade,		// 도로 방책
	Ambush,			// 도로 습격 (바퀴·장갑이 바닥났을 때)
	CreatureDen,	// 괴물 소굴 노드
	Fortress,		// 성채 노드
}

public static class CCombatKindText
{
	// 로그 등에 쓸 이름
	public static string GetName(ECombatKind kind)
	{
		switch (kind)
		{
			case ECombatKind.Node:
				return "노드 전투";
			case ECombatKind.Barricade:
				return "방책 전투";
			case ECombatKind.Ambush:
				return "습격 전투";
			case ECombatKind.CreatureDen:
				return "소굴 전투";
			case ECombatKind.Fortress:
				return "성채 전투";
		}

		return "전투";
	}

	// 지도 노드 종류 → 전투 종류 (전투가 아닌 노드는 None)
	public static ECombatKind FromNode(ENodeType type)
	{
		switch (type)
		{
			case ENodeType.Combat:
				return ECombatKind.Node;
			case ENodeType.CreatureDen:
				return ECombatKind.CreatureDen;
			case ENodeType.Fortress:
				return ECombatKind.Fortress;
		}

		return ECombatKind.None;
	}
}

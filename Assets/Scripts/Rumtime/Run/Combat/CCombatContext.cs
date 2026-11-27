using UnityEngine;

// 지금 열린 전투가 어디서 왔는지 → 전투 씬을 올리기 전에 채운다
public static class CCombatContext
{
	// 전투 종류 (노드·방책·습격·소굴·성채, 런 밖이면 None)
	public static ECombatKind Kind { get; private set; }
	// 지도 노드 종류 (도로 전투면 None)
	public static ENodeType NodeType { get; private set; }
	// 도로 이벤트의 전투 키
	public static string CombatId { get; private set; } = string.Empty;
	// 노드 이름·도로 이벤트 제목
	public static string PlaceName { get; private set; } = string.Empty;
	// 런에서 연 전투인지
	public static bool IsActive => Kind != ECombatKind.None;

	// 에디터에서 도메인 리로드를 꺼 둬도 지난 재생 값이 남지 않게
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetOnPlay()
	{
		Clear();
	}

	public static void Set(ECombatKind kind, ENodeType nodeType, string combatId, string placeName)
	{
		Kind = kind;
		NodeType = nodeType;
		CombatId = combatId ?? string.Empty;
		PlaceName = placeName ?? string.Empty;
	}

	public static void Clear()
	{
		Kind = ECombatKind.None;
		NodeType = ENodeType.None;
		CombatId = string.Empty;
		PlaceName = string.Empty;
	}
}

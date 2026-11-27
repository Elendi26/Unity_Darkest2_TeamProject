using System.Collections.Generic;
using UnityEngine;

// 런 쪽 화면이 내는 효과음 → 팀 사운드 매니저(CSoundManager)에 소리 ID 로 넘긴다
public static class CRunSound
{
	#region 상수
	public const string OPEN_UI = "OpenUI";
	public const string CLOSE_UI = "CloseUI";
	public const string DRIVING_WHIP = "OnDrivingWip";
	public const string DRIVING_HORSE = "OnDrivingHorse";
	public const string DRIVING_LOOP = "OnDrivingLoop";
	#endregion

	#region 내부 변수
	// 소리 ID → 마지막으로 낸 프레임
	private static readonly Dictionary<string, int> _lastFrame = new Dictionary<string, int>();
	#endregion

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetOnPlay()
	{
		_lastFrame.Clear();
	}

	// 팝업·지도·짐칸 등을 펼칠 때
	public static void PlayOpenUI()
	{
		PlayOnce(OPEN_UI);
	}

	// 팝업·지도·짐칸 등을 닫을 때
	public static void PlayCloseUI()
	{
		PlayOnce(CLOSE_UI);
	}

	public static void PlayOnce(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return;
		}

		int frame = Time.frameCount;

		if (_lastFrame.TryGetValue(id, out int last) && last == frame)
		{
			return;
		}

		_lastFrame[id] = frame;
		CSoundManager.Instance?.PlaySfxOneShot(id);
	}

	// 노드에 도착해 들어가기 전 나레이션 (없는 노드는 null)
	public static string GetNodeArriveSound(ENodeType type)
	{
		switch (type)
		{
			case ENodeType.Crossroad:
				return "OnCross";
			case ENodeType.Hospital:
				return "OnHospital";
			case ENodeType.Cache:
				return "OnShop";
			case ENodeType.Store:
				return "OnTrasure";
			case ENodeType.Watchtower:
				return "OnTower";
			case ENodeType.Inn:
				return "OnInn";
		}

		return null;
	}
}

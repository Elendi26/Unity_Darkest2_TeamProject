using UnityEngine;

// 런 기록을 팀 세이브(프로필 파일)에 넣고 꺼낸다 → 세이브 파일은 DataSaveManager 하나로 관리
public static class CRunProfileSave
{
	// 씬이 열리기 전에 구독 → 씬에 오브젝트를 놓지 않아도 된다
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Init()
	{
		// 도메인 리로드를 꺼 둬도 두 번 붙지 않게
		CRunCarry.OnRunSaved -= HandleRunSaved;
		CRunCarry.OnRunSaved += HandleRunSaved;
	}

	private static void HandleRunSaved(CRunSaveData save)
	{
		UserProfileData profile = LoadProfile(CTutorialProgress.CurrentSlot);

		if (profile == null)
		{
			CPrint.Warn("CRunProfileSave : 고른 프로필이 없어 런을 파일에 저장하지 않습니다. (타이틀에서 프로필을 고르고 시작하세요)");
			return;
		}

		profile.Run = save;
		DataSaveManager.Instance.SaveProfile(profile, CTutorialProgress.CurrentSlot);
	}

	// 이어할 런이 있으면 꺼낸다
	public static CRunSaveData LoadRun(int slot)
	{
		UserProfileData profile = LoadProfile(slot);

		return profile != null ? profile.Run : null;
	}

	// 새 모험을 시작하면 지난 런을 지운다
	public static void ClearRun(int slot)
	{
		UserProfileData profile = LoadProfile(slot);

		if (profile == null || profile.Run == null)
		{
			return;
		}

		profile.Run = null;
		DataSaveManager.Instance.SaveProfile(profile, slot);
	}

	public static UserProfileData LoadProfile(int slot)
	{
		if (slot < 0 || DataSaveManager.Instance == null)
		{
			return null;
		}

		return DataSaveManager.Instance.LoadProfile(slot);
	}
}

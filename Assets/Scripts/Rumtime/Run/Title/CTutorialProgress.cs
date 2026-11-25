// 프로필별 튜토리얼 완료 여부 → 팀 세이브(UserProfileData.TutorialDone)에 함께 저장한다
public static class CTutorialProgress
{
	// 지금 고른 프로필 슬롯 (프로필 없이 재생하면 -1)
	public static int CurrentSlot => DataSaveManager.Instance != null ? DataSaveManager.Instance.CurrentSlotIndex : -1;

	public static bool IsDone(int slot)
	{
		UserProfileData profile = CRunProfileSave.LoadProfile(slot);

		return profile != null && profile.TutorialDone;
	}

	public static void MarkDone(int slot)
	{
		SetDone(slot, true);
	}

	// 원정 포기 → 다음 모험을 튜토리얼부터
	public static void ResetDone(int slot)
	{
		SetDone(slot, false);
	}

	private static void SetDone(int slot, bool done)
	{
		UserProfileData profile = CRunProfileSave.LoadProfile(slot);

		if (profile == null)
		{
			CPrint.Warn("CTutorialProgress : 프로필이 없어 튜토리얼 완료 여부를 기록하지 않습니다.");
			return;
		}

		if (profile.TutorialDone == done)
		{
			return;
		}

		profile.TutorialDone = done;
		DataSaveManager.Instance.SaveProfile(profile, slot);
	}
}

using System;
using System.Collections.Generic;
using UnityEngine;

// 파티 영웅들의 체력을 최대 체력의 일정 비율만큼 채우고 스트레스를 덜어 준다 (병원 등)
//  ㄴ 영웅 체력·스트레스는 팀 세이브(프로필 파티)에 있다 → 프로필 값을 고쳐 저장하고 파티 칸을 다시 그린다
//  ㄴ 쓰러진(체력 0) 영웅은 되살리지 않는다
[Serializable]
public class CPartyHealEffect : IRunEffect
{
	#region 인스펙터
	// 최대 체력의 몇 %를 채울지 (0~1)
	[SerializeField, Range(0f, 1f)] private float _hpPercent = 0.5f;
	// 덜어 줄 스트레스
	[SerializeField] private int _stressRelief = 0;
	// 영웅 원본 데이터(최대 체력)를 찾을 Resources 폴더
	[SerializeField] private string _heroDataPath = "EntityBasicDataSO";
	#endregion

	public void Apply(CRunContext ctx)
	{
		DataSaveManager saveManager = DataSaveManager.Instance;

		if (saveManager == null || saveManager.CurrentSlotIndex < 0)
		{
			CPrint.Warn("CPartyHealEffect : 프로필이 없어 파티를 치료하지 않습니다.");
			return;
		}

		UserProfileData profile = saveManager.LoadProfile(saveManager.CurrentSlotIndex);

		if (profile == null || profile.PartyHeroes == null)
		{
			return;
		}

		Dictionary<string, CEntityBasicDataSO> heroes = LoadHeroData();
		int healed = 0;

		for (int i = 0; i < profile.PartyHeroes.Count; i++)
		{
			UserProfileData.HeroSaveData save = profile.PartyHeroes[i];

			if (save == null || save.PartySlotIndex < 0 || save.CurrentHP <= 0)
			{
				continue;
			}

			if (!heroes.TryGetValue(save.HeroID, out CEntityBasicDataSO data))
			{
				continue;
			}

			int maxHp = Mathf.Max(1, data.MaxHP);
			int hp = Mathf.Min(maxHp, save.CurrentHP + Mathf.CeilToInt(maxHp * _hpPercent));
			int stress = Mathf.Max(0, save.CurrentStress - _stressRelief);

			if (hp == save.CurrentHP && stress == save.CurrentStress)
			{
				continue;
			}

			save.CurrentHP = hp;
			save.CurrentStress = stress;
			healed++;
		}

		if (healed == 0)
		{
			return;
		}

		saveManager.SaveProfile(profile, saveManager.CurrentSlotIndex);

		// 주행 화면 아래 파티 칸의 체력·스트레스 표시를 바로 바꾼다
		CPartyBarView partyBar = UnityEngine.Object.FindFirstObjectByType<CPartyBarView>();

		if (partyBar != null)
		{
			partyBar.Refresh();
		}

		CPrint.Log($"CPartyHealEffect : 영웅 {healed}명 치료 (체력 +{Mathf.RoundToInt(_hpPercent * 100f)}%, 스트레스 -{_stressRelief})");
	}

	private Dictionary<string, CEntityBasicDataSO> LoadHeroData()
	{
		Dictionary<string, CEntityBasicDataSO> heroes = new Dictionary<string, CEntityBasicDataSO>();
		CEntityBasicDataSO[] all = Resources.LoadAll<CEntityBasicDataSO>(_heroDataPath);

		for (int i = 0; i < all.Length; i++)
		{
			if (all[i] == null || string.IsNullOrEmpty(all[i].HeroID) || heroes.ContainsKey(all[i].HeroID))
			{
				continue;
			}

			heroes.Add(all[i].HeroID, all[i]);
		}

		return heroes;
	}
}

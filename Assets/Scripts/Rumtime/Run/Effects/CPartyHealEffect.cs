using System;
using System.Collections.Generic;
using UnityEngine;

// 파티 영웅들의 체력을 최대 체력의 일정 비율만큼 채운다
[Serializable]
public class CPartyHealEffect : IRunEffect, IRunEffectText
{
	#region 인스펙터
	// 최대 체력의 몇 %를 채울지 (0~1)
	[SerializeField, Range(0f, 1f)] private float _hpPercent = 0.5f;
	// 비율과 함께 더 채울 고정 체력
	[SerializeField] private int _hpFlat = 0;
	// 덜어 줄 스트레스
	[SerializeField] private int _stressRelief = 0;
	// 가장 필요한 영웅 1명만 (체력을 채우면 가장 다친 영웅, 아니면 스트레스가 가장 높은 영웅)
	[SerializeField] private bool _singleTarget = false;
	// 영웅 원본 데이터(최대 체력)를 찾을 Resources 폴더
	[SerializeField] private string _heroDataPath = "EntityBasicDataSO";
	#endregion

	public CPartyHealEffect()
	{
		// Do Nothing
	}

	// 코드에서 고정 체력만 채울 때
	public CPartyHealEffect(int hpFlat)
	{
		_hpPercent = 0f;
		_hpFlat = hpFlat;
	}

	private bool HealsHp => _hpPercent > 0f || _hpFlat > 0;

	public string GetText()
	{
		List<string> parts = new List<string>();

		if (_hpPercent > 0f)
		{
			parts.Add($"체력 {Mathf.RoundToInt(_hpPercent * 100f)}% 회복");
		}

		if (_hpFlat > 0)
		{
			parts.Add($"체력 +{_hpFlat}");
		}

		if (_stressRelief > 0)
		{
			parts.Add($"스트레스 -{_stressRelief}");
		}

		string text = string.Join(", ", parts);

		if (_singleTarget)
		{
			text = $"{(HealsHp ? "가장 다친" : "스트레스가 가장 높은")} 영웅 1명 {text}";
		}

		return text;
	}

	// 지금 효과를 받을 영웅이 있는지 → 없으면 아이템을 써도 낭비라 막는다
	public bool HasTarget()
	{
		UserProfileData profile = LoadProfile();

		if (profile == null)
		{
			return false;
		}

		Dictionary<string, CEntityBasicDataSO> heroes = LoadHeroData();

		for (int i = 0; i < profile.PartyHeroes.Count; i++)
		{
			if (GetNeed(profile.PartyHeroes[i], heroes) > 0f)
			{
				return true;
			}
		}

		return false;
	}

	public void Apply(CRunContext ctx)
	{
		UserProfileData profile = LoadProfile();

		if (profile == null)
		{
			CPrint.Warn("CPartyHealEffect : 프로필이 없어 파티를 치료하지 않습니다.");
			return;
		}

		Dictionary<string, CEntityBasicDataSO> heroes = LoadHeroData();
		UserProfileData.HeroSaveData single = _singleTarget ? FindNeediest(profile, heroes) : null;
		int healed = 0;

		for (int i = 0; i < profile.PartyHeroes.Count; i++)
		{
			UserProfileData.HeroSaveData save = profile.PartyHeroes[i];

			if (_singleTarget && save != single)
			{
				continue;
			}

			if (GetNeed(save, heroes) <= 0f)
			{
				continue;
			}

			int maxHp = Mathf.Max(1, heroes[save.HeroID].MaxHP);

			save.CurrentHP = Mathf.Min(maxHp, save.CurrentHP + Mathf.CeilToInt(maxHp * _hpPercent) + Mathf.Max(0, _hpFlat));
			save.CurrentStress = Mathf.Max(0, save.CurrentStress - _stressRelief);
			healed++;
		}

		if (healed == 0)
		{
			return;
		}

		DataSaveManager saveManager = DataSaveManager.Instance;

		saveManager.SaveProfile(profile, saveManager.CurrentSlotIndex);
		RefreshViews();

		CPrint.Log($"CPartyHealEffect : 영웅 {healed}명 치료 ({GetText()})");
	}

	// 0 이면 받을 게 없다. 클수록 더 필요하다 (체력은 잃은 비율, 스트레스는 쌓인 양)
	private float GetNeed(UserProfileData.HeroSaveData save, Dictionary<string, CEntityBasicDataSO> heroes)
	{
		if (save == null || save.PartySlotIndex < 0 || save.CurrentHP <= 0)
		{
			return 0f;
		}

		if (!heroes.TryGetValue(save.HeroID, out CEntityBasicDataSO data))
		{
			return 0f;
		}

		int maxHp = Mathf.Max(1, data.MaxHP);
		float hpNeed = HealsHp && save.CurrentHP < maxHp ? 1f - (float)save.CurrentHP / maxHp : 0f;
		float stressNeed = _stressRelief > 0 ? save.CurrentStress : 0f;

		// 체력을 채우는 효과는 체력 쪽을 먼저 본다
		if (_singleTarget && HealsHp)
		{
			return hpNeed;
		}

		return hpNeed + stressNeed;
	}

	private UserProfileData.HeroSaveData FindNeediest(UserProfileData profile, Dictionary<string, CEntityBasicDataSO> heroes)
	{
		UserProfileData.HeroSaveData best = null;
		float bestNeed = 0f;

		for (int i = 0; i < profile.PartyHeroes.Count; i++)
		{
			float need = GetNeed(profile.PartyHeroes[i], heroes);

			if (need > bestNeed)
			{
				bestNeed = need;
				best = profile.PartyHeroes[i];
			}
		}

		return best;
	}

	private static UserProfileData LoadProfile()
	{
		DataSaveManager saveManager = DataSaveManager.Instance;

		if (saveManager == null || saveManager.CurrentSlotIndex < 0)
		{
			return null;
		}

		UserProfileData profile = saveManager.LoadProfile(saveManager.CurrentSlotIndex);

		return profile != null && profile.PartyHeroes != null ? profile : null;
	}

	// 주행 화면 아래 파티 칸과 여관 의자 이름표의 체력·스트레스 표시를 바로 바꾼다
	private static void RefreshViews()
	{
		CPartyBarView partyBar = UnityEngine.Object.FindFirstObjectByType<CPartyBarView>();

		if (partyBar != null)
		{
			partyBar.Refresh();
		}

		CInnPartyView innParty = UnityEngine.Object.FindFirstObjectByType<CInnPartyView>();

		if (innParty != null)
		{
			innParty.RefreshLabels();
		}
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

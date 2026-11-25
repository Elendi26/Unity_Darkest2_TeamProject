using System;
using UnityEngine;

// 유물 → 짐칸을 차지하는 화폐. 칸에 겹친 개수에 따라 아이콘이 자루 → 상자로 커진다
[CreateAssetMenu(menuName = "Item/Relic_")]
public class CRelicItemData : CItemDataBase
{
	// 이 개수 이상이면 이 아이콘
	[Serializable]
	public class CIconStep
	{
		public int Min;
		public Sprite Icon;
	}

	#region 인스펙터
	// 겹친 개수별 아이콘 (비우면 기본 아이콘 하나)
	[SerializeField] private CIconStep[] _iconSteps;
	#endregion

	public override Sprite GetIcon(int count)
	{
		Sprite icon = Icon;

		if (_iconSteps == null)
		{
			return icon;
		}

		// 조건을 만족하는 단계 중 가장 높은 것
		int best = int.MinValue;

		for (int i = 0; i < _iconSteps.Length; i++)
		{
			CIconStep step = _iconSteps[i];

			if (step == null || step.Icon == null || count < step.Min || step.Min < best)
			{
				continue;
			}

			best = step.Min;
			icon = step.Icon;
		}

		return icon;
	}

	public override string GetDetailNote(CRunData runData)
	{
		return "(상점에서 값을 치를 때 쓴다)";
	}
}

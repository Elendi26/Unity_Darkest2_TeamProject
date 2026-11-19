using System;
using UnityEngine;

// 정찰 → 앞쪽의 가려진 지점·경로를 몇 곳 밝힌다
[Serializable]
public class CScoutEffect : IRunEffect
{
	#region 인스펙터
	// 밝히는 지점·경로 개수
	[SerializeField] private int _count = 3;
	#endregion

	public void Apply(CRunContext ctx)
	{
		if (ctx == null || ctx.Map == null)
		{
			CPrint.Warn("CScoutEffect : 지도가 없어 정찰할 수 없습니다.");
			return;
		}

		ctx.Map.ScoutRandom(_count, ctx.Random);
	}
}

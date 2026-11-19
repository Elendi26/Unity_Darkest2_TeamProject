using System;

// 도로 이벤트에서 전투로 넘어갈 때 부르는 곳 → 전투 쪽 컴포넌트가 구현한다
public interface IRoadCombatStarter
{
	void StartCombat(CRoadEventData roadEvent, CRunContext ctx, Action onFinished);
}

// 전투 쪽이 아직 연결되지 않았을 때 쓰는 임시 처리 → 로그만 남기고 바로 지나간다
public class CPendingRoadCombatStarter : IRoadCombatStarter
{
	public void StartCombat(CRoadEventData roadEvent, CRunContext ctx, Action onFinished)
	{
		// TODO : 전투 시스템(CCombatRuntimeManager 등)에 IRoadCombatStarter 를 구현해 CStagecoachController 의 Combat Starter Source 에 연결 / 경태
		CPrint.Warn($"도로 전투 '{roadEvent.CombatId}' 가 아직 연결되지 않아 전투 없이 지나갑니다.");
		onFinished?.Invoke();
	}
}

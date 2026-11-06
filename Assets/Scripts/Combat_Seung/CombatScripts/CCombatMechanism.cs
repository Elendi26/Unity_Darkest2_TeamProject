using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CCombatMechanism : MonoBehaviour
{
    private CCombatData _combatData;
    private CHeroTeamData _heroTeamData;
    private CEnemyGroupCaller _enemyGroupCaller;

    private CTeamData<CEntityRuntimeData> _heroRuntimeData = new CTeamData<CEntityRuntimeData>(4);
    private CTeamData<CEntityRuntimeData> _enemyRuntimeData = new CTeamData<CEntityRuntimeData>(4);

    public void CombatStart()
    {
        // 영웅의 런타임 데이터 및 적 그룹 데이터를 받아옴
        // 여기서 영웅 팀 / 적 그룹 데이터를 받아서 각 랭크에 붙여넣는 작업까지

        _heroRuntimeData = _heroTeamData.GetTeam();
        _enemyRuntimeData = _enemyGroupCaller.EnemyGroupCaller(0, 0, 0);
    }

    public void RoundStart()
    {
        // 속도 기반으로 턴 설정
    }

    public void TurnStart()
    {
        // 턴 시작
        // 스킬 사용
    }

    public void TurnOver()
    {
        // 턴 종료
        // 다음 턴을 가진 대상의 턴 시작
    }

    public void CombatOver()
    {
        // 전투 종료
    }
}

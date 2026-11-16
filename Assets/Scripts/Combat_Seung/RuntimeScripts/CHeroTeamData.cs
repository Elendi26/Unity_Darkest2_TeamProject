using UnityEngine;

public class CHeroTeamData : MonoBehaviour
{
    // 히어로 팀의 데이터를 가지고 있을 클래스
    // 먼저 교차로에서 단순 인덱스로만 구분을 해 놓고, 게임 진입 시 인덱스 기반으로, 오른쪽에서부터 AddHero를 하면 OK

    private CTeamData<CEntityRuntimeData> _teamSlots = new CTeamData<CEntityRuntimeData>(4);

    public CTeamData<CEntityRuntimeData> GetTeam()
    {
        return _teamSlots;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CCombatRuntimeData : MonoBehaviour
{
    [SerializeField] private CCombatRank[] _teamRanks = new CCombatRank[4];
    [SerializeField] private CCombatRank[] _enemyRanks = new CCombatRank[4];

    private CTeamData<CEntityRuntimeData> _teamData;
    private CTeamData<CEntityRuntimeData> _enemyData;

    private CEnemyGroupCaller _enemyGroupCaller;

    // 영웅 외에, 공통 데이터들
    // EX) 횃불이나 뭐 적에게 유리한 전투 이런 것들? 전투에서만 사용되는 것들

    private void OnEnable()
    {
        InputTeamData();
        InputEnemyData();
    }

    public void InputTeamData()
    {
        _teamData = CTeamRuntimeData.Instance.GetTeamData();

        for (int i = 0;  i < _teamRanks.Length; i++)
        {
            if (_teamData.Get(i) is CEntityRuntimeData entity)
            {
                _teamRanks[i].InputEntityData(entity);
            }

            else
            {
                continue;
            }
        }
    }

    public void InputEnemyData()
    {

    }

}

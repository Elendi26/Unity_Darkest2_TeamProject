using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CEnemyGroupCaller : MonoBehaviour
{
    // EnemyGroup의 데이터베이스 및 조건에 따른 랜덤 생성

    private CTeamData<CEntityRuntimeData> _enemyGroupData;

    public CTeamData<CEntityRuntimeData> EnemyGroupCaller(int index, int mapIndex, int typeIndex)
    {


        return _enemyGroupData;
    }
}

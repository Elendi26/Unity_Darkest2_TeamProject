using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CEnemyGroupData : MonoBehaviour
{
    [SerializeField] private CEnemyGroupCaller _caller;
    private CEnemyGroupDataSO _enemyGroupDataSO;
    private CTeamData<CEntityRuntimeData> _entityRuntimeData;

}

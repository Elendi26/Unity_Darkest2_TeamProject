using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CCombatData : MonoBehaviour
{
    [SerializeField] private CCombatRank[] _heroRanks = new CCombatRank[4];
    [SerializeField] private CCombatRank[] _enemyRanks = new CCombatRank[4];

    // 영웅 외에, 공통 데이터들
    // EX) 횃불이나 뭐 적에게 유리한 전투 이런 것들? 전투에서만 사용되는 것들
}

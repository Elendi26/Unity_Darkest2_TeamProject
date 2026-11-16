using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CCombatRuntimeManager : MonoBehaviour
{
    [SerializeField] private CCombatCurrentTurn _currentTurn;
    [SerializeField] private List<CEntityRuntimeData> _entityTurn; // 열어둘 필요 없으나 테스트용

    private CCombatState _combatState;

    private void OnEnable()
    {
        // 데이터 로드 이후 각 팀의 
    }
}

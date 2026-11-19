using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 전투 총괄 매니저
public class CCombatStateManager : MonoBehaviour
{
    public static CCombatStateManager Instance;

    #region 인스펙터

    [SerializeField] private List<CEntityRuntimeData> _onCombatEntities = new List<CEntityRuntimeData>();
    [SerializeField] private CTeamData<CEntityRuntimeData> _testEnemies;
    [SerializeField] private CCombatCurrentTurn _currentTurn;

    #endregion

    private List<CEntityRuntimeData> _orderedTurn = new List<CEntityRuntimeData>();

    private CCombatState _currentState;

    private bool _roundOver = false;

    #region 프로퍼티

    public List<CEntityRuntimeData> OnCombatEntities => _onCombatEntities;
    public List<CEntityRuntimeData> OrderedTurn
    {
        get => _orderedTurn;
        set => _orderedTurn = value;
    }
    public CCombatCurrentTurn CombatCurrentTurn => _currentTurn;

    #endregion

    private void Awake()
    {
        // 첫 시작 시 라운드 시작 상태로 진입
        ChangeState(new CCombatState_RoundStart(this));
    }

    public void ChangeState(CCombatState nextState)
    {
        if (_currentState != null)
        {
            _currentState.Exit();
        }

        _currentState = nextState;

        if (_currentState != null)
        {
            _currentState.Enter();
        }
    }

    public void InputToCurrentTurn()
    {
        if (_orderedTurn.Count <= 0)
        {
            _roundOver = true;
        }

        else
        {
            _roundOver = false;
        }

        _currentTurn.InputCurrentTurnEntity(_orderedTurn.First());

        _orderedTurn.RemoveAt(0);
    }

    public void TurnOver()
    {
        ChangeState(new CCombatState_TurnEnd(this));
    }
}


// 상태 패턴의 부모가 될 추상 클래스
public abstract class CCombatState
{
    protected CCombatStateManager _manager;
    public CCombatState(CCombatStateManager manager)
    {
        _manager = manager;
    }

    public abstract void Enter();
    public abstract void Exit();
}


// 라운드 시작
public class CCombatState_RoundStart : CCombatState
{
    // 부모 생성자 호출 필수
    public CCombatState_RoundStart(CCombatStateManager manager) : base(manager) { }

    public override void Enter()
    {
        _manager.OrderedTurn.Clear();

        _manager.OrderedTurn = OrderTurn();

        _manager.ChangeState(new CCombatState_TurnStart(_manager));
    }

    private List<CEntityRuntimeData> OrderTurn()
    {
        var entities = _manager.OnCombatEntities;
        int[] extraSpeed = new int[entities.Count];

        for (int i = 0; i < extraSpeed.Length; i++)
        {
            extraSpeed[i] = Random.Range(0, 6);
        }

        for (int i = 0; i < entities.Count; i++)
        {
            entities[i].ModifyStat(EEntityStat.CurrentSpeed, entities[i].GetStat(EEntityStat.Speed) + extraSpeed[i]);
        }

        List<CEntityRuntimeData> sortEntities = entities.OrderByDescending(x => x.GetStat(EEntityStat.CurrentSpeed)).ToList();

        for (int i = 0; i < entities.Count; i++)
        {
            entities[i].ResetStatToDefault(EEntityStat.CurrentSpeed);
        }

        return sortEntities;
    }

    public override void Exit()
    {

    }
}


// 턴 시작 상태
public class CCombatState_TurnStart : CCombatState
{
    public CCombatState_TurnStart(CCombatStateManager manager) : base(manager) { }

    public override void Enter()
    {
        _manager.InputToCurrentTurn();
    }


    public override void Exit()
    {

    }
}

// 턴 끝 상태
public class CCombatState_TurnEnd : CCombatState
{
    public CCombatState_TurnEnd(CCombatStateManager manager) : base(manager) { }

    public override void Enter()
    {
        if (_manager.OrderedTurn.Count >= 1)
        {
            _manager.ChangeState(new CCombatState_TurnStart(_manager));
        }
    }

    public void TurnEndToken()
    {
        
    }

    public override void Exit()
    {

    }
}

public class CCombatState_CombatEnd : CCombatState
{
    public CCombatState_CombatEnd(CCombatStateManager manager) : base(manager) { }

    public override void Enter()
    {

    }

    public void TurnEndToken()
    {

    }

    public override void Exit()
    {

    }
}
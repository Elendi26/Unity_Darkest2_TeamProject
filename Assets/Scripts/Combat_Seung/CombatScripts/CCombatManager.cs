using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ECombatPhase
{
    None,
    CombatStart,
    RoundStart,
    TurnStart,
    TurnOver,
    RoundOver,
    CombatOver
}

// 전투 총괄 매니저
public class CCombatManager : MonoBehaviour
{
    public static CCombatManager Instance { get; private set; }

    #region 인스펙터

    [SerializeField] private List<CEntityRuntimeData> _onCombatEntities = new List<CEntityRuntimeData>();
    [SerializeField] private CTeamData<CEntityRuntimeData> _testEnemies;
    [SerializeField] private CCombatCurrentTurn _currentTurn;

    #endregion

    #region 내부변수

    private List<CEntityRuntimeData> _orderedTurn = new List<CEntityRuntimeData>();

    private ECombatPhase _currentPhase = ECombatPhase.CombatStart;

    private bool _roundOver = false;

    #endregion

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
        CombatMechanism();
    }

    private void CombatMechanism()
    {
        switch (_currentPhase)
        {
            case ECombatPhase.None:

                Debug.Log("컴뱃 페이즈가 비었음");

                break;

            case ECombatPhase.CombatStart:

                CombatStart();

                break;

            case ECombatPhase.RoundStart:

                RoundStart();

                break;

            case ECombatPhase.TurnStart:

                TurnStart();

                break;

            case ECombatPhase.TurnOver:

                TurnOver();

                break;

            case ECombatPhase.RoundOver:

                break;

            case ECombatPhase.CombatOver:

                break;

            default:

                Debug.Log("컴뱃 페이즈 오류");

                break;
        }
    }



    public void ChangePhase(ECombatPhase nextPhase)
    {
        _currentPhase = nextPhase;

        CombatMechanism();
    }

    private void CombatStart()
    {
        // 데이터 로딩

        ChangePhase(ECombatPhase.RoundStart);
    }


    private void RoundStart()
    {
        OrderTurn();

        ChangePhase(ECombatPhase.TurnStart);
    }

    private List<CEntityRuntimeData> OrderTurn()
    {
        var entities = _onCombatEntities;
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


    private void TurnStart()
    {
        InputToCurrentTurn();
    }

    private void InputToCurrentTurn()
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
        if (_orderedTurn.Count >= 1)
        {
            ChangePhase(ECombatPhase.TurnStart);
        }
    }


}


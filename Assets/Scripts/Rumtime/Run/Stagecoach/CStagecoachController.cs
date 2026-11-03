using System;
using UnityEngine;

public class CStagecoachController : MonoBehaviour, IRoadTravelProvider
{
    #region 인스펙터
    [SerializeField] private CStagecoachConfig _config;
    [SerializeField] private int _startGold = 0;
    [SerializeField] private int _startFaction = 0;
    [SerializeField] private int _startTorch = 100;
    [SerializeField] private int _maxTorch = 100;
    [SerializeField] private int _seed = 0;
    [SerializeField] private MonoBehaviour _eventResolverSource;
    #endregion

    #region 내부 변수
    private CRunData _runData;
    private CStagecoach _stagecoach;
    private CRoadTravel _travel;
    #endregion

    // 역마차 설정 (주행 거리 등)
    public CStagecoachConfig Config => _config;
    public CRunData RunData => _runData;
    public CStagecoach Stagecoach => _stagecoach;
    public CRoadTravel Travel => _travel;
    IRoadTravel IRoadTravelProvider.Travel => _travel;

    private void Awake()
    {
        if (_config == null)
        {
            enabled = false;
            return;
        }

        _runData = new CRunData();
        _runData.Register(ERunValue.Gold, _startGold, int.MaxValue);
        _runData.Register(ERunValue.Faction, _startFaction, int.MaxValue);
        _runData.Register(ERunValue.Torch, _startTorch, _maxTorch);

        _stagecoach = new CStagecoach(_config, _runData);

        int seed = _seed != 0 ? _seed : Environment.TickCount;
        CRunContext ctx = new CRunContext
        {
            RunData = _runData,
            Random = new System.Random(seed),
        };

        // 런 데이터(CRunContext)는 역마차를 모른다 → 역마차는 주행에 따로 넘긴다
        _travel = new CRoadTravel(_config, _stagecoach, ctx, GetEventResolver());
    }

    private void OnValidate()
    {
        if (_eventResolverSource != null && !(_eventResolverSource is IRoadEventResolver))
        {
            CPrint.Warn($"CStagecoachController : {_eventResolverSource.name} 는 IRoadEventResolver 가 아닙니다.");
        }
    }

    private IRoadEventResolver GetEventResolver()
    {
        if (_eventResolverSource is IRoadEventResolver resolver)
        {
            return resolver;
        }

        return new CAutoRoadEventResolver();
    }
}

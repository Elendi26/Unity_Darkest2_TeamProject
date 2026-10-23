using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class CMapController : MonoBehaviour
{
    #region 인스펙터
    [SerializeField] private CMapConfig _config;
    // IRoadTravelProvider 를 구현한 컴포넌트 (역마차 등). 비우면 이동 없이 바로 도착
    [SerializeField, FormerlySerializedAs("_stagecoachController")] private MonoBehaviour _travelSource;
    [SerializeField] private MonoBehaviour[] _handlerSources;	// INodeHandler 를 구현한 컴포넌트 (전투·상점 등). 없는 종류는 통과 처리
    [SerializeField] private int _seed = 0;
    #endregion

    #region 이벤트
    // 런너가 만들어졌을 때 (맵 UI 가 구독)
    public event Action<CMapRunner> OnRunnerCreated;
    #endregion

    #region 내부 변수
    private CMapRunner _runner;
    #endregion

    // 진행 중인 맵 런너
    public CMapRunner Runner => _runner;

    // 연결한 컴포넌트가 이동 담당이 아니면 에디터에서 바로 알린다
    private void OnValidate()
    {
        if (_travelSource != null && !(_travelSource is IRoadTravelProvider))
        {
            CPrint.Warn($"CMapController : {_travelSource.name} 은 IRoadTravelProvider 가 아닙니다.");
        }
    }

    private void Start()
    {
        int seed = _seed != 0 ? _seed : Environment.TickCount;
        CMapGraph graph = new CMapGenerator(_config).Generate(seed);

        if (graph == null)
        {
            return;
        }

        _runner = new CMapRunner(graph, GetTravel());

        RegisterSceneHandlers();
        RegisterPassHandlers();

        OnRunnerCreated?.Invoke(_runner);
        _runner.Begin();
    }

    // IRoadTravelProvider 인터페이스로만 이동 담당을 꺼낸다
    private IRoadTravel GetTravel()
    {
        if (_travelSource == null)
        {
            CPrint.Warn("CMapController : 이동 담당이 연결되지 않아 이동 없이 바로 도착합니다.");
            return null;
        }

        IRoadTravelProvider provider = _travelSource as IRoadTravelProvider;

        if (provider == null)
        {
            CPrint.Error($"CMapController : {_travelSource.name} 은 IRoadTravelProvider 가 아닙니다.");
            return null;
        }

        return provider.Travel;
    }

    private void RegisterSceneHandlers()
    {
        if (_handlerSources == null)
        {
            return;
        }

        for (int i = 0; i < _handlerSources.Length; i++)
        {
            // 빈 칸은 조용히 넘긴다
            if (_handlerSources[i] == null)
            {
                continue;
            }

            INodeHandler handler = _handlerSources[i] as INodeHandler;

            // 잘못 넣은 컴포넌트는 알린다
            if (handler == null)
            {
                CPrint.Warn($"CMapController : {_handlerSources[i]} 은 INodeHandler 가 아닙니다.");
                continue;
            }

            _runner.RegisterHandler(handler);
        }
    }

    private void RegisterPassHandlers()
    {
        ENodeType[] types = (ENodeType[])Enum.GetValues(typeof(ENodeType));

        for (int i = 0; i < types.Length; i++)
        {
            if (types[i] == ENodeType.None || _runner.HasHandler(types[i]))
            {
                continue;
            }

            _runner.RegisterHandler(new CPassNodeHandler(types[i]));
        }
    }
}

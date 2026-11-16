using System;
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
    // 노드 이벤트를 띄울 팝업. 비우면 씬에서 찾는다
    [SerializeField] private CRoadEventPopup _eventPopup;
    // 상점 화면. 비우면 씬에서 찾는다
    [SerializeField] private CShopView _shopView;
    #endregion

    #region 이벤트
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
        RegisterEventHandlers();
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

    // 이벤트가 붙은 노드 종류는 팝업 핸들러로 처리한다
    private void RegisterEventHandlers()
    {
        CRunContext ctx = _travelSource is IRunContextProvider provider ? provider.Context : null;

        if (_eventPopup == null)
        {
            _eventPopup = FindFirstObjectByType<CRoadEventPopup>(FindObjectsInactive.Include);
        }

        if (_shopView == null)
        {
            _shopView = FindFirstObjectByType<CShopView>(FindObjectsInactive.Include);
        }

        if (ctx == null)
        {
            CPrint.Warn("CMapController : 런 데이터가 없어 노드 이벤트 없이 통과합니다.");
            return;
        }

        RegisterEventHandler(_config.StarNode, ctx);
        RegisterEventHandler(_config.PreStartNode, ctx);
        RegisterEventHandler(_config.EndNode, ctx);
        RegisterEventHandler(_config.PreEndNode, ctx);
        RegisterEventHandler(_config.fallbackNode, ctx);

        if (_config.NodePool == null)
        {
            return;
        }

        for (int i = 0; i < _config.NodePool.Count; i++)
        {
            RegisterEventHandler(_config.NodePool[i], ctx);
        }
    }

    private void RegisterEventHandler(CNodeTypeData data, CRunContext ctx)
    {
        if (data == null || _runner.HasHandler(data.Type))
        {
            return;
        }

        CStagecoach stagecoach = _travelSource is IStagecoachProvider provider ? provider.Stagecoach : null;

        // 상점 화면이 있으면 상점이 이벤트 팝업보다 먼저
        if (data.Shop != null && _shopView != null)
        {
            _runner.RegisterHandler(new CShopNodeHandler(data.Type, data.Shop, _shopView, ctx, stagecoach));
            return;
        }

        if (data.Event != null && _eventPopup != null)
        {
            _runner.RegisterHandler(new CNodeEventHandler(data.Type, data.Event, _eventPopup, ctx, stagecoach));
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

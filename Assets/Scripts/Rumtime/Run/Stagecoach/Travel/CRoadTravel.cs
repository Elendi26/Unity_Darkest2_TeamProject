using System;
using System.Collections.Generic;
using UnityEngine;

public class CRoadTravel : IRoadTravel
{
    #region 이벤트
    public event Action<ETravelState, ETravelState> OnStateChanged;
    public event Action<float> OnProgress;
    public event Action<CRoadEventData> OnRoadEventStarted;
    #endregion

    #region 내부 변수
    private readonly CStagecoachConfig _config;
    private readonly CStagecoach _stagecoach;
    private readonly CRunContext _ctx;
    private readonly IRoadEventResolver _eventResolver;
    private readonly List<CScheduledEvent> _schedule = new List<CScheduledEvent>();
    private ETravelState _state = ETravelState.None;
    private CNodeLink _link;
    private Action _onArrived;
    private float _progress;
    private int _nextEventIndex;
    private int _torchDrainTotal;
    private int _torchDrained;
    private bool _logState = true;
    #endregion

    private class CScheduledEvent
    {
        public float Position;
        public CRoadEventData Data;
    }

    public ETravelState State => _state;
    public float Progress => _progress;
    public bool IsTraveling => _state != ETravelState.None;
    // 이번 경로에 잡힌 도로 이벤트 수
    public int EventCount => _schedule.Count;

    public float GetEventPosition(int index)
    {
        if (index < 0 || index >= _schedule.Count)
        {
            return -1f;
        }

        return _schedule[index].Position;
    }

    public CRoadTravel(CStagecoachConfig config, CStagecoach stagecoach, CRunContext ctx, IRoadEventResolver eventResolver)
    {
        _config = config;
        _stagecoach = stagecoach;
        _ctx = ctx;
        _eventResolver = eventResolver;
    }

    public void Travel(CNodeLink link, Action onArrived)
    {
        if (link == null)
        {
            CPrint.Error("경로가 없습니다.");
            onArrived?.Invoke();
            return;
        }

        if (IsTraveling)
        {
            CPrint.Warn("이미 주행 중입니다.");
            return;
        }

        _link = link;
        _onArrived = onArrived;
        _progress = 0f;
        _nextEventIndex = 0;
        _torchDrained = 0;

        SetState(ETravelState.Departing);
        Depart();
        SetState(ETravelState.Driving);
    }

    // 주행 화면이 달린 거리(m)를 넣는다
    public void Advance(float meters)
    {
        if (_state != ETravelState.Driving || meters <= 0f)
        {
            return;
        }

        AdvanceProgress(meters / Mathf.Max(0.01f, _config.TravelDistance));
    }

    private void AdvanceProgress(float delta)
    {
        _progress = Mathf.Min(1f, _progress + delta);
        TickTorchDrain();
        OnProgress?.Invoke(_progress);

        if (_nextEventIndex < _schedule.Count && _progress >= _schedule[_nextEventIndex].Position)
        {
            CRoadEventData data = _schedule[_nextEventIndex].Data;

            _nextEventIndex++;
            StartRoadEvent(data);
            return;
        }

        if (_progress >= 1f)
        {
            Arrive();
        }
    }

    private void Depart()
    {
        ApplyEffects(_link.RouteData != null ? _config.GetRouteEffects(_link.RouteData.Type) : null);
        ScheduleEvents(_link.RoadEventCount);

        float multiplier = Mathf.Max(0f, 1f + _stagecoach.GetStat(ERunStat.TorchDrainPercent));

        _torchDrainTotal = Mathf.RoundToInt(_config.TorchDrainPerTravel * multiplier);
    }

    private void ScheduleEvents(int count)
    {
        _schedule.Clear();

        // 길 종류마다 정해진 이벤트 (험한 길 → 바퀴 손상 등)
        //  ㄴ 이 이벤트가 있는 길은 무작위 이벤트를 넣지 않는다 → 길당 한 번만
        CRouteTypeData route = _link.RouteData;

        if (route != null && route.PassEvent != null)
        {
            _schedule.Add(new CScheduledEvent { Position = route.PassEventPosition, Data = route.PassEvent });
            return;
        }

        if (count > _config.EventPositions.Count)
        {
            CPrint.Once("road_event_overflow", $"이벤트 {count}개 중 위치가 정해진 {_config.EventPositions.Count}개만 배치합니다.");
        }

        for (int i = 0; i < count && i < _config.EventPositions.Count; i++)
        {
            CRoadEventData data = PickRoadEvent();

            if (data == null)
            {
                continue;
            }

            float jitter = (float)(_ctx.Random.NextDouble() * 2.0 - 1.0) * _config.EventPositionJitter;
            float position = Mathf.Clamp(_config.EventPositions[i] + jitter, 0.05f, 0.99f);

            _schedule.Add(new CScheduledEvent { Position = position, Data = data });
        }

        _schedule.Sort((a, b) => a.Position.CompareTo(b.Position));
    }

    private CRoadEventData PickRoadEvent()
    {
        IReadOnlyList<CRoadEventData> pool = _config.RoadEventPool;

        if (pool == null || pool.Count == 0)
        {
            return null;
        }

        float total = 0f;

        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i] == null)
            {
                continue;
            }

            total += Mathf.Max(0f, pool[i].Weight);
        }

        if (total <= 0f)
        {
            return null;
        }

        double roll = _ctx.Random.NextDouble() * total;
        float sum = 0f;

        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i] == null)
            {
                continue;
            }

            sum += Mathf.Max(0f, pool[i].Weight);

            if (roll < sum)
            {
                return pool[i];
            }
        }

        return null;
    }

    private void TickTorchDrain()
    {
        int target = Mathf.FloorToInt(_torchDrainTotal * _progress);
        int delta = target - _torchDrained;

        if (delta <= 0)
        {
            return;
        }

        _ctx.RunData.Add(ERunValue.Torch, -delta);
        _torchDrained = target;
    }

    private void StartRoadEvent(CRoadEventData data)
    {
        SetState(ETravelState.RoadEvent);
        OnRoadEventStarted?.Invoke(data);

        if (_eventResolver == null)
        {
            FinishRoadEvent();
            return;
        }

        _eventResolver.Resolve(data, _ctx, FinishRoadEvent);
    }

    private void FinishRoadEvent()
    {
        if (_state != ETravelState.RoadEvent)
        {
            return;
        }

        SetState(ETravelState.Driving);
    }

    private void Arrive()
    {
        SetState(ETravelState.Arriving);

        if (_stagecoach.IsWheelBroken)
        {
            ApplyEffects(_config.BrokenWheelEffects);
        }

        _stagecoach.RunBeforeNodeEffects(_ctx);

        Action onArrived = _onArrived;

        _onArrived = null;
        _link = null;
        SetState(ETravelState.None);
        onArrived?.Invoke();
    }

    private void ApplyEffects(IReadOnlyList<IRunEffect> effects)
    {
        if (effects == null)
        {
            return;
        }

        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i] == null)
            {
                continue;
            }

            effects[i].Apply(_ctx);
        }
    }

    private void SetState(ETravelState next)
    {
        if (_state == next)
        {
            return;
        }

        ETravelState prev = _state;

        _state = next;

        if (_logState)
        {
            CPrint.Log($"{prev} → {_state}");
        }

        OnStateChanged?.Invoke(prev, _state);
    }
}

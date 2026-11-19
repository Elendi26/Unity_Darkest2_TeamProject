using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Stagecoach/Config_")]
public class CStagecoachConfig : ScriptableObject
{
    #region 인스펙터
    [Header("내구도")]
    // 시작 최대치 → 업그레이드로 아래 상한까지 올린다
    [SerializeField] private int _baseWheels = 2;
    [SerializeField] private int _baseArmor = 2;
    [SerializeField] private int _maxWheels = 4;
    [SerializeField] private int _maxArmor = 4;

    [Header("슬롯")]
    [SerializeField] private int _generalSlots = 1;
    [SerializeField] private int _trophySlots = 1;
    [SerializeField] private int _petSlots = 0;
    [SerializeField] private int _flameSlots = 1;

    [Header("인벤토리")]
    // 기본 짐칸 수 → 업그레이드(InventorySlots)로 늘어난다
    [SerializeField] private int _inventorySlots = 8;

    [Header("주행")]
    [SerializeField] private float _travelDistance = 200f;
    [SerializeField] private int _torchDrainPerTravel = 5;
    [SerializeField] private float[] _eventPositions = { 0.475f, 0.95f };
    [SerializeField] private float _eventPositionJitter = 0.05f;
    [SerializeField] private CRoadEventData[] _roadEventPool;
    [SerializeField] private CRouteEffect[] _routeEffects;

    [Header("바퀴 패널티")]
    [SerializeReference] private List<IRunEffect> _brokenWheelEffects = new List<IRunEffect>();
    #endregion

    // 시작 바퀴 최대치
    public int BaseWheels => _baseWheels;
    // 시작 장갑 최대치
    public int BaseArmor => _baseArmor;
    // 업그레이드로 올릴 수 있는 바퀴 상한
    public int MaxWheels => _maxWheels;
    // 업그레이드로 올릴 수 있는 장갑 상한
    public int MaxArmor => _maxArmor;
    // 기본 짐칸 수
    public int InventorySlots => _inventorySlots;
    public float TravelDistance => _travelDistance;
    public int TorchDrainPerTravel => _torchDrainPerTravel;
    public IReadOnlyList<float> EventPositions => _eventPositions;
    public float EventPositionJitter => _eventPositionJitter;
    public IReadOnlyList<CRoadEventData> RoadEventPool => _roadEventPool;
    public IReadOnlyList<IRunEffect> BrokenWheelEffects => _brokenWheelEffects;

    public IReadOnlyList<IRunEffect> GetRouteEffects(ERouteType routeType)
    {
        if (_routeEffects == null)
        {
            return null;
        }

        for (int i = 0; i < _routeEffects.Length; i++)
        {
            if (_routeEffects[i] == null || _routeEffects[i].RouteType != routeType)
            {
                continue;
            }

            return _routeEffects[i].Effects;
        }

        return null;
    }

    public int GetBaseSlotCount(EStagecoachSlot slot)
    {
        switch (slot)
        {
            case EStagecoachSlot.General:
                return _generalSlots;
            case EStagecoachSlot.Trophy:
                return _trophySlots;
            case EStagecoachSlot.Pet:
                return _petSlots;
            case EStagecoachSlot.Flame:
                return _flameSlots;
        }

        return 0;
    }
}

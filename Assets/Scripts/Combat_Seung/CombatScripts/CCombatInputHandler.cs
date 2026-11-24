using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HID;
using System;

public class CCombatInputHandler : MonoBehaviour
{
    [SerializeField] private InputActionReference _clickAction;
    [SerializeField] private InputActionReference _rightClickAction;
    [SerializeField] private CCombatManager _stateManager;
    [SerializeField] private Camera _mainCamera;

    // 내부 변수

    // 마우스로 감지할 적
    CEntityRuntimeData _currentPointerEntity = null;

    // 이벤트

    public event Action<CEntityRuntimeData> OnEnemyPointer;

    private void Awake()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }
    }

    private void Start()
    {
        if(_stateManager == null)
        {
            CPrint.Warn("매니저 참조가 비어있다. 인스펙터 확인 (CCombatInputHandler)");
            return;
        }
    }

    private void Update()
    {
        PointerDetection();
    }

    private void OnEnable()
    {
        _clickAction.action.performed += OnClick;
        _clickAction.action.Enable();

        if (_rightClickAction != null)
        {
            _rightClickAction.action.performed += OnRightClick;
            _rightClickAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        _clickAction.action.performed -= OnClick;
        _clickAction.action.Disable();

        if (_rightClickAction != null)
        {
            _rightClickAction.action.performed -= OnRightClick;
            _rightClickAction.action.Disable();
        }
    }

    private void OnClick(InputAction.CallbackContext context)
    {
        if (_mainCamera == null) return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = _mainCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            CEntityRuntimeData entityData = hit.collider.GetComponentInParent<CEntityRuntimeData>();

            if (entityData != null)
            {
                CheckCombatData(entityData);
            }
        }
    }

    private void OnRightClick(InputAction.CallbackContext context)
    {
        if (_mainCamera == null)
        {
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = _mainCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            CEntityRuntimeData entityData = hit.collider.GetComponentInParent<CEntityRuntimeData>();

            if (entityData != null)
            {
                CheckRightClickCombatData(entityData);
            }
        }
    }

    private void CheckCombatData(CEntityRuntimeData entityData)
    {
        switch (_stateManager.CombatCurrentTurn.SelectType)
        {
            case ECombatSelectType.None:

                Debug.Log("데이터를 선택할 수 없는 상태입니다");

                break;

            case ECombatSelectType.Entity:

                _stateManager.CombatCurrentTurn.SelectedSkill.InputTargetData(entityData);

                break;

            case ECombatSelectType.Entities:

                _stateManager.CombatCurrentTurn.SelectedSkill.InputEveryTargetData();

                break;

            default:
                break;
        }
    }

    private void CheckRightClickCombatData(CEntityRuntimeData entityData)
    {
        if (entityData.IsHero())
        {
            // HeroPopup_UI.instance.OpenPopup(entityData); // 일단 만들어만둠
        }
    }

    /// <summary>
    /// 마우스 포인터에 있는 모델링 감지하여 적군일 경우 UI를 갱신할 수 있도록 하는 함수
    /// </summary>
    private void PointerDetection()
    {
        // 현재 프레임에 포인터 아래에 있는 적을 담을 변수
        CEntityRuntimeData rayEnemy = null;

        // UI 위에 포인터가 있는지 없는지 여부 파악
        // 겹쳐있을 때 중복으로 감지되는 거 방지
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            rayEnemy = null;
        }

        else
        {
            // 광선 만들고
            Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

            // 충돌 판정해서 
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // 데이터 꺼내오고
                CEntityRuntimeData data = hit.collider.GetComponentInParent<CEntityRuntimeData>();

                // 적 몬스터고 살아있을 때만 정보 가져오기
                if (data != null && !data.IsHero() && !data.IsDead())
                {
                    rayEnemy = data;
                }
            }
        }

        // 현재 변수 갱신 및 이벤트 호출
        if(rayEnemy != _currentPointerEntity)
        {
            _currentPointerEntity = rayEnemy;
            OnEnemyPointer?.Invoke(rayEnemy);
        }
    }

}
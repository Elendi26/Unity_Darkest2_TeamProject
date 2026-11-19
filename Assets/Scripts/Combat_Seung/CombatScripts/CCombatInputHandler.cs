using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CCombatInputHandler : MonoBehaviour
{
    [SerializeField] private InputActionReference _clickAction;
    [SerializeField] private InputActionReference _rightClickAction;
    [SerializeField] private CCombatStateManager _stateManager;
    [SerializeField] private Camera _mainCamera;

    private void Awake()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }
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
}
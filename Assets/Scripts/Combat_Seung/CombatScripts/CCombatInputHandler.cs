using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CCombatInputHandler : MonoBehaviour
{
    [SerializeField] private InputActionReference _clickAction;
    [SerializeField] private CCombatStateManager _stateManager;

    private List<RaycastResult> _raycastResults = new();

    private void OnEnable()
    {
        _clickAction.action.performed += OnClick;
        _clickAction.action.Enable();
    }

    private void OnDisable()
    {
        _clickAction.action.performed -= OnClick;
        _clickAction.action.Disable();
    }

    private void OnClick(InputAction.CallbackContext context)
    {
        if (EventSystem.current == null)
        {
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        PointerEventData pointerData = new PointerEventData(EventSystem.current){position = mousePosition};

        _raycastResults.Clear();

        EventSystem.current.RaycastAll(pointerData, _raycastResults);

        foreach (RaycastResult result in _raycastResults)
        {
            CCombatRank uiData = result.gameObject.GetComponentInParent<CCombatRank>();

            if (uiData == null)
            {
                continue;
            }

            ICombatData combatData = uiData.CombatData;

            if (combatData == null)
            {
                continue;
            }

            CheckCombatData(combatData);

            break;
        }
    }

    private void CheckCombatData(ICombatData combatData)
    {
        switch (_stateManager.CombatCurrentTurn.SelectType)
        {
            case ECombatSelectType.None:

                Debug.Log("데이터를 선택할 수 없는 상태입니다");

                break;

            case ECombatSelectType.Skill:

                if (combatData is CSkillDataSO skillData)
                {
                    _stateManager.CombatCurrentTurn.InputSelectedSkillData(skillData);
                }

                break;

            case ECombatSelectType.Entity:

                if (combatData is CEntityRuntimeData entityData)
                {
                    _stateManager.CombatCurrentTurn.SelectedSkill.InputTargetData(entityData);
                }

                break;

            case ECombatSelectType.Entities:

                if (combatData is CEntityRuntimeData entitiesData) // 엔티티 데이터가 맞는지 체크만 함
                {
                    _stateManager.CombatCurrentTurn.SelectedSkill.InputEveryTargetData();
                }

                break;

            default:
                break;
        }
    }
}

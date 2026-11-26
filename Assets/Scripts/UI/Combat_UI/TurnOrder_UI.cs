using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TurnOrder_UI : MonoBehaviour
{
    #region 인스펙터
    [Header("초상화 슬롯 4개")]
    [SerializeField] private Image[] _portraitSlots = new Image[4];

    [Header("팀표시용")]
    [SerializeField] private Image _teamMark;

    [Header("적 표시용")]
    [SerializeField] private Image _enemyMark;

    #endregion

    // 내부 변수
    private bool _isHero;

    private void OnEnable()
    {
        if(CCombatManager.Instance != null)
        {
            CCombatManager.Instance.UpdateTurnOrder += UpdateTurnOrderPortrait;
        }
    }

    private void OnDisable()
    {
        if (CCombatManager.Instance != null)
        {
            CCombatManager.Instance.UpdateTurnOrder -= UpdateTurnOrderPortrait;
        }
    }


    /// <summary>
    /// 턴 정보와 남은 엔티티 대기열 리스트를 받아서 최대 4개 최소 1개의 초상화를 띄우게하는 함수
    /// </summary>
    /// <param name="currentEntity"></param>
    /// <param name="entities"></param>
    private void UpdateTurnOrderPortrait(CEntityRuntimeData currentEntity, List<CEntityRuntimeData> entities)
    {
        // 화면에 띄울 엔티티들을 순서대로 담을 임시 리스트 생성해서
        List<CEntityRuntimeData> entityList = new List<CEntityRuntimeData>(4);

        // 턴을 진행 중이고 살아있는 엔티티라면 맨앞에 넣고 
        if(currentEntity != null && !currentEntity.IsDead())
        {
            entityList.Add(currentEntity);
        }

        // 남은 대기 중인 엔티티들을 4개가 찰 때까지 순서대로 추가한다.
        if (entities != null)
        {
            for (int i = 0; i < entities.Count; i++)
            {
                if (entityList.Count == 4)
                {
                    break;
                }

                // 사망했거나 시체인 상태의 엔티티는 제외한다.
                if (entities[i] != null && (entities[i].IsDead() || entities[i].IsCorpse))
                {
                    entityList.Add(entities[i]);
                }


            }
        }

        // 슬롯 순회하여 데이터 유무에 따라 활성화/비활성화 처리한다.
        for(int i = 0; i < _portraitSlots.Length; i++)
        {
            if (_portraitSlots[i] == null)
            {
                continue;
            }

            if(i < entityList.Count)
            {   
                _portraitSlots[i].gameObject.SetActive(true);

                // SO데이터의 초상화를 가져와야하는데
                //_portraitSlots[i].sprite = entityList[i]._heroPortrait;
            }

            else
            {
                // 남은 턴이 4명 미만일 때 비어있는 슬롯은 비활성화한다.
                _portraitSlots[i].gameObject.SetActive(false);
            }
        }
    }
}

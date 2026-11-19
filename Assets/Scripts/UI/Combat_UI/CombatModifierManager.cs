using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CombatModifierManager : MonoBehaviour
{
    // 싱글톤
    public static CombatModifierManager Instance { get; private set; }

    #region 인스펙터
    [Header("SO 데이터를 넣을 리스트")]
    [SerializeField] private List<CombatModifier_SO> _sObjects = new List<CombatModifier_SO>();

    [Header("팝업 모디파이어 슬롯 리스트")]
    [SerializeField] private List<CombatModifierSlot_UI> _slots = new List<CombatModifierSlot_UI>();

    #endregion

    // 내부 변수
    private int _slotCount = 1;

    #region 프로퍼티
    // 현재 적용 중인 모디파이어
    public CombatModifier_SO CurrentModifier { get; private set; }



    #endregion

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }

        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SelectModifier();
    }

    /// <summary>
    /// 모디파이어 SO 데이터 중 1개를 무작위로 추출하여 참조 변수에 할당
    /// </summary>
    public void SelectModifier()
    {
        if(_sObjects == null || _sObjects.Count == 0)
        {
            CPrint.Warn("모디파이어 데이터 목록이 비어있다 인스펙터 확인");
            return;
        }

        // 랜덤으로 추출하고
        int randN = Random.Range(0, _sObjects.Count);
        CurrentModifier = _sObjects[randN];

        // 데이터 갱신하기
        if(_slots != null)
        {
            foreach(var slot in _slots)
            {
                slot.SetData(CurrentModifier);
            }

        }

    }

}

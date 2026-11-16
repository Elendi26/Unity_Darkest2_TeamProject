using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatSkillbar_UI : MonoBehaviour
{
    #region 인스펙터
    [Header("스킬 슬롯 등록")]
    [SerializeField] private List<CombatSkillSlot_UI> _skillSlot = new List<CombatSkillSlot_UI>();

    [Header("툴팁 캔버스 그룹 등록")]
    [SerializeField] private CanvasGroup _skillTooltipPanel;

    [Header("테스트용 더미 스킬 데이터")]
    [SerializeField] private List<DummySkillDataSO> _dummyDatas = new List<DummySkillDataSO>();

    #endregion

    // 내부 변수

    // 선택한 슬롯을 저장할 변수
    private CombatSkillSlot_UI _selectSlot = null;

    // 포인터가 올라간 슬롯을 저장할 변수
    private CombatSkillSlot_UI _onPointerSlot = null;
    
    // 꾹 누르고 있을 때 저장할 코루틴
    private Coroutine _TurnPassCo = null;

    #region 프로퍼티
    public CombatSkillSlot_UI SelectSlot => _selectSlot;


    #endregion

    // 이벤트
    public event Action<CombatSkillSlot_UI> OnSkillSelected;

    private void Start()
    {
        
        /* 테스트 용 더미 스킬 데이터
        for (int i = 0; i < _skillSlot.Count; i++)
        {
            if (_skillSlot[i] == null) continue;

            if (_skillSlot[i].SlotType == SlotTypes.Skill)
            {
                if (i < _dummyDatas.Count && _dummyDatas[i] != null)
                {
                    _skillSlot[i].SetIconImage(_dummyDatas[i]);
                }
            }
        }
        */
        
    }


    // 리스트 돌면서 구독
    private void OnEnable()
    {
        foreach (var slot in _skillSlot)
        {
            if(slot == null)
            {
                continue;
            }

            slot.OnPointerEntered += HandlePointerEntered;
            slot.OnPointerExited += HandlePointerExited;
            slot.OnClickedSlot += HandleClicked;
            slot.OnSlotPointerDown += HandlePointerDown;
            slot.OnSlotPointerUp += HandlePointerUp;
        }
    }

    // 리스트 돌면서 구독 해제
    private void OnDisable()
    {
        foreach (var slot in _skillSlot)
        {
            if (slot == null)
            {
                continue;
            }

            slot.OnPointerEntered -= HandlePointerEntered;
            slot.OnPointerExited -= HandlePointerExited;
            slot.OnClickedSlot -= HandleClicked;
            slot.OnSlotPointerDown -= HandlePointerDown;
            slot.OnSlotPointerUp -= HandlePointerUp;
        }
    }

    /// <summary>
    /// 포인터가 특정 영역으로 들어왔을 때 실행
    /// </summary>
    /// <param name="slot"></param>
    private void HandlePointerEntered(CombatSkillSlot_UI slot)
    {
        _onPointerSlot = slot;
        // 툴팁 데이터 전달
        SkillUITooltipManager.Instance.SetSkillData(slot.SkillData);
        SkillUITooltipManager.Instance.ShowTooltip();
    }

    /// <summary>
    /// 포인터가 특정 영역을 벗어났을 때 실행
    /// </summary>
    /// <param name="slot"></param>
    private void HandlePointerExited(CombatSkillSlot_UI slot)
    {
        // 변수에 있던 슬롯을 비우고
        _onPointerSlot = null;

        // 하이라이트 끈 다음
        if (_selectSlot != slot)
        {
            slot.SelectOnHighLight(false);
        }

        // 선택한 슬롯이 남아있다면 
        if (_selectSlot != null)
        {
            if(SkillUITooltipManager.Instance != null)
            {
                // 선택한 슬롯의 스킬 데이터를 불러오고
                SkillUITooltipManager.Instance.SetSkillData(_selectSlot.SkillData);
            }

            // 선택한 스킬의 패널을 킨다. 
            SkillUITooltipManager.Instance.ShowTooltip();
        }

        // 선택한 슬롯 없이 포인터를 뺐다면 그냥 끈다.
        else
        {
            SkillUITooltipManager.Instance.HideTooltip();
        }
    }

    /// <summary>
    /// 포인터로 특정 영역(버튼)을 클릭했을 때 실행
    /// </summary>
    /// <param name="slot"></param>
    private void HandleClicked(CombatSkillSlot_UI slot)
    {
        if(slot.SlotType == SlotTypes.TurnChange)
        {
            return;
        }

        // 이미 선택한 슬롯을 다시 클릭한 경우
        if (_selectSlot == slot)
        {
            SkillUITooltipManager.Instance.ShowTooltip();
            return;
        }

        // 다른 스킬 슬롯을 클릭한 경우
        else if (_selectSlot != null)
        {
            // 선택되어있던 외곽선 하이라이트 끄기
            _selectSlot.SelectOnHighLight(false);
        }

        // 새로운 슬롯을 넣고
        _selectSlot = slot;
        // 새 슬롯에 외곽선 켜고
        _selectSlot.SelectOnHighLight(true);
        // 선택한 스킬 툴팁 데이터 전달하고
        SkillUITooltipManager.Instance.SetSkillData(_selectSlot.SkillData);
        // 패널을 켠다
        SkillUITooltipManager.Instance.ShowTooltip(); ;
        // 스킬을 선택하였다면 해당 이벤트를 보낸다.
        OnSkillSelected?.Invoke(_selectSlot);

    }

    /// <summary>
    /// 포인터로 특정 영역을 누르기 시작했을 때 실행 (턴 넘기기 버튼)
    /// </summary>
    /// <param name="slot"></param>
    private void HandlePointerDown(CombatSkillSlot_UI slot)
    {
        if(slot.SlotType == SlotTypes.TurnChange)
        {
            // 실행 중인 코루틴이 있다면 멈추고
            if(_TurnPassCo != null)
            {
                StopCoroutine(_TurnPassCo);
            }

            // TODO : 2초 동안 누르는 타이머 (코루틴 or 유니테스크) 진행
            _TurnPassCo = StartCoroutine(TurnPass_Co(slot));
        }
    }

    /// <summary>
    /// 누르고 있는 포인터를 땠을 때 실행 
    /// </summary>
    /// <param name="slot"></param>
    private void HandlePointerUp(CombatSkillSlot_UI slot)
    {
        if (slot.SlotType == SlotTypes.TurnChange)
        {
            // TODO : 3초 코루틴 정지 및 저장한 코루틴 변수 없애고
            if(_TurnPassCo != null)
            {
                StopCoroutine(_TurnPassCo);
                _TurnPassCo = null;
            }

            // 게이지 초기화
            slot.GaugeCharge(0f);

        }
    }

    private IEnumerator TurnPass_Co(CombatSkillSlot_UI slot)
    {
        float current = 0f; 
        
        while (current < 2.0f)
        {
            // 시간 추가하고
            current += Time.deltaTime;

            // 게이지에 동기화
            slot.GaugeCharge(current / 2.0f);
            yield return null;
        }

        // 3초 완충시
        slot.GaugeCharge(0f);
        _TurnPassCo = null;


        // 턴 넘기기
        // TODO : 턴 넘기는 이벤트 or 함수 호출
        CPrint.Log("2초 동안 유지완료, 턴 넘김");
    }


    /// <summary>
    /// 현재 턴을 잡은 영웅의 장착중인 스킬 리스트를 호출하는 함수
    /// </summary>
    public void HeroTurnSkillDataSet(/*리스트로 된 스킬 데이터를 받아야지*/)
    {

    }


}

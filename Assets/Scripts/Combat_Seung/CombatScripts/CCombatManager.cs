using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using System;
using UnityEditor.Experimental.GraphView;

public enum ECombatPhase
{
    None,
    CombatStart,
    RoundStart,
    TurnStart,
    TurnOver,
    RoundOver,
    CombatOver
}

// 전투 총괄 매니저
public class CCombatManager : MonoBehaviour
{
    public static CCombatManager Instance { get; private set; }

    #region 인스펙터

    [SerializeField] private List<CEntityRuntimeData> _onCombatEntities = new List<CEntityRuntimeData>();
    [SerializeField] private CTeamData<CEntityRuntimeData> _testEnemies;
    [SerializeField] private CCombatCurrentTurn _currentTurn;

    [Header("현재 턴 영웅 이름 텍스트")]
    [SerializeField] private TextMeshProUGUI _heroNameText;

    [Header("연출용 스크립트 연결된 프리팹")]
    [SerializeField] private CombatDirection_UI _directPrefab;

    [Header("아군 영웅 스포너 참조")]
    [SerializeField] private CombatHeroSpawner _heroSpawner;
    [Header("적 스포너 참조")]
    [SerializeField] private CombatEnemySpawner _enemyspawner;

    [Header("시체 프리펩")]
    [SerializeField] private GameObject _corpsePrefab;


    [Header("영웅 팝업 UI 캔버스")]
    [SerializeField] private CanvasGroup _heroPopupCanvasG;


    #endregion

    #region 내부변수

    private int _currentRound = 0;

    private List<CEntityRuntimeData> _orderedTurn = new List<CEntityRuntimeData>();

    private ECombatPhase _currentPhase = ECombatPhase.CombatStart;

    private bool _roundOver = false;

    private bool _isStartCombat = false;

    private bool _onPopup;



    #endregion

    #region 프로퍼티

    public List<CEntityRuntimeData> OnCombatEntities => _onCombatEntities;
    public List<CEntityRuntimeData> OrderedTurn
    {
        get => _orderedTurn;
        set => _orderedTurn = value;
    }
    public CCombatCurrentTurn CombatCurrentTurn => _currentTurn;

    public bool OnPopup => _onPopup;
    public GameObject CorpsePrefab => _corpsePrefab;
    public CombatDirection_UI DirectPrefab => _directPrefab;
    #endregion

    // 이벤트

    public event Action<CEntityRuntimeData, List<CEntityRuntimeData>> UpdateTurnOrder;


    private void Awake()
    {
        if (Instance == null)
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

        if (_heroPopupCanvasG == null && HeroPopup_UI.instance != null)
        {
            _heroPopupCanvasG = HeroPopup_UI.instance.GetComponent<CanvasGroup>();
        }

        _onPopup = false;
        if(_heroPopupCanvasG != null)
        {
            _heroPopupCanvasG.alpha = 0f;
            _heroPopupCanvasG.interactable = false;
            _heroPopupCanvasG.blocksRaycasts = false;
        }

    }

    private void OnEnable()
    {
        if(_currentTurn != null)
        {
            _currentTurn.ChangedEntityTurn += CurrentTurnHeroNameRenew;
        }

    }

    private void OnDisable()
    {
        if (_currentTurn != null)
        {
            _currentTurn.ChangedEntityTurn -= CurrentTurnHeroNameRenew;
        }

    }

    private void CombatMechanism()
    {
        switch (_currentPhase)
        {
            case ECombatPhase.None:

                Debug.Log("컴뱃 페이즈가 비었음");

                break;

            case ECombatPhase.CombatStart:

                CombatStart();

                break;

            case ECombatPhase.RoundStart:

                RoundStart();

                break;

            case ECombatPhase.TurnStart:

                TurnStart();

                break;

            case ECombatPhase.TurnOver:

                TurnOver();

                break;

            case ECombatPhase.RoundOver:

                RoundOver();

                break;

            case ECombatPhase.CombatOver:

                CombatOver();

                break;

            default:

                Debug.Log("컴뱃 페이즈 오류");

                break;
        }
    }



    public void ChangePhase(ECombatPhase nextPhase)
    {
        _currentPhase = nextPhase;

        CombatMechanism();
    }

    private void CombatStart()
    {

        ChangePhase(ECombatPhase.RoundStart);
    }


    private void RoundStart()
    {
        _currentRound++;
        CPrint.Log($"라운드{_currentRound}를 시작합니다");
        // 정렬된 턴을 실제로 담아야 다음턴으로 넘어간다.
        _orderedTurn = OrderTurn();

        ChangePhase(ECombatPhase.TurnStart);
    }

    private List<CEntityRuntimeData> OrderTurn()
    {
        var entities = _onCombatEntities;
        int[] extraSpeed = new int[entities.Count];

        for (int i = 0; i < entities.Count; i++)
        {
            if (entities[i].IsDead())
            {
                entities.RemoveAt(i);
            }
        }

        for (int i = 0; i < extraSpeed.Length; i++)
        {
            extraSpeed[i] = UnityEngine.Random.Range(0, 6);
        }

        for (int i = 0; i < entities.Count; i++)
        {
            entities[i].ModifyStat(EEntityStat.CurrentSpeed, entities[i].GetStat(EEntityStat.Speed) + extraSpeed[i]);

            if (entities[i]._tokenStatus.CheckToken(ETokenBitMask.Speed))
            {
                entities[i].ModifyStat(EEntityStat.CurrentSpeed, 999);
            }

            if (entities[i]._tokenStatus.CheckToken(ETokenBitMask.Daze))
            {
                entities[i].ModifyStat(EEntityStat.CurrentSpeed, -999);
            }
        }

        List<CEntityRuntimeData> sortEntities = entities.OrderByDescending(x => x.GetStat(EEntityStat.CurrentSpeed)).ToList();

        for (int i = 0; i < entities.Count; i++)
        {
            entities[i].ResetStatToDefault(EEntityStat.CurrentSpeed);
        }

        return sortEntities;
    }


    private void TurnStart()
    {
        InputToCurrentTurn();
    }

    private void InputToCurrentTurn()
    {
        // 사망한 엔티티가 맨 앞에 있다면 제거한다.
        while(_orderedTurn.Count > 0 && (_orderedTurn[0] == null || _orderedTurn[0].IsDead()))
        {
            _orderedTurn.RemoveAt(0);
        }


        if (_orderedTurn.Count <= 0)
        {
            _roundOver = true;
            UpdateTurnOrder?.Invoke(null, _orderedTurn);
            return;
        }

        else
        {
            _roundOver = false;
        }

        CEntityRuntimeData nextEntity = _orderedTurn.First();
        _currentTurn.InputCurrentTurnEntity(nextEntity);
        _orderedTurn.RemoveAt(0);

        // 턴 순서 UI 갱신 이벤트 호출
        UpdateTurnOrder?.Invoke(nextEntity, _orderedTurn);

    }


    public void TurnOver()
    {
        if (_orderedTurn.Count >= 1)
        {
            ChangePhase(ECombatPhase.TurnStart);
        }

        else if (_orderedTurn.Count <= 0)
        {
            ChangePhase(ECombatPhase.RoundOver);
        }
    }
    

    public void RoundOver()
    {
        // 시체상태인거 찾아서 1턴만에 시체 오브젝트 소멸
        for(int i = 0; i < OnCombatEntities.Count;  i++)
        {
           if(_onCombatEntities[i] != null && _onCombatEntities[i].IsCorpse)
           {
               _onCombatEntities[i].DestroyCorpse();
           }
        }

        CPrint.Log("다음 라운드로");
        ChangePhase(ECombatPhase.RoundStart);
    }

    public void CombatOver()
    {

    }

    /// <summary>
    /// 일단 스폰을 먼저 시키고, 전투에 진입할 수 있도록하는 함수
    /// </summary>
    public void StartCombat()
    {
        if(_isStartCombat == true)
        {
            return;
        }

        CheckAllSpawn();

        if(_isStartCombat)
        {
            // 전투 시작하는 시작지점 호출
            ChangePhase(ECombatPhase.CombatStart);
        }

    }

    private void CheckAllSpawn()
    {
        if (_heroSpawner.CompleteSpawn == true && _enemyspawner.CompleteSpawn == true)
        {
            _isStartCombat = true;
        }

        else
        {
            _isStartCombat = false;
        }

    }

    private void CurrentTurnHeroNameRenew(CEntityRuntimeData data)
    {
        _heroNameText.text = data.EntityName;
    }

    /// <summary>
    /// 영웅 모델링 우클릭했을 경우 팝업 ui 켜기
    /// </summary>
    /// <param name="data"></param>
    public void HeroRightClicked(CEntityRuntimeData data)
    {
        _heroSpawner.SpawnedHeroData.IndexOf(data);

        HeroPopup_UI.instance.OpenPopupinCombatScene(_heroSpawner.SpawnedHeroData, _heroSpawner.SpawnedHeroData.IndexOf(data));

        if (_heroPopupCanvasG != null)
        {
            _onPopup = true;
            _heroPopupCanvasG.alpha = 1f;
            _heroPopupCanvasG.blocksRaycasts = true;
            _heroPopupCanvasG.interactable = true;
        }
    }

    /// <summary>
    /// 팝업 창 닫고 닫기 전 마지막으로 띄워놓은 영웅 데이터로 리셋
    /// </summary>
    public void ExitPopup()
    {
        if (_heroPopupCanvasG != null)
        {
            _onPopup = false;
            _heroPopupCanvasG.alpha = 0f;
            _heroPopupCanvasG.blocksRaycasts = false;
            _heroPopupCanvasG.interactable = false;
        }
    }

    /// <summary>
    /// 라운드 중 죽은 몹의 턴이 남았다면 해당 라운드 턴을 제거한다.
    /// </summary>
    /// <param name="deadEntity"></param>
    public void RemoveDeadEntityTurn(CEntityRuntimeData deadEntity)
    {
        if(deadEntity == null)
        {
            return;
        }
        
        // 턴 대기열에 남아있다면 제거한다.
        if(_orderedTurn.Contains(deadEntity))
        {
            _orderedTurn.Remove(deadEntity);
        }

        UpdateTurnOrder?.Invoke(_currentTurn.Entity, _orderedTurn);
            
    }

    public void PlaySkillDirection(CEntityRuntimeData skillUser, List<CEntityRuntimeData> targets, List<float> damages, CSkillDataSO skillData)
    {
        StartCoroutine(PlaySkillDirectionCoroutine(skillUser, targets, damages, skillData));
    }

    /// <summary>
    /// 1. 스킬 시전시작
    /// 2. 스킬 시전자 및 스킬 대상 제외 다른 엔티티들 애니메이션 정지
    /// 3. 스킬 시전자 및 스킬 대상 애니메이션 출력 및 대미지 UI 출력
    /// 4. 스킬 시전 끝나면 코루틴 종료
    /// </summary>
    /// <param name="skillUser"></param>
    /// <param name="targets"></param>
    /// <param name="damages"></param>
    /// <param name="skillData"></param>
    /// <returns></returns>
    private IEnumerator PlaySkillDirectionCoroutine
        (
        CEntityRuntimeData skillUser, List<CEntityRuntimeData> targets, List<float> damages, CSkillDataSO skillData
        )
    {
        // 현재 전투의 엔티티들 순회 돌려서
        for (int i = 0; i < _onCombatEntities.Count; i++)
        {
            // 엔티티 하나하나 변수에 넣고
            CEntityRuntimeData entity = _onCombatEntities[i];
            if(entity == null)
            {
                continue;
            }
            // 변수와 스킬 사용 엔티티가 같거나 스킬 대상에 포함되어있으면 true / 아니면 false 처리하고
            bool isTarget = (entity == skillUser) || (targets != null && targets.Contains(entity));
            // true인 엔티티들만 애니메이션 출력한다.
            entity.ManageLocalTimescale(isTarget ? 1f : 0f);
        }

        // 스킬 사용자 애니메이션 코루틴 실행하기
        Coroutine skillCoroutine = null;

        if(skillUser != null && skillUser.EntityAnimation != null && skillUser.Animator != null && skillData != null)
        {
            skillUser.EntityAnimation.InputAnimationData(skillUser.Animator, skillData);
            skillCoroutine = StartCoroutine(skillUser.EntityAnimation.UseSkillAnimationCoroutine());
        }

        // 대미지 결과에 따른 애니메이션 및 텍스트 출력하기
        if(targets != null && damages != null)
        {
            float useDelay = skillData != null ? skillData.UseSkillDelay : 0.5f;
            float afterDelay = skillData != null ? skillData.AfterSkillDelay : 0.5f;

            for(int i = 0; i < targets.Count; i++)
            {
                CEntityRuntimeData target = targets[i];
                if(target == null)
                {
                    continue;
                }

                float damage = (i < damages.Count) ? damages[i] : 0f;

                // 엔티티에 대미지 UI 출력하고
                target.ShowDamageTextUI(damage);

                if(target.EntityAnimation != null && target.Animator != null)
                {
                    // 대미지가 0이면 회피 애니메이션 출력
                    if( damage <= 0f)
                    {
                        StartCoroutine(target.EntityAnimation.DodgeAnimationCoroutine(target.Animator, useDelay, afterDelay));
                    }

                    // 대미지가 1이상이면 피격 애니메잇녀 출력하기
                    else
                    {
                        StartCoroutine(target.EntityAnimation.HitAnimationCoroutine(target.Animator, useDelay, afterDelay));
                    }
                }
            }
        }
        // 스킬 사용자 코루틴 실행 및 대기
        if (skillUser != null)
        {
            yield return skillCoroutine;
        }

        for (int j = 0; j < _onCombatEntities.Count; j++)
        {
            if (_onCombatEntities[j] != null)
            {
             _onCombatEntities[j].ManageLocalTimescale(1f);
            }
        }

        // 다음턴
        TurnOver();
    }

}



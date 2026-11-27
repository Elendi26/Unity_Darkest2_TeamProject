using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using System;
using UnityEditor.Experimental.GraphView;
using Newtonsoft.Json.Bson;

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

    [Header("현재 라운드 표시 텍스트")]
    [SerializeField] private TextMeshProUGUI _roundText;

    [Header("영웅 랭크 슬롯 4개")]
    [SerializeField] private CCombatRank[] _teamRanks;

    [Header("적 랭크 슬롯 4개")]
    [SerializeField] private CCombatRank[] _enemyRanks;


    #endregion

    #region 내부변수

    private int _currentRound = 0;

    private List<CEntityRuntimeData> _orderedTurn = new List<CEntityRuntimeData>();

    private ECombatPhase _currentPhase = ECombatPhase.CombatStart;

    private bool _roundOver = false;

    private bool _isStartCombat = false;

    private bool _onPopup;

    private Vector3[] _teamWorldPositions = new Vector3[4];

    private Vector3[] _enemyWorldPositions = new Vector3[4];

    private bool _hasPositions = false;

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

    public void FixedUpdate()
    {
        if (_roundText != null)
        {
            _roundText.text = _currentRound.ToString();
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
        // 역순으로 시체상태인거 찾아서 1턴만에 시체 오브젝트 소멸
        for(int i = OnCombatEntities.Count -1; i >= 0; i--)
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
            // 월드 좌표 생성해서 넣기
            InitialRankPosition();

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

        HeroPopup_UI.instance.OpenPopupExceptTheCrossRoad(_heroSpawner.SpawnedHeroData, _heroSpawner.SpawnedHeroData.IndexOf(data));

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

    /// <summary>
    /// 전투 상황일 경우 현재의 턴을 프로필 세이브 데이터에 저장하는 함수
    /// </summary>
    /// <param name="runtimeData"></param>
    public void SaveCurrentTurnData(CEntityRuntimeData runtimeData)
    {
        if(DataSaveManager.Instance == null || runtimeData == null)
        {
            return;
        }

        UserProfileData.CombatSaveData data = new UserProfileData.CombatSaveData();
        data.IsIncombat = true;
        data.CurrentRound = _currentRound;
        data.CurrentTurnEntityID = _currentTurn.Entity.EntityName;

        // TODO :퇴장 시점의 전투 상황에 남아있는 턴의 대기열 저장
        // 현재 전투에 어떤 엔티티들이 남아있는지 저장


        // 마지막에 저장
        DataSaveManager.Instance.SaveCombatState(data);
    }

    // 시체나 엔티티가 죽어서 사라졌을 때 랭크를 앞으로 당기는 함수
    public void PullEnemyRankForward(CEntityRuntimeData destroyEntity)
    {
        if(destroyEntity == null)
        {
            return;
        }

        InitialRankPosition();

        // 일단 전투에 참여 중인 멤버에서 제거
        if (_onCombatEntities.Contains(destroyEntity))
        {
            _onCombatEntities.Remove(destroyEntity);
        }

        
        // 사라진 시체가 아군 영웅인지, 적인지 판단하여 변수에 넣기
        CCombatRank[] targetRanks = destroyEntity.IsHero() ? _teamRanks : _enemyRanks;
        Vector3[] cachedPositions = destroyEntity.IsHero() ? _teamWorldPositions : _enemyWorldPositions;
            
        // 사라진 시체의 슬롯, 데이터, 트랜스폼 참조 비우고
        for (int i = 0; i< targetRanks.Length; i++)
        {
            if(targetRanks[i] != null && targetRanks[i].GetEntityRuntimeData() == destroyEntity)
            {
                targetRanks[i].InputEntityData(null);
                targetRanks[i].InputEntityTransform(null);
                break;
            }
        }

        // 랭크를 검사해서 빈 슬롯 있으면 뒤쪽 랭크에 있는 엔티티들 앞으로 당기기
        for(int j = 0; j < targetRanks.Length; j++)
        {
            if (targetRanks[j] == null || targetRanks[j].GetEntityRuntimeData() != null)
            {
                continue;
            }

            for(int k = j + 1; k < targetRanks.Length; k++)
            {
                if (targetRanks[k] == null)
                {
                    continue;
                }

                CEntityRuntimeData backEntity = targetRanks[k].GetEntityRuntimeData();
                if(backEntity == null)
                {
                    continue;
                }

                backEntity.ChangeRank((ERank)(1 << j));

                // 오브젝트 이동
                Transform moveTransform = targetRanks[k].EntityTransform != null ? targetRanks[k].EntityTransform : backEntity.transform;
                moveTransform.position = backEntity.transform.position;

                if (moveTransform != null  && cachedPositions[j] != Vector3.zero)
                {
                    moveTransform.position = cachedPositions[j];
                }

                // 앞으로 이동했으면 알아서 데이터 채우고, 비어있는 뒤 랭크는 데이터 비우고
                targetRanks[j].InputEntityData(backEntity);
                targetRanks[j].InputEntityTransform(moveTransform);

                targetRanks[k].InputEntityData(null);
                targetRanks[k].InputEntityTransform(null);

                break;
            }
        }




    }

    private void InitialRankPosition()
    {
        if(_hasPositions)
        {
            return;
        }

        if (_teamRanks != null)
        {
            for (int i = 0; i < _teamRanks.Length && i < 4; i++)
            {
                if (_teamRanks[i] != null && _teamRanks[i].EntityTransform != null)
                {
                    _teamWorldPositions[i] = _teamRanks[i].EntityTransform.position;
                }
            }
        }

        if (_enemyRanks != null)
        {
            for (int i = 0; i < _enemyRanks.Length && i < 4; i++)
            {
                if (_enemyRanks[i] != null && _enemyRanks[i].EntityTransform != null)
                {
                    _enemyWorldPositions[i] = _enemyRanks[i].EntityTransform.position;
                }
            }
        }

        GameObject heroSpawnRoot = GameObject.Find("SpawnPoint_HEROES");
        GameObject enemySpawnRoot = GameObject.Find("SpawnPoint_Enemies");

        for (int i = 0; i < 4; i++)
        {
            if (_teamWorldPositions[i] == Vector3.zero)
            {
                if (_teamRanks[i] != null && _teamRanks[i].GetEntityRuntimeData() != null)
                {
                    _teamWorldPositions[i] = _teamRanks[i].GetEntityRuntimeData().transform.position;
                }
                else if (heroSpawnRoot != null && heroSpawnRoot.transform.childCount > i)
                {
                    _teamWorldPositions[i] = heroSpawnRoot.transform.GetChild(i).position;
                }
            }

            if (_enemyWorldPositions[i] == Vector3.zero)
            {
                if (_enemyRanks[i] != null && _enemyRanks[i].GetEntityRuntimeData() != null)
                {
                    _enemyWorldPositions[i] = _enemyRanks[i].GetEntityRuntimeData().transform.position;
                }
                else if (enemySpawnRoot != null && enemySpawnRoot.transform.childCount > i)
                {
                    _enemyWorldPositions[i] = enemySpawnRoot.transform.GetChild(i).position;
                }
            }
        }

        _hasPositions = true;
    }


}



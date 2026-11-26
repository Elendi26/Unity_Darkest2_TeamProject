using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

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
    #endregion

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
            extraSpeed[i] = Random.Range(0, 6);
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
        if (_orderedTurn.Count <= 0)
        {
            _roundOver = true;
            return;
        }

        else
        {
            _roundOver = false;
        }

        _currentTurn.InputCurrentTurnEntity(_orderedTurn.First());

        _orderedTurn.RemoveAt(0);
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
            
    }


}



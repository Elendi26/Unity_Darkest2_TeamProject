using JetBrains.Annotations;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;
using System;
public interface ICombatData
{
    public ICombatData ReturnCombatData();
}

public enum EEntityStat
{
    HP,
    MaxHP,
    Stress,
    MaxStress,
    Speed,
    CurrentSpeed,
    Movement,
    BleedRes,
    BlightRes,
    BurnRes,
    DiseaseRes,
    StunRes,
    MoveRes,
    DebuffRes,
    DeathblowRes,
    ExtraCritical,
    Hero,
    Dead
}

public class CEntityRuntimeData : MonoBehaviour, IRankEntity, ICombatData
{
    private CEntityBasicDataSO _heroBasicData;

    private string _name;
    private string _race;

    private int _maxHP;
    private int _currentHP;
    private int _maxStress;
    private int _currentStress;

    private Dictionary<string, int> _relationships;

    private int _speed;
    private int _currentSpeed;

    private float _bleedRes;
    private float _blightRes;
    private float _burnRes;
    private float _diseaseRes;
    private float _stunRes;
    private float _moveRes;
    private float _debuffRes;
    private float _deathblowRes;

    private ERank _currentRank;
    private int _movement;

    public CEntitySkillData _skillData;
    public CTokenStatus _tokenStatus = new CTokenStatus();
    public List<SDamageoverTime> _damageoverTime = new List<SDamageoverTime>();

    private Animator _animator;
    private CEntityAnimation _entityAnimation;



    private bool _dead = false;
    private bool _hero = true;
    private float _extraCritical = 0f;
    private bool _isCorpse = false;
    private int _corpseTurn = 1;
    private float _entityTimeScale = 1f;
    private Renderer[] _originRenderers;

    private GameObject _entityModel;
    private GameObject _corpseModel;

    private CMonsterBrain _monsterBrain;

    // 프로퍼티

    public string EntityName => _name;
    public string Race => _race;
    public bool IsCorpse => _isCorpse;
    public int CorpseTurn => _corpseTurn;
    public Animator Animator => _animator;
    public CEntityAnimation EntityAnimation => _entityAnimation;
    public float LocalDeltaTime => Time.timeScale * _entityTimeScale;
    

    // 원본 SO데이터에 존재하는 초상화 프로퍼티에 연결
    public Sprite TurnPortrait => _heroBasicData != null ? _heroBasicData.HeroPortrait : null;



    public CMonsterBrain MonsterBrain { get; set; }

    public CEntityBasicDataSO HeroBasicData => _heroBasicData;



    // 이벤트

    public event Action<float, float> ChangedHP;

    public event Action<float, float> ChangedStress;

    private void Awake()
    {
        if(_heroBasicData != null)
        {
            InitializeRuntimeData();
        }

    }

    private void InitializeRuntimeData()
    {
        if (_heroBasicData == null)
        {
            Debug.LogError($"[{gameObject.name}] CHeroBasicDataSO가 할당되지 않았습니다!", this);
            return;
        }

        _name = _heroBasicData.Name;
        _race = _heroBasicData.JobTitle;

        _currentHP = _heroBasicData.MaxHP;
        _maxHP = _heroBasicData.MaxHP;
        _currentStress = 0;
        _maxStress = 10;

        _speed = _heroBasicData.Speed;
        _currentSpeed = 0;

        _bleedRes = _heroBasicData.BleedRes;
        _blightRes = _heroBasicData.BlightRes;
        _burnRes = _heroBasicData.BurnRes;
        _diseaseRes = _heroBasicData.DiseaseRes;
        _stunRes = _heroBasicData.StunRes;
        _moveRes = _heroBasicData.MoveRes;
        _debuffRes = _heroBasicData.DebuffRes;
        _deathblowRes = _heroBasicData.DeathblowRes;

        _movement = _heroBasicData.Movement;

        _hero = _heroBasicData.Hero;
        _extraCritical = 0f;

        _relationships = new Dictionary<string, int>();

        _tokenStatus.LoadTokenData();

        Debug.Log($"[{_name}] 런타임 데이터 초기화 완료 (MaxHP: {_currentHP}, Speed: {_speed})");
    }

    public float ModifyStat(EEntityStat statType, float amount)
    {
        switch (statType)
        {
            case EEntityStat.HP:
                // 대미지를 입기 전에 이미 죽음의 문턱에 진입하여 체력이 0인지 기록
                bool isAlready0HP = (_currentHP <= 0);

                _currentHP += Mathf.RoundToInt(amount);
                _currentHP = Mathf.Clamp(_currentHP, 0, _heroBasicData != null ? _heroBasicData.MaxHP : _currentHP);
                ChangedHP?.Invoke(GetStat(EEntityStat.HP), GetStat(EEntityStat.MaxHP));

                if (_currentHP <= 0 && amount < 0) OnDeathblow(isAlready0HP);
                else
                {
                    ShowDamageTextUI(Mathf.Abs(amount));
                }
                    break;

            case EEntityStat.MaxHP:
                Debug.Log("최대체력값은 변경할 수 없습니다");
                break;

            case EEntityStat.Stress:
                _currentStress += Mathf.RoundToInt(amount);
                _currentStress = Mathf.Clamp(_currentStress, 0, 10);
                ChangedStress?.Invoke(GetStat(EEntityStat.Stress), GetStat(EEntityStat.MaxStress));

                if (_currentStress >= 10) OnMeltdown();
                break;
            case EEntityStat.MaxStress:
                Debug.Log("최대 스트레스값은 변경할 수 없습니다");
                break;

            case EEntityStat.Speed:
                Debug.Log("기본 속도는 변경할 수 없습니다");
                break;
            case EEntityStat.CurrentSpeed:
                _currentSpeed += Mathf.RoundToInt(amount);
                break;

            case EEntityStat.Movement:
                _movement += Mathf.RoundToInt(amount);
                break;

            case EEntityStat.BleedRes: _bleedRes += amount; break;
            case EEntityStat.BlightRes: _blightRes += amount; break;
            case EEntityStat.BurnRes: _burnRes += amount; break;
            case EEntityStat.DiseaseRes: _diseaseRes += amount; break;
            case EEntityStat.StunRes: _stunRes += amount; break;
            case EEntityStat.MoveRes: _moveRes += amount; break;
            case EEntityStat.DebuffRes: _debuffRes += amount; break;
            case EEntityStat.DeathblowRes: _deathblowRes += amount; break;
            case EEntityStat.ExtraCritical: _extraCritical += amount; break;
            case EEntityStat.Dead:
                Debug.Log("죽음은 EntityDead로 변경");
                break;
            case EEntityStat.Hero:
                Debug.Log("Hero 여부는 변경할 수 없습니다");
                break;
        }

        Debug.Log($"[{_name}] 스탯 변경됨 -> {statType} : {amount}");

        return amount;
    }

    public float GetStat(EEntityStat statType)
    {
        return statType switch
        {
            EEntityStat.HP => _currentHP,
            EEntityStat.MaxHP => _maxHP,
            EEntityStat.Stress => _currentStress,
            EEntityStat.MaxStress => _maxStress,
            EEntityStat.Speed => _speed,
            EEntityStat.CurrentSpeed => _currentSpeed,
            EEntityStat.Movement => _movement,
            EEntityStat.BleedRes => _bleedRes,
            EEntityStat.BlightRes => _blightRes,
            EEntityStat.BurnRes => _burnRes,
            EEntityStat.DiseaseRes => _diseaseRes,
            EEntityStat.StunRes => _stunRes,
            EEntityStat.MoveRes => _moveRes,
            EEntityStat.DebuffRes => _debuffRes,
            EEntityStat.DeathblowRes => _deathblowRes,
            EEntityStat.ExtraCritical => _extraCritical,
            EEntityStat.Hero => _hero ? 1f : 0f,

            _ => 0f,
        };
    }

    public void ResetStatToDefault(EEntityStat statType)
    {
        if (_heroBasicData == null)
        {
            Debug.LogError($"[{gameObject.name}] CHeroBasicDataSO가 없어 스탯을 초기화할 수 없습니다.", this);
            return;
        }

        switch (statType)
        {
            case EEntityStat.HP:
                _currentHP = _heroBasicData.MaxHP;
                ChangedHP?.Invoke(_currentHP, _heroBasicData.MaxHP);
                break;
            case EEntityStat.MaxHP:
                _maxHP = _heroBasicData.MaxHP;
                break;

            case EEntityStat.Stress:
                _currentStress = 0;
                break;
            case EEntityStat.MaxStress:
                _maxStress = 10;
                break;

            case EEntityStat.Speed:
                _speed = _heroBasicData.Speed;
                break;
            case EEntityStat.CurrentSpeed:
                _currentSpeed = _speed;
                break;

            case EEntityStat.Movement:
                _movement = _heroBasicData.Movement;
                break;

            case EEntityStat.BleedRes: _bleedRes = _heroBasicData.BleedRes; break;
            case EEntityStat.BlightRes: _blightRes = _heroBasicData.BlightRes; break;
            case EEntityStat.BurnRes: _burnRes = _heroBasicData.BurnRes; break;
            case EEntityStat.DiseaseRes: _diseaseRes = _heroBasicData.DiseaseRes; break;
            case EEntityStat.StunRes: _stunRes = _heroBasicData.StunRes; break;
            case EEntityStat.MoveRes: _moveRes = _heroBasicData.MoveRes; break;
            case EEntityStat.DebuffRes: _debuffRes = _heroBasicData.DebuffRes; break;
            case EEntityStat.DeathblowRes: _deathblowRes = _heroBasicData.DeathblowRes; break;

            case EEntityStat.ExtraCritical:
                _extraCritical = 0f;
                break;
            case EEntityStat.Hero:
                _hero = _heroBasicData.Hero;
                break;
        }

        Debug.Log($"[{_name}] 스탯 초기화됨 -> {statType}이(가) 기본값으로 복구되었습니다.");
    }

    /// <summary>
    /// 스포너에서 받은 외부 데이터를 순서대로 데이터 호출 및 저장을 용이하게 해주는 함수
    /// </summary>
    /// <param name="basicData"></param>
    /// <param name="rank"></param>
    /// <param name="savedHP"></param>
    /// <param name="savedStress"></param>
    public void Init(CEntityBasicDataSO basicData, ERank rank, int savedHP, int savedStress)
    {
        if(basicData == null)
        {
            CPrint.Warn("데이터 연결 실패 영웅 데이터가 비어있습니다");
            return;
        }

        if(savedHP <= 0)
        {
            CPrint.Warn("이미 사망한 영웅입니다");
            return;
        }

        // 먼저 데이터 넣어주고
        _heroBasicData = basicData;
        
        // 이름, 체력, 속도, 저항치 채워주고
        InitializeRuntimeData();

        _currentHP = Mathf.Clamp(savedHP, 1, _maxHP);

        ChangedHP?.Invoke(_currentHP, _heroBasicData.MaxHP);

        _currentStress = Mathf.Clamp(savedStress, 0, _maxStress);


        // 스킬 데이터 생성
        _skillData = new CEntitySkillData(basicData.HeroSkills.Count);

        if (_skillData != null)
        {
            _skillData.InputSkillData(basicData.HeroSkills);
        }

        InitializeRank(rank);

        // 기존 방식은 콜리더랑 런타임데이터까지 꺼지기 때문에 수정했습니다.
        _animator = GetComponentInChildren<Animator>();
        _entityAnimation = GetComponentInChildren<CEntityAnimation>();
        _originRenderers = GetComponentsInChildren<Renderer>(true);


    }

    public void InitializeRank(ERank rank)
    {
        _currentRank = rank;
    }

    public void ChangeRank(ERank rank)
    {
        _currentRank = rank;
    }

    public ERank GetRank()
    {
        return _currentRank;
    }

    public ICombatData ReturnCombatData()
    {
        return this;
    }

    public bool IsHero()
    {
        return _hero;
    }

    public void EntityDead()
    {
        // 이미 시체인데 체력이 0이하인 경우 파괴
        if(_isCorpse)
        {
            _corpseTurn = 0;
            DestroyCorpse();
            return;
        }

        _dead = true;
        _isCorpse = true;
        _corpseTurn = 1;

        // 렌더러만 비활성화
        if(_originRenderers != null)
        {
            for(int i = 0; i < _originRenderers.Length; i++)
            {
                if( _originRenderers[i] != null )
                {
                    _originRenderers[i].enabled = false;
                }
            }
        }

        // 시체 소환
        if(IsHero())
        {
            _corpseModel = Instantiate(CCombatManager.Instance.HCorpsePrefab, transform.position, Quaternion.Euler(0,45f,0), transform);
            _corpseModel.transform.localPosition = new Vector3(0, 0.6f, 0);
            _corpseModel.transform.localScale = transform.localScale;
        }

        else
        {
            _corpseModel = Instantiate(CCombatManager.Instance.ECorpsePrefab, transform.position, Quaternion.Euler(0, 45f, 0), transform);
            _corpseModel.transform.localPosition = new Vector3(0, 0.6f, 0);
            _corpseModel.transform.localScale = transform.localScale;
        }

        // 시체 체력을 죽은 엔티티의 최대 체력으로 다시 부여 및 체력바 갱신
        float maxHP = GetStat(EEntityStat.MaxHP);
        ModifyStat(EEntityStat.HP, maxHP);
        ChangedHP?.Invoke(GetStat(EEntityStat.HP), maxHP);

        // 시체 프리팹 찾아서 강제 연결하기
        CombatEntityHUD rootHUD = GetComponentInChildren<CombatEntityHUD>(true);
        if(rootHUD != null)
        {
            rootHUD.RenewHUD(this);
        }

        CombatEntityHUD corpseHUD = _corpseModel.GetComponentInChildren<CombatEntityHUD>(true);
        if(corpseHUD != null)
        {
            corpseHUD.RenewHUD(this);
        }

        // 아무 행동도 할 수 없기 때문에 턴 대기열에서는 제거
        if (CCombatManager.Instance != null)
        {
            CCombatManager.Instance.RemoveDeadEntityTurn(this);
        }

    }

    public bool IsDead()
    {
        return _dead;
    }

    public void InputAnimator(Animator animator)
    {
        _animator = animator;
    }

    public Animator GetAnimator()
    {
        return _animator;
    }

    public void AddDamageoverTime(SDamageoverTime dot)
    {
        _damageoverTime.Add(dot);
    }

    public void CheckDamageoverTime()
    {
        foreach(var dot in _damageoverTime)
        {

        }
    }

    private void OnDeathblow(bool already0HP)
    {
        // 영웅일때는 죽음의 문턱 발생
        if(_hero)
        {
            CPrint.Log("죽음의 문턱 도달!");
            float value = UnityEngine.Random.Range(0, 100);

            if(value <= _deathblowRes)
            {
                CPrint.Log("죽음의 일격 저항!");
                ShowDeathsDoorTextUI(already0HP);
            }

            else
            {
                Debug.Log($"{_name}이(가) 쓰러졌습니다!");
                ShowDeathblowImageUI();
                EntityDead();
            }
        }


        // 적일때는 바로 사망판정으로
        else if(!_hero)
        {
            Debug.Log($"{_name}이(가) 쓰러졌습니다!");
            EntityDead();
        }

    }

    private void OnMeltdown()
    {
        Debug.Log($"{_name}이(가) 붕괴(Meltdown) 상태가 되었습니다!");
    }

    public void AnimateTrigger(string trigger)
    {
        _animator.SetTrigger(trigger);
    }

    /// <summary>
    /// 스킬을 사용하지 않고있는 다른 엔티티들의 아이들 애니메이션을 멈추게하기 위한 함수
    /// </summary>
    /// <param name="scale"></param>
    public void ManageLocalTimescale(float scale)
    {
        _entityTimeScale = scale;

        if(_animator != null)
        {
            _animator.speed = scale;
        }
    }

    public void ShowDamageTextUI(float damage)
    {
        if(CCombatManager.Instance == null || CCombatManager.Instance.DirectPrefab == null)
        {
            CPrint.Warn("인스펙터 참조 오류 인스펙터 확인");
            return;
        }

        // 엔티티 머리 윗 부분에 UI 출력
        Vector3 spawnPos = transform.position + new Vector3(0.6f, 3, 0) - (Camera.main.transform.forward * 0.5f);
        CombatDirection_UI directionUI = Instantiate(CCombatManager.Instance.DirectPrefab, spawnPos, Quaternion.identity);

        directionUI.DamageDiretion(damage);

    }


    /// <summary>
    /// 죽음의 문턱 진입시 텍스트를 출력하는 함수
    /// </summary>
    /// <param name="aleady0hp"></param>
    public void ShowDeathsDoorTextUI(bool aleady0hp)
    {
        if(CCombatManager.Instance == null || CCombatManager.Instance.DirectPrefab == null)
        {
            return;
        }

        Vector3 spawnPos = transform.position + new Vector3(0.3f, 1, 0) - (Camera.main.transform.forward * 0.5f);
        CombatDirection_UI directionUI = Instantiate(CCombatManager.Instance.DirectPrefab, spawnPos, Quaternion.identity);

        directionUI.OnDeathDoor(aleady0hp);
    }

    /// <summary>
    /// 영웅이 죽음의 저항을 실패하여 결국 사망에 이르게 되었을 때, 죽음의 일격 이미지를 출력하는 함수
    /// </summary>
    public void ShowDeathblowImageUI()
    {
        if(CCombatManager.Instance == null || CCombatManager.Instance.DirectPrefab == null)
        {
            return;
        }

        Vector3 spawnPos = transform.position + new Vector3(0.3f, 1, 0) - (Camera.main.transform.forward * 0.5f);
        CombatDirection_UI directionUI = Instantiate(CCombatManager.Instance.DirectPrefab, spawnPos, Quaternion.identity);
        directionUI.OnDeathblow();
    }

    /// <summary>
    /// 시체 체력이 0됐거나 라운드가 끝나서 시체 유지 라운드가 0이 되었을 대 파괴하는 함수
    /// </summary>
    public void DestroyCorpse()
    {
        if(_isCorpse == false)
        {
            return;
        }

        // 시간 줄이고
        _corpseTurn--;

        // 전투 라인업에서 제외
        if( _corpseTurn <= 0 )
        {
            if (CCombatManager.Instance != null)
            {
                CCombatManager.Instance.OnCombatEntities.Remove(this);
            }
        }

        // 루트 HUD 찾아서 HUD 비활성화 하기
        CombatEntityHUD rootHUD = GetComponentInChildren<CombatEntityHUD>(true);
        if (rootHUD != null)
        {
            rootHUD.gameObject.SetActive(false);
        }

        if(_corpseModel != null)
        {
            CombatEntityHUD corpseHUD = _corpseModel.GetComponentInChildren<CombatEntityHUD>(true);
            if(corpseHUD != null)
            {
                corpseHUD.gameObject.SetActive(false);
            }
            // 모델링 파괴하고
            Destroy(_corpseModel);

            // 변수 비우기
            _corpseModel = null;
        }

        if(CCombatManager.Instance != null)
        {
            CCombatManager.Instance.PullEnemyRankForward(this);
        }

        Destroy(gameObject);
    }

}
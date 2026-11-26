using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UserProfileData;

public class CombatEnemySpawner : MonoBehaviour
{
    #region 인스펙터
    [Header("소환할 적 몬스터 SO")]
    [SerializeField] private CEnemyGroupDataSO[] _enemiesGroupSO;

    [Header("적 몬스터 스폰 포인트")]
    [SerializeField] private GameObject[] _spawnPoints;

    [Header("적 랭크 칸")]
    [SerializeField] private CCombatRank[] _enemyRank = new CCombatRank[4];

    [Header("체력 HUD 프리팹")]
    [SerializeField] private CombatEntityHUD _hudPrefab;

    [Header("모델링 회전값")]
    [SerializeField] private float _rotationAngle = -45f;

    [Header("모델링 곱할 스케일값")]
    [SerializeField] private float _scaleMulti = 0.01f;

    #endregion

    // 내부 변수

    // 한 함수에 국한되지 않고 이 스크립트에서 전체적으로 사용하기 위해 내부 변수로 전환
    private CEntityBasicDataSO[] _spawnEnemies;

    // 소환한 적들의 대한 정보를 전달할 수 있도록 리스트로 만들어 추가하고 프로퍼티로 외부에서 확인할 수 있게 한다.
    private List<CEntityRuntimeData> _spawnedEnemyData = new List<CEntityRuntimeData>();

    private bool _completeSpawned = false;

    // 프로퍼티

    public List<CEntityRuntimeData> SpawnedEnemyData => _spawnedEnemyData;

    public bool CompleteSpawn => _completeSpawned;


    private void Awake()
    {
        if(_enemiesGroupSO == null || _enemiesGroupSO.Length == 0)
        {
            CPrint.Warn("인스펙터가 비어있다. 적 영웅 그룹을 넣으시오");
            return;
        }
    }


    void Start()
    {
        RandomGroupChoise();
    }

    /// <summary>
    /// 여러개의 몬스터 그룹 중 하나를 뽑아 스폰 준비를 시키는 함수
    /// </summary>
    public void RandomGroupChoise()
    {
        // 인덱스 하나를 랜덤으로 뽑아서
        int groupIndex = Random.Range(0, _enemiesGroupSO.Length);

        // 그룹을 선택
        CEnemyGroupDataSO mainGroup = _enemiesGroupSO[groupIndex];

        if (mainGroup != null)
        {
            _spawnEnemies = mainGroup.EnemyBasicData;

            EnemyModelSpawn();
        }
    }

    public void EnemyModelSpawn()
    {
        // 어느 한쪽의 배열의 길이가 넘어가지 못하도록 가장 낮은 값을 먼저 잡고
        int range = Mathf.Min(_spawnEnemies.Length, _spawnPoints.Length, _enemyRank.Length);

        for (int i = 0; i < range; i++)
        {
            // 일단 비어있는 슬롯 넘기고
            if (_spawnEnemies[i] == null || _spawnEnemies[i].HeroModel == null)
            {
                continue;
            }

            // 모델링 스폰시키고
            GameObject spawnEnemy = Instantiate(_spawnEnemies[i].HeroModel, _spawnPoints[i].transform);

            // 런타임 데이터 가져오고
            CEntityRuntimeData runtimeData = spawnEnemy.GetComponent<CEntityRuntimeData>();

            // 애니메이터 가져와서
            Animator eAnimator = spawnEnemy.GetComponent<Animator>();

            if(eAnimator != null)
            {
                runtimeData.InputAnimator(eAnimator);
            }


            // 몬스터 AI 주입시키고
            CMonsterBrain monsterAI = new CMonsterBrain();

            // 리스트에 욕망 추가하고
            monsterAI.SkillDesireSet.Add(new CSkillSelectionSelfHpWeighted());
            monsterAI.TargetDesireSet.Add(new CTargetSelectionHpRatioWeighted());

            // 런타임데이터에 할당
            runtimeData.MonsterBrain = monsterAI;

            // HUD UI소환
            CombatEntityHUD hudUI = Instantiate(_hudPrefab, _spawnPoints[i].transform);

            // 위치, 회전, 스케일 값 고정
            spawnEnemy.transform.localPosition = new Vector3(0, 0.1f, 0);
            spawnEnemy.transform.localRotation = Quaternion.Euler(0, _rotationAngle, 0);
            spawnEnemy.transform.localScale = spawnEnemy.transform.localScale * _scaleMulti;

            hudUI.transform.localPosition = Vector3.zero;

            ERank rank = ERank.None;

            // 각 번호를 ERank에 대입
            switch (i)
            {
                case 0:
                    rank = ERank.One;
                    break;
                case 1:
                    rank = ERank.Two;
                    break;
                case 2:
                    rank = ERank.Three;
                    break;
                case 3:
                    rank = ERank.Four;
                    break;
            }

            if (runtimeData != null)
            {
                // 데이터 넣어주고
                if ( rank != ERank.None)
                {
                    runtimeData.Init(_spawnEnemies[i], rank, _spawnEnemies[i].MaxHP, 0);
                }

                // HUD 동기화
                hudUI.RenewHUD(runtimeData);

                // 여기도 데이터 넣어주고
                if (_enemyRank[i] != null)
                {
                    _enemyRank[i].InputEntityData(runtimeData);
                }

                // 리스트에 담기
                _spawnedEnemyData.Add(runtimeData);

                // 매니저에도 넣기
                if(CCombatManager.Instance != null)
                {
                    CCombatManager.Instance.OnCombatEntities.Add(runtimeData);
                }
            }
        }

        _completeSpawned = true;

        
        CCombatManager.Instance?.StartCombat();
    }
}

using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CombatHeroSpawner : MonoBehaviour
{
    #region 인스펙터
    [Header("존재하는 영웅 SO 모두 넣기")]
    [SerializeField] private CEntityBasicDataSO[] _heroes;

    [Header("영웅 4명 스폰 포인트")]
    [SerializeField] private Transform[] _spawnPoints;

    [Header("영웅 전용 HUD 프리팹")]
    [SerializeField] private CombatEntityHUD _prefabHUD;

    [Header("영웅 모델링 회전값")]
    [SerializeField] private float _rotationAngle = 45f;

    [Header("영웅 모델링 곱할 스케일값")]
    [SerializeField] private float _scaleMulti = 0.01f;

    #endregion

    // 내부 변수

    // 데이터를 찾아볼 수 있게 메모리에 딕셔너리 구축하기
    Dictionary<string, CEntityBasicDataSO> _heroDataDict = new Dictionary<string, CEntityBasicDataSO>();

    // 나중에 확장을 위해 해당 슬롯에 위치한 영웅의 정보를 쉽게 찾을 수 있도록 하는 변수
    private GameObject[] _heroesinParty = new GameObject[4];
    
    // 소환한 영웅들의 대한 정보를 전달할 수 있도록 리스트로 만들어 추가하고 프로퍼티로 외부에서 확인할 수 있게 한다.
    private List<CEntityRuntimeData> _spawnedHeroData = new List<CEntityRuntimeData>();

    // 프로퍼티

    public List<CEntityRuntimeData> SpawnedHeroData => _spawnedHeroData;

    private void Awake()
    {
        for(int i = 0;  i < _heroes.Length; i++)
        {
            // 등록된 영웅이 널이 아니고, 고유 아이디도 비어있지 않아야하고, 키도 포함되어있지 않아야한다.
            if (_heroes[i] != null && _heroes[i].HeroID != null && !_heroDataDict.ContainsKey(_heroes[i].HeroID))
            {
                // 딕셔너리 추가
                _heroDataDict.Add(_heroes[i].HeroID, _heroes[i]);
            }
        }
        
    }


    void Start()
    {
        // 프로필 데이터를 불러오고
        if (DataSaveManager.Instance != null)
        {
            UserProfileData userData = DataSaveManager.Instance.LoadProfile(DataSaveManager.Instance.CurrentSlotIndex);

            if(userData == null || userData.PartyHeroes.Count == 0)
            {
                CPrint.Warn("저장된 데이터가 없다.");
                return;
            }

            // 파티 영웅 리스트 순회해서 각 영웅의 세이브 데이터에서 파티 슬롯 인덱스를 검사하고
            for(int i = 0; i < userData.PartyHeroes.Count; i++)
            {
                // 파티에 포함되어 있는 영웅 데이터를 찾아서
                if(userData.PartyHeroes[i].PartySlotIndex != -1)
                {
                    // 파티 슬롯 번호를 변수로 저장하고
                    int slotIndex = userData.PartyHeroes[i].PartySlotIndex;

                    // 원본 SO파일을 꺼낸 뒤에
                    if (_heroDataDict.TryGetValue(userData.PartyHeroes[i].HeroID, out CEntityBasicDataSO data))
                    {
                        // 해당 파티 슬롯에 해당 영웅 모델을 스폰시킨다.
                        HeroModelSpawn(data, slotIndex);
                    }

                    else { CPrint.Warn($"딕셔너리 매칭 실패 ID: {userData.PartyHeroes[i].HeroID}"); }
                }
            }
        }

        else
        {
            CPrint.Error("현재 세이브 데이터가 존재하지 않음");
            return;
        }
    }

    /// <summary>
    /// 스폰 포인트에 영웅을 본인이 가지고있는 슬롯 정보에 맞춰 모델링을 스폰시키는 함수.
    /// </summary>
    /// <param name="heroData"></param>
    /// <param name="slotIndex"></param>
    private void HeroModelSpawn(CEntityBasicDataSO heroData, int slotIndex)
    {
        if(heroData == null || heroData.HeroModel == null)
        {
            CPrint.Warn("영웅 데이터 혹은 영웅 모델링이 존재하지 않는다.");
            return;
        }

        if(_spawnPoints == null || slotIndex < 0 || slotIndex >= _spawnPoints.Length || _spawnPoints[slotIndex] == null)
        {
            CPrint.Warn("인스펙터가 비어있다. 인스펙터 확인");
            return;
        }

        // 모델링 스폰포인트 자식으로 생성시키기
        GameObject spawnModel = Instantiate(heroData.HeroModel, _spawnPoints[slotIndex]);

        //CEntityRuntimeData spawnHero = new CEntityRuntimeData();
        

        //if (spawnHero != null)
        //{
        //    _spawnedHeroData.Add(spawnHero);
        //}


        CombatEntityHUD hudUI = Instantiate(_prefabHUD, _spawnPoints[slotIndex]);

        _heroesinParty[slotIndex] = spawnModel;

        // 위치, 회전, 스케일 값 고정
        spawnModel.transform.localPosition = Vector3.zero;
        spawnModel.transform.localRotation = Quaternion.Euler(0, _rotationAngle, 0);
        spawnModel.transform.localScale = spawnModel.transform.localScale * _scaleMulti;

        hudUI.transform.localPosition = Vector3.zero;

    }


}

using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

// 전투 노드·도로 전투 → 전투 씬(팀원 Combat_test)을 덧붙여 열고, 끝나면 닫고 다음으로
//  ㄴ 전투 씬은 저장된 파티와 무작위 적을 스스로 꺼내 싸운다 → 여기서는 열기·닫기와 끝 판정만 한다
//  ㄴ 전투 쪽 끝 알림(CCombatManager.CombatOver)이 아직 비어 있다 → 양쪽 스포너를 지켜보다가 한쪽이 모두 쓰러지면 끝으로 본다
public class CCombatNodeHandler : MonoBehaviour, INodeHandler, INodeHandlerGroup, IRoadCombatStarter
{
	// 괴물 소굴·성채 노드도 이 전투로 받는다 → 지도 런너는 종류마다 핸들러 하나라 종류별 입구를 따로 둔다
	private class CCombatNodeEntry : INodeHandler
	{
		private readonly CCombatNodeHandler _owner;
		private readonly ENodeType _type;

		public ENodeType Type => _type;

		public CCombatNodeEntry(CCombatNodeHandler owner, ENodeType type)
		{
			_owner = owner;
			_type = type;
		}

		public void Enter(CNodeInstance node, Action onComplete)
		{
			_owner.Enter(node, onComplete);
		}
	}

	#region 상수
	private const string FIRE_IMAGE_NAME = "fireImage";
	private const string POPUP_PANEL_NAME = "background";
	// 전투 UI(최대 300)·영웅 정보 창(300)보다 위, 끌기 아이콘(1000)보다 아래
	private const int PAUSE_SORTING_ORDER = 900;
	private const string DONT_DESTROY_SCENE = "DontDestroyOnLoad";
	#endregion

	#region 인스펙터
	// 게임 매니저 없이 주행 씬만 재생할 때 직접 열 씬
	[SerializeField] private string _combatScenePath = "Assets/Scenes/UI/Combat_test.unity";
	// 마지막 적이 쓰러지고 닫기까지 기다리는 시간 (쓰러지는 모습을 보여 주게)
	[SerializeField] private float _closeDelay = 1.5f;
	// 전투에 들어가고 나올 때 화면을 검게 덮었다 걷는 시간
	[SerializeField] private float _fadeDuration = 0.4f;
	// 전투가 끝나지 않을 때 이겼다고 치고 넘어가는 키 → 전투 쪽 마무리 전까지 임시
	[SerializeField] private bool _allowSkipKey = true;
	[SerializeField] private Key _skipKey = Key.F9;
	// 전투 중 일시정지의 원정 포기 자리에 쓸 글자
	[SerializeField] private string _abandonLabel = "전투 포기";
	// 괴물 소굴·성채 노드에 도착해도 전투를 연다
	[SerializeField] private bool _handleCreatureDen = true;
	[SerializeField] private bool _handleFortress = true;
	// 전투를 닫고 화면이 밝아지면 결과창을 띄운다 ('계속' 을 눌러야 주행이 이어진다)
	[SerializeField] private bool _showResult = true;
	// 결과창 글꼴 (비우면 기본 글꼴)
	[SerializeField] private Font _resultFont;
	// 이겼을 때 보상 (유물·횃불·장신구 확률)
	[SerializeField] private CCombatRewardTable _victoryReward = new CCombatRewardTable();
	// 건너뛰기(F9)도 이긴 것으로 보고 보상을 준다 → 전투 쪽 마무리 전까지 시험용
	[SerializeField] private bool _rewardOnSkip = true;

	[Header("배경 (마차 옆 길가를 찍어 깐다)")]
	[SerializeField] private bool _useBackdrop = true;
	[SerializeField] private float _backdropHeight = 1.8f;
	// 마차에서 옆으로 떨어진 거리 → 마차가 찍히지 않게
	[SerializeField] private float _backdropSideOffset = 3.5f;
	[SerializeField] private float _backdropFov = 55f;
	// 아래로 숙이는 각도 → 지평선이 영웅 머리 위쯤 오게
	[SerializeField] private float _backdropPitch = 6f;
	// 영웅·적이 돋보이게 배경을 어둡게
	[SerializeField] private Color _backdropTint = new Color(0.6f, 0.55f, 0.5f, 1f);
	[SerializeField, Range(0f, 1f)] private float _backdropShade = 0.85f;

	[SerializeField] private bool _logState = true;
	#endregion

	#region 내부 변수
	private readonly List<Behaviour> _hidden = new List<Behaviour>();
	private readonly CCombatBackdrop _backdrop = new CCombatBackdrop();
	private CRunPauseMenu _pauseMenu;
	private Action _onComplete;
	// 바퀴·장갑이 다 닳아 벌어진 길 위 전투 → 이기면 그 값을 하나 되돌린다 (그 밖의 전투는 None)
	private ERunValue _repairOnWin;
	private Scene _combatScene;
	private CombatHeroSpawner _heroSpawner;
	private CombatEnemySpawner _enemySpawner;
	private string _combatName;
	private List<INodeHandler> _extraHandlers;
	private bool _isBusy;
	private bool _isFighting;
	// 게임 매니저의 전투 모드로 열었는지 → 닫을 때도 같은 쪽으로 닫는다
	private bool _isOpenedByMode;
	private bool _isDrivingHidden;
	// 이번 전투 영웅에게 장신구 능력치를 더했는지
	private bool _isTrinketApplied;
	private bool _isTokenReady;
	// 라운드가 바뀐 것을 알아보려고 기억하는 행동 순서 목록
	private List<CEntityRuntimeData> _lastRoundOrder;
	private readonly System.Random _tokenRandom = new System.Random();
	// 결과창에 띄울 내용 (전투 씬을 내리기 전에 모은다)
	private CCombatResultData _result;
	#endregion

	public ENodeType Type => ENodeType.Combat;

	// 전투 노드 말고 더 받을 노드 종류 (괴물 소굴·성채)
	public IReadOnlyList<INodeHandler> ExtraHandlers
	{
		get
		{
			if (_extraHandlers == null)
			{
				_extraHandlers = new List<INodeHandler>();

				if (_handleCreatureDen)
				{
					_extraHandlers.Add(new CCombatNodeEntry(this, ENodeType.CreatureDen));
				}

				if (_handleFortress)
				{
					_extraHandlers.Add(new CCombatNodeEntry(this, ENodeType.Fortress));
				}
			}

			return _extraHandlers;
		}
	}

	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= HandleSceneLoaded;
		_backdrop.Release();
	}

	// 지도의 전투 노드에 도착했을 때
	public void Enter(CNodeInstance node, Action onComplete)
	{
		CNodeTypeData data = node != null ? node.TypeData : null;
		ECombatKind kind = data != null ? CCombatKindText.FromNode(data.Type) : ECombatKind.Node;

		if (kind == ECombatKind.None)
		{
			kind = ECombatKind.Node;
		}

		Begin(kind, data != null ? data.Type : ENodeType.Combat, null, data != null ? data.DisplayName : null, onComplete);
	}

	// 도로 이벤트가 전투로 이어질 때 (방책·습격 등)
	public void StartCombat(CRoadEventData roadEvent, CRunContext ctx, Action onFinished)
	{
		ECombatKind kind = roadEvent != null ? roadEvent.CombatKind : ECombatKind.Ambush;

		Begin(kind, ENodeType.None, roadEvent != null ? roadEvent.CombatId : null, roadEvent != null ? roadEvent.Title : null, onFinished, GetRepairOnWin(roadEvent, ctx));
	}

	// 바퀴·장갑이 0 이라 벌어진 전투인지 (길을 막아 늘 벌어지는 전투는 값이 남아 있으면 아니다)
	private static ERunValue GetRepairOnWin(CRoadEventData roadEvent, CRunContext ctx)
	{
		if (roadEvent == null)
		{
			return ERunValue.None;
		}

		ERunValue type = roadEvent.CombatWhenEmpty;

		if (type != ERunValue.Wheels && type != ERunValue.Armor)
		{
			return ERunValue.None;
		}

		CRunValue value = ctx != null && ctx.RunData != null ? ctx.RunData.Get(type) : null;

		return value != null && value.Current <= 0 ? type : ERunValue.None;
	}

	private void Begin(ECombatKind kind, ENodeType nodeType, string combatId, string place, Action onComplete, ERunValue repairOnWin = ERunValue.None)
	{
		string combatName = string.IsNullOrEmpty(combatId) ? $"{CCombatKindText.GetName(kind)} / {place}" : $"{CCombatKindText.GetName(kind)} / {combatId}";

		if (_isBusy)
		{
			CPrint.Warn($"CCombatNodeHandler : 이미 전투 중이라 '{combatName}' 을(를) 열지 않습니다.");
			return;
		}

		// 전투 씬은 저장된 파티로 영웅을 세운다 → 이번 런 파티가 없으면 싸울 영웅이 없어 끝나지 않는다
		if (!CRunCarry.PartyChosen)
		{
			CPrint.Warn($"CCombatNodeHandler : 아직 파티를 꾸리지 않아 '{combatName}' 을(를) 전투 없이 지나갑니다.");
			onComplete?.Invoke();
			return;
		}

		_combatName = combatName;
		_repairOnWin = repairOnWin;
		// 전투 씬이 올라오기 전에 채운다 → 적 스포너가 Start 에서 읽을 수 있게
		CCombatContext.Set(kind, nodeType, combatId, place);
		_onComplete = onComplete;
		StartCoroutine(EnterRoutine());
	}

	#region 입장
	private IEnumerator EnterRoutine()
	{
		_isBusy = true;
		_isFighting = false;
		_isOpenedByMode = false;
		_isDrivingHidden = false;
		_isTrinketApplied = false;
		_isTokenReady = false;
		_lastRoundOrder = null;
		_combatScene = default;

		// 전투 씬을 올리기 전에 길가를 찍어 둔다 → 올린 뒤에는 원점의 전투 모델이 함께 찍힐 수 있다
		CaptureBackdrop();

		// 주행 숨기기·영웅 세우기가 끝날 때까지 검게 덮어 둔다
		yield return CRunScreenFade.Cover(_fadeDuration);

		// 씬이 올라오는 즉시 주행 쪽을 숨긴다 → 카메라·EventSystem 이 둘씩 겹치지 않게
		SceneManager.sceneLoaded -= HandleSceneLoaded;
		SceneManager.sceneLoaded += HandleSceneLoaded;

		CGameManager gameManager = FindGameManager();

		if (gameManager != null)
		{
			CAddressableSceneManager sceneManager = CAddressableSceneManager.Instance;

			// 다른 씬 전환이 끝날 때까지 기다린다
			yield return new WaitWhile(() => sceneManager != null && sceneManager.IsTransitioning);

			bool loaded = false;

			yield return gameManager.ChangeGameModeAsync(gameManager.CombatModeId).ToCoroutine(result => loaded = result);

			_isOpenedByMode = loaded;

			if (!loaded)
			{
				CPrint.Warn("CCombatNodeHandler : 게임 매니저로 전투를 열지 못해 씬을 직접 엽니다.");
			}
		}

		// 게임 매니저 없이 주행 씬만 재생할 때 (테스트) → 씬을 직접 연다
		if (!_isOpenedByMode)
		{
			AsyncOperation load = LoadCombatScene();

			if (load == null)
			{
				SceneManager.sceneLoaded -= HandleSceneLoaded;
				Finish();
				yield break;
			}

			yield return load;
		}

		SceneManager.sceneLoaded -= HandleSceneLoaded;

		// 전투 매니저는 같은 오브젝트의 팀원 스크립트(CTeamRuntimeData)가 DontDestroyOnLoad 로 빼 간다 → 스포너로 씬을 찾는다
		if (!_combatScene.IsValid())
		{
			_combatScene = FindLoadedCombatScene();
		}

		_heroSpawner = FindInScene<CombatHeroSpawner>(_combatScene);
		_enemySpawner = FindInScene<CombatEnemySpawner>(_combatScene);

		if (_heroSpawner == null || _enemySpawner == null)
		{
			CPrint.Error($"CCombatNodeHandler : 열린 전투 씬에 영웅·적 스포너가 없습니다. ({_combatScenePath})");
			_backdrop.Release();
			ShowDrivingScene();
			yield return UnloadCombatScene();
			yield return CRunScreenFade.Uncover(_fadeDuration);
			Finish();
			yield break;
		}

		// 로드 콜백에서 이미 숨겼으면 다시 숨기지 않는다 (숨긴 목록을 잃지 않게)
		if (!_isDrivingHidden)
		{
			HideDrivingScene(_combatScene);
		}

		LinkTorch();
		LinkPauseMenu();
		_isFighting = true;

		if (_logState)
		{
			string skip = _allowSkipKey ? $" ({_skipKey} 키로 건너뛰기)" : string.Empty;

			CPrint.Log($"CCombatNodeHandler : '{_combatName}' 전투 시작{skip}");
		}

		// 영웅·적은 스포너 Start 에서 선다 → 한두 프레임 기다렸다가 걷는다
		yield return null;
		yield return null;
		yield return CRunScreenFade.Uncover(_fadeDuration);
	}

	// 에디터에서는 빌드 설정에 없어도 경로로 불러온다
	private AsyncOperation LoadCombatScene()
	{
#if UNITY_EDITOR
		return EditorSceneManager.LoadSceneAsyncInPlayMode(_combatScenePath, new LoadSceneParameters(LoadSceneMode.Additive));
#else
		if (SceneUtility.GetBuildIndexByScenePath(_combatScenePath) < 0)
		{
			CPrint.Error($"CCombatNodeHandler : {_combatScenePath} 이 빌드 설정에 없어 전투를 건너뜁니다.");
			return null;
		}

		return SceneManager.LoadSceneAsync(_combatScenePath, LoadSceneMode.Additive);
#endif
	}

	// 전투 매니저가 든 씬이 Additive 로 올라오면 → 첫 프레임 전에 주행 쪽을 숨긴다
	private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (mode != LoadSceneMode.Additive || scene == gameObject.scene || _isDrivingHidden)
		{
			return;
		}

		// 전투 매니저는 Awake 에서 이미 DontDestroyOnLoad 로 빠져나갔을 수 있다 → 영웅 스포너로 알아본다
		if (FindInScene<CombatHeroSpawner>(scene) == null)
		{
			return;
		}

		SceneManager.sceneLoaded -= HandleSceneLoaded;
		_combatScene = scene;
		HideDrivingScene(scene);
	}

	// 전투 씬 횃불을 런 횃불과 잇고 마차 횃불 같은 불빛을 붙인다
	private void LinkTorch()
	{
		TorchManager manager = FindInScene<TorchManager>(_combatScene);
		TorchiSlot_UI slot = FindInScene<TorchiSlot_UI>(_combatScene);
		CStagecoachController controller = FindFirstObjectByType<CStagecoachController>();
		CRunValue runTorch = controller != null && controller.RunData != null ? controller.RunData.Get(ERunValue.Torch) : null;

		if (manager == null || slot == null)
		{
			CPrint.Warn("CCombatNodeHandler : 전투 씬에 횃불(TorchManager·횃불 칸)이 없어 횃불을 잇지 않습니다.");
			return;
		}

		if (runTorch == null)
		{
			CPrint.Warn("CCombatNodeHandler : 런 횃불이 없어 전투 횃불 수치를 맞추지 않습니다.");
		}

		CCombatTorchView view = slot.gameObject.AddComponent<CCombatTorchView>();

		view.Setup(runTorch, manager, FindFireImage(slot.transform), FindPopupPanel(_combatScene));
	}

	// 횃불 툴팁의 판
	private static RectTransform FindPopupPanel(Scene scene)
	{
		TorchiPopup_UI popup = FindInScene<TorchiPopup_UI>(scene);

		if (popup == null)
		{
			return null;
		}

		Transform panel = popup.transform.Find(POPUP_PANEL_NAME);

		if (panel == null && popup.transform.childCount > 0)
		{
			panel = popup.transform.GetChild(0);
		}

		return panel as RectTransform;
	}

	// 횃불 칸의 불꽃 그림
	private static Image FindFireImage(Transform root)
	{
		Image[] images = root.GetComponentsInChildren<Image>(true);
		Image empty = null;

		for (int i = 0; i < images.Length; i++)
		{
			if (images[i].name == FIRE_IMAGE_NAME)
			{
				return images[i];
			}

			if (empty == null && images[i].sprite == null && images[i].transform != root)
			{
				empty = images[i];
			}
		}

		return empty;
	}

	// 영웅 스포너가 든 씬 (주행 씬 밖)
	private Scene FindLoadedCombatScene()
	{
		CombatHeroSpawner[] spawners = FindObjectsByType<CombatHeroSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);

		for (int i = 0; i < spawners.Length; i++)
		{
			if (spawners[i] != null && spawners[i].gameObject.scene != gameObject.scene)
			{
				return spawners[i].gameObject.scene;
			}
		}

		return default;
	}

	// 전투가 끝난 뒤 DontDestroyOnLoad 로 빠져나가 남은 전투 매니저를 치운다
	private void CleanupLeakedCombatManager()
	{
		CCombatManager[] managers = FindObjectsByType<CCombatManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);

		for (int i = 0; i < managers.Length; i++)
		{
			if (managers[i] == null || managers[i].gameObject.scene.name != DONT_DESTROY_SCENE)
			{
				continue;
			}

			Destroy(managers[i].gameObject);
		}
	}

	// 마차 옆 길가를 찍는다
	private void CaptureBackdrop()
	{
		if (!_useBackdrop)
		{
			return;
		}

		CCoach3DView coach = FindFirstObjectByType<CCoach3DView>();
		Transform anchor = coach != null ? coach.transform : null;

		if (anchor == null)
		{
			CCoachPhysics physics = FindFirstObjectByType<CCoachPhysics>();

			anchor = physics != null ? physics.transform : null;
		}

		if (!_backdrop.Capture(anchor, FindDrivingCamera(), _backdropHeight, _backdropSideOffset, _backdropFov, _backdropPitch))
		{
			CPrint.Warn("CCombatNodeHandler : 마차를 찾지 못해 전투 배경 없이 엽니다.");
		}
	}

	// 지금 화면을 그리는 주행 카메라 (배경색·하늘 설정을 빌린다)
	private Camera FindDrivingCamera()
	{
		Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);

		for (int i = 0; i < cameras.Length; i++)
		{
			if (cameras[i] != null && cameras[i].enabled && cameras[i].targetTexture == null && cameras[i].gameObject.scene == gameObject.scene)
			{
				return cameras[i];
			}
		}

		return null;
	}

	// 전투 화면을 그리는 카메라
	private static Camera FindCombatCamera(Scene scene)
	{
		if (!scene.IsValid() || !scene.isLoaded)
		{
			return null;
		}

		GameObject[] roots = scene.GetRootGameObjects();

		for (int i = 0; i < roots.Length; i++)
		{
			if (roots[i] == null)
			{
				continue;
			}

			Camera[] cameras = roots[i].GetComponentsInChildren<Camera>();

			for (int j = 0; j < cameras.Length; j++)
			{
				if (cameras[j].enabled && cameras[j].targetTexture == null)
				{
					return cameras[j];
				}
			}
		}

		return null;
	}

	private static T FindInScene<T>(Scene scene) where T : Component
	{
		if (!scene.IsValid() || !scene.isLoaded)
		{
			return null;
		}

		GameObject[] roots = scene.GetRootGameObjects();

		for (int i = 0; i < roots.Length; i++)
		{
			if (roots[i] == null)
			{
				continue;
			}

			T found = roots[i].GetComponentInChildren<T>(true);

			if (found != null)
			{
				return found;
			}
		}

		return null;
	}

	// 씬에 놓인 게임 매니저 중 전투 모드가 있는 것만 → Instance 로 빈 매니저를 새로 만들지 않는다
	private static CGameManager FindGameManager()
	{
		CGameManager gameManager = FindFirstObjectByType<CGameManager>();

		if (gameManager == null || gameManager.GetGameModeData(gameManager.CombatModeId) == null)
		{
			return null;
		}

		return gameManager;
	}
	#endregion

	#region 끝 판정
	private void Update()
	{
		if (!_isFighting)
		{
			return;
		}

		ApplyTrinkets();
		TickTrinketRound();

		ECombatResult result = DecideResult();

		if (result == ECombatResult.None)
		{
			return;
		}

		_isFighting = false;
		StartCoroutine(LeaveRoutine(result));
	}

	// 영웅이 다 세워지면 (전투 쪽 Init 으로 능력치가 초기화된 뒤) 끼운 장신구 능력치를 한 번 더한다
	private void ApplyTrinkets()
	{
		if (_isTrinketApplied || _heroSpawner == null || !_heroSpawner.CompleteSpawn)
		{
			return;
		}

		_isTrinketApplied = true;

		List<CEntityRuntimeData> heroes = _heroSpawner.SpawnedHeroData;
		int applied = 0;

		for (int i = 0; i < heroes.Count; i++)
		{
			applied += CHeroTrinketEffects.Apply(heroes[i], _logState);
		}

		if (_logState)
		{
			CPrint.Log($"CCombatNodeHandler : 장신구 효과 {applied}줄 적용");
		}

		// 토큰 데이터를 못 읽으면 전투 쪽 토큰 판정이 오류를 낸다 → 그때는 토큰을 주지 않는다
		_isTokenReady = CHeroTrinketEffects.IsTokenDataReady();

		if (!_isTokenReady)
		{
			CPrint.Warn("CCombatNodeHandler : 전투 쪽 토큰 데이터(TokenStructData)를 읽지 못해 장신구 토큰을 주지 않습니다.");
			return;
		}

		GrantTrinketTokens(ETrinketTokenTiming.CombatStart);
	}

	// 라운드가 바뀌면 (전투 매니저가 라운드마다 행동 순서 목록을 새로 만든다) 라운드 시작 토큰을 준다
	private void TickTrinketRound()
	{
		if (!_isTrinketApplied || !_isTokenReady || CCombatManager.Instance == null)
		{
			return;
		}

		List<CEntityRuntimeData> order = CCombatManager.Instance.OrderedTurn;

		if (order == null || ReferenceEquals(order, _lastRoundOrder))
		{
			return;
		}

		_lastRoundOrder = order;
		GrantTrinketTokens(ETrinketTokenTiming.RoundStart);
	}

	private void GrantTrinketTokens(ETrinketTokenTiming timing)
	{
		List<CEntityRuntimeData> heroes = _heroSpawner.SpawnedHeroData;
		int granted = 0;

		for (int i = 0; i < heroes.Count; i++)
		{
			granted += CHeroTrinketEffects.GrantTokens(heroes[i], timing, _tokenRandom, _logState);
		}

		if (_logState && granted > 0)
		{
			CPrint.Log($"CCombatNodeHandler : 장신구 토큰 {granted}개 ({timing})");
		}
	}

	private ECombatResult DecideResult()
	{
		if (_allowSkipKey && Keyboard.current != null && Keyboard.current[_skipKey].wasPressedThisFrame)
		{
			return ECombatResult.Skipped;
		}

		// 한쪽이라도 아직 세우는 중이면 기다린다
		if (_heroSpawner == null || _enemySpawner == null || !_heroSpawner.CompleteSpawn || !_enemySpawner.CompleteSpawn)
		{
			return ECombatResult.None;
		}

		if (IsAllDown(_enemySpawner.SpawnedEnemyData))
		{
			return ECombatResult.Victory;
		}

		if (IsAllDown(_heroSpawner.SpawnedHeroData))
		{
			return ECombatResult.Defeat;
		}

		return ECombatResult.None;
	}

	// 쓰러진(시체 포함) 것과 사라진 것은 쓰러진 것으로 본다
	private static bool IsAllDown(List<CEntityRuntimeData> entities)
	{
		if (entities == null)
		{
			return true;
		}

		for (int i = 0; i < entities.Count; i++)
		{
			if (entities[i] != null && !entities[i].IsDead())
			{
				return false;
			}
		}

		return true;
	}
	#endregion

	#region 퇴장
	private IEnumerator LeaveRoutine(ECombatResult result)
	{
		LogResult(result);

		// 이기거나 졌을 때만 쓰러지는 모습을 기다린다 (건너뛰기·포기는 바로)
		bool isFought = result == ECombatResult.Victory || result == ECombatResult.Defeat;

		if (isFought && _closeDelay > 0f)
		{
			yield return new WaitForSeconds(_closeDelay);
		}

		// 전투 화면을 검게 덮은 뒤 주행 쪽으로 바꾼다 → 주행 화면이 툭 튀어나오지 않게
		yield return CRunScreenFade.Cover(_fadeDuration);

		// 씬을 내리기 전에 영웅 상태를 남긴다 → 내릴 때 파티 줄이 다시 읽는다 (결과창 내용도 이때 모은다)
		// 건너뛰기를 이긴 것으로 보는 동안은 결과창도 승리로
		bool isSkipWin = _rewardOnSkip && result == ECombatResult.Skipped;

		_result = new CCombatResultData
		{
			Result = isSkipWin ? ECombatResult.Victory : result,
			Kind = CCombatContext.Kind,
			PlaceName = CCombatContext.PlaceName,
		};
		SaveHeroes();
		GiveRewards(result);
		UnlinkPauseMenu();

		// 전투 쪽 리스너·EventSystem 을 먼저 끄고 주행 쪽을 켠다 → 언로드 동안 둘씩 겹치지 않게
		MuteScene(_combatScene);
		_backdrop.Release();
		ShowDrivingScene();

		yield return UnloadCombatScene();

		CleanupLeakedCombatManager();
		yield return null;
		yield return CRunScreenFade.Uncover(_fadeDuration);

		// 화면이 다 밝아진 뒤 결과창 → '계속' 을 누를 때까지 주행을 멈춰 둔다
		if (_showResult && _result != null)
		{
			bool isClosed = false;

			CCombatResultView.Show(transform, _result, _resultFont, () => isClosed = true);
			yield return new WaitUntil(() => isClosed);
		}

		_result = null;
		Finish();
	}

	private void LogResult(ECombatResult result)
	{
		switch (result)
		{
			case ECombatResult.Victory:
				if (_logState)
				{
					CPrint.Log($"CCombatNodeHandler : '{_combatName}' 승리 → 전투를 닫고 다음으로");
				}
				break;
			case ECombatResult.Defeat:
				// TODO : 파티 전멸 → 런 종료 처리 (지금은 이긴 것처럼 지나간다)
				CPrint.Warn($"CCombatNodeHandler : '{_combatName}' 파티 전멸 → 런 종료 처리가 아직 없어 그대로 지나갑니다.");
				break;
			case ECombatResult.Skipped:
				CPrint.Warn($"CCombatNodeHandler : '{_combatName}' 전투를 건너뜁니다. ({_skipKey})");
				break;
			case ECombatResult.Abandoned:
				if (_logState)
				{
					CPrint.Log($"CCombatNodeHandler : '{_combatName}' 전투 포기 → 주행으로 돌아갑니다.");
				}
				break;
		}
	}

	#region 일시정지
	private void LinkPauseMenu()
	{
		PausePopup_UI combatPause = FindInScene<PausePopup_UI>(_combatScene);

		if (combatPause != null)
		{
			combatPause.HideUI();
			combatPause.enabled = false;
		}

		_pauseMenu = FindInScene<CRunPauseMenu>(gameObject.scene);

		if (_pauseMenu == null)
		{
			CPrint.Warn("CCombatNodeHandler : 주행 씬에 일시정지 메뉴가 없어 전투 중 일시정지를 쓸 수 없습니다.");
			return;
		}

		_pauseMenu.BeginOverlay(_abandonLabel, AbandonCombat, PAUSE_SORTING_ORDER);
	}

	private void UnlinkPauseMenu()
	{
		if (_pauseMenu == null)
		{
			return;
		}

		_pauseMenu.EndOverlay();
		_pauseMenu = null;
	}

	// 일시정지의 전투 포기 버튼
	private void AbandonCombat()
	{
		if (!_isFighting)
		{
			return;
		}

		_isFighting = false;
		StartCoroutine(LeaveRoutine(ECombatResult.Abandoned));
	}
	#endregion

	// 전투에서 바뀐 체력·스트레스를 프로필에 쓴다
	private void SaveHeroes()
	{
		if (_heroSpawner == null)
		{
			return;
		}

		DataSaveManager saveManager = DataSaveManager.Instance;
		bool hasSlot = saveManager != null && saveManager.CurrentSlotIndex >= 0;
		UserProfileData profile = hasSlot ? saveManager.LoadProfile(saveManager.CurrentSlotIndex) : null;
		bool canSave = profile != null && profile.PartyHeroes != null;
		List<CEntityRuntimeData> heroes = _heroSpawner.SpawnedHeroData;
		int saved = 0;

		for (int i = 0; i < heroes.Count; i++)
		{
			CEntityRuntimeData hero = heroes[i];

			if (hero == null || hero.HeroBasicData == null)
			{
				continue;
			}

			UserProfileData.HeroSaveData save = canSave ? FindSave(profile, hero.HeroBasicData.HeroID) : null;
			bool isDead = hero.IsDead();
			int hp = isDead ? 0 : Mathf.Max(0, Mathf.RoundToInt(hero.GetStat(EEntityStat.HP)));
			int stress = Mathf.Max(0, Mathf.RoundToInt(hero.GetStat(EEntityStat.Stress)));

			// 전투 전 값은 아직 덮어쓰지 않은 프로필에서 (프로필이 없으면 변화 없이)
			AddResultRow(hero.HeroBasicData, save != null ? save.CurrentHP : hp, hp, save != null ? save.CurrentStress : stress, stress, isDead);

			if (save == null)
			{
				continue;
			}

			save.CurrentHP = hp;
			save.CurrentStress = stress;
			saved++;
		}

		if (saved == 0)
		{
			return;
		}

		saveManager.SaveProfile(profile, saveManager.CurrentSlotIndex);
	}

	// 이겼을 때만 (건너뛰기는 시험용으로 켜 두면) 유물·횃불·장신구를 준다
	private void GiveRewards(ECombatResult result)
	{
		bool isWin = result == ECombatResult.Victory || (_rewardOnSkip && result == ECombatResult.Skipped);

		if (!isWin || _victoryReward == null)
		{
			return;
		}

		CStagecoachController controller = FindFirstObjectByType<CStagecoachController>();

		if (controller == null || controller.RunData == null)
		{
			CPrint.Warn("CCombatNodeHandler : 런 데이터가 없어 전투 보상을 주지 않습니다.");
			return;
		}

		List<string> lines = _result != null ? _result.Rewards : new List<string>();

		_victoryReward.Give(controller.RunData, controller.Stagecoach != null ? controller.Stagecoach.Inventory : null, _tokenRandom, lines);
		RepairOnWin(controller.RunData, lines);

		if (_logState && lines.Count > 0)
		{
			CPrint.Log($"CCombatNodeHandler : 전투 보상 → {string.Join(", ", lines)}");
		}
	}

	// 바퀴·장갑이 다 닳아 벌어진 전투에서 이기면 그 값을 하나 되돌린다
	private void RepairOnWin(CRunData runData, List<string> lines)
	{
		if (_repairOnWin == ERunValue.None || runData == null || runData.Get(_repairOnWin) == null)
		{
			return;
		}

		int added = runData.Add(_repairOnWin, 1);

		if (added > 0)
		{
			lines.Add($"{(_repairOnWin == ERunValue.Wheels ? "바퀴" : "장갑")} +{added}");
		}
	}

	private void AddResultRow(CEntityBasicDataSO data, int hpBefore, int hpAfter, int stressBefore, int stressAfter, bool isDead)
	{
		if (_result == null)
		{
			return;
		}

		_result.Heroes.Add(new CCombatResultData.CHeroRow
		{
			Name = data.Name,
			Portrait = data.HeroPortrait,
			HpBefore = hpBefore,
			HpAfter = hpAfter,
			MaxHp = data.MaxHP,
			StressBefore = stressBefore,
			StressAfter = stressAfter,
			MaxStress = data.MaxStress,
			IsDead = isDead,
		});
	}

	private static UserProfileData.HeroSaveData FindSave(UserProfileData profile, string heroId)
	{
		for (int i = 0; i < profile.PartyHeroes.Count; i++)
		{
			UserProfileData.HeroSaveData save = profile.PartyHeroes[i];

			if (save != null && save.HeroID == heroId)
			{
				return save;
			}
		}

		return null;
	}

	private IEnumerator UnloadCombatScene()
	{
		if (_isOpenedByMode)
		{
			_isOpenedByMode = false;

			CGameManager gameManager = FindGameManager();

			if (gameManager != null)
			{
				yield return gameManager.CloseAdditiveModeAsync(gameManager.CombatModeId, false).ToCoroutine();
				yield break;
			}
		}

		if (_combatScene.IsValid() && _combatScene.isLoaded)
		{
			yield return SceneManager.UnloadSceneAsync(_combatScene);
		}
	}

	// 콜백 전에 비운다
	private void Finish()
	{
		_isBusy = false;
		_isFighting = false;
		_repairOnWin = ERunValue.None;
		CCombatContext.Clear();
		_combatScene = default;
		_heroSpawner = null;
		_enemySpawner = null;

		Action onComplete = _onComplete;

		_onComplete = null;
		onComplete?.Invoke();
	}
	#endregion

	#region 주행 씬 숨기기
	private void HideDrivingScene(Scene combatScene)
	{
		_isDrivingHidden = true;
		_hidden.Clear();

		HideAllExcept<Camera>(combatScene);
		HideAllExcept<AudioListener>(combatScene);
		HideAllExcept<EventSystem>(combatScene);
		HideRootCanvases();

		if (_useBackdrop)
		{
			_backdrop.HideWorld(gameObject.scene);
			_backdrop.Show(FindCombatCamera(combatScene), combatScene, _backdropTint, _backdropShade);
		}
	}

	private int HideAllExcept<T>(Scene keep) where T : Behaviour
	{
		T[] all = FindObjectsByType<T>(FindObjectsSortMode.None);
		int count = 0;

		for (int i = 0; i < all.Length; i++)
		{
			if (all[i] == null || !all[i].enabled || all[i].gameObject.scene == keep)
			{
				continue;
			}

			// 영웅 정보 창 모델 미리보기처럼 텍스처에 그리는 카메라는 그대로
			if (all[i] is Camera camera && camera.targetTexture != null)
			{
				continue;
			}

			all[i].enabled = false;
			_hidden.Add(all[i]);
			count++;
		}

		return count;
	}

	// 짐칸·횃불·파티 줄 등 주행 화면 UI 가 전투 화면 위에 그려지지 않게
	private int HideRootCanvases()
	{
		Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
		int count = 0;

		for (int i = 0; i < canvases.Length; i++)
		{
			Canvas canvas = canvases[i];

			// 주행 씬 것만 → 페이드 화면·영웅 정보 창 같은 공용(DontDestroyOnLoad) 캔버스는 그대로
			if (canvas == null || !canvas.enabled || !canvas.isRootCanvas || canvas.gameObject.scene != gameObject.scene)
			{
				continue;
			}

			canvas.enabled = false;
			_hidden.Add(canvas);
			count++;
		}

		return count;
	}

	// 곧 내릴 씬의 AudioListener·EventSystem 을 끈다 (씬과 함께 사라지니 되돌리지 않는다)
	private static void MuteScene(Scene scene)
	{
		if (!scene.IsValid() || !scene.isLoaded)
		{
			return;
		}

		GameObject[] roots = scene.GetRootGameObjects();

		for (int i = 0; i < roots.Length; i++)
		{
			if (roots[i] == null)
			{
				continue;
			}

			AudioListener[] listeners = roots[i].GetComponentsInChildren<AudioListener>();

			for (int j = 0; j < listeners.Length; j++)
			{
				listeners[j].enabled = false;
			}

			EventSystem[] eventSystems = roots[i].GetComponentsInChildren<EventSystem>();

			for (int j = 0; j < eventSystems.Length; j++)
			{
				eventSystems[j].enabled = false;
			}
		}
	}

	private void ShowDrivingScene()
	{
		_isDrivingHidden = false;

		for (int i = 0; i < _hidden.Count; i++)
		{
			if (_hidden[i] != null)
			{
				_hidden[i].enabled = true;
			}
		}

		_hidden.Clear();
	}
	#endregion
}

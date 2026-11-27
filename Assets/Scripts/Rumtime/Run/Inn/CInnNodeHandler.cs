using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class CInnNodeHandler : MonoBehaviour, INodeHandler
{
	#region 인스펙터
	[SerializeField] private string _innScenePath = "Assets/Scenes/Inn/Inn.unity";
	// 여관 떠나기 → 화면을 검게 덮는 시간 (마지막 여관이면 덮은 채로 다음 노드 씬으로 넘어가 그쪽 시작 연출이 밝힌다)
	[SerializeField] private float _leaveFadeDuration = 0.6f;
	// 보급상 (아이템)
	[SerializeField] private CShopData _shopData;
	// 마차 정비공 (수리·강화)
	[SerializeField] private CShopData _garageData;
	// IRunContextProvider·IStagecoachProvider 를 구현한 컴포넌트 (비우면 씬에서 찾는다)
	[SerializeField] private MonoBehaviour _runSource;
	// 상점 화면 (비우면 여관 씬 밖에서 찾는다)
	[SerializeField] private CShopView _shopView;
	[SerializeField] private bool _logState = true;
	#endregion

	#region 내부 변수
	private readonly List<Behaviour> _hidden = new List<Behaviour>();
	private CNodeInstance _node;
	private Action _onComplete;
	private Scene _innScene;
	private CInnScene _inn;
	private CInnSession _session;
	private bool _isBusy;
	// 게임 매니저의 여관 모드로 열었는지 → 닫을 때도 같은 쪽으로 닫는다
	private bool _isOpenedByMode;
	#endregion

	public ENodeType Type => ENodeType.Inn;

	public void Enter(CNodeInstance node, Action onComplete)
	{
		if (_isBusy)
		{
			CPrint.Warn("CInnNodeHandler : 이미 여관에 들어가 있습니다.");
			return;
		}

		_node = node;
		_onComplete = onComplete;
		StartCoroutine(EnterRoutine());
	}

	private IEnumerator EnterRoutine()
	{
		_isBusy = true;
		_isOpenedByMode = false;

		// 씬이 올라오는 즉시 주행 쪽을 숨긴다 → 페이드 도중 AudioListener 가 둘씩 겹치지 않게
		SceneManager.sceneLoaded -= HandleSceneLoaded;
		SceneManager.sceneLoaded += HandleSceneLoaded;

		// 팀 공용 게임 매니저로 연다 (페이드·Additive 핸들 관리 포함)
		CGameManager gameManager = FindGameManager(mode => mode.InnModeId);

		if (gameManager != null)
		{
			CAddressableSceneManager sceneManager = CAddressableSceneManager.Instance;

			// 다른 씬 전환이 끝날 때까지 기다린다 → 노드 처리는 건너뛸 수 없으니 무시하지 않는다
			yield return new WaitWhile(() => sceneManager != null && sceneManager.IsTransitioning);

			bool loaded = false;

			yield return gameManager.ChangeGameModeAsync(gameManager.InnModeId).ToCoroutine(result => loaded = result);

			_isOpenedByMode = loaded;

			if (!loaded)
			{
				CPrint.Warn("CInnNodeHandler : 게임 매니저로 여관을 열지 못해 씬을 직접 엽니다.");
			}
		}

		// 게임 매니저 없이 주행 씬만 재생할 때 (테스트) → 씬을 직접 연다
		if (!_isOpenedByMode)
		{
			AsyncOperation load = LoadInnScene();

			if (load == null)
			{
				SceneManager.sceneLoaded -= HandleSceneLoaded;
				Finish();
				yield break;
			}

			yield return load;
		}

		SceneManager.sceneLoaded -= HandleSceneLoaded;

		_inn = FindLoadedInnScene();
		_innScene = _inn != null ? _inn.gameObject.scene : default;

		if (_inn == null)
		{
			CPrint.Error($"CInnNodeHandler : 열린 여관 씬에 CInnScene 이 없습니다. ({_innScenePath})");
			ShowDrivingScene();
			yield return UnloadInnScene();
			Finish();
			yield break;
		}

		// 로드 콜백에서 이미 숨겼으면 다시 숨기지 않는다 (숨긴 목록을 잃지 않게)
		if (_hidden.Count == 0)
		{
			HideDrivingScene();
		}

		_session = new CInnSession(_inn, _node, GetRunContext(), GetStagecoach(), FindShopView(), _shopData, _garageData);
		_session.Begin(Leave, SaveAndQuit);

		if (_logState)
		{
			CPrint.Log($"CInnNodeHandler : 여관 입장 (ID {_node.Id}, 행 {_node.Row})");
		}
	}

	// 에디터에서는 빌드 설정에 없어도 경로로 불러온다
	private AsyncOperation LoadInnScene()
	{
#if UNITY_EDITOR
		return EditorSceneManager.LoadSceneAsyncInPlayMode(_innScenePath, new LoadSceneParameters(LoadSceneMode.Additive));
#else
		if (SceneUtility.GetBuildIndexByScenePath(_innScenePath) < 0)
		{
			CPrint.Error($"CInnNodeHandler : {_innScenePath} 이 빌드 설정에 없어 여관을 건너뜁니다.");
			return null;
		}

		return SceneManager.LoadSceneAsync(_innScenePath, LoadSceneMode.Additive);
#endif
	}

	// CInnScene 이 든 씬이 Additive 로 올라오면 → 첫 프레임 전에 주행 쪽을 숨긴다
	private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (mode != LoadSceneMode.Additive || scene == gameObject.scene || _hidden.Count > 0)
		{
			return;
		}

		GameObject[] roots = scene.GetRootGameObjects();
		bool hasInn = false;

		for (int i = 0; i < roots.Length; i++)
		{
			if (roots[i] != null && roots[i].GetComponentInChildren<CInnScene>(true) != null)
			{
				hasInn = true;
				break;
			}
		}

		if (!hasInn)
		{
			return;
		}

		SceneManager.sceneLoaded -= HandleSceneLoaded;

		// 숨길 때 여관 씬 것은 건너뛰어야 하니 먼저 기억한다
		_innScene = scene;
		HideDrivingScene();
	}

	// 어드레서블로 열면 씬 경로를 알 수 없다 → 주행 씬이 아닌 곳의 CInnScene 을 찾는다
	private CInnScene FindLoadedInnScene()
	{
		CInnScene[] inns = FindObjectsByType<CInnScene>(FindObjectsInactive.Include, FindObjectsSortMode.None);

		for (int i = 0; i < inns.Length; i++)
		{
			if (inns[i] != null && inns[i].gameObject.scene != gameObject.scene)
			{
				return inns[i];
			}
		}

		return null;
	}

	private IEnumerator UnloadInnScene()
	{
		if (_isOpenedByMode)
		{
			_isOpenedByMode = false;

			CGameManager gameManager = FindGameManager(mode => mode.InnModeId);

			if (gameManager != null)
			{
				yield return gameManager.CloseAdditiveModeAsync(gameManager.InnModeId, !CRunScreenFade.IsCovered).ToCoroutine();
				yield break;
			}
		}

		if (_innScene.IsValid() && _innScene.isLoaded)
		{
			yield return SceneManager.UnloadSceneAsync(_innScene);
		}
	}

	// 씬에 놓인 게임 매니저 중 해당 모드 데이터가 있는 것만 → Instance 로 빈 매니저를 새로 만들지 않는다
	private static CGameManager FindGameManager(Func<CGameManager, string> getModeId)
	{
		CGameManager gameManager = FindFirstObjectByType<CGameManager>();

		if (gameManager == null || gameManager.GetGameModeData(getModeId(gameManager)) == null)
		{
			return null;
		}

		return gameManager;
	}

	// 여관 씬의 테스트용 상점 화면은 건너뛴다 → 주행 씬의 상점 화면(짐칸 패널과 함께)을 쓴다
	private CShopView FindShopView()
	{
		if (_shopView != null)
		{
			return _shopView;
		}

		CShopView[] views = FindObjectsByType<CShopView>(FindObjectsInactive.Include, FindObjectsSortMode.None);

		for (int i = 0; i < views.Length; i++)
		{
			if (views[i].gameObject.scene != _innScene)
			{
				_shopView = views[i];
				return _shopView;
			}
		}

		return null;
	}

	private CRunContext GetRunContext()
	{
		IRunContextProvider provider = FindRunSource<IRunContextProvider>();

		return provider != null ? provider.Context : null;
	}

	private CStagecoach GetStagecoach()
	{
		IStagecoachProvider provider = FindRunSource<IStagecoachProvider>();

		return provider != null ? provider.Stagecoach : null;
	}

	private T FindRunSource<T>() where T : class
	{
		if (_runSource is T source)
		{
			return source;
		}

		MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

		for (int i = 0; i < behaviours.Length; i++)
		{
			if (behaviours[i] is T found && behaviours[i].gameObject.scene != _innScene)
			{
				return found;
			}
		}

		return null;
	}

	#region 주행 씬 숨기기
	private void HideDrivingScene()
	{
		_hidden.Clear();
		HideAll<Camera>();
		HideAll<AudioListener>();
		SetHudVisible(false);
	}

	// 바퀴·장갑 표시가 여관 제목(보급상 등)과 겹친다 → 여관에 있는 동안 숨긴다
	private static void SetHudVisible(bool visible)
	{
		CStagecoachHUD[] huds = FindObjectsByType<CStagecoachHUD>(FindObjectsSortMode.None);

		for (int i = 0; i < huds.Length; i++)
		{
			if (huds[i] == null)
			{
				continue;
			}

			huds[i].SetVisible(visible);
		}
	}

	private void HideAll<T>() where T : Behaviour
	{
		T[] all = FindObjectsByType<T>(FindObjectsSortMode.None);

		for (int i = 0; i < all.Length; i++)
		{
			if (all[i] == null || !all[i].enabled || all[i].gameObject.scene == _innScene)
			{
				continue;
			}

			all[i].enabled = false;
			_hidden.Add(all[i]);
		}
	}

	// 곧 내릴 여관 씬의 AudioListener 를 끈다 (씬과 함께 사라지니 되돌리지 않는다)
	private void MuteInnScene()
	{
		if (!_innScene.IsValid() || !_innScene.isLoaded)
		{
			return;
		}

		GameObject[] roots = _innScene.GetRootGameObjects();

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
		}
	}

	private void ShowDrivingScene()
	{
		for (int i = 0; i < _hidden.Count; i++)
		{
			if (_hidden[i] != null)
			{
				_hidden[i].enabled = true;
			}
		}

		_hidden.Clear();
		SetHudVisible(true);
	}
	#endregion

	#region 떠나기
	private void Leave()
	{
		StartCoroutine(LeaveRoutine());
	}

	private IEnumerator LeaveRoutine()
	{
		// 여관 화면부터 검게 덮는다 → 주행 화면이 잠깐 비치지 않게
		bool isLastNode = _node != null && _node.IsEnd;

		yield return CRunScreenFade.Cover(_leaveFadeDuration);
		EndSession();

		// 여관 쪽 리스너를 먼저 끄고 주행 쪽을 켠다 → 언로드 동안 둘씩 겹치지 않게
		MuteInnScene();
		ShowDrivingScene();

		yield return UnloadInnScene();

		if (_logState)
		{
			CPrint.Log("CInnNodeHandler : 여관을 떠납니다.");
		}

		Finish();

		// 지도 끝 여관이면 덮은 채로 다음 노드 씬이 열린다 → 그 씬 시작 연출(CDriveIntroFade)이 밝힌다
		if (!isLastNode)
		{
			yield return null;
			yield return CRunScreenFade.Uncover(_leaveFadeDuration);
		}
	}

	private void EndSession()
	{
		if (_session != null)
		{
			_session.End();
			_session = null;
		}

		_inn = null;
	}

	// 콜백 전에 비운다
	private void Finish()
	{
		_isBusy = false;

		Action onComplete = _onComplete;

		_onComplete = null;
		onComplete?.Invoke();
	}
	#endregion

	#region 저장하고 메인 메뉴로
	// 씬 전환은 팀 공용 게임 매니저(타이틀 모드)에 맡긴다
	private void SaveAndQuit()
	{
		// 타이틀 모드가 없으면 (게임 매니저 없이 주행 씬만 재생 등) 여관에 남는다
		CGameManager gameManager = FindGameManager(mode => mode.TitleModeId);

		if (gameManager == null)
		{
			CPrint.Warn("CInnNodeHandler : 타이틀 모드를 찾을 수 없어 메인 메뉴로 돌아가지 않습니다.");
			return;
		}

		CAddressableSceneManager sceneManager = CAddressableSceneManager.Instance;

		if (sceneManager == null || sceneManager.IsTransitioning)
		{
			return;
		}

		CRunContext ctx = GetRunContext();
		CStagecoach stagecoach = GetStagecoach();

		if (ctx == null || ctx.RunData == null || stagecoach == null)
		{
			CPrint.Warn("CInnNodeHandler : 런 데이터나 역마차가 없어 저장할 수 없습니다.");
			return;
		}

		// 이어하기는 지금 주행 씬부터 다시 연다
		// TODO : 지도 진행(현재 노드)도 저장해서 여관 다음 칸부터 이어가기 / 경태
		CRunCarry.Save(ctx.RunData, stagecoach, gameObject.scene.path);

		if (_logState)
		{
			CPrint.Log("CInnNodeHandler : 저장하고 메인 메뉴로 돌아갑니다.");
		}

		// 타이틀은 Single 로 열린다 → 여관·주행 씬은 함께 내려가고 세션은 OnDestroy 에서 정리된다
		gameManager.ChangeGameMode(gameManager.TitleModeId);
	}
	#endregion

	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= HandleSceneLoaded;
		EndSession();
	}
}

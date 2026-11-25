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
				Finish();
				yield break;
			}

			yield return load;
		}

		_inn = FindLoadedInnScene();
		_innScene = _inn != null ? _inn.gameObject.scene : default;

		if (_inn == null)
		{
			CPrint.Error($"CInnNodeHandler : 열린 여관 씬에 CInnScene 이 없습니다. ({_innScenePath})");
			yield return UnloadInnScene();
			Finish();
			yield break;
		}

		HideDrivingScene();

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
				yield return gameManager.CloseAdditiveModeAsync(gameManager.InnModeId).ToCoroutine();
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
		EndSession();
		ShowDrivingScene();

		yield return UnloadInnScene();

		if (_logState)
		{
			CPrint.Log("CInnNodeHandler : 여관을 떠납니다.");
		}

		Finish();
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
		EndSession();
	}
}

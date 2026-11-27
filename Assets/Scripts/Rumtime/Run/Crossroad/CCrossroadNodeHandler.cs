using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

// 교차로 노드 → 교차로 씬(파티 편성)을 덧붙여 열고, 출정하면 닫고 다음 길로
public class CCrossroadNodeHandler : MonoBehaviour, INodeHandler
{
	#region 인스펙터
	// 게임 매니저 없이 주행 씬만 재생할 때 직접 열 씬
	[SerializeField] private string _crossroadScenePath = "Assets/Scenes/UI/The_Crossroads_Test.unity";
	[SerializeField] private bool _logState = true;
	#endregion

	#region 내부 변수
	private readonly List<Behaviour> _hidden = new List<Behaviour>();
	private Action _onComplete;
	private Scene _crossroadScene;
	private bool _isBusy;
	private bool _isLeaving;
	// 게임 매니저의 교차로 모드로 열었는지 → 닫을 때도 같은 쪽으로 닫는다
	private bool _isOpenedByMode;
	#endregion

	public ENodeType Type => ENodeType.Crossroad;

	private void OnDestroy()
	{
		TheCrossroadManager.OnDeparted -= HandleDeparted;
		SceneManager.sceneLoaded -= HandleSceneLoaded;
	}

	public void Enter(CNodeInstance node, Action onComplete)
	{
		if (_isBusy)
		{
			CPrint.Warn("CCrossroadNodeHandler : 이미 교차로에 들어가 있습니다.");
			return;
		}

		_onComplete = onComplete;
		StartCoroutine(EnterRoutine());
	}

	private IEnumerator EnterRoutine()
	{
		_isBusy = true;
		_isLeaving = false;
		_isOpenedByMode = false;

		SceneManager.sceneLoaded -= HandleSceneLoaded;
		SceneManager.sceneLoaded += HandleSceneLoaded;

		CGameManager gameManager = FindGameManager();

		if (gameManager != null)
		{
			CAddressableSceneManager sceneManager = CAddressableSceneManager.Instance;

			// 다른 씬 전환이 끝날 때까지 기다린다 → 노드 처리는 건너뛸 수 없다
			yield return new WaitWhile(() => sceneManager != null && sceneManager.IsTransitioning);

			bool loaded = false;

			yield return gameManager.ChangeGameModeAsync(gameManager.CrossroadsModeId).ToCoroutine(result => loaded = result);

			_isOpenedByMode = loaded;

			if (!loaded)
			{
				CPrint.Warn("CCrossroadNodeHandler : 게임 매니저로 교차로를 열지 못해 씬을 직접 엽니다.");
			}
		}

		// 게임 매니저 없이 주행 씬만 재생할 때 (테스트) → 씬을 직접 연다
		if (!_isOpenedByMode)
		{
			AsyncOperation load = LoadCrossroadScene();

			if (load == null)
			{
				SceneManager.sceneLoaded -= HandleSceneLoaded;
				Finish();
				yield break;
			}

			yield return load;
		}

		SceneManager.sceneLoaded -= HandleSceneLoaded;

		TheCrossroadManager manager = FindLoadedManager();

		if (manager == null)
		{
			CPrint.Error($"CCrossroadNodeHandler : 열린 교차로 씬에 TheCrossroadManager 가 없습니다. ({_crossroadScenePath})");
			ShowDrivingScene();
			yield return UnloadCrossroadScene();
			Finish();
			yield break;
		}

		_crossroadScene = manager.gameObject.scene;

		// 로드 콜백에서 이미 숨겼으면 다시 숨기지 않는다
		if (_hidden.Count == 0)
		{
			HideDrivingScene();
		}

		TheCrossroadManager.OnDeparted -= HandleDeparted;
		TheCrossroadManager.OnDeparted += HandleDeparted;

		if (_logState)
		{
			CPrint.Log("CCrossroadNodeHandler : 교차로 입장 → 파티를 꾸리고 출정하면 다음 길로");
		}
	}

	// 에디터에서는 빌드 설정에 없어도 경로로 불러온다
	private AsyncOperation LoadCrossroadScene()
	{
#if UNITY_EDITOR
		return EditorSceneManager.LoadSceneAsyncInPlayMode(_crossroadScenePath, new LoadSceneParameters(LoadSceneMode.Additive));
#else
		if (SceneUtility.GetBuildIndexByScenePath(_crossroadScenePath) < 0)
		{
			CPrint.Error($"CCrossroadNodeHandler : {_crossroadScenePath} 이 빌드 설정에 없어 교차로를 건너뜁니다.");
			return null;
		}

		return SceneManager.LoadSceneAsync(_crossroadScenePath, LoadSceneMode.Additive);
#endif
	}

	// 교차로 매니저가 든 씬이 Additive 로 올라오면 → 첫 프레임 전에 주행 쪽을 숨긴다
	private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (mode != LoadSceneMode.Additive || scene == gameObject.scene || _hidden.Count > 0)
		{
			return;
		}

		if (!HasComponentInScene<TheCrossroadManager>(scene))
		{
			return;
		}

		SceneManager.sceneLoaded -= HandleSceneLoaded;
		HideDrivingScene();
	}

	private static bool HasComponentInScene<T>(Scene scene) where T : Component
	{
		GameObject[] roots = scene.GetRootGameObjects();

		for (int i = 0; i < roots.Length; i++)
		{
			if (roots[i] != null && roots[i].GetComponentInChildren<T>(true) != null)
			{
				return true;
			}
		}

		return false;
	}

	// 어드레서블로 열면 씬 경로를 알 수 없다 → 주행 씬이 아닌 곳의 매니저를 찾는다
	private TheCrossroadManager FindLoadedManager()
	{
		TheCrossroadManager[] managers = FindObjectsByType<TheCrossroadManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);

		for (int i = 0; i < managers.Length; i++)
		{
			if (managers[i] != null && managers[i].gameObject.scene != gameObject.scene)
			{
				return managers[i];
			}
		}

		return null;
	}

	// 씬에 놓인 게임 매니저 중 교차로 모드가 있는 것만 → Instance 로 빈 매니저를 새로 만들지 않는다
	private static CGameManager FindGameManager()
	{
		CGameManager gameManager = FindFirstObjectByType<CGameManager>();

		if (gameManager == null || gameManager.GetGameModeData(gameManager.CrossroadsModeId) == null)
		{
			return null;
		}

		return gameManager;
	}

	#region 출정
	private void HandleDeparted()
	{
		if (!_isBusy || _isLeaving)
		{
			return;
		}

		// 이번 런 파티가 정해졌다 → 주행 화면 아래 파티 줄이 뜬다
		CRunCarry.MarkPartyChosen();
		StartCoroutine(LeaveRoutine());
	}

	private IEnumerator LeaveRoutine()
	{
		_isLeaving = true;
		TheCrossroadManager.OnDeparted -= HandleDeparted;

		// 교차로 쪽 리스너·EventSystem 을 먼저 끄고 주행 쪽을 켠다 → 언로드 동안 둘씩 겹치지 않게
		MuteScene(_crossroadScene);
		ShowDrivingScene();

		yield return UnloadCrossroadScene();

		if (_logState)
		{
			CPrint.Log("CCrossroadNodeHandler : 출정 → 교차로를 떠납니다.");
		}

		Finish();
	}

	private IEnumerator UnloadCrossroadScene()
	{
		if (_isOpenedByMode)
		{
			_isOpenedByMode = false;

			CGameManager gameManager = FindGameManager();

			if (gameManager != null)
			{
				yield return gameManager.CloseAdditiveModeAsync(gameManager.CrossroadsModeId).ToCoroutine();
				yield break;
			}
		}

		if (_crossroadScene.IsValid() && _crossroadScene.isLoaded)
		{
			yield return SceneManager.UnloadSceneAsync(_crossroadScene);
		}
	}

	// 콜백 전에 비운다
	private void Finish()
	{
		_isBusy = false;
		_isLeaving = false;
		_crossroadScene = default;

		Action onComplete = _onComplete;

		_onComplete = null;
		onComplete?.Invoke();
	}
	#endregion

	#region 주행 씬 숨기기
	// 교차로 씬은 카메라·EventSystem·화면 UI 를 따로 갖고 있다 → 겹치지 않게 주행 쪽을 꺼 둔다
	private void HideDrivingScene()
	{
		_hidden.Clear();
		HideAll<Camera>();
		HideAll<AudioListener>();
		HideAll<EventSystem>();
		HideRootCanvases();
	}

	private void HideAll<T>() where T : Behaviour
	{
		T[] all = FindObjectsByType<T>(FindObjectsSortMode.None);

		for (int i = 0; i < all.Length; i++)
		{
			// 주행 씬 것만 → 교차로 씬·공용(DontDestroyOnLoad) 오브젝트는 그대로
			if (all[i] == null || !all[i].enabled || all[i].gameObject.scene != gameObject.scene)
			{
				continue;
			}

			all[i].enabled = false;
			_hidden.Add(all[i]);
		}
	}

	// 짐칸·횃불·바퀴 표시 등 주행 화면 UI 가 교차로 화면 위에 그려지지 않게
	private void HideRootCanvases()
	{
		Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);

		for (int i = 0; i < canvases.Length; i++)
		{
			Canvas canvas = canvases[i];

			// 주행 씬 것만 → 페이드 화면 같은 공용(DontDestroyOnLoad) 캔버스는 그대로
			if (canvas == null || !canvas.enabled || !canvas.isRootCanvas || canvas.gameObject.scene != gameObject.scene)
			{
				continue;
			}

			canvas.enabled = false;
			_hidden.Add(canvas);
		}
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

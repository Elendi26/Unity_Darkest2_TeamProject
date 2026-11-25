using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

// 지도를 끝까지 가면 다음 씬을 연다
//  ㄴ 튜토리얼 → NodeTestScene, NodeTestScene → 자기 자신(새 지도로 무한 반복)
public class CRunSceneFlow : MonoBehaviour
{
	#region 인스펙터
	// 지도 런너를 만드는 쪽 (비우면 씬에서 찾는다)
	[SerializeField] private CMapController _mapController;
	// 다음 씬에 넘길 런 데이터를 가진 역마차 (비우면 씬에서 찾는다)
	[SerializeField] private CStagecoachController _stagecoachController;
	// 다음 씬으로 런 데이터(자원·짐칸·업그레이드)를 넘긴다
	[SerializeField] private bool _carryRun = true;
	// 지도를 끝내면 열 씬 (비우면 지금 씬을 다시 연다)
	[SerializeField] private string _nextScenePath = "Assets/Scenes/Node/NodeTestScene.unity";
	// 게임 매니저가 있으면 이 모드로 다음 씬을 연다 (비우거나 매니저가 없으면 _nextScenePath 로 연다)
	[SerializeField] private string _nextModeId = "Node";
	// 이 지도를 끝내면 튜토리얼을 마친 것으로 기록한다 (튜토리얼 씬에서만 켠다)
	[SerializeField] private bool _completesTutorial = false;
	// 마지막 노드를 마치고 다음 씬을 열기까지 기다리는 시간
	[SerializeField] private float _loadDelay = 0.5f;
	[SerializeField] private bool _logState = true;
	#endregion

	#region 내부 변수
	private CMapRunner _runner;
	private bool _isLoading;
	#endregion

	private void Awake()
	{
		if (_mapController == null)
		{
			_mapController = FindFirstObjectByType<CMapController>();
		}
	}

	private void OnEnable()
	{
		if (_mapController == null)
		{
			CPrint.Warn("CRunSceneFlow : CMapController 가 없어 지도가 끝나도 다음 씬으로 넘어가지 않습니다.");
			return;
		}

		_mapController.OnRunnerCreated += BindRunner;

		// 이미 런너가 만들어져 있으면 바로 붙는다
		if (_mapController.Runner != null)
		{
			BindRunner(_mapController.Runner);
		}
	}

	private void OnDisable()
	{
		if (_mapController != null)
		{
			_mapController.OnRunnerCreated -= BindRunner;
		}

		UnBindRunner();
	}

	private void BindRunner(CMapRunner runner)
	{
		UnBindRunner();

		_runner = runner;

		if (_runner != null)
		{
			_runner.OnStateChanged += HandleStateChanged;
		}
	}

	private void UnBindRunner()
	{
		if (_runner != null)
		{
			_runner.OnStateChanged -= HandleStateChanged;
		}

		_runner = null;
	}

	private void HandleStateChanged(EMapState prev, EMapState next)
	{
		if (next != EMapState.Completed || _isLoading)
		{
			return;
		}

		StartCoroutine(LoadNextRoutine());
	}

	private IEnumerator LoadNextRoutine()
	{
		_isLoading = true;

		if (_loadDelay > 0f)
		{
			yield return new WaitForSecondsRealtime(_loadDelay);
		}

		string path = string.IsNullOrEmpty(_nextScenePath) ? SceneManager.GetActiveScene().path : _nextScenePath;

		if (_carryRun)
		{
			SaveRun(path);
		}

		if (_completesTutorial)
		{
			CTutorialProgress.MarkDone(CTutorialProgress.CurrentSlot);
		}

		if (TryChangeMode())
		{
			yield break;
		}

		if (_logState)
		{
			CPrint.Log($"CRunSceneFlow : 지도 완료 → {path} 을 엽니다.");
		}

		LoadScene(path);
	}

	// 타이틀부터 들어온 경우 → 게임 매니저로 연다 (페이드·어드레서블 핸들 관리)
	//  ㄴ 씬에 놓인 매니저만 찾는다 → Instance 로 빈 매니저를 새로 만들지 않는다
	private bool TryChangeMode()
	{
		if (string.IsNullOrEmpty(_nextModeId))
		{
			return false;
		}

		CGameManager gameManager = FindFirstObjectByType<CGameManager>();

		if (gameManager == null || gameManager.GetGameModeData(_nextModeId) == null)
		{
			return false;
		}

		if (_logState)
		{
			CPrint.Log($"CRunSceneFlow : 지도 완료 → {_nextModeId} 모드로 넘어갑니다.");
		}

		gameManager.ChangeGameMode(_nextModeId);
		return true;
	}

	// 씬을 닫기 전에 지금 런을 기록해 둔다 → 다음 씬 역마차가 이어받는다
	private void SaveRun(string nextScenePath)
	{
		if (_stagecoachController == null)
		{
			_stagecoachController = FindFirstObjectByType<CStagecoachController>();
		}

		if (_stagecoachController == null || _stagecoachController.RunData == null)
		{
			CPrint.Warn("CRunSceneFlow : 역마차가 없어 런 데이터를 넘기지 않습니다.");
			return;
		}

		CRunCarry.Save(_stagecoachController.RunData, _stagecoachController.Stagecoach, nextScenePath);
	}

	// 에디터에서는 빌드 설정에 없어도 경로로 연다
	private static void LoadScene(string path)
	{
#if UNITY_EDITOR
		EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
#else
		if (SceneUtility.GetBuildIndexByScenePath(path) < 0)
		{
			CPrint.Error($"CRunSceneFlow : {path} 이 빌드 설정에 없어 다음 씬을 열 수 없습니다.");
			return;
		}

		SceneManager.LoadScene(path, LoadSceneMode.Single);
#endif
	}
}

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CGameManager : CManagerSingleton<CGameManager>
{
    protected override bool IsDontDestroy => true;

    #region 인스펙터
    [SerializeField] private CGameModeDatabaseSO _gameModeDB;

    [Header("기본 모드 ID")]
    [SerializeField] private string _titleModeId = "Title";
    [SerializeField] private string _nodeModeId = "Node";
    [SerializeField] private string _crossroadsModeId = "Crossroads";
    [SerializeField] private string _combatModeId = "Combat";
    [SerializeField] private string _innModeId = "Inn";
    #endregion

    // 내부 변수
    private bool _isQuitting;
  
    public CGameModeDataSO CurrentGameMode { get; private set; }

    public string TitleModeId => _titleModeId;
    public string NodeModeId => _nodeModeId;
    public string CrossroadsModeId => _crossroadsModeId;
    public string CombatModeId => _combatModeId;
    public string InnModeId => _innModeId;

    /// <summary>
    /// 현재 게임 모드가 변경되었을 때 발생합니다. (이전 모드, 현재 모드)
    /// </summary>
    public event Action<CGameModeDataSO, CGameModeDataSO> OnGameModeChanged;

    /// <summary>
    /// 애디티브 게임 모드가 닫혔을 때 발생합니다. (닫힌 모드 ID)
    /// </summary>
    public event Action<string> OnAdditiveModeClosed;

    protected override void Awake()
    {
        base.Awake();

        if (_gameModeDB == null)
        {
            CPrint.Error("CGameModeDatabaseSO가 할당되지 않았습니다.");
        }
    }

    /// <summary>
    /// 모드 ID에 해당하는 게임 모드 데이터를 조회합니다.
    /// </summary>
    public CGameModeDataSO GetGameModeData(string modeId)
    {
        if (_gameModeDB == null)
        {
            CPrint.Warn("CGameModeDatabaseSO가 할당되지 않았습니다.");
            return null;
        }

        return _gameModeDB.GetGameModeData(modeId);
    }

    /// <summary>
    /// 씬 로드 없이 현재 게임 모드 상태와 변경 이벤트만 갱신합니다.
    /// </summary>
    public void SetCurrentGameModeWithoutLoad(string modeId)
    {
        CGameModeDataSO modeData = GetGameModeData(modeId);
        if (modeData == null)
        {
            return;
        }

        CGameModeDataSO previousMode = CurrentGameMode;
        CurrentGameMode = modeData;

        OnGameModeChanged?.Invoke(previousMode, CurrentGameMode);
    }

    /// <summary>
    /// 지정한 게임 모드로 비동기 전환을 요청합니다.
    /// </summary>
    public void ChangeGameMode(string modeId)
    {
        ChangeGameModeAsync(modeId).Forget();
    }

    /// <summary>
    /// 지정한 게임 모드로 씬을 로드하고 현재 모드를 전환합니다.
    /// </summary>
    public async UniTask<bool> ChangeGameModeAsync(string modeId)
    {
        CGameModeDataSO nextModeData = GetGameModeData(modeId);
        if (nextModeData == null)
        {
            return false;
        }

        if (CAddressableSceneManager.Instance == null)
        {
            CPrint.Warn("CAddressableSceneManager.Instance가 없어 씬을 로드할 수 없습니다.");
            return false;
        }

        bool loaded = false;

        if (nextModeData.HasValidSceneReference)
        {
            loaded = await CAddressableSceneManager.Instance.LoadSceneWithFadeAsync
            (
                sceneRef: nextModeData.SceneReference,
                loadMode: nextModeData.LoadMode,
                modeId: nextModeData.Id,
                useFade: nextModeData.UseFade
            );
        }
        else if (!string.IsNullOrWhiteSpace(nextModeData.SceneAddress))
        {
            loaded = await CAddressableSceneManager.Instance.LoadSceneWithFadeAsync
            (
                sceneAddress: nextModeData.SceneAddress,
                loadMode: nextModeData.LoadMode,
                modeId: nextModeData.Id,
                useFade: nextModeData.UseFade
            );
        }
        else
        {
            CPrint.Warn($"'{modeId}' 게임 모드에 유효한 SceneReference와 SceneAddress가 모두 없습니다.");
            return false;
        }

        if (!loaded)
        {
            return false;
        }

        CGameModeDataSO previousMode = CurrentGameMode;
        CurrentGameMode = nextModeData;

        string prevId = previousMode != null ? previousMode.Id : "None";
        CPrint.Log($"게임 모드 변경 완료: {prevId} → {CurrentGameMode.Id}");
        OnGameModeChanged?.Invoke(previousMode, CurrentGameMode);
        return true;
    }

    /// <summary>
    /// 활성화된 애디티브 게임 모드 닫기를 비동기로 요청합니다.
    /// </summary>
    public void CloseAdditiveMode(string modeId)
    {
        CloseAdditiveModeAsync(modeId).Forget();
    }

    /// <summary>
    /// 애디티브 씬을 언로드하고 남은 애디티브 씬이 없으면 기본 Node 모드로 복구합니다.
    /// </summary>
    public async UniTask<bool> CloseAdditiveModeAsync(string modeId, bool useFade = true)
    {
        if (CAddressableSceneManager.Instance == null)
        {
            return false;
        }

        bool unloaded = await CAddressableSceneManager.Instance.UnloadAdditiveSceneWithFadeAsync
        (
            keyOrModeId: modeId,
            useFade: useFade,
            onUnloaded: () => OnAdditiveModeClosed?.Invoke(modeId)
        );

        if (!unloaded)
        {
            return false;
        }

        bool hasRemainingAdditive = CAddressableSceneManager.Instance.HasAnyAdditiveScene;
        if (!hasRemainingAdditive && CurrentGameMode != null && CurrentGameMode.Id == modeId)
        {
            CGameModeDataSO nodeMode = GetGameModeData(_nodeModeId);
            if (nodeMode != null)
            {
                CurrentGameMode = nodeMode;
            }
        }

        return true;
    }

    /// <summary>
    /// 게임을 종료합니다.
    /// </summary>
    public void QuitGame()
    {
        QuitGameAsync().Forget();
    }

    /// <summary>
    /// 페이드 연출 후 애플리케이션을 종료합니다.
    /// </summary>
    public async UniTask QuitGameAsync(bool useFade = true)
    {
        if (_isQuitting)
        {
            return;
        }

        _isQuitting = true;

        if (!Mathf.Approximately(Time.timeScale, 1f))
        {
            Time.timeScale = 1f;
        }

        if (useFade && CFadeController.Instance != null)
        {
            await CFadeController.Instance.FadeInAsync();
        }

        CPrint.Log("CGameManager : 게임 종료 요청");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
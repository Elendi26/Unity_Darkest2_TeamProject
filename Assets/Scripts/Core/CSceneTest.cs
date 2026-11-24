using UnityEngine;

public class CSceneTest : CManagerSingleton<CSceneTest>
{
    protected override bool IsDontDestroy => true;

    // 내부 변수
    private bool _isInnStepDone;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            LoadNextScene();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ReturnToTitle();
        }
    }

    public void LoadNextScene()
    {
        if (CGameManager.Instance == null)
        {
            return;
        }

        if (CAddressableSceneManager.Instance != null && CAddressableSceneManager.Instance.IsTransitioning)
        {
            return;
        }

        string titleId = CGameManager.Instance.TitleModeId;
        string nodeId = CGameManager.Instance.NodeModeId;
        string crossroadsId = CGameManager.Instance.CrossroadsModeId;
        string combatId = CGameManager.Instance.CombatModeId;
        string innId = CGameManager.Instance.InnModeId;

        if (CGameManager.Instance.CurrentGameMode == null)
        {
            CGameManager.Instance.SetCurrentGameModeWithoutLoad(titleId);
        }

        if (CGameManager.Instance.CurrentGameMode == null)
        {
            return;
        }

        string currentId = CGameManager.Instance.CurrentGameMode.Id;
        string nextId = string.Empty;

        if (currentId == titleId)
        {
            _isInnStepDone = false;
            nextId = nodeId;
        }
        else if (currentId == nodeId)
        {
            if (!_isInnStepDone)
            {
                nextId = crossroadsId;
            }
            else
            {
                nextId = titleId;
            }
        }
        else if (currentId == crossroadsId)
        {
            CPrint.Log($"CSceneTest {currentId} → {nodeId} (Additive 닫기)");
            CGameManager.Instance.CloseAdditiveMode(crossroadsId);
            return;
        }
        else if (currentId == combatId)
        {
            CPrint.Log($"CSceneTest {currentId} → {nodeId} (Additive 닫기)");
            CGameManager.Instance.CloseAdditiveMode(combatId);
            return;
        }
        else if (currentId == innId)
        {
            _isInnStepDone = true;
            CPrint.Log($"CSceneTest {currentId} → {nodeId} (Additive 닫기)");
            CGameManager.Instance.CloseAdditiveMode(innId);
            return;
        }
        else
        {
            nextId = titleId;
        }

        CPrint.Log($"CSceneTest {currentId} → {nextId} 전환");
        LoadScene(nextId);
    }

    /// <summary>
    /// Node에서 Crossroads → Combat → Inn 순서로 서브 모드를 열 때 쓰는 보조 메서드입니다.
    /// </summary>
    public void LoadNextSubModeFromNode()
    {
        if (CGameManager.Instance == null || CAddressableSceneManager.Instance == null)
        {
            return;
        }

        if (CAddressableSceneManager.Instance.IsTransitioning || CAddressableSceneManager.Instance.HasAnyAdditiveScene)
        {
            return;
        }

        string currentId = CGameManager.Instance.CurrentGameMode != null
            ? CGameManager.Instance.CurrentGameMode.Id
            : CGameManager.Instance.NodeModeId;

        if (currentId != CGameManager.Instance.NodeModeId)
        {
            LoadNextScene();
            return;
        }

        string targetId = CGameManager.Instance.CrossroadsModeId;
        CPrint.Log($"CSceneTest {currentId} → {targetId} 전환");
        LoadScene(targetId);
    }

    /// <summary>
    /// 타이틀 씬으로 전환합니다.
    /// </summary>
    public void ReturnToTitle()
    {
        if (CGameManager.Instance == null)
        {
            return;
        }

        if (CAddressableSceneManager.Instance != null && CAddressableSceneManager.Instance.IsTransitioning)
        {
            return;
        }

        _isInnStepDone = false;
        CGameManager.Instance.ChangeGameMode(CGameManager.Instance.TitleModeId);
    }

    /// <summary>
    /// 지정한 모드 ID의 씬으로 직접 전환합니다.
    /// </summary>
    public void LoadScene(string modeId)
    {
        if (CGameManager.Instance == null || string.IsNullOrWhiteSpace(modeId))
        {
            return;
        }

        if (CAddressableSceneManager.Instance != null && CAddressableSceneManager.Instance.IsTransitioning)
        {
            return;
        }

        CGameManager.Instance.ChangeGameMode(modeId);
    }
}
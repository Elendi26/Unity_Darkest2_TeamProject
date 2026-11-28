using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class CAdditiveSceneReturn : MonoBehaviour
{
    // 내부 변수
    private bool _isReturning;

    /// <summary>
    /// 현재 활성화된 애디티브 모드를 닫고 노드로 복귀
    /// </summary>
    public void ReturnToMain()
    {
        string targetModeId = ResolveTargetAdditiveModeId();
        ReturnToMainAsync(targetModeId).Forget();
    }

    /// <summary>
    /// 지정한 모드 ID의 애디티브 모드를 닫고 노드로 복귀
    /// </summary>
    public void ReturnToMain(string modeId)
    {
        ReturnToMainAsync(modeId).Forget();
    }

    /// <summary>
    /// InputAction과 연동합니다.
    /// </summary>
    public void OnReturnInput(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        ReturnToMain();
    }

    private string ResolveTargetAdditiveModeId()
    {
        if (CGameManager.Instance == null)
        {
            return string.Empty;
        }

        CGameModeDataSO currentMode = CGameManager.Instance.CurrentGameMode;
        if (currentMode != null && currentMode.LoadMode == LoadSceneMode.Additive)
        {
            return currentMode.Id;
        }

        return string.Empty;
    }

    /// <summary>
    /// 지정한 additiveMode를 비동기로 닫고 노드로 복귀합니다.
    /// </summary>
    public async UniTask<bool> ReturnToMainAsync(string modeId)
    {
        if (_isReturning || string.IsNullOrWhiteSpace(modeId))
        {
            return false;
        }

        if (CGameManager.Instance == null || CAddressableSceneManager.Instance == null)
        {
            CPrint.Warn("CSubSceneReturnToNode : CGameManager 또는 CAddressableSceneManager가 없습니다.");
            return false;
        }

        if (CAddressableSceneManager.Instance.IsTransitioning)
        {
            return false;
        }

        _isReturning = true;

        try
        {
            return await CGameManager.Instance
                .CloseAdditiveModeAsync(modeId)
                .AttachExternalCancellation(this.GetCancellationTokenOnDestroy());
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        finally
        {
            _isReturning = false;
        }
    }
}
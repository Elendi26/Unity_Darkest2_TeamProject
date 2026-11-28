using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class CNodeSceneModeTrigger : MonoBehaviour
{
    // 내부 변수
    private bool _isRequesting;

    /*
    // OpenAdditiveMode(CGameManager.Instance.modeId); 로 사용합니다.
    */

    /// <summary>
    /// Crossroads 를 Additive로 열기
    /// </summary>
    public void OpenCrossroads()
    {
        TryOpenByManager(gm => gm.CrossroadsModeId);
    }

    /// <summary>
    /// Combat 를 Additive로 열기
    /// </summary>
    public void OpenCombat()
    {
        TryOpenByManager(gm => gm.CombatModeId);
    }

    /// <summary>
    /// Inn 를 Additive로 열기
    /// </summary>
    public void OpenInn()
    {
        TryOpenByManager(gm => gm.InnModeId);
    }

    /// <summary>
    /// 지정한 모드 ID을 Additive로 엽니다.
    /// </summary>
    public void OpenAdditiveMode(string modeId)
    {
        OpenAdditiveModeAsync(modeId).Forget();
    }

    /// <summary>
    /// Crossroads InputAction 콜백
    /// </summary>
    public void OnCrossroadsInput(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        OpenCrossroads();
    }

    /// <summary>
    /// Combat InputAction 콜백
    /// </summary>
    public void OnCombatInput(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        OpenCombat();
    }

    /// <summary>
    /// Inn InputAction 콜백
    /// </summary>
    public void OnInnInput(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        OpenInn();
    }

    private void TryOpenByManager(Func<CGameManager, string> modeIdSelector)
    {
        if (CGameManager.Instance == null || modeIdSelector == null)
        {
            return;
        }

        OpenAdditiveMode(modeIdSelector(CGameManager.Instance));
    }

    private async UniTask OpenAdditiveModeAsync(string modeId)
    {
        if (_isRequesting || string.IsNullOrWhiteSpace(modeId))
        {
            return;
        }

        if (CGameManager.Instance == null || CAddressableSceneManager.Instance == null)
        {
            CPrint.Warn("CNodeSceneModeTrigger : CGameManager 또는 CAddressableSceneManager가 없습니다.");
            return;
        }

        if (CAddressableSceneManager.Instance.IsTransitioning || CAddressableSceneManager.Instance.HasAnyAdditiveScene)
        {
            return;
        }

        _isRequesting = true;

        try
        {
            await CGameManager.Instance
                .ChangeGameModeAsync(modeId)
                .AttachExternalCancellation(this.GetCancellationTokenOnDestroy());
        }
        catch (OperationCanceledException)
        {
            // 오브젝트 파괴 시 전환 대기 중단
        }
        finally
        {
            _isRequesting = false;
        }
    }

    private void OnDestroy()
    {
        _isRequesting = false;
    }
}
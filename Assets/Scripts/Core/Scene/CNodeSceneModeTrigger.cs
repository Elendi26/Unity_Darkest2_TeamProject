using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class CNodeSceneModeTrigger : MonoBehaviour
{
    #region 내부 변수
    private bool _isRequesting;
    #endregion

    #region 버튼 / 직접 호출용 메서드
    /*
    // OpenAdditiveMode(CGameManager.Instance.modeId); 로 사용합니다.
    */

    /// <summary>
    /// Crossroads 를 Additive로 열기
    /// </summary>
    public void OpenCrossroads()
    {
        if (CGameManager.Instance == null)
        {
            return;
        }

        OpenAdditiveMode(CGameManager.Instance.CrossroadsModeId);
    }

    /// <summary>
    /// Combat 를 Additive로 열기
    /// </summary>
    public void OpenCombat()
    {
        if (CGameManager.Instance == null)
        {
            return;
        }

        OpenAdditiveMode(CGameManager.Instance.CombatModeId);
    }

    /// <summary>
    /// Inn 를 Additive로 열기
    /// </summary>
    public void OpenInn()
    {
        if (CGameManager.Instance == null)
        {
            return;
        }

        OpenAdditiveMode(CGameManager.Instance.InnModeId);
    }

    /// <summary>
    /// 지정한 모드 ID의 애디티브 모드 전환을 요청합니다.
    /// </summary>
    public void OpenAdditiveMode(string modeId)
    {
        OpenAdditiveModeAsync(modeId).Forget();
    }
    #endregion

    #region 인풋 매니저 연결용
    /// <summary>
    /// Crossroads 인풋 매니저 콜백을 처리합니다.
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
    /// Combat 입력 액션 콜백을 처리합니다.
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
    /// Inn 입력 액션 콜백을 처리합니다.
    /// </summary>
    public void OnInnInput(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        OpenInn();
    }
    #endregion

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
            await CGameManager.Instance.ChangeGameModeAsync(modeId).AttachExternalCancellation(this.GetCancellationTokenOnDestroy());
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
}
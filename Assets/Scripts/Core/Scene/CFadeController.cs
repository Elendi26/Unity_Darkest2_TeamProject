using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;

public class CFadeController : CManagerSingleton<CFadeController>
{
    protected override bool IsDontDestroy => true;

    #region 인스펙터
    [Header("어드레서블 페이드 UI 설정")]
    [SerializeField] private AssetReferenceGameObject _fadePrefabRef;
    [SerializeField] private string _fadePrefabAddress = "Assets/Prefabs/FadeCanvas.prefab";

    [Header("페이드 연출 시간")]
    [SerializeField] private float _fadeInDuration = 0.25f;
    [SerializeField] private float _fadeOutDuration = 0.25f;
    #endregion

    #region 내부 변수
    private CanvasGroup _group;
    private GameObject _ui;
    private AsyncOperationHandle<GameObject> _handle;
    private bool _loading;
    private bool _prevNav;
    private int _version;
    private readonly List<InputDevice> _disabledDevices = new List<InputDevice>();
    #endregion

    // 프로퍼티
    public bool IsReady => _group != null;

    protected override void Awake()
    {
        base.Awake();
        PreloadFadeUIAsync().Forget();
    }

    /// <summary>
    /// 외부 초기화 흐름에서 선제 로드를 끝까지 기다릴 때 호출합니다.
    /// </summary>
    public async UniTask<bool> PreloadFadeUIAsync()
    {
        return await LoadAsync();
    }

    /// <summary>
    /// 화면을 어둡게(알파 1) 전환합니다.
    /// </summary>
    public async UniTask FadeInAsync(float duration = -1f)
    {
        if (!await PreloadFadeUIAsync())
        {
            return;
        }

        float targetDuration = duration >= 0f ? duration : _fadeInDuration;
        await RunFadeAsync(1f, targetDuration, blocksRaycasts: true);
    }

    /// <summary>
    /// 화면을 밝게(알파 0) 전환합니다.
    /// </summary>
    public async UniTask FadeOutAsync(float duration = -1f)
    {
        if (!await PreloadFadeUIAsync())
        {
            return;
        }

        float targetDuration = duration >= 0f ? duration : _fadeOutDuration;
        await RunFadeAsync(0f, targetDuration, blocksRaycasts: false);
    }

    private void LockInput()
    {
        if (EventSystem.current != null)
        {
            _prevNav = EventSystem.current.sendNavigationEvents;
            EventSystem.current.sendNavigationEvents = false;
            EventSystem.current.SetSelectedGameObject(null);
        }

        _disabledDevices.Clear();
        foreach (InputDevice device in InputSystem.devices)
        {
            if (device != null && device.added && device.enabled)
            {
                InputSystem.DisableDevice(device);
                _disabledDevices.Add(device);
            }
        }
    }

    private void UnlockInput()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.sendNavigationEvents = _prevNav;
        }

        for (int i = 0; i < _disabledDevices.Count; i++)
        {
            InputDevice device = _disabledDevices[i];
            if (device != null && device.added)
            {
                InputSystem.EnableDevice(device);
            }
        }

        _disabledDevices.Clear();
    }

    private async UniTask<bool> LoadAsync()
    {
        if (_group != null)
        {
            return true;
        }

        if (_loading)
        {
            try
            {
                await UniTask.WaitUntil
                (
                    () => !_loading,
                    cancellationToken: this.GetCancellationTokenOnDestroy()
                );
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            return _group != null;
        }

        _loading = true;

        try
        {
            if (_fadePrefabRef != null && _fadePrefabRef.RuntimeKeyIsValid())
            {
                _handle = Addressables.InstantiateAsync(_fadePrefabRef, transform);
            }
            else if (!string.IsNullOrWhiteSpace(_fadePrefabAddress))
            {
                _handle = Addressables.InstantiateAsync(_fadePrefabAddress, transform);
            }
            else
            {
                CPrint.Warn("CFadeController : _fadePrefabRef와 _fadePrefabAddress가 모두 비어 있습니다.");
                return false;
            }

            _ui = await _handle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());

            if (_handle.Status == AsyncOperationStatus.Succeeded && _ui != null)
            {
                _group = _ui.GetComponentInChildren<CanvasGroup>(true);

                if (_group == null)
                {
                    CPrint.Error("CFadeController : FadeCanvas 프리팹에 CanvasGroup이 없습니다.");
                    return false;
                }

                ApplyState(0f, blocksRaycasts: false);
                CPrint.Log("CFadeController : FadeCanvas 선제 로드 완료");
                return true;
            }

            CPrint.Error("CFadeController : FadeCanvas 인스턴스화 실패");
            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            CPrint.Error($"CFadeController : FadeCanvas 로드 예외 - {ex.Message}");
            return false;
        }
        finally
        {
            _loading = false;
        }
    }

    private void ApplyState(float alpha, bool blocksRaycasts)
    {
        if (_group == null)
        {
            return;
        }

        _group.alpha = Mathf.Clamp01(alpha);
        _group.interactable = blocksRaycasts;
        _group.blocksRaycasts = blocksRaycasts;
    }

    private async UniTask RunFadeAsync(float targetAlpha, float duration, bool blocksRaycasts)
    {
        if (_group == null)
        {
            return;
        }

        int currentVersion = ++_version;

        _group.blocksRaycasts = true;
        _group.interactable = true;
        LockInput();

        try
        {
            float startAlpha = _group.alpha;
            if (duration <= 0f || Mathf.Approximately(startAlpha, targetAlpha))
            {
                if (currentVersion == _version && _group != null)
                {
                    ApplyState(targetAlpha, blocksRaycasts);
                }
                return;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (currentVersion != _version || _group == null)
                {
                    return;
                }

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _group.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: this.GetCancellationTokenOnDestroy());
            }

            if (currentVersion == _version && _group != null)
            {
                ApplyState(targetAlpha, blocksRaycasts);
            }
        }
        finally
        {
            if (currentVersion == _version)
            {
                UnlockInput();
            }
        }
    }

    protected override void OnDestroy()
    {
        _version++;
        UnlockInput();

        if (_handle.IsValid())
        {
            Addressables.ReleaseInstance(_handle);
        }

        _ui = null;
        _group = null;
        base.OnDestroy();
    }
}
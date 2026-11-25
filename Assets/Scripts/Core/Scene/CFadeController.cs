using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
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
    private CanvasGroup _fadeCanvasGroup;
    private GameObject _fadeInstance;
    private AsyncOperationHandle<GameObject> _loadHandle;
    private bool _isLoadingPrefab;
    private int _fadeVersion;
    #endregion

    public bool IsReady => _fadeCanvasGroup != null;

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
        return await EnsureFadeUIAsync();
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
        await FadeToAsync(1f, targetDuration, blocksRaycasts: true);
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
        await FadeToAsync(0f, targetDuration, blocksRaycasts: false);
    }

    private async UniTask<bool> EnsureFadeUIAsync()
    {
        if (_fadeCanvasGroup != null)
        {
            return true;
        }

        if (_isLoadingPrefab)
        {
            try
            {
                await UniTask.WaitUntil
                (
                    () => !_isLoadingPrefab,
                    cancellationToken: this.GetCancellationTokenOnDestroy()
                );
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            return _fadeCanvasGroup != null;
        }

        _isLoadingPrefab = true;

        try
        {
            if (_fadePrefabRef != null && _fadePrefabRef.RuntimeKeyIsValid())
            {
                _loadHandle = Addressables.InstantiateAsync(_fadePrefabRef, transform);
            }
            else if (!string.IsNullOrWhiteSpace(_fadePrefabAddress))
            {
                _loadHandle = Addressables.InstantiateAsync(_fadePrefabAddress, transform);
            }
            else
            {
                CPrint.Warn("CFadeController : _fadePrefabRef와 _fadePrefabAddress가 모두 비어 있습니다.");
                return false;
            }

            _fadeInstance = await _loadHandle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());

            if (_loadHandle.Status == AsyncOperationStatus.Succeeded && _fadeInstance != null)
            {
                _fadeCanvasGroup = _fadeInstance.GetComponentInChildren<CanvasGroup>(true);

                if (_fadeCanvasGroup == null)
                {
                    CPrint.Error("CFadeController : FadeCanvas 프리팹에 CanvasGroup이 없습니다.");
                    return false;
                }

                SetAlphaImmediate(0f, blocksRaycasts: false);
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
            _isLoadingPrefab = false;
        }
    }

    private void SetAlphaImmediate(float alpha, bool blocksRaycasts)
    {
        if (_fadeCanvasGroup == null)
        {
            return;
        }

        _fadeCanvasGroup.alpha = Mathf.Clamp01(alpha);
        _fadeCanvasGroup.interactable = blocksRaycasts;
        _fadeCanvasGroup.blocksRaycasts = blocksRaycasts;
    }

    private async UniTask FadeToAsync(float targetAlpha, float duration, bool blocksRaycasts)
    {
        if (_fadeCanvasGroup == null)
        {
            return;
        }

        int currentVersion = ++_fadeVersion;

        _fadeCanvasGroup.blocksRaycasts = true;
        _fadeCanvasGroup.interactable = true;

        float startAlpha = _fadeCanvasGroup.alpha;
        if (duration <= 0f || Mathf.Approximately(startAlpha, targetAlpha))
        {
            SetAlphaImmediate(targetAlpha, blocksRaycasts);
            return;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (currentVersion != _fadeVersion || _fadeCanvasGroup == null)
            {
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        if (currentVersion == _fadeVersion && _fadeCanvasGroup != null)
        {
            SetAlphaImmediate(targetAlpha, blocksRaycasts);
        }
    }

    protected override void OnDestroy()
    {
        _fadeVersion++;

        if (_loadHandle.IsValid())
        {
            Addressables.ReleaseInstance(_loadHandle);
        }

        _fadeInstance = null;
        _fadeCanvasGroup = null;
        base.OnDestroy();
    }
}
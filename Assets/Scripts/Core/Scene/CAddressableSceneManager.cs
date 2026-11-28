using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class CAddressableSceneManager : CManagerSingleton<CAddressableSceneManager>
{
    protected override bool IsDontDestroy => true;

    #region 내부 변수
    private readonly Dictionary<string, AsyncOperationHandle<SceneInstance>> _additiveHandles
       = new Dictionary<string, AsyncOperationHandle<SceneInstance>>();
    private readonly Dictionary<string, string> _modeMap
       = new Dictionary<string, string>();
    private AsyncOperationHandle<SceneInstance> _singleHandle;
    private bool _transitioning;
    #endregion

    public bool IsTransitioning => _transitioning;
    public bool HasAnyAdditiveScene => _additiveHandles.Count > 0;

    /// <summary>
    /// 지정한 씬 키 또는 모드 ID에 해당하는 Additive 씬이 현재 로드되어 있는지 확인합니다.
    /// </summary>
    public bool IsAdditiveLoaded(string keyOrModeId)
    {
        if (string.IsNullOrWhiteSpace(keyOrModeId))
        {
            return false;
        }

        string resolvedKey = ResolveKey(keyOrModeId);
        return _additiveHandles.TryGetValue(resolvedKey, out var handle) && handle.IsValid();
    }

    /// <summary>
    /// 어드레서블 주소(문자열)로 씬을 로드합니다.
    /// </summary>
    public async UniTask<bool> LoadSceneWithFadeAsync
        (
        string sceneAddress,
        LoadSceneMode loadMode = LoadSceneMode.Single,
        string modeId = null,
        bool useFade = true,
        Action<SceneInstance> onLoaded = null
        )
    {
        if (string.IsNullOrWhiteSpace(sceneAddress))
        {
            CPrint.Warn("CAddressableSceneManager.LoadSceneWithFadeAsync : sceneAddress가 비어 있습니다.");
            return false;
        }

        return await LoadAsync
        (
            sceneKey: sceneAddress,
            loadSceneFunc: mode => Addressables.LoadSceneAsync(sceneAddress, mode),
            sourceLabel: "Address",
            loadMode: loadMode,
            modeId: modeId,
            useFade: useFade,
            onLoaded: onLoaded
        );
    }

    /// <summary>
    /// AssetReference로 어드레서블 씬을 로드합니다.
    /// </summary>
    public async UniTask<bool> LoadSceneWithFadeAsync
        (
        AssetReference sceneRef,
        LoadSceneMode loadMode = LoadSceneMode.Single,
        string modeId = null,
        bool useFade = true,
        Action<SceneInstance> onLoaded = null
        )
    {
        if (sceneRef == null || !sceneRef.RuntimeKeyIsValid())
        {
            CPrint.Warn("CAddressableSceneManager.LoadSceneWithFadeAsync : 유효하지 않은 AssetReference입니다.");
            return false;
        }

        string sceneKey = sceneRef.RuntimeKey.ToString();
        return await LoadAsync
        (
            sceneKey: sceneKey,
            loadSceneFunc: mode => Addressables.LoadSceneAsync(sceneRef, mode),
            sourceLabel: "AssetReference",
            loadMode: loadMode,
            modeId: modeId,
            useFade: useFade,
            onLoaded: onLoaded
        );
    }

    /// <summary>
    /// 페이드 연출을 포함해 Additive 씬을 언로드합니다.
    /// </summary>
    public async UniTask<bool> UnloadAdditiveSceneWithFadeAsync
        (
        string keyOrModeId,
        bool useFade = true,
        Action onUnloaded = null
        )
    {
        if (string.IsNullOrWhiteSpace(keyOrModeId))
        {
            CPrint.Warn("CAddressableSceneManager.UnloadAdditiveSceneWithFadeAsync : 키/모드 ID가 비어 있습니다.");
            return false;
        }

        if (_transitioning)
        {
            CPrint.Warn($"CAddressableSceneManager : 이미 씬 전환 중입니다. 언로드 무시 ({keyOrModeId})");
            return false;
        }

        string sceneKey = ResolveKey(keyOrModeId);
        if (!_additiveHandles.TryGetValue(sceneKey, out var handle))
        {
            CPrint.Warn($"CAddressableSceneManager : 언로드할 Additive 씬 핸들이 없습니다 ({keyOrModeId})");
            onUnloaded?.Invoke();
            return false;
        }

        _transitioning = true;
        bool succeeded = false;

        try
        {
            if (useFade && CFadeController.Instance != null)
            {
                await CFadeController.Instance.FadeInAsync();
            }

            if (handle.IsValid())
            {
                AsyncOperationHandle<SceneInstance> unloadHandle = Addressables.UnloadSceneAsync(handle);
                await unloadHandle.ToUniTask();
                succeeded = unloadHandle.Status == AsyncOperationStatus.Succeeded;
            }
            else
            {
                succeeded = true;
            }

            RemoveModeMap(sceneKey);
            _additiveHandles.Remove(sceneKey);

            if (succeeded)
            {
                CPrint.Log($"어드레서블 Additive 씬 언로드 성공 : {sceneKey}");
                onUnloaded?.Invoke();
            }
            else
            {
                CPrint.Error($"어드레서블 Additive 씬 언로드 실패 : {sceneKey}");
            }
        }
        catch (Exception ex)
        {
            CPrint.Error($"어드레서블 Additive 씬 언로드 예외 ({sceneKey}) : {ex.Message}");
        }
        finally
        {
            try
            {
                if (useFade && CFadeController.Instance != null)
                {
                    await CFadeController.Instance.FadeOutAsync();
                }
            }
            finally
            {
                _transitioning = false;
            }
        }

        return succeeded;
    }

    private async UniTask<bool> LoadAsync
        (
        string sceneKey,
        Func<LoadSceneMode, AsyncOperationHandle<SceneInstance>> loadSceneFunc,
        string sourceLabel,
        LoadSceneMode loadMode,
        string modeId,
        bool useFade,
        Action<SceneInstance> onLoaded
        )
    {
        if (_transitioning)
        {
            CPrint.Warn($"CAddressableSceneManager : 이미 씬 전환 중입니다. 요청 무시 ({sceneKey})");
            return false;
        }

        _transitioning = true;
        bool succeeded = false;

        try
        {
            if (useFade && CFadeController.Instance != null)
            {
                await CFadeController.Instance.FadeInAsync();
            }

            AsyncOperationHandle<SceneInstance> handle = loadSceneFunc(loadMode);
            SceneInstance sceneInstance = await handle.ToUniTask();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                RegisterScene(sceneKey, modeId, handle, loadMode);
                CPrint.Log($"어드레서블 씬 로드 성공({sourceLabel}, {loadMode}) : {sceneInstance.Scene.name} [{sceneKey}]");
                onLoaded?.Invoke(sceneInstance);
                succeeded = true;
            }
            else
            {
                CPrint.Error($"어드레서블 씬 로드 실패({sourceLabel}, {loadMode}) : {sceneKey}");
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
        }
        catch (Exception ex)
        {
            CPrint.Error($"어드레서블 씬 로드 예외({sourceLabel}, {sceneKey}) : {ex.Message}");
        }
        finally
        {
            try
            {
                if (useFade && CFadeController.Instance != null)
                {
                    await CFadeController.Instance.FadeOutAsync();
                }
            }
            finally
            {
                _transitioning = false;
            }
        }

        return succeeded;
    }

    private string ResolveKey(string keyOrModeId)
    {
        if (_modeMap.TryGetValue(keyOrModeId, out string mappedKey))
        {
            return mappedKey;
        }

        return keyOrModeId;
    }

    private void RegisterScene(string sceneKey, string modeId, AsyncOperationHandle<SceneInstance> handle, LoadSceneMode loadMode)
    {
        if (loadMode == LoadSceneMode.Single)
        {
            _additiveHandles.Clear();
            _modeMap.Clear();
            _singleHandle = handle;
            return;
        }

        _additiveHandles[sceneKey] = handle;

        if (!string.IsNullOrWhiteSpace(modeId))
        {
            _modeMap[modeId] = sceneKey;
        }
    }

    private void RemoveModeMap(string sceneKey)
    {
        List<string> keysToRemove = null;
        foreach (var pair in _modeMap)
        {
            if (pair.Value == sceneKey)
            {
                keysToRemove ??= new List<string>();
                keysToRemove.Add(pair.Key);
            }
        }

        if (keysToRemove == null)
        {
            return;
        }

        for (int i = 0; i < keysToRemove.Count; i++)
        {
            _modeMap.Remove(keysToRemove[i]);
        }
    }

    protected override void OnDestroy()
    {
        foreach (var pair in _additiveHandles)
        {
            if (pair.Value.IsValid())
            {
                Addressables.UnloadSceneAsync(pair.Value);
            }
        }

        _additiveHandles.Clear();
        _modeMap.Clear();
        _singleHandle = default;
        base.OnDestroy();
    }
}
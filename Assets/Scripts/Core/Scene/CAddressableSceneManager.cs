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
    private readonly Dictionary<string, AsyncOperationHandle<SceneInstance>> _additiveHandlesByKey
       = new Dictionary<string, AsyncOperationHandle<SceneInstance>>();
    private readonly Dictionary<string, string> _modeIdToSceneKey
       = new Dictionary<string, string>();
    private AsyncOperationHandle<SceneInstance> _singleSceneHandle;
    private bool _isTransitioning;
    #endregion

    public bool IsTransitioning => _isTransitioning;
    public bool HasAnyAdditiveScene => _additiveHandlesByKey.Count > 0;

    /// <summary>
    /// 지정한 씬 키 또는 모드 ID에 해당하는 Additive 씬이 현재 로드되어 있는지 확인합니다.
    /// </summary>
    public bool IsAdditiveLoaded(string keyOrModeId)
    {
        if (string.IsNullOrWhiteSpace(keyOrModeId))
        {
            return false;
        }

        string resolvedKey = ResolveSceneKey(keyOrModeId);
        return _additiveHandlesByKey.TryGetValue(resolvedKey, out var handle) && handle.IsValid();
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

        return await LoadSceneInternalAsync
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
        return await LoadSceneInternalAsync
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

        if (_isTransitioning)
        {
            CPrint.Warn($"CAddressableSceneManager : 이미 씬 전환 중입니다. 언로드 무시 ({keyOrModeId})");
            return false;
        }

        string sceneKey = ResolveSceneKey(keyOrModeId);
        if (!_additiveHandlesByKey.TryGetValue(sceneKey, out var handle))
        {
            CPrint.Warn($"CAddressableSceneManager : 언로드할 Additive 씬 핸들이 없습니다 ({keyOrModeId})");
            onUnloaded?.Invoke();
            return false;
        }

        _isTransitioning = true;
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

            RemoveModeMappingsForKey(sceneKey);
            _additiveHandlesByKey.Remove(sceneKey);

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
                _isTransitioning = false;
            }
        }

        return succeeded;
    }

    private async UniTask<bool> LoadSceneInternalAsync
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
        if (_isTransitioning)
        {
            CPrint.Warn($"CAddressableSceneManager : 이미 씬 전환 중입니다. 요청 무시 ({sceneKey})");
            return false;
        }

        _isTransitioning = true;
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
                RegisterLoadedScene(sceneKey, modeId, handle, loadMode);
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
                _isTransitioning = false;
            }
        }

        return succeeded;
    }

    private string ResolveSceneKey(string keyOrModeId)
    {
        if (_modeIdToSceneKey.TryGetValue(keyOrModeId, out string mappedKey))
        {
            return mappedKey;
        }

        return keyOrModeId;
    }

    private void RegisterLoadedScene(string sceneKey, string modeId, AsyncOperationHandle<SceneInstance> handle, LoadSceneMode loadMode)
    {
        if (loadMode == LoadSceneMode.Single)
        {
            _additiveHandlesByKey.Clear();
            _modeIdToSceneKey.Clear();
            _singleSceneHandle = handle;
            return;
        }

        _additiveHandlesByKey[sceneKey] = handle;

        if (!string.IsNullOrWhiteSpace(modeId))
        {
            _modeIdToSceneKey[modeId] = sceneKey;
        }
    }

    private void RemoveModeMappingsForKey(string sceneKey)
    {
        List<string> keysToRemove = null;
        foreach (var pair in _modeIdToSceneKey)
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
            _modeIdToSceneKey.Remove(keysToRemove[i]);
        }
    }

    protected override void OnDestroy()
    {
        foreach (var pair in _additiveHandlesByKey)
        {
            if (pair.Value.IsValid())
            {
                Addressables.UnloadSceneAsync(pair.Value);
            }
        }

        _additiveHandlesByKey.Clear();
        _modeIdToSceneKey.Clear();
        _singleSceneHandle = default;
        base.OnDestroy();
    }
}
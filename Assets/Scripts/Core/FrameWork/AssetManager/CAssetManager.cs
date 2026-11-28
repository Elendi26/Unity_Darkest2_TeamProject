using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;


public class CAssetManager : CManagerSingleton<CAssetManager>
{
    protected override bool IsDontDestroy => true;

    private readonly Dictionary<string, UnityEngine.Object> _assetCache = new Dictionary<string, UnityEngine.Object>();
    private readonly Dictionary<string, AsyncOperationHandle> _handleMap = new Dictionary<string, AsyncOperationHandle>();


    /// <summary>
    /// 에셋을 동기 방식으로 즉시 로드합니다.
    /// </summary>
    /// <typeparam name="T">로드할 에셋 타입</typeparam>
    /// <param name="address">어드레서블 주소(경로)</param>
    public T LoadSync<T>(string address) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(address))
        {
            CPrint.Warn("로드할 에셋 주소가 비어 있습니다.");
            return null;
        }

        // 1. 이미 캐시된 에셋이 있다면 즉시 반환
        if (_assetCache.TryGetValue(address, out var cachedAsset))
        {
            return cachedAsset as T;
        }

        // 2. 캐시에 없으면 어드레서블 동기 로드 수행
        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(address);
        T asset = handle.WaitForCompletion();

        if (asset != null)
        {
            _assetCache[address] = asset;
            _handleMap[address] = handle;
        }
        else
        {
            CPrint.Error($"에셋 동기 로드 실패 - Address: {address}");
        }

        return asset;
    }

    /// <summary>
    /// 에셋을 비동기 방식으로 로드하고, 완료 시 콜백을 호출합니다.
    /// </summary>
    /// <typeparam name="T">로드할 에셋 타입</typeparam>
    /// <param name="address">어드레서블 주소(경로)</param>
    /// <param name="callback">로드가 완료되었을 때 실행할 콜백 함수</param>
    public void LoadAsync<T>(string address, Action<T> callback) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(address))
        {
            CPrint.Warn("로드할 에셋 주소가 비어 있습니다.");
            callback?.Invoke(null);
            return;
        }

        // 캐시가 있으면 즉시 콜백 호출
        if (_assetCache.TryGetValue(address, out var cachedAsset))
        {
            callback?.Invoke(cachedAsset as T);
            return;
        }

        LoadAsyncInternal(address, callback).Forget();
    }

    private async UniTaskVoid LoadAsyncInternal<T>(string address, Action<T> callback) where T : UnityEngine.Object
    {
        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(address);
        _handleMap[address] = handle;

        T asset = await handle.ToUniTask();

        if (asset != null)
        {
            _assetCache[address] = asset;
            callback?.Invoke(asset);
        }
        else
        {
            CPrint.Error($"에셋 비동기 로드 실패 - Address: {address}");
            callback?.Invoke(null);
        }
    }

    /// <summary>
    /// 로드된 에셋의 메모리를 해제합니다.
    /// </summary>
    /// <param name="address">어드레서블 주소(경로)</param>
    public void Release(string address)
    {
        if (_handleMap.TryGetValue(address, out var handle))
        {
            Addressables.Release(handle);
            _handleMap.Remove(address);
            _assetCache.Remove(address);
        }
    }
}
#if (YOOASSET || YOOASSET_ENABLED) && UNITASK

using System;
using System.Collections.Concurrent;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace Bear.ResourceSystem.External
{
    /// <summary>
    /// YooAsset 加载策略 - UniTask 扩展
    /// </summary>
    public partial class YooAssetLoader : IResourceLoader
    {
        /// <summary>
        /// Handle 缓存（路径 -> AssetHandle）
        /// </summary>
        private readonly ConcurrentDictionary<string, AssetHandle> _handleCache = new();

        /// <summary>
        /// 异步加载资源
        /// </summary>
        public async UniTask<T> LoadAsync<T>(string path, Action<float> onProgress = null, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            var package = GetPackage();
            if (package == null)
            {
                Debug.LogError($"[YooAssetLoader] Package not found: {_packageName}");
                return null;
            }

            // 检查缓存
            if (_handleCache.TryGetValue(path, out var cachedHandle) && cachedHandle.IsValid && cachedHandle.IsDone)
            {
                onProgress?.Invoke(1f);
                return cachedHandle.GetAssetObject<T>();
            }

            AssetHandle handle = package.LoadAssetAsync<T>(path);
            _handleCache[path] = handle;

            // 等待加载完成，支持进度
            while (!handle.IsDone)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    handle.Release();
                    _handleCache.TryRemove(path, out _);
                    return null;
                }
                
                onProgress?.Invoke(handle.Progress);
                await UniTask.Yield();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                handle.Release();
                _handleCache.TryRemove(path, out _);
                return null;
            }

            if (handle.Status == EOperationStatus.Succeed)
            {
                onProgress?.Invoke(1f);
                return handle.GetAssetObject<T>();
            }

            Debug.LogError($"[YooAssetLoader] Failed to load: {path}");
            handle.Release();
            _handleCache.TryRemove(path, out _);
            return null;
        }

        /// <summary>
        /// 预加载资源
        /// </summary>
        public async UniTask<bool> PreloadAsync<T>(string path) where T : UnityEngine.Object
        {
            var package = GetPackage();
            if (package == null) return false;

            // 如果已缓存，直接返回
            if (_handleCache.TryGetValue(path, out var cached) && cached.IsValid && cached.IsDone)
            {
                return true;
            }

            AssetHandle handle = package.LoadAssetAsync<T>(path);
            _handleCache[path] = handle;

            await handle.ToUniTask();

            return handle.Status == EOperationStatus.Succeed;
        }
    }
}

#endif

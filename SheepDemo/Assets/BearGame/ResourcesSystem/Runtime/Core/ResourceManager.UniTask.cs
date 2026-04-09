#if UNITASK

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Bear.ResourceSystem
{
    /// <summary>
    /// ResourceManager UniTask 扩展 - 提供异步加载功能
    /// 需要安装 UniTask 包才能使用
    /// </summary>
    public partial class ResourceManager
    {
        /// <summary>
        /// 加载中的任务（路径 -> TaskCompletionSource）
        /// 用于防止重复加载同一资源
        /// </summary>
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, UniTaskCompletionSource<UnityEngine.Object>> _loadingTasks = new();

        #region 异步加载（线程安全）

        /// <summary>
        /// 异步加载资源（线程安全，自动防止重复加载）
        /// </summary>
        public async UniTask<T> LoadAsync<T>(string path, Action<float> onProgress = null, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            // 1. 检查缓存
            if (_assetCache.TryGetValue(path, out var cached) && cached != null)
            {
                onProgress?.Invoke(1f);
                return cached as T;
            }

            // 2. 检查是否正在加载，复用已有任务
            if (_loadingTasks.TryGetValue(path, out var existingTcs))
            {
                onProgress?.Invoke(0.5f);
                var existingAsset = await existingTcs.Task.AttachExternalCancellation(cancellationToken);
                onProgress?.Invoke(1f);
                return existingAsset as T;
            }

            // 3. 创建新的加载任务
            var tcs = new UniTaskCompletionSource<UnityEngine.Object>();
            
            // 尝试添加，如果已存在则使用已存在的
            if (!_loadingTasks.TryAdd(path, tcs))
            {
                // 其他线程已经添加，使用它的任务
                tcs = _loadingTasks[path];
                var asset = await tcs.Task.AttachExternalCancellation(cancellationToken);
                return asset as T;
            }

            try
            {
                var loader = GetAvailableLoader();
                if (loader == null)
                {
                    throw new InvalidOperationException($"No available loader for: {path}");
                }

                // 执行异步加载
                var loadedAsset = await loader.LoadAsync<T>(path, onProgress, cancellationToken);

                if (loadedAsset == null)
                {
                    throw new InvalidOperationException($"Failed to load asset: {path}");
                }

                // 缓存资源
                _assetCache[path] = loadedAsset;

                // 完成任务
                tcs.TrySetResult(loadedAsset);

                return loadedAsset;
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
                throw;
            }
            finally
            {
                // 移除加载中标记
                _loadingTasks.TryRemove(path, out _);
            }
        }

        /// <summary>
        /// 异步加载并实例化（确保在主线程实例化）
        /// </summary>
        public async UniTask<T> LoadAndInstantiateAsync<T>(string path, Transform parent = null, Action<float> onProgress = null, CancellationToken cancellationToken = default) where T : Component
        {
            // 加载预制体
            var prefab = await LoadAsync<GameObject>(path, onProgress, cancellationToken);
            if (prefab == null) return null;

            // 确保在主线程实例化
            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            // 实例化
            var instance = Instantiate(prefab, parent);
            var component = instance.GetComponent<T>();

            // 记录实例与路径的映射
            _instanceToPath.Add(instance, path);

            return component;
        }

        #endregion

        #region 预加载

        /// <summary>
        /// 预加载资源
        /// </summary>
        public async UniTask<bool> PreloadAsync<T>(string path) where T : UnityEngine.Object
        {
            var asset = await LoadAsync<T>(path);
            return asset != null;
        }

        /// <summary>
        /// 批量预加载
        /// </summary>
        public async UniTask PreloadBatchAsync<T>(IEnumerable<string> paths, Action<float> onProgress = null) where T : UnityEngine.Object
        {
            var pathList = paths.ToList();
            int total = pathList.Count;
            int completed = 0;

            // 使用 WhenAll 并行加载
            var tasks = pathList.Select(async path =>
            {
                await PreloadAsync<T>(path);
                Interlocked.Increment(ref completed);
                onProgress?.Invoke((float)completed / total);
            });

            await UniTask.WhenAll(tasks);
        }

        #endregion
    }
}

#endif // UNITASK

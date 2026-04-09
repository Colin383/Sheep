using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bear.ResourceSystem
{
    // Auto-refresh trigger: 2026-03-31 19:22:00
    /// <summary>
    /// 资源管理器 - 统一管理所有资源加载策略
    /// 线程安全设计，所有对外接口可在任意线程调用
    /// </summary>
    public partial class ResourceManager : MonoBehaviour
    {
        private static ResourceManager _instance;
        public static ResourceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("ResourceManager");
                    _instance = go.AddComponent<ResourceManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        /// <summary>
        /// 加载器列表（按优先级排序）
        /// </summary>
        private readonly List<IResourceLoader> _loaders = new();
        private readonly object _loaderLock = new();

        /// <summary>
        /// 资源缓存（路径 -> 资源对象）
        /// 使用线程安全的 ConcurrentDictionary
        /// </summary>
        private readonly ConcurrentDictionary<string, UnityEngine.Object> _assetCache = new();

        /// <summary>
        /// 实例化对象到资源路径的映射（用于释放时查找）
        /// </summary>
        private readonly ConditionalWeakTable<UnityEngine.Object, string> _instanceToPath = new();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #region 加载器管理

        /// <summary>
        /// 注册加载器
        /// </summary>
        public void RegisterLoader(IResourceLoader loader)
        {
            if (loader == null) return;

            lock (_loaderLock)
            {
                if (_loaders.Any(l => l.LoaderName == loader.LoaderName))
                {
                    Debug.LogWarning($"[ResourceManager] Loader '{loader.LoaderName}' already registered.");
                    return;
                }

                _loaders.Add(loader);
                _loaders.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            }

            Debug.Log($"[ResourceManager] Registered loader: {loader.LoaderName} (Priority: {loader.Priority})");
        }

        /// <summary>
        /// 注销加载器
        /// </summary>
        public void UnregisterLoader(string loaderName)
        {
            lock (_loaderLock)
            {
                var loader = _loaders.FirstOrDefault(l => l.LoaderName == loaderName);
                if (loader != null)
                {
                    loader.ReleaseAll();
                    _loaders.Remove(loader);
                    Debug.Log($"[ResourceManager] Unregistered loader: {loaderName}");
                }
            }
        }

        /// <summary>
        /// 获取可用的加载器
        /// </summary>
        private IResourceLoader GetAvailableLoader()
        {
            lock (_loaderLock)
            {
                return _loaders.FirstOrDefault(l => l.IsAvailable);
            }
        }

        #endregion

        #region 同步加载

        /// <summary>
        /// 同步加载资源
        /// </summary>
        public T Load<T>(string path) where T : UnityEngine.Object
        {
            // 检查缓存
            if (_assetCache.TryGetValue(path, out var cached) && cached != null)
            {
                return cached as T;
            }

            var loader = GetAvailableLoader();
            if (loader == null)
            {
                Debug.LogError($"[ResourceManager] No available loader for: {path}");
                return null;
            }

            var asset = loader.Load<T>(path);
            if (asset != null)
            {
                _assetCache[path] = asset;
            }

            return asset;
        }

        #endregion

        #region 资源释放

        /// <summary>
        /// 通过路径释放资源
        /// </summary>
        public void Release(string path)
        {
            if (_assetCache.TryRemove(path, out var asset))
            {
                var loader = GetAvailableLoader();
                loader?.Release(path);
            }
        }

        /// <summary>
        /// 通过实例释放资源
        /// </summary>
        public void ReleaseInstance(UnityEngine.Object instance)
        {
            if (instance == null) return;

            // 查找实例对应的资源路径
            if (_instanceToPath.TryGetValue(instance, out var path))
            {
                // 销毁实例
                Destroy(instance);
                
                // 可选：减少引用计数，当引用为0时释放资源
                // 这里简化处理，直接保留资源在缓存中
            }
            else
            {
                // 未跟踪的实例直接销毁
                Destroy(instance);
            }
        }

        /// <summary>
        /// 释放所有资源
        /// </summary>
        public void ReleaseAll()
        {
            var paths = _assetCache.Keys.ToList();
            foreach (var path in paths)
            {
                Release(path);
            }

            lock (_loaderLock)
            {
                foreach (var loader in _loaders)
                {
                    loader.ReleaseAll();
                }
            }
        }

        #endregion

        #region 查询接口

        /// <summary>
        /// 检查资源是否已加载
        /// </summary>
        public bool IsLoaded(string path)
        {
            return _assetCache.ContainsKey(path) && _assetCache[path] != null;
        }

        /// <summary>
        /// 获取已加载资源列表
        /// </summary>
        public IReadOnlyCollection<string> GetLoadedAssets()
        {
            return _assetCache.Keys.ToList().AsReadOnly();
        }

        #endregion

        private void OnDestroy()
        {
            ReleaseAll();
        }
    }
}

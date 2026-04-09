#if YOOASSET || YOOASSET_ENABLED

using System;
using UnityEngine;
using YooAsset;

namespace Bear.ResourceSystem.External
{
    /// <summary>
    /// YooAsset 加载策略
    /// 放在 External 文件夹，表示对外部库的适配
    /// </summary>
    public partial class YooAssetLoader : IResourceLoader
    {
        public string LoaderName => "YooAssetLoader";
        public int Priority { get; set; } = 10;
        public bool IsAvailable => YooAssets.Initialized;

        private readonly string _packageName;
        private ResourcePackage _package;

        public YooAssetLoader(string packageName = null)
        {
            _packageName = packageName;
        }

        private ResourcePackage GetPackage()
        {
            if (_package != null) return _package;
            
            _package = string.IsNullOrEmpty(_packageName) 
                ? YooAssets.GetPackage("DefaultPackage") 
                : YooAssets.GetPackage(_packageName);
            
            return _package;
        }

        #region 同步加载

        public T Load<T>(string path) where T : UnityEngine.Object
        {
            var package = GetPackage();
            if (package == null)
            {
                Debug.LogError($"[YooAssetLoader] Package not found: {_packageName}");
                return null;
            }

            AssetHandle handle = package.LoadAssetSync<T>(path);
            if (handle.IsValid)
            {
                return handle.GetAssetObject<T>();
            }

            return null;
        }

        #endregion

        #region 资源释放

        public void Release(string path)
        {
            // 同步模式下没有缓存，不需要释放
        }

        public void ReleaseAll()
        {
            // 同步模式下没有缓存，不需要释放
        }

        #endregion
    }
}

#endif // YOOASSET || YOOASSET_ENABLED

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Guru.SDK.Framework.Core.Firebase.RemoteConfig;
using UnityEngine;
using Game.ConfigModule;

namespace SDK.Remote
{
    /// <summary>
    /// Firebase Remote Config 相关的工具封装。
    /// 后续如有更多 remote key，可在此集中管理。
    /// </summary>
    public static class RemoteConfigService
    {
        private const string LevelConfigKey = "level_config";

        /// <summary>
        /// 需要在启动阶段拉取的 Remote Config key 列表。
        /// 可在其他地方动态增减。
        /// </summary>
        public static readonly List<string> RemoteConfigKeys = new List<string>
        {
            LevelConfigKey
        };

        /// <summary>
        /// 适配 LoadManager 的统一入口：
        /// 使用当前的 RemoteConfigKeys 列表进行拉取和日志输出。
        /// </summary>
        public static UniTask UpdateRemoteConfigsAsync()
        {
            return UpdateRemoteConfigsAsync(RemoteConfigKeys);
        }

        /// <summary>
        /// 按传入的 key 列表拉取 Remote Config。
        /// </summary>
        public static async UniTask UpdateRemoteConfigsAsync(IReadOnlyList<string> keys)
        {
            if (keys == null || keys.Count == 0)
            {
                Debug.LogWarning("[RemoteConfigService] No remote config keys specified to update.");
                return;
            }

            var remote = RemoteConfigManager.Instance;

            // 强制从服务端拉取最新配置
            const float forceFetchMaxWaitSeconds = 10f;
            var forceFetchTask = remote.ForceFetch();
            var forceFetchTimeoutTask = UniTask.Delay(TimeSpan.FromSeconds(forceFetchMaxWaitSeconds));

            // 用一个 gate 把“forceFetch 完成 / 超时”变成一个可 await 的结果。
            // - forceFetch 先完成：继续走正常流程（异常会正常抛出）
            // - 超时先到：继续使用缓存/默认值，不阻塞启动流程
            var gate = new UniTaskCompletionSource<bool>();

            async UniTaskVoid WaitForceFetch()
            {
                try
                {
                    await forceFetchTask;
                    gate.TrySetResult(true);
                }
                catch (Exception e)
                {
                    // 即使超时后也要“观察”异常，避免未观察异常污染日志。
                    if (!gate.TrySetException(e))
                    {
                        Debug.LogWarning($"[RemoteConfigService] ForceFetch failed after timeout: {e.Message}");
                    }
                }
            }

            async UniTaskVoid WaitForceFetchTimeout()
            {
                await forceFetchTimeoutTask;
                gate.TrySetResult(false);
            }

            WaitForceFetch().Forget();
            WaitForceFetchTimeout().Forget();

            var forceFetchCompletedInTime = await gate.Task;
            if (!forceFetchCompletedInTime)
            {
                Debug.LogWarning(
                    $"[RemoteConfigService] ForceFetch timed out after {forceFetchMaxWaitSeconds} seconds. Cached or default values will be used.");
            }

            foreach (var key in keys)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                var msg = remote.GetString(key);
                if (string.IsNullOrEmpty(msg))
                {
                    Debug.LogWarning($"[RemoteConfigService] RemoteConfig key '{key}' is null or empty.");
                    continue;
                }

                Debug.Log($"[RemoteConfigService] RemoteConfig '{key}' raw json: {msg}");

                 // 缓存到 ConfigManager.RemoteConfig，方便游戏内逻辑访问
                 ConfigManager.RemoteConfig.SetRaw(key, msg);
            }
        }

        /// <summary>
        /// 拉取 Remote Config 并输出 custom_config 的调试日志。
        /// </summary>
        public static async UniTask UpdateCustomConfigAsync()
        {
            // 兼容旧接口，内部复用统一实现
            await UpdateRemoteConfigsAsync(new[] { LevelConfigKey });
        }

        /// <summary>
        /// 适配 LoadManager 的加载流程，提供 IEnumerator&lt;float&gt; 形式的进度协程。
        /// </summary>
        public static IEnumerator<float> UpdateRemoteConfigsForLoadManager()
        {
            var task = UpdateRemoteConfigsAsync();

            // 简单进度：等待远程拉取完成前一直返回 0，完成后返回 1
            while (!task.Status.IsCompleted())
            {
                yield return 0f;
            }

            yield return 1f;
        }
    }
}


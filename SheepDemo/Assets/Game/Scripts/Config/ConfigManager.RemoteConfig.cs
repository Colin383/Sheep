using System.Collections.Generic;
using Config.LubanConfig;

namespace Game.ConfigModule
{
    /// <summary>
    /// ConfigManager 的 RemoteConfig 扩展：
    /// 用于存储并解析从 RemoteConfigService 拉取到的配置 JSON。
    /// </summary>
    public partial class ConfigManager
    {
        public static class RemoteConfig
        {
            public const string LevelConfigKey = "level_config";
            public const string EnabledFieldKey = "enabled";
            public const string LevelSortKey = "level_sort";
            public const string ShowTipsTimeLimitFieldKey = "showTipsTimeLimit";
            public const string ShowTipsFailCountFieldKey = "showTipsFailCount";
            public const string ShowRatingLevelKey = "showRatingLevel";

            public const string AdInterstitialCDFieldKey = "ad_interstitialCD";
            public const string AdRewardResetInterstitialCDFieldKey = "ad_rewardResetInterstitialCD";
            public const string AdInterstitialShowMinLevelFieldKey = "ad_interstitialShowMinLevel";
            public const string AdInterstitialShowIntervalCountFieldKey = "ad_interstitialShowIntervalCount";
            public const string AdInterstitialShowFailedCountFieldKey = "ad_interstitialShowFailedCount";

            private static GlobalConst globalConst;

            /// <summary>
            /// 记录某个 remote key 对应的原始 JSON。
            /// 同步写入 DB.GameData.RemoteConfigCache 并保存，便于下次启动使用。
            /// </summary>
            public static void SetRaw(string remoteKey, string json)
            {
                if (string.IsNullOrEmpty(remoteKey))
                {
                    return;
                }

                if (string.IsNullOrEmpty(json))
                {
                    PersistRemoteConfigCache(remoteKey, remove: true);
                    return;
                }

                PersistRemoteConfigCache(remoteKey, json);
            }

            private static void PersistRemoteConfigCache(string remoteKey, string json = null, bool remove = false)
            {
                if (DB.GameData == null)
                {
                    return;
                }

                var cache = DB.GameData.RemoteConfigCache;
                if (cache == null)
                {
                    cache = new Dictionary<string, string>();
                    DB.GameData.RemoteConfigCache = cache;
                }

                if (remove)
                {
                    cache.Remove(remoteKey);
                }
                else
                {
                    cache[remoteKey] = json;
                }

                DB.GameData.Save();
            }

            /// <summary>
            /// 获取某个 remote key 的原始 JSON（来自 DB.GameData.RemoteConfigCache）。
            /// </summary>
            public static bool TryGetRaw(string remoteKey, out string json)
            {
                json = null;
                if (string.IsNullOrEmpty(remoteKey))
                {
                    return false;
                }

                var cache = DB.GameData?.RemoteConfigCache;
                if (cache == null || !cache.TryGetValue(remoteKey, out json) || string.IsNullOrEmpty(json))
                {
                    json = null;
                    return false;
                }

                return true;
            }

            /// <summary>
            /// 使用 JsonUtils 从指定的 remote key JSON 中解析字段。
            /// </summary>
            public static bool TryGetValue<T>(string remoteKey, string fieldKey, out T value)
            {
                value = default;

                if (string.IsNullOrEmpty(fieldKey))
                {
                    return false;
                }

                if (!TryGetRaw(remoteKey, out var json))
                {
                    return false;
                }

                return JsonUtils.TryGetValue<T>(json, fieldKey, out value);
            }

            private static bool TryGetGlobalConst(out GlobalConst value)
            {
                if (globalConst != null)
                {
                    value = globalConst;
                    return true;
                }

                var manager = ConfigManager.Instance;
                if (manager == null || !manager.IsInitialized || manager.Tables == null)
                {
                    value = null;
                    return false;
                }

                var list = manager.Tables.TbGlobalConst?.DataList;
                if (list == null || list.Count == 0)
                {
                    value = null;
                    return false;
                }

                globalConst = list[0];
                value = globalConst;
                return true;
            }

            public static string GetGlobalTest()
            {
                return TryGetGlobalConst(out var value) ? value.Test : string.Empty;
            }

            public static int GetGlobalTipCostCount()
            {
                return TryGetGlobalConst(out var value) ? value.TipCostCount : 0;
            }

            public static int GetShowTipsFailCount()
            {
                if (TryGetValue<int>(LevelConfigKey, ShowTipsFailCountFieldKey, out var remoteValue))
                {
                    return remoteValue;
                }

                return TryGetGlobalConst(out var value) ? value.ShowTipsFailCount : 0;
            }

            public static float GetShowTipsTimeLimit()
            {
                if (TryGetValue<float>(LevelConfigKey, ShowTipsTimeLimitFieldKey, out var remoteValue))
                {
                    return remoteValue;
                }

                return TryGetGlobalConst(out var value) ? value.ShowTipsTimeLimit : 0f;
            }

            public static int GetAdInterstitialCD()
            {
                if (TryGetValue<int>(LevelConfigKey, AdInterstitialCDFieldKey, out var remoteValue))
                {
                    return remoteValue;
                }

                return TryGetGlobalConst(out var value) ? value.AdInterstitialCD : 0;
            }

            public static int GetAdRewardResetInterstitialCD()
            {
                if (TryGetValue<int>(LevelConfigKey, AdRewardResetInterstitialCDFieldKey, out var remoteValue))
                {
                    return remoteValue;
                }

                return TryGetGlobalConst(out var value) ? value.AdRewardResetInterstitialCD : 0;
            }

            public static int GetAdInterstitialShowMinLevel()
            {
                if (TryGetValue<int>(LevelConfigKey, AdInterstitialShowMinLevelFieldKey, out var remoteValue))
                {
                    return remoteValue;
                }

                return TryGetGlobalConst(out var value) ? value.AdInterstitialShowMinLevel : 0;
            }

            public static int GetAdInterstitialShowIntervalCount()
            {
                if (TryGetValue<int>(LevelConfigKey, AdInterstitialShowIntervalCountFieldKey, out var remoteValue))
                {
                    return remoteValue;
                }

                return TryGetGlobalConst(out var value) ? value.AdInterstitialShowIntervalCount : 0;
            }

            public static int GetAdInterstitialShowFailedCount()
            {
                if (TryGetValue<int>(LevelConfigKey, AdInterstitialShowFailedCountFieldKey, out var remoteValue))
                {
                    return remoteValue;
                }

                return TryGetGlobalConst(out var value) ? value.AdInterstitialShowFailedCount : 0;
            }

            public static int GetShowRatingLevel()
            {
                if (TryGetValue<int>(LevelConfigKey, ShowRatingLevelKey, out var remoteValue))
                {
                    return remoteValue;
                }

                return TryGetGlobalConst(out var value) ? value.ShowRatingLevel : 0;
            }
        }
    }
}


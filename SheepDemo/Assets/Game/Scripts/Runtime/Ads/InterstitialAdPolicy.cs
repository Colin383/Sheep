using System;
using Bear.EventSystem;
using Bear.Logger;
using Game.ConfigModule;
using Game.Events;
using GameCommon;

namespace Game
{
    /// <summary>
    /// 插屏广告展示条件判断。
    /// 使用 GlobalConst / RemoteConfig 中的配置：
    /// - 插屏展示 CD
    /// - 激励视频后插屏重置 CD
    /// - 插屏最小显示关卡数
    /// - 插屏显示间隔关卡数
    /// - 插屏失败次数触发阈值
    /// </summary>
    public class InterstitialAdPolicy
    {
        private EventSubscriber _subscriber;

        private static void Log(string message)
        {
            BearLogger.Log($"[InterstitialAdPolicy] {message}", typeof(InterstitialAdPolicy));
        }

        public InterstitialAdPolicy()
        {
            InitEvents();
        }

        private void InitEvents()
        {
            EventsUtils.ResetEvents(ref _subscriber);
            _subscriber.Subscribe<PlayRewardADEvent>(OnPlayRewardAd);
            _subscriber.Subscribe<PlayInterstitialAdEvent>(OnPlayInterstitialAd);
            _subscriber.Subscribe<GameResetEvent>(OnGameReset);
        }

        private void OnPlayRewardAd(PlayRewardADEvent evt)
        {
            OnRewardAdShown();
        }

        private void OnPlayInterstitialAd(PlayInterstitialAdEvent evt)
        {
            OnInterstitialShown();
        }

        private void OnGameReset(GameResetEvent evt)
        {
            OnPlayerFailedOrRetried();
        }

        /// <summary>
        /// 当前插屏剩余冷却时间（秒）。小于等于 0 表示无冷却。
        /// </summary>
        public int CurrentInterstitialCD
        {
            get
            {
                var interstitialCd = ConfigManager.RemoteConfig.GetAdInterstitialCD();
                var now = GetNowSeconds();

                var interstitialRemain = 0;
                var lastInterstitial = DB.GameData.LastInterstitialDate;
                if (interstitialCd > 0 && lastInterstitial > 0)
                {
                    var elapsed = now - lastInterstitial;
                    if (elapsed < interstitialCd)
                    {
                        interstitialRemain = (int)(interstitialCd - elapsed);
                    }
                }

                var rewardResetCd = ConfigManager.RemoteConfig.GetAdRewardResetInterstitialCD();
                var rewardRemain = 0;
                var lastRewardAd = DB.GameData.LastRewardAdDate;
                if (rewardResetCd > 0 && lastRewardAd > 0)
                {
                    var elapsed = now - lastRewardAd;
                    if (elapsed < rewardResetCd)
                    {
                        rewardRemain = (int)(rewardResetCd - elapsed);
                    }
                }

                return Math.Max(interstitialRemain, rewardRemain);
            }
        }

        /// <summary>
        /// 在显示插屏广告成功时调用，用于记录时间并重置计数。
        /// </summary>
        public void OnInterstitialShown()
        {
            var now = GetNowSeconds();
            DB.GameData.LastInterstitialDate = now;
            DB.GameData.Save();
            Log($"OnInterstitialShown -> save lastInterstitial:{now}, reset failureCount");
        }

        /// <summary>
        /// 在显示激励视频广告成功时调用，用于记录时间。
        /// </summary>
        public void OnRewardAdShown()
        {
            DB.GameData.LastRewardAdDate = GetNowSeconds();
            DB.GameData.Save();
            Log($"OnRewardAdShown -> lastRewardTimestamp:{DB.GameData.LastRewardAdDate}");
        }

        /// <summary>
        /// 在玩家死亡或点击重试时调用，用于累计失败次数。
        /// </summary>
        public void OnPlayerFailedOrRetried()
        {
            DB.GameData.InterstitialFailCount++;
            DB.GameData.Save();
            Log($"OnPlayerFailedOrRetried -> failureCount:{DB.GameData.InterstitialFailCount}");
        }

        public void ResetFailCount()
        {
            DB.GameData.InterstitialFailCount = 0;
            DB.GameData.Save();
            Log($"OnPlayerFailedOrRetried -> failureCount:{DB.GameData.InterstitialFailCount}");
        }

        public void ResetSuccessCount()
        {
            DB.GameData.InterstitialLevelSuccessCount = 0;
            DB.GameData.Save();
            Log($"OnPlayerFailedOrRetried -> failureCount:{DB.GameData.InterstitialLevelSuccessCount}");
        }

        /// <summary>
        /// 判断当前是否可以展示插屏广告。
        /// 不同 placement 使用不同触发规则：
        /// - InGameFailed: 失败次数触发
        /// - InGameVictory: 关卡间隔触发
        /// </summary>
        /// <param name="currentLevel">当前关卡 ID（或序号）。</param>
        /// <param name="placement">插屏广告位。</param>
        public bool CanShowInterstitial(int currentLevel, Config.Game.InterstitialPlacement placement)
        {
            var now = GetNowSeconds();
            int _failureCount = DB.GameData.InterstitialFailCount;
            var _lastRewardTimestamp = DB.GameData.LastRewardAdDate;
            var sinceLastInterstitial = DB.GameData.LastInterstitialDate > 0 ? now - DB.GameData.LastInterstitialDate : -1;
            Log($"CanShowInterstitial start -> level:{currentLevel}, placement:{placement}, now:{now}, lastInterstitial:{DB.GameData.LastInterstitialDate}, sinceLastInterstitial:{sinceLastInterstitial}");

            // 1) 插屏冷却时间
            var interstitialCd = ConfigManager.RemoteConfig.GetAdInterstitialCD();
            var lastInterstitial = DB.GameData.LastInterstitialDate;
            if (interstitialCd > 0 && lastInterstitial > 0 && now - lastInterstitial < interstitialCd)
            {
                var remain = interstitialCd - (now - lastInterstitial);
                Log($"CanShowInterstitial blocked by interstitial CD -> cd:{interstitialCd}, remain:{remain}");
                return false;
            }

            // 2) 激励视频后冷却时间，在 CD 内不弹插屏s
            var rewardResetCd = ConfigManager.RemoteConfig.GetAdRewardResetInterstitialCD();
            if (rewardResetCd > 0 && now - _lastRewardTimestamp < rewardResetCd)
            {
                var remain = rewardResetCd - (now - _lastRewardTimestamp);
                Log($"CanShowInterstitial blocked by reward reset CD -> cd:{rewardResetCd}, remain:{remain}");
                return false;
            }

            var minLevel = ConfigManager.RemoteConfig.GetAdInterstitialShowMinLevel();
            var interval = ConfigManager.RemoteConfig.GetAdInterstitialShowIntervalCount();
            var failCountThreshold = ConfigManager.RemoteConfig.GetAdInterstitialShowFailedCount();

            // 3) 关卡间隔触发：前 minLevel-1 关不弹，从 minLevel 开始每 interval 关触发一次
            var levelTrigger = false;
            if (currentLevel >= minLevel && interval > 0)
            {
                var delta = currentLevel - minLevel;
                int successCount = DB.GameData.InterstitialLevelSuccessCount;
                if (delta >= 0 && successCount - interval >= 0)
                {
                    levelTrigger = true;
                }
            }

            // 4) 失败/重试次数触发
            var failTrigger = failCountThreshold > 0 && _failureCount >= failCountThreshold;
            var canShow = false;
            if (placement == Config.Game.InterstitialPlacement.InGameFailed)
            {
                canShow = failTrigger;
            }
            else if (placement == Config.Game.InterstitialPlacement.InGameVictory)
            {
                canShow = levelTrigger;
            }
            else
            {
                canShow = levelTrigger || failTrigger;
            }

            Log($"CanShowInterstitial triggers -> placement:{placement}, levelTrigger:{levelTrigger}, failTrigger:{failTrigger}, resultByPlacement:{canShow}, minLevel:{minLevel}, interval:{interval}, failThreshold:{failCountThreshold}, failureCount:{_failureCount}");

            if (!canShow)
            {
                Log("CanShowInterstitial result:false -> no trigger matched");
                return false;
            }

            Log("CanShowInterstitial result:true");
            return true;
        }

        private static long GetNowSeconds()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }


}


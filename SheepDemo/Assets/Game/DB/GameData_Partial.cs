using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Bear.SaveModule
{
    public partial class GameData
    {
        /// <summary>
        /// 初始化数据（设置默认值）
        /// </summary>
        public override void Init()
        {
            currentLevel = 1;
            maxLevel = 0;
            unlockLevels = new List<int>();
            passedLevels = new List<int>();
            chioceLevelPanelPageIndex = 0;
            playCount = 0;
            playDays = 0;
            gameTools = new Dictionary<string, int>();
            purchaseCache = new Dictionary<string, int>();
            lastInterstitialDate = 0;
            lastRewardAdDate = 0;
            interstitialFailCount = 0;
            interstitialLevelSuccessCount = 0;
            hasRatingPopup = false;
            levelPlayCount = new Dictionary<string, int>();
            levelSuccessCount = new Dictionary<string, int>();
            levelReviveCount = new Dictionary<string, int>();
            levelPlayDuration = new Dictionary<string, long>();
            remoteConfigCache = new Dictionary<string, string>();
            adGetTools = new Dictionary<string, int>();
            purchaseGetTools = new Dictionary<string, int>();
        }

        public int CurrentLevel
        {
            get => currentLevel;
            set => currentLevel = value;
        }

        public int MaxLevel
        {
            get => maxLevel;
            set => maxLevel = value;
        }

        public List<int> UnlockLevels
        {
            get => unlockLevels;
            set => unlockLevels = value;
        }

        public List<int> PassedLevels
        {
            get => passedLevels;
            set => passedLevels = value;
        }

        public int ChioceLevelPanelPageIndex
        {
            get => chioceLevelPanelPageIndex;
            set => chioceLevelPanelPageIndex = value;
        }

        public int PlayCount
        {
            get => playCount;
            set => playCount = value;
        }

        public int PlayDays
        {
            get => playDays;
            set => playDays = value;
        }

        public Dictionary<string, int> GameTools
        {
            get => gameTools;
            set => gameTools = value;
        }

        public Dictionary<string, int> PurchaseCache
        {
            get => purchaseCache;
            set => purchaseCache = value;
        }

        public long LastInterstitialDate
        {
            get => lastInterstitialDate;
            set => lastInterstitialDate = value;
        }

        public long LastRewardAdDate
        {
            get => lastRewardAdDate;
            set => lastRewardAdDate = value;
        }

        public int InterstitialFailCount
        {
            get => interstitialFailCount;
            set => interstitialFailCount = value;
        }

        public int InterstitialLevelSuccessCount
        {
            get => interstitialLevelSuccessCount;
            set => interstitialLevelSuccessCount = value;
        }

        public bool HasRatingPopup
        {
            get => hasRatingPopup;
            set => hasRatingPopup = value;
        }

        public Dictionary<string, int> LevelPlayCount
        {
            get => levelPlayCount;
            set => levelPlayCount = value;
        }

        public Dictionary<string, int> LevelSuccessCount
        {
            get => levelSuccessCount;
            set => levelSuccessCount = value;
        }

        public Dictionary<string, int> LevelReviveCount
        {
            get => levelReviveCount;
            set => levelReviveCount = value;
        }

        public Dictionary<string, long> LevelPlayDuration
        {
            get => levelPlayDuration;
            set => levelPlayDuration = value;
        }

        public Dictionary<string, string> RemoteConfigCache
        {
            get => remoteConfigCache;
            set => remoteConfigCache = value;
        }

        public Dictionary<string, int> AdGetTools
        {
            get => adGetTools;
            set => adGetTools = value;
        }

        public Dictionary<string, int> PurchaseGetTools
        {
            get => purchaseGetTools;
            set => purchaseGetTools = value;
        }

    }
}

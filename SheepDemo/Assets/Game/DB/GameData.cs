using System.Collections.Generic;
using UnityEngine;

namespace Bear.SaveModule
{
    // [CreateAssetMenu(fileName = "GameData", menuName = "Save Data/GameData")]
    public partial class GameData : BaseSaveDataSO
    {
        public static StorageType StorageType = StorageType.PlayerPrefs;

        // 当前关卡
        [SerializeField] private int currentLevel = 1;

        // 最大通关关卡
        [SerializeField] private int maxLevel = 0;

        // 已解锁关卡
        [SerializeField] private List<int> unlockLevels = new List<int>();

        // 已通关关卡
        [SerializeField] private List<int> passedLevels = new List<int>();

        // ChoiceLevelPanel 页数记录
        [SerializeField] private int chioceLevelPanelPageIndex = 0;
        // 开启游戏次数
        [SerializeField] private int playCount = 0;
        // 游玩天数
        [SerializeField] private int playDays = 0;

        // 背包道具内容
        [SerializeField] private Dictionary<string, int> gameTools = new Dictionary<string, int>();

        // 内购数据缓存 <product_id, count>
        [SerializeField] private Dictionary<string, int> purchaseCache = new Dictionary<string, int>();

        // 上一次播放插屏时间
        [SerializeField] private long lastInterstitialDate = 0;

        // 上一次播放激励时间
        [SerializeField] private long lastRewardAdDate = 0;
        // 用于插屏计算失败次数
        [SerializeField] private int interstitialFailCount = 0;

        // 用于插屏计算成功通关次数，包含广告通关
        [SerializeField] private int interstitialLevelSuccessCount = 0;

        // 评分弹窗已经弹出
        [SerializeField] private bool hasRatingPopup = false;

        // 对应关卡游玩次数, <levelPath, count>
        [SerializeField] private Dictionary<string, int> levelPlayCount = new Dictionary<string, int>();
        // 对应关卡胜利次数, <levelPath, count>
        [SerializeField] private Dictionary<string, int> levelSuccessCount = new Dictionary<string, int>();

        // 对应关卡复活次数, <levelPath, count>
        [SerializeField] private Dictionary<string, int> levelReviveCount = new Dictionary<string, int>();

        // 对应关卡游玩时长 mm , <levelPath, duration>
        [SerializeField] private Dictionary<string, long> levelPlayDuration = new Dictionary<string, long>();

        [SerializeField] private Dictionary<string, string> remoteConfigCache = new Dictionary<string, string>();

        // 打点需要

        #region 打点

        [SerializeField] private Dictionary<string, int> adGetTools = new Dictionary<string, int>();
        [SerializeField] private Dictionary<string, int> purchaseGetTools = new Dictionary<string, int>();

        #endregion

    }
}

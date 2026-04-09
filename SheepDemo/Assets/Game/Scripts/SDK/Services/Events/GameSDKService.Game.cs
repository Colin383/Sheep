
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Config.Game;
using Cysharp.Threading.Tasks;
using GameCommon;

// 游戏事件接口
public partial class GameSDKService
{
    private const string ScreenGameView = "GameView";

    private const int InterstitialShownIntervalLevelCount = 3;  // 插屏 间隔关卡
    private const int MinInterstitialEnableLevel = 3; // 插屏 新手保护关卡
    private const int MinBannerEnableLevel = 2; // Banner 新手保护关卡
    private const int MaxLevelSuccessReportLevelId = 50; // 最大成功关卡上报数

    private int _currentIvIntervalLevelCount = 0;
    private int _curLevelId = 0;

    public void LevelStartEvent(int status)
    {
        var playCtrl = PlayCtrl.Instance;
        var levelPath = GetCurrentLevelPathOrDefault();
        DB.GameData.LevelPlayCount.TryGetValue(levelPath, out int playCount);
        DB.GameData.LevelReviveCount.TryGetValue(levelPath, out int reviveCount);

        if (playCount <= 0)
            UnityEngine.Debug.LogWarning($"Wrong playcount in level: {levelPath}");

        var count = GetLevelPassedCount();
        // 关卡启动事件        
        LevelStartEvent(
            levelId: count + 1,
            levelPath: levelPath,
            playCount: playCount + reviveCount,
            levelStartType: status);
    }

    /// <summary>
    /// Level 启动事件
    /// </summary>
    /// <param name="levelId">关卡 ID</param>
    /// <param name="levelStartType">关卡启动类型</param>
    /// <param name="scene">当前的游戏场景</param>
    public void LevelStartEvent(int levelId,
    string levelPath,
    int playCount,
    int levelStartType = 0, string scene = "GameView")
    {
        // 上报 Screen
        SetScreen(ScreenGameView);

        // levelStartType
        // 0: 新关卡开始
        // 1: 重新开始
        // 2: 其他定义

        _curLevelId = levelId;

        // 打点上报
        var data = new Dictionary<string, object>()
        {
            ["level"] = levelId,
            ["level_name"] = levelPath,
            ["item_category"] = "main",
            ["play_count"] = playCount
        };

        var startType = GetLevelStartType(levelStartType);
        if (!string.IsNullOrEmpty(startType))
        {
            data["start_type"] = startType;
        }

        LogEvent("level_start", data);
    }

    private string GetLevelStartType(int levelStartType)
    {
        return levelStartType switch
        {
            LevelStartTypeNewGame => "play",
            LevelStartTypeReplay => "replay",
            LevelStartTypeContinue => "continue",
            _ => ""
        };
    }


    /*     private async UniTask DelayShowBanner()
        {
            if (BannerIsShown) return;
            await UniTask.Delay(TimeSpan.FromSeconds(2));
            ShowBanner();
        } */

    public void LevelEndEvent(int status)
    {
        var playCtrl = PlayCtrl.Instance;
        var levelPath = GetCurrentLevelPathOrDefault();

        // 记录时间
        playCtrl.Level.CurrentLevelState.RecordLevelDuration();
        DB.GameData.LevelPlayDuration.TryGetValue(levelPath, out var totalDuration);
        var count = GetLevelPassedCount();

        var level = playCtrl.Level;
        var passedCount = status == GameSDKService.LevelEndTypeSuccess ? count : count + 1;
        var levelId = level.CurrentLevelState.LevelId;
        var duration = level?.CurrentLevelState != null
            ? level.CurrentLevelState.GetCurrentLevelTimeSeconds()
            : 0L;

        // totoal Duration 暂时用 durtion，因为BI 打点统计的规范问题    
        LevelEndEvent(
            levelId: levelId,
            passedCount: passedCount,
            levelPath: levelPath,
            duration: duration,
            totalDuration: totalDuration,
            reviveCount: 0,
            levelEndType: status);
    }

    /// <summary>
    /// 关卡结束事件
    /// </summary>
    /// <param name="levelId"></param>
    /// <param name="levelEndType"></param>
    /// <param name="scene"></param>
    /// <param name="duration">单局游戏时长</param>
    /// <param name="totalDuration">这关总时长</param>
    /// 
    public void LevelEndEvent(int levelId,
    int passedCount,
    string levelPath,
    long duration,
    long totalDuration,
    int reviveCount,
    int levelEndType = 0, string scene = "GameView")
    {
        // levelEndType
        // 0: 关卡成功
        // 1: 关卡失败
        // 2: 其他定义

        // HideBanner();

        // TODO: 这里只放打点范例, 并非最终打点参数, 项目组需要自己补全
        // 上报 Level End
        var data = new Dictionary<string, object>()
        {
            ["level"] = passedCount,
            ["level_name"] = levelPath,
            ["item_category"] = "main",
            ["duration"] = duration,
            ["level_duration"] = duration,
            ["revive"] = reviveCount,
            ["total_duration"] = totalDuration
        };

        var leveResult = GetLevelResult(levelEndType);
        if (!string.IsNullOrEmpty(leveResult))
        {
            data["result"] = leveResult;
        }

        // 打点上报
        LogEvent("level_end", data);

        if (levelEndType == LevelEndTypeSuccess)
        {
            var count = GetLevelPassedCount();
            LevelEndSuccessEvent(count, levelId);
        }
    }

    // 买量点上报
    private void LevelEndSuccessEvent(int count, int levelId)
    {
        if (count > MaxLevelSuccessReportLevelId)
            return;


        LogEvent($"level_end_success_{count}", new Dictionary<string, object>()
        {
            ["level"] = levelId,
        });
    }



    private string GetLevelResult(int levelEndType)
    {
        return levelEndType switch
        {
            LevelEndTypeSuccess => "success",
            LevelEndTypeFailed => "fail",
            LevelEndTypeExit => "exit",
            _ => ""
        };
    }

    /// <summary>
    /// 获得灯泡
    /// </summary>
    public void BulbsGet(int value, int oldCount, int newCount, RewardType type)
    {
        var (levelPath, totalDuration, reviveCount) = GetLevelRuntimeContext();

        var data = new Dictionary<string, object>
        {
            ["level_name"] = levelPath,
            ["item_category"] = type.ToString(),
            ["value"] = value,
            ["balance"] = newCount,
            ["balance_before"] = oldCount,
            ["revive"] = reviveCount,
            ["level_duration"] = totalDuration,
        };

        LogEvent("bulbs_get", data);
    }

    /// <summary>
    /// 灯泡消耗
    /// </summary>
    public void BulbsCost(int value, int oldCount, int newCount, CostType type)
    {
        var (levelPath, totalDuration, reviveCount) = GetLevelRuntimeContext();

        var data = new Dictionary<string, object>
        {
            ["level_name"] = levelPath,
            ["item_category"] = type.ToString(),
            ["value"] = value,
            ["balance"] = newCount,
            ["balance_before"] = oldCount,
            ["revive"] = reviveCount,
            ["level_duration"] = totalDuration,
        };

        LogEvent("bulbs_cost", data);
    }

    /// <summary>
    /// 获取当前关卡用于打点的基础上下文
    /// </summary>
    private (string levelPath, long totalDuration, int reviveCount) GetLevelRuntimeContext()
    {
        var levelPath = GetCurrentLevelPathOrDefault();

        DB.GameData.LevelPlayDuration.TryGetValue(levelPath, out var totalDuration);
        DB.GameData.LevelReviveCount.TryGetValue(levelPath, out var reviveCount);

        return (levelPath, totalDuration, reviveCount);
    }

    /// <summary>
    /// 安全获取当前关卡路径，避免 CurrentLevelData 为空导致异常
    /// </summary>
    private string GetCurrentLevelPathOrDefault()
    {
        var playCtrl = PlayCtrl.Instance;
        var level = playCtrl?.Level;
        var levelData = level?.CurrentLevelData;
        if (levelData == null)
        {
            UnityEngine.Debug.LogWarning("CurrentLevelData is null when reporting game analytics. Use 'no_start' as level path.");
            return "no_start";
        }

        return levelData.Path;
    }

    /// <summary>
    /// 获取道具或者金币时候
    /// </summary>
    /// <param name="prop"></param>
    /// <param name="value"></param>
    /// <param name="type"></param>
    /// <param name="newCount"></param>
    public void EarnVirtualCurrentcy(GameProps prop, int value, RewardType type, int newCount)
    {
        var levelPath = GetCurrentLevelPathOrDefault();

        var data = new Dictionary<string, object>()
        {
            ["virtual_currency_name"] = prop.ToString(),
            ["item_category"] = type == RewardType.Purchase ? "iap_buy" : type.ToString(),
            ["level_name"] = levelPath,
            ["value"] = value,
            ["balance"] = newCount,
        };

        LogEvent("earn_virtual_currency", data);
    }

    /// <summary>
    /// 获取道具或者金币时候
    /// </summary>
    /// <param name="prop"></param>
    /// <param name="value"></param>
    /// <param name="type"></param>
    /// <param name="newCount"></param>
    public void SpendVirtualCurrentcy(GameProps prop, int value, CostType type, int newCount)
    {
        var levelPath = GetCurrentLevelPathOrDefault();

        var data = new Dictionary<string, object>()
        {
            ["virtual_currency_name"] = prop.ToString(),
            ["item_category"] = type.ToString(),
            ["level_name"] = levelPath,
            ["value"] = value,
            ["balance"] = newCount,
        };

        LogEvent("spend_virtual_currency", data);
    }

    public void Rating_Star(int star, int status)
    {
        var data = new Dictionary<string, object>()
        {
            ["star"] = star.ToString(),
            ["status"] = status.ToString(),
        };
        LogEvent("rating_star", data);
    }


    #region Obsolete
    //--------- 局间插屏显示 Obsolete 不用这个 ---------------

    private void TryShowLevelInterstitialAd(string placement = AdsSceneGameStart)
    {
        ShowInterstitial(placement, (scene, success) =>
        {
            // TODO:插屏展示完毕, 项目组可根据结果处理对用的回调
        }, GameInterstitialValidator);
    }


    /// <summary>
    /// 插屏验证器, 项目组自行加入扩展
    /// </summary>
    /// <returns></returns>
    private UniTask<bool> GameInterstitialValidator()
    {
        // 新手保护
        if (_curLevelId < MinInterstitialEnableLevel)
            return UniTask.FromResult(false);

        // 局间计数不足
        if (_currentIvIntervalLevelCount < InterstitialShownIntervalLevelCount)
        {
            _currentIvIntervalLevelCount++; // 关卡间隔数递增
            return UniTask.FromResult(false);
        }

        // 条件判定成功, 可显示插屏
        _currentIvIntervalLevelCount = 0; // 标记清零
        return UniTask.FromResult(true);
    }

    #endregion
}

using System;
using Bear.EventSystem;
using Bear.Logger;
using Config.Game;
using Game;
using Game.Events;

/// <summary>
/// 插屏广告播放辅助工具。
/// - 根据 InterstitialAdPolicy 判断是否可以展示；
/// - 封装 GameSDKService 的展示逻辑；
/// - 成功播放后派发 PlayInterstitialAdEvent。
/// </summary>
public static class InterstitialAdHelper
{
    public static int TriggerCount = 0;
    private static void Log(string message)
    {
        BearLogger.Log($"[InterstitialAdHelper] {message}", typeof(InterstitialAdHelper));
    }


    /// <summary>
    /// 尝试播放插屏广告（根据当前关卡和策略判断）。
    /// </summary>
    /// <param name="placement">广告位标识。</param>
    /// <param name="onResult">回调：placement, 是否成功展示。</param>
    public static void TryToShowInterstitial(string placement, Action<string, bool> onResult = null)
    {
        var sdk = GameSDKService.Instance;
        var playCtrl = PlayCtrl.Instance;

        if (sdk == null || playCtrl == null)
        {
            Log($"sdk or playCtrl is null. sdkNull:{sdk == null}, playCtrlNull:{playCtrl == null}, placement:{placement}");
            onResult?.Invoke(placement, false);
            return;
        }

        var policy = playCtrl.InterstitialAdPolicy;
        if (policy == null)
        {
            Log($"InterstitialAdPolicy is null, placement:{placement}");
            onResult?.Invoke(placement, false);
            return;
        }

        var currentLevel = playCtrl.Level.CurrentLevel;
        if (!Enum.TryParse(placement, out InterstitialPlacement placementType))
        {
            Log($"Invalid placement string: {placement}");
            onResult?.Invoke(placement, false);
            return;
        }

        if (!policy.CanShowInterstitial(currentLevel, placementType))
        {
            Log($"CanShowInterstitial return false. level:{currentLevel}, placement:{placementType}");
            onResult?.Invoke(placement, false);
            return;
        }

        Log($"Try show interstitial. level:{currentLevel}, placement:{placement}");

        void InternalCallback(string p, bool success)
        {
            Log($"Interstitial result. placement:{p}, success:{success}");

            if (success)
            {
                TriggerCount = 0;
                // 通知策略 & 其他监听者
                playCtrl.DispatchEvent(Witness<PlayInterstitialAdEvent>._);

                if (placementType == InterstitialPlacement.InGameFailed)
                    policy.ResetFailCount();
                else if (placementType == InterstitialPlacement.InGameVictory) 
                    policy.ResetSuccessCount();
            }

            onResult?.Invoke(p, success);
        }

        TriggerCount++;
        sdk.ShowInterstitial(placement, InternalCallback);
    }
}


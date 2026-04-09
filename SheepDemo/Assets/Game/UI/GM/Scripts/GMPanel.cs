
using System;
using System.Collections.Generic;
using System.Text;
using Bear.EventSystem;
using Bear.SaveModule;
using Bear.UI;
using Game;
using Game.ConfigModule;
using Game.Events;
using Game.Play;
using GameCommon;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class GMPanel : BaseUIView, IEventSender
{
    public ScrollRect View1;
    public ScrollRect View2;
    private bool _isView1Visible;

    [SerializeField] private TMP_InputField gmConfig;

    public override void OnCreate()
    {
        base.OnCreate();

        _isView1Visible = View1 != null && View1.gameObject.activeSelf;

        if (SwitchBtn != null)
        {
            SwitchBtn.OnClick += OnSwitchBtnClick;
        }

        RewardAdBtn.OnClick += TestForRewardAd;
        InterstitialBtn.OnClick += TestForInterstitialAd;

        NextLevelBtn.OnClick += NextLevel;
        LastLevelBtn.OnClick += LastLevel;
        EnterLevelLoadingBtn.OnClick += EnterLevelLoadingPanel;
        DeleteBtn.OnClick += DeleteAllData;
        VictoryBtn.OnClick += SendVictoryEvent;
        // Purchase1Btn.OnClick += TryToPurchase5Tips;
        RestoreBtn.OnClick += TryToRestorePurchase;

        RatingPopupBtn.OnClick += ShowRating;
        DebugBtn.OnClick += ShowMaxDebug;
        UnlockBtn.OnClick += UnlockAllLevel;

        SetView1Visible(false);

        InitGmConfigDisplay();
    }

    private void ShowRating(CustomButton btn)
    {
        RatingPopup.Create();
    }

    private void UnlockAllLevel(CustomButton btn)
    {
        var level = PlayCtrl.Instance.Level;
        var datas = level.LevelSorts;

        for (int i = 0; i < datas.Count; i++)
        {
            level.UnlockLevel(datas[i].Id);
        }
    }

    public override void OnShow()
    {
        base.OnShow();
        RefreshGmConfigRemoteDisplay();
    }

    /// <summary>
    /// gmConfig：只读多行展示 DB 中的 RemoteConfig 原始 JSON（按 key 分行 + 缩进美化）。
    /// </summary>
    private void InitGmConfigDisplay()
    {
        if (gmConfig == null)
        {
            return;
        }

        gmConfig.readOnly = true;
        gmConfig.lineType = TMP_InputField.LineType.MultiLineNewline;
    }

    private void RefreshGmConfigRemoteDisplay()
    {
        if (gmConfig == null)
        {
            return;
        }

        var cache = DB.GameData?.RemoteConfigCache;
        if (cache == null || cache.Count == 0)
        {
            gmConfig.text = "(无 RemoteConfig 缓存)\n键值来自远端拉取后写入 DB.GameData.RemoteConfigCache";
            return;
        }

        var keys = new List<string>(cache.Keys);
        keys.Sort(StringComparer.Ordinal);

        var sb = new StringBuilder();
        for (var i = 0; i < keys.Count; i++)
        {
            var key = keys[i];
            if (!cache.TryGetValue(key, out var json) || string.IsNullOrEmpty(json))
            {
                continue;
            }

            if (sb.Length > 0)
            {
                sb.AppendLine();
            }

            sb.Append('[');
            sb.Append(key);
            sb.AppendLine("]");
            sb.AppendLine(FormatRemoteJsonBlock(json));
        }

        gmConfig.text = sb.Length > 0 ? sb.ToString().TrimEnd() : "(RemoteConfig 缓存为空字符串)";
    }

    private static string FormatRemoteJsonBlock(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "(empty)";
        }

        try
        {
            var token = JToken.Parse(raw);
            return token.ToString(Formatting.Indented);
        }
        catch (JsonException)
        {
            return raw;
        }
    }

    private void ShowMaxDebug(CustomButton btn)
    {
        GameSDKService.Instance.ShowMaxDebugger();
    }


    private void TryToRestorePurchase(CustomButton btn)
    {
        GameManager.Instance.Purchase.Restore();
    }
    private void TryToPurchase5Tips(CustomButton btn)
    {
        PlayCtrl.Instance.Bag.AddTool(Config.Game.GameProps.Tips, 5, RewardType.Other);
    }

    private void ShowWaitingPopup(CustomButton btn)
    {
        WaitingPopup.Create();
    }

    private void SendVictoryEvent(CustomButton btn)
    {
        // GameVictoryPanel.Create(PlayCtrl.Instance.Level.CurrentLevelData);
        this.DispatchEvent(Witness<SwitchGameStateEvent>._, GamePlayStateName.SUCCESS);
    }

    private void DeleteAllData(CustomButton btn)
    {
        DBManager.Instance.ClearPlayerPrefsCache();
        DBManager.Instance.ClearJsonCache();

        Application.Quit();
    }

    /// <summary>
    /// SwitchBtn 点击后切换 View1 显示/隐藏
    /// </summary>
    private void OnSwitchBtnClick(CustomButton btn)
    {
        SetView1Visible(!_isView1Visible);

        SwitchTxt.text = "Gm: " + _isView1Visible;
    }

    /// <summary>
    /// 提供 bool 用于切换 View1 是否展示
    /// </summary>
    public void SetView1Visible(bool isShow)
    {
        _isView1Visible = isShow;

        if (View1 != null)
        {
            View1.gameObject.SetActive(isShow);
            View2.gameObject.SetActive(isShow);
        }

        if (gmConfig != null)
        {
            gmConfig.gameObject.SetActive(isShow);
        }
    }

    private void TestForRewardAd(CustomButton btn)
    {
        Debug.Log($"[GMPanel AdTest] AD is Ready {GameSDKService.Instance.IsAdsReady} ");
        Debug.Log($"[GMPanel AdTest] ReawrdAD is Ready {GameSDKService.Instance.IsRewardAdReady} ");
        Debug.Log($"[GMPanel AdTest] SDK is Ready {GameSDKService.Instance.IsInitialized} ");

        RewardAdHelper.TryToShowRewardAd(Config.Game.RewardPlacement.GameTest.ToString(), onResult: (placement, isSuc) =>
        {
            GameSDKService.Instance.LoadRewardAd();
        });
    }

    private void TestForInterstitialAd(CustomButton btn)
    {
        Debug.Log($"[GMPanel AdTest] AD is Ready {GameSDKService.Instance.IsAdsReady} ");
        Debug.Log($"[GMPanel AdTest] Interstitial is Ready {GameSDKService.Instance.IsInterstitialAdReady} ");
        // Debug.Log($"[GMPanel AdTest] SDK is Ready {GameSDKService.Instance.IsInitialized} ");

        GameSDKService.Instance.ShowInterstitial(Config.Game.InterstitialPlacement.GameTest.ToString(), (placement, isSuc) =>
        {
            Debug.Log($"[GMPanel AdTest] placement: {placement}, isSuc: {isSuc}");
            if (isSuc)
            {
                this.DispatchEvent(Witness<PlayInterstitialAdEvent>._);
            }

            // GameSDKService.Instance.LoadInterstitialAd();
        });
    }

    private void NextLevel(CustomButton btn)
    {
        var level = PlayCtrl.Instance.Level;
        var levelIndex = level.CurrentLevel;
        level.SetCurrentLevel(++levelIndex);

        this.DispatchEvent(Witness<Game.Events.EnterLevelEvent>._, level.CurrentLevelSort);
    }

    private void LastLevel(CustomButton btn)
    {
        var level = PlayCtrl.Instance.Level;
        var levelIndex = level.CurrentLevel;
        level.SetCurrentLevel(--levelIndex);

        this.DispatchEvent(Witness<Game.Events.EnterLevelEvent>._, level.CurrentLevelSort);
    }

    private void EnterLevelLoadingPanel(CustomButton btn)
    {
        EnterLevelLoading.Create();
    }

    public static GMPanel Create()
    {
        var panel = UIManager.Instance.OpenUI<GMPanel>($"{typeof(GMPanel).Name}", UILayer.System);
        return panel;
    }


    const float margin = 20f;
    const float width = 730f;
    const float height = 60f;
    private void OnGUI()
    {
        if (DebugModeSwitchAllBtns.IsDebugModeActive)
        {
            if (!DebugModeSwitchAllBtns.isShow)
                return;
        }
        
        ShowInterstitialData();
        ShowTrackData();

        if (!_isView1Visible)
        {
            return;
        }

        var rect = new Rect(margin, margin, width, height);
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 30,
            normal = { textColor = Color.white }
        };
        GUI.Label(rect, $"Current Language: {DB.GameSetting.CurrentLanguageKeyCode}", style);


        // UDID Button
        var udid = SystemInfo.deviceUniqueIdentifier;
        var buttonRect = new Rect(margin, margin + height + 10f, width, height);

        var skin = GUI.skin.button;
        skin.fontSize = 25;
        if (GUI.Button(buttonRect, $"DevID: {udid}", skin))
        {
            GUIUtility.systemCopyBuffer = udid;
            Debug.Log($"[GMPanel] DevID copied: {udid}");
        }

        buttonRect = new Rect(width + margin, margin + height + 10f, width, height);
        string uid = Guru.SDK.Framework.Core.Account.AccountDataStore.Instance.Uid;
        if (GUI.Button(buttonRect, $"User ID: {uid}", skin))
        {
            GUIUtility.systemCopyBuffer = uid;
            Debug.Log($"[GMPanel] UDID copied: {uid}");
        }


    }

    private void ShowInterstitialData()
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 30,
            normal = { textColor = Color.blue }
        };

        var rowGap = 10f;
        var rowHeight = height + rowGap;
        var bottomY = Screen.height - margin - rowHeight * 5f;
        var failCountRect = new Rect(margin, bottomY, width * 2f, height);
        GUI.Label(failCountRect, $"插屏记录失败次数: {DB.GameData.InterstitialFailCount}", style);

        var lastDateRect = new Rect(margin, bottomY + rowHeight, width * 2f, height);
        var interstitialCd = PlayCtrl.Instance != null && PlayCtrl.Instance.InterstitialAdPolicy != null
            ? PlayCtrl.Instance.InterstitialAdPolicy.CurrentInterstitialCD
            : 0;
        GUI.Label(lastDateRect, $"插屏记录CD: {interstitialCd}", style);

        var triggerCountRect = new Rect(margin, bottomY + rowHeight * 2f, width * 2f, height);
        var interval = ConfigManager.RemoteConfig.GetAdInterstitialShowIntervalCount();
        var interstitialIntervalDiff = interval - DB.GameData.InterstitialLevelSuccessCount;
        GUI.Label(triggerCountRect,
            $"插屏间隔差值: {interstitialIntervalDiff}  (Remote 间隔 - 成功关卡数)",
            style);
    }

    private void ShowTrackData()
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 30,
            normal = { textColor = Color.blue }
        };
        var rowGap = 10f;
        var rowHeight = height + rowGap;
        var bottomY = Screen.height - margin - rowHeight * 5f;
        var levelState = PlayCtrl.Instance != null && PlayCtrl.Instance.Level != null
            ? PlayCtrl.Instance.Level.CurrentLevelState
            : null;
        var levelPath = levelState != null ? levelState.LevelPath : string.Empty;
        var currentAttemptSeconds = levelState != null ? levelState.CurrentLevelTimeSeconds : 0f;

        if (string.IsNullOrEmpty(levelPath))
            return;

        DB.GameData.LevelPlayDuration.TryGetValue(levelPath, out var levelDuration);

        var currentAttemptRect = new Rect(margin, bottomY + rowHeight * 3f, width * 2f, height);
        GUI.Label(currentAttemptRect, $"当次时长: {currentAttemptSeconds:F2}", style);

        var levelDurationRect = new Rect(margin, bottomY + rowHeight * 4f, width * 3f, height);
        GUI.Label(levelDurationRect, $"总时长（毫秒）[{levelPath}]: {levelDuration}", style);
    }

    private static string FormatInterstitialDate(long unixSeconds)
    {
        if (unixSeconds <= 0)
        {
            return "N/A (0)";
        }

        try
        {
            var time = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToLocalTime();
            return $"{time:yyyy-MM-dd HH:mm:ss} ({unixSeconds})";
        }
        catch (ArgumentOutOfRangeException)
        {
            return $"Invalid ({unixSeconds})";
        }
    }

}

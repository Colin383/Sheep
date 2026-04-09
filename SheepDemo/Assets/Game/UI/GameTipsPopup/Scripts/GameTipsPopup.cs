using Bear.EventSystem;
using Bear.Logger;
using Bear.UI;
using Config.Game;
using Game.ConfigModule;
using Game.Events;
using Game.Play;
using GameCommon;
using I2.Loc;
using UnityEngine;

public partial class GameTipsPopup : BaseUIView, IEventSender, IDebuger
{
    public enum TipPopupState
    {
        // 正常状态
        Normal = 0,
        // 缺少道具
        Lack,
        // 直接通关
        PassLevel
    }

    [SerializeField] private UISpineCtrl spineCtrl;

    [SerializeField] private TipPopupState state = TipPopupState.Normal;

    public override void OnCreate()
    {
        base.OnCreate();
        CloseBtn.OnClick += Close;
        NoBtn.OnClick += Close;
        YesBtn.OnClick += state switch
        {
            TipPopupState.Normal => TryToUseTips,
            TipPopupState.Lack => ShowRewardAd,
            TipPopupState.PassLevel => TryToPassLevel,
            _ => (CustomButton btn) => { }

        };
    }

    public override void OnOpen()
    {
        base.OnOpen();
        RefreshTips();
        PlayAnim();
    }

    public override void OnShow()
    {
        base.OnShow();
        // var ui = UIManager.Instance;
        this.DispatchEvent(Witness<SwitchGameStateEvent>._, GamePlayStateName.PAUSE);
        this.Log("--------------- tipspopup show ");
    }

    private void PlayAnim()
    {
        string spineName = state switch
        {
            TipPopupState.Normal => "dizziness",
            TipPopupState.Lack => "want",
            TipPopupState.PassLevel => "understand",
            _ => "dizziness",
        };

        string animIn = string.Format("ui_{0}_in", spineName);
        string animIdle = string.Format("ui_{0}_idle", spineName);

        var track = spineCtrl.PlayAnimation(animIn, false);
        track.Complete += (track) =>
        {
            spineCtrl.PlayAnimation(animIdle, true);
        };
    }

    private void RefreshTips()
    {
        this.Log("RefreshTipsPopup ------------ ");
        if (state == TipPopupState.PassLevel)
        {
            var tipsKey = PlayCtrl.Instance.Level.CurrentLevelData.AnswerTips;
            TipTxt.text = LocalizationManager.GetTranslation(tipsKey);

            this.Log("RefreshTipsPopup ------------ 2: " + TipTxt.text);
        }
    }

    private void TryToPassLevel(CustomButton button)
    {
        RewardAdHelper.TryToShowRewardAd(Config.Game.RewardPlacement.InGamePassLevel.ToString(),
        success: LocalizationManager.GetTranslation("U_HurryTips_RewardVideo_succeed"),
        failed: LocalizationManager.GetTranslation("U_HurryTips_RewardVideo_failed"),
        onResult: OnPassLevelRewardAdCallback);
    }


    private void OnPassLevelRewardAdCallback(string placement, bool isSuc)
    {
        if (isSuc)
        {
            // 看广告
            UIManager.Instance.CloseAllUI(UILayer.Popup);
            // 直接通关
            this.DispatchEvent(Witness<SwitchGameStateEvent>._, GamePlayStateName.SUCCESS);
        }
    }

    private void ShowRewardAd(CustomButton button)
    {
        RewardAdHelper.TryToShowRewardAd(Config.Game.RewardPlacement.InGameTips.ToString(), onResult: OnRewardAdCallback);
    }

    private void OnRewardAdCallback(string placement, bool isSuc)
    {
        if (isSuc)
        {
            // 看广告获得
            PlayCtrl.Instance.Bag.AddTool(GameProps.Tips, 1, RewardType.Ad);
            TryToUseTips(null);
        }
    }

    private void TryToUseTips(CustomButton btn)
    {
        // 无限道具
        if (PlayCtrl.Instance.Bag.HasTool(GameProps.UnlimitTips))
        {
            this.DispatchEvent(Witness<UseTipsEvent>._);
            Create();
            return;
        }

        var count = PlayCtrl.Instance.Bag.GetToolCount(GameProps.Tips);
        var cost = ConfigManager.Instance.Tables.TbGlobalConst.DataList[0].TipCostCount;
        if (count < cost)
        {
            CreateLackStatePopup();
            return;
        }

        PlayCtrl.Instance.Bag.RemoveTool(GameProps.Tips, cost, CostType.Hint);
        this.DispatchEvent(Witness<UseTipsEvent>._);
        // UIManager.Instance.CloseUI(this);

        // 进入 passLevel
        Create();
    }

    private void Close(CustomButton btn)
    {
        this.DispatchEvent(Witness<SwitchGameStateEvent>._, GamePlayStateName.PLAYING);
        if (PlayCtrl.Instance.Level.CurrentLevelState.HasTips)
            UIManager.Instance.CloseAllUI(UILayer.Popup);
        else
            UIManager.Instance.CloseUI(this);
    }

    private static GameTipsPopup CreateLackStatePopup()
    {
        string panelName = $"{typeof(GameTipsPopup).Name}_2";
        var panel = UIManager.Instance.OpenUI<GameTipsPopup>(panelName, UILayer.Popup);
        return panel;
    }

    public static GameTipsPopup Create()
    {
        string panelName = $"{typeof(GameTipsPopup).Name}";
        if (PlayCtrl.Instance.Level.CurrentLevelState.HasTips)
        {
            // GameTipsPopup_3
            panelName += "_3";

            // UIManager.Instance.CloseTopUI(UILayer.Popup);
        }

        var panel = UIManager.Instance.OpenUI<GameTipsPopup>(panelName, UILayer.Popup);

        return panel;
    }
}

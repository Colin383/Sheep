using Bear.EventSystem;
using Config;
using Config.Game;
using GameCommon;
using UnityEngine;
using System.Collections.Generic;

namespace Game.Events
{
    #region Playing

    // 暂停游戏

    public class GamePauseEvent : EventBase
    {

    }

    // 重置游戏
    public class GameResetEvent : EventBase<GameResetType>
    {
        public GameResetType Type => Field1;
    }

    // 开始游戏
    public class GameStartPlayEvent : EventBase
    {

    }

    // 用于触发游戏场景中的一些特殊事件
    public class OnTiggerItemEvent : EventBase<int>
    {
        public int EventId => Field1;
    }

    /// <summary>
    /// 游戏失败事件
    /// </summary>
    public class  GameFailedEvent : EventBase<GameFailedType>
    {
        public GameFailedType Type => Field1;
    }

    /// <summary>
    /// 游戏复活事件
    /// </summary>
    public class GameReviveEvent : EventBase {}

    /// <summary>
    /// 进入技能状态
    /// </summary>
    public class EnterSkillEvent : EventBase<SkillType>
    {
        public SkillType SkillType => Field1;
    }

    // 退出技能模式
    public class ExitSkillEvent : EventBase {}

    #endregion

    #region GameState 

    public class SwitchGameStateEvent : EventBase<string>
    {
        public string NewState => Field1;
    }

    /// <summary>
    /// 进入对应关卡
    /// </summary>
    public class EnterLevelEvent : EventBase<LevelSort>
    {
        public LevelSort Data => Field1;
    }

    public class EnterNextLevelEvent : EventBase
    {

    }

    #endregion 

    #region UI

    // gamepanel pause 状态恢复事件
    public class GameResumeEvent : EventBase { }

    public class GamePlayPanelFadeInEvent : EventBase { }

    public class MusicToggleEvent : EventBase<bool>
    {
        public bool isOn => Field1;
    }

    public class SfxToggleEvent : EventBase<bool>
    {
        public bool isOn => Field1;
    }

    public class VibrationToggleEvent : EventBase<bool>
    {
        public bool isOn => Field1;
    }

    public class SwitchObjActiveEvent : EventBase<bool>
    {
        public bool isShow => Field1;
    }

    // 展示遮罩    
    public class GamePlayPanelSwitchBlockEvent : EventBase<bool>
    {
        public bool IsShow => Field1;
    }


    // tips 引导提示
    public class GamePlayPanelSwitchTipsEvent : EventBase<bool>
    {
        public bool IsShow => Field1;
    }

    /// <summary>
    /// 切换语言
    /// </summary>
    public class SwitchLanguageEvent : EventBase<string>
    {

    }

    /// <summary>
    /// 内购恢复成功后
    /// </summary>
    public class OnRestoreSuccessEvent : EventBase { }

    #endregion

    #region Bag

    /// <summary>
    /// 道具数量更新事件
    /// </summary>
    public class UpdatePropEvent : EventBase<GameProps, int, int>
    {
        public GameProps Prop => Field1;

        public int OldCount => Field2;

        public int NewCount => Field3;
    }

    public class UseTipsEvent : EventBase { }

    /// <summary>
    /// 购买道具事件，通过 rewards 控制发放奖励
    /// </summary>
    public class PurchaseEvent : EventBase<string, Dictionary<GameProps, int>>
    {
        public string GoodsId => Field1;
        public Dictionary<GameProps, int> Rewards => Field2;
    }

    #endregion 

    #region AD

    // 播放激励
    public class PlayRewardADEvent : EventBase
    {

    }


    // 播放插屏
    public class PlayInterstitialAdEvent : EventBase
    {

    }

    #endregion 
}
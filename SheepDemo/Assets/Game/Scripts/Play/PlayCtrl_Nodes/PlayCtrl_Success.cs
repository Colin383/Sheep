using Bear.EventSystem;
using Bear.Fsm;
using Bear.Game;
using Bear.Logger;
using Game.Events;
using UnityEngine;

namespace Game.Play
{
    /// <summary>
    /// 游戏成功状态节点
    /// </summary>
    [StateMachineNode(typeof(PlayCtrl), GamePlayStateName.SUCCESS, false)]
    public class PlayCtrl_Success : StateNode, IEventSender, IDebuger
    {
        private PlayCtrl owner;

        private GameVictoryPanel _gameVictoryPanel;

        public override void OnEnter()
        {
            Debug.Log($"{nameof(PlayCtrl_Success)} Enter");

            owner = _owner as PlayCtrl;

            // 记录通关
            owner.Level.CurrentLevelState.TryToAddSuccessCount();
            // 播放插屏广告
            InterstitialAdHelper.TryToShowInterstitial(Config.Game.InterstitialPlacement.InGameVictory.ToString(), OnInterstitialCallback);

            // 关卡成功事件 ========================================================= 
            GameSDKService.Instance.LevelEndEvent(GameSDKService.LevelEndTypeSuccess);

            if (RatingPopupPolicy.TryToShowRating(owner.Level.CurrentLevel))
            {
                RatingPopup.Create(null, ShowVictory);
            }
            else
            {
                ShowVictory();
            }
        }

        private void ShowVictory()
        {
            // 处理等级
            _gameVictoryPanel = GameVictoryPanel.Create(owner.Level.CurrentLevelData);
            owner.Level.Victory();
        }

        private void OnInterstitialCallback(string placement, bool isSuc)
        {

        }

        public override void OnExecute()
        {
            Debug.Log($"{nameof(PlayCtrl_Success)} Execute");
        }

        public override void OnUpdate()
        {
            // Debug.Log($"{nameof(PlayCtrl_Success)} Update");
        }

        public override void OnExit()
        {
            Debug.Log($"{nameof(PlayCtrl_Success)} Exit");
        }
    }
}


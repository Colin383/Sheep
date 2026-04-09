using Bear.EventSystem;
using Bear.Fsm;
using Bear.Logger;
using Game.ConfigModule;
using Game.Events;
using Game.Scripts.Common;
using UnityEngine;

namespace Game.Play
{
    /// <summary>
    /// 游戏进行中状态节点
    /// </summary>
    [StateMachineNode(typeof(PlayCtrl), GamePlayStateName.PLAYING, false)]
    public class PlayCtrl_Playing : StateNode, IDebuger, IEventSender
    {
        private PlayCtrl owner;

        private LevelRuntimeState state;
        private float showTipsTimeLimit;
        private int showTipsFailCount;

        public override void OnEnter()
        {
            Debug.Log($"{nameof(PlayCtrl_Playing)} Enter");

            owner = _owner as PlayCtrl;
            state = owner.Level.CurrentLevelState;
            showTipsTimeLimit = ConfigManager.RemoteConfig.GetShowTipsTimeLimit();
            showTipsFailCount = ConfigManager.RemoteConfig.GetShowTipsFailCount();

            this.Log("showTipsTimeLimit: " + showTipsTimeLimit);
            this.Log("showTipsFailCount: " + showTipsFailCount);

            if (owner.SceneRoot == null)
                owner.SceneRoot = GameObject.Find("Scene").transform;

            // this.Log("AudioManager.IsCurrentMusicTag: " + AudioManager.IsCurrentMusicTag("musicInGame"));
            
            if (!AudioManager.IsCurrentMusicTag("musicInGame"))
            {
                AudioManager.PlayMusic("musicInGame");
            }

        }

        public override void OnExecute()
        {
            Debug.Log($"{nameof(PlayCtrl_Playing)} Execute");
        }

        public override void OnUpdate()
        {
            // this.Log($"{nameof(PlayCtrl_Playing)} Update" + state.CurrentAttemptTimeSeconds);

            if (state == null)
                return;

            state.Tick(Time.deltaTime);

            // this.Log($"{nameof(PlayCtrl_Playing)} state.HasTips" +state.HasClickTips);

            if (state.HasTips || state.HasClickTips)
                return;

            // this.Log($"{nameof(PlayCtrl_Playing)} Update" + state.CurrentAttemptTimeSeconds);

            bool playTimeFetch = state.CurrentAttemptTimeSeconds > showTipsTimeLimit;
            bool failCountFetch = state.FailCount + state.ResetCount >= showTipsFailCount;
            if (failCountFetch || playTimeFetch)
            {
                state.SwitchClickTips(true);
                this.DispatchEvent(Witness<GamePlayPanelSwitchTipsEvent>._, true);

                this.Log("Show Tips waring");
            }
        }

        public override void OnExit()
        {
            this.Log($"{nameof(PlayCtrl_Playing)} Exit");
        }

    }
}


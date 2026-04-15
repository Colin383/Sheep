using Bear.EventSystem;
using Bear.Fsm;
using Bear.Logger;
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
        public override void OnEnter()
        {
            if (!AudioManager.IsMusicPlaying || !AudioManager.IsCurrentMusicTag("musicInGame"))
                AudioManager.PlayMusic("musicInGame");

            this.DispatchEvent(Witness<EnterPlayingEvent>._);
        }

        public override void OnExecute()
        {
            Debug.Log($"{nameof(PlayCtrl_Playing)} Execute");
            this.DispatchEvent(Witness<EnterPlayingEvent>._);
        }

        public override void OnUpdate()
        {

        }

        public override void OnExit()
        {
            this.Log($"{nameof(PlayCtrl_Playing)} Exit");
            this.DispatchEvent(Witness<ExitPlayingEvent>._);
        }

    }
}


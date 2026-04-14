using Bear.EventSystem;
using Bear.Fsm;
using Game.Events;
using UnityEngine;

namespace Game.Play
{
    /// <summary>
    /// 技能状态节点
    /// </summary>
    [StateMachineNode(typeof(PlayCtrl), GamePlayStateName.SKILL, false)]
    public class PlayCtrl_Skill : StateNode, IEventSender
    {
        public override void OnEnter()
        {
            Debug.Log($"{nameof(PlayCtrl_Skill)} Enter");
        }

        public override void OnExecute()
        {
            Debug.Log($"{nameof(PlayCtrl_Skill)} Execute");
        }

        public override void OnUpdate()
        {
        }

        public override void OnExit()
        {
            this.DispatchEvent(Witness<ExitSkillEvent>._);
            Debug.Log($"{nameof(PlayCtrl_Skill)} Exit");
        }
    }
}

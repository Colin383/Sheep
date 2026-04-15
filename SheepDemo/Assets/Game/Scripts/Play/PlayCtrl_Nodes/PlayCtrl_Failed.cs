using Bear.Fsm;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Play
{
    /// <summary>
    /// 游戏失败状态节点
    /// </summary>
    [StateMachineNode(typeof(PlayCtrl), GamePlayStateName.FAILED, false)]
    public class PlayCtrl_Failed : StateNode
    {
        public override void OnEnter()
        {
            Debug.Log($"{nameof(PlayCtrl_Failed)} Enter");

            ShowRevivePopup().Forget();

        }

        private async UniTask ShowRevivePopup()
        {
            await UniTask.WaitForSeconds(1f);
            RevivePopup.Create();

        }

        public override void OnExecute()
        {
            Debug.Log($"{nameof(PlayCtrl_Failed)} Execute");
        }

        public override void OnUpdate()
        {
            // Debug.Log($"{nameof(PlayCtrl_Failed)} Update");
        }

        public override void OnExit()
        {
            Debug.Log($"{nameof(PlayCtrl_Failed)} Exit");
        }
    }
}


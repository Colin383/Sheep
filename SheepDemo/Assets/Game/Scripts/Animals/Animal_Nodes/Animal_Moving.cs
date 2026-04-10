using UnityEngine;
using Bear.Fsm;
using Bear.Logger;

/// <summary>
/// Animal moving state node.
/// </summary>
[StateMachineNode(typeof(BaseAnimal), AnimalStateName.MOVING, false)]
public class Animal_Moving : StateNode, IDebuger
{
    private BaseAnimal owner;
    private bool canMove = false;

    private Vector3 nextTarget;

    private float minDistance = 0.1f;

    public override void OnEnter()
    {
        owner = _owner as BaseAnimal;
        canMove = owner.Level.CheckMoveTarget(owner, out nextTarget);

        this.Log($"Enter {owner.Id}");
    }

    public override void OnUpdate()
    {
        if (!canMove)
        {
            // 播放晕头效果
            owner.EnterIdleState();
        }
        else
        {
            Moving();
        }
    }

    private void Moving()
    {
        // 控制 owner.transform 朝向 nextTarget 移动。

        if (IsMovComplete())
        {
            OnMoveComplete();
        }
    }

    private bool IsMovComplete()
    {
        return Vector3.Distance(owner.transform.position, nextTarget) < minDistance;
    }

    private void OnMoveComplete()
    {
        // 到达位置后，检查是否移动到边界，可以会到农场
        if (owner.Level.IsAnimCanBack(nextTarget))
        {
            owner.Level.BackToFarm(owner);
            return;
        }

        // 检查下一次移动
        canMove = owner.Level.CheckMoveTarget(owner, out nextTarget);
    }

    public override void OnExit()
    {
        this.Log($"Exit {owner.Id}");
    }
}

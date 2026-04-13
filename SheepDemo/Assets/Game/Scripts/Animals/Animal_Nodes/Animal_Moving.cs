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
    private bool canMove;
    private bool isRotating;
    private DirectionEnum currentStepDirection = DirectionEnum.Down;

    private Vector3 nextTarget;
    private Quaternion desiredRotation;

    // 从 owner 获取移动速度
    private float MoveSpeed => owner?.MoveSpeed ?? 3f;
    private float RotateSpeed => owner?.RotateSpeed ?? 540f;
    // 到点阈值
    private float minDistance = 0.05f;
    // 旋转完成阈值（度）
    private float rotateEpsilon = 1f;

    public override void OnEnter()
    {
        owner = _owner as BaseAnimal;
        canMove = false;
        if (owner != null && owner.Level != null)
        {
            var directions = owner.GetMovableDirections();
            if (directions != null && directions.Count > 0)
            {
                var prevPos = owner.CurrentPos;
                foreach (var dir in directions)
                {
                    if (owner.Level.CheckMoveTarget(owner, dir, out nextTarget))
                    {
                        if (!owner.CanMoveTo(owner.CurrentPos))
                        {
                            owner.RollbackCurrentPos(prevPos);
                            continue;
                        }
                        owner.OnMoveStepConfirmed(owner.CurrentPos);
                        currentStepDirection = dir;
                        canMove = true;
                        break;
                    }
                }
            }
        }

        if (canMove)
            PrepareCurrentStep();

        this.Log($"Enter {(owner != null ? owner.Id : -1)}");
    }

    public override void OnUpdate()
    {
        if (!canMove)
        {
            // 当前不可移动，回到 Idle。
            owner?.EnterIdleState();
            return;
        }

        Moving();
    }

    private void Moving()
    {
        // 需要转向时先原地旋转，旋转完成后再移动。
        if (isRotating)
        {
            RotateInPlace();
            return;
        }

        MoveForward();
        if (IsMovComplete())
        {
            OnMoveComplete();
        }
    }

    private bool IsMovComplete()
    {
        return Vector3.Distance(owner.transform.position, nextTarget) <= minDistance;
    }

    private void OnMoveComplete()
    {
        // 到点后吸附，避免浮点误差累积。
        owner.transform.position = nextTarget;
        // 注意：CurrentPos 已在 CheckMoveTarget 返回 true 时更新，此处不再重复更新。

        // 到达边界或出界则回收。
        if (owner.Level.IsAnimCanBack(nextTarget))
        {
            owner.Level.BackToFarm(owner);
            return;
        }

        // 继续计算下一步目标；若可移动则继续"先转后移"。
        canMove = false;
        var directions = owner.GetMovableDirections();
        if (directions != null && directions.Count > 0)
        {
            var prevPos = owner.CurrentPos;
            foreach (var dir in directions)
            {
                if (owner.Level.CheckMoveTarget(owner, dir, out nextTarget))
                {
                    if (!owner.CanMoveTo(owner.CurrentPos))
                    {
                        owner.RollbackCurrentPos(prevPos);
                        continue;
                    }
                    owner.OnMoveStepConfirmed(owner.CurrentPos);
                    currentStepDirection = dir;
                    canMove = true;
                    break;
                }
            }
        }
        if (canMove)
        {
            PrepareCurrentStep();
        }
    }

    private void PrepareCurrentStep()
    {
        // 用目标点方向作为本步目标朝向。
        var forward = nextTarget - owner.transform.position;
        forward.y = 0f;

        if (forward.sqrMagnitude <= 1e-6f)
        {
            isRotating = false;
            return;
        }

        desiredRotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        float angle = Quaternion.Angle(owner.transform.rotation, desiredRotation);

        if (angle <= rotateEpsilon)
        {
            owner.transform.rotation = desiredRotation;
            isRotating = false;
            return;
        }

        isRotating = true;
    }

    private void RotateInPlace()
    {
        owner.transform.rotation = Quaternion.RotateTowards(
            owner.transform.rotation,
            desiredRotation,
            RotateSpeed * Time.deltaTime);

        if (Quaternion.Angle(owner.transform.rotation, desiredRotation) <= rotateEpsilon)
        {
            owner.transform.rotation = desiredRotation;
            isRotating = false;
        }
    }

    private void MoveForward()
    {
        owner.transform.position = Vector3.MoveTowards(
            owner.transform.position,
            nextTarget,
            MoveSpeed * Time.deltaTime);
    }

    public override void OnExit()
    {
        this.Log($"Exit {(owner != null ? owner.Id : -1)}");
    }
}

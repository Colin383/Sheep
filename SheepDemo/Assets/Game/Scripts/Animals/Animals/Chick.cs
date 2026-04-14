using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// "sheep" implementation placeholder.
/// Add sheep-specific behavior here later.
/// </summary>
public class Chick : BaseAnimal
{
    private readonly HashSet<Vector2Int> _visitedGrids = new();

    public override AnimalType Type => AnimalType.Chick;

    public override void Init(int id, int row, int col, string direction, string param = null)
    {
        base.Init(id, row, col, direction, param);
        _visitedGrids.Clear();
    }

    /// <summary>
    /// Chick 被点击不触发移动，由 LevelCtrl 统一触发。
    /// </summary>
    public override void OnClickTrigger()
    {
        // base.OnClickTrigger();
    }

    public override void EnterIdleState()
    {
        _visitedGrids.Clear();
        base.EnterIdleState();
    }

    public override void TryMoving()
    {
        _visitedGrids.Clear();
        _visitedGrids.Add(CurrentPos);
        base.TryMoving();
    }

    public override void OnMoveStepConfirmed(Vector2Int gridPos)
    {
        _visitedGrids.Add(gridPos);
    }

    public override bool CanMoveTo(Vector2Int gridPos)
    {
        return !_visitedGrids.Contains(gridPos);
    }

    /// <summary>
    /// Chick 可以往前、左、右、后四个方向移动。
    /// 优先沿当前朝向（transform 旋转）前进，若被阻挡再尝试变向。
    /// </summary>
    public override IReadOnlyList<DirectionEnum> GetMovableDirections()
    {
        var facing = GetVisualFacingDirection();
        return new[]
        {
            facing,
            facing.TurnLeft(),
            facing.TurnRight(),
            facing.TurnLeft().TurnLeft()
        };
    }

    /// <summary>
    /// 根据当前 transform 的朝向获取实际的 facing 方向。
    /// </summary>
    private DirectionEnum GetVisualFacingDirection()
    {
        var forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 1e-6f)
            return FacingDirection;

        float absX = Mathf.Abs(forward.x);
        float absZ = Mathf.Abs(forward.z);

        if (absX > absZ)
            return forward.x > 0f ? DirectionEnum.Right : DirectionEnum.Left;
        else
            return forward.z > 0f ? DirectionEnum.Up : DirectionEnum.Down;
    }
}

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
    /// </summary>
    public override IReadOnlyList<DirectionEnum> GetMovableDirections()
    {
        var facing = FacingDirection;
        return new[]
        {
            facing,
            facing.TurnLeft(),
            facing.TurnRight(),
            facing.TurnLeft().TurnLeft()
        };
    }
}

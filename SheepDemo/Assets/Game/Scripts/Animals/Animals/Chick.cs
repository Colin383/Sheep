using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Chick：采用 BFS 缓存路径寻路，优先寻找最近出口。
/// </summary>
public class Chick : BaseAnimal
{
    private readonly HashSet<Vector2Int> _visitedGrids = new();
    private List<DirectionEnum> _cachedPath = new();
    private int _pathIndex;

    public override AnimalType Type => AnimalType.Chick;

    public override void Init(int id, int row, int col, string direction, string param = null)
    {
        base.Init(id, row, col, direction, param);
        _visitedGrids.Clear();
        _cachedPath.Clear();
        _pathIndex = 0;
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
        _cachedPath.Clear();
        _pathIndex = 0;
        base.EnterIdleState();
    }

    public override void TryMoving()
    {
        _visitedGrids.Clear();
        _visitedGrids.Add(CurrentPos);
        _cachedPath.Clear();
        _pathIndex = 0;
        base.TryMoving();
    }

    public override void OnMoveStepConfirmed(Vector2Int gridPos)
    {
        _visitedGrids.Add(gridPos);
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        _cachedPath.Clear();
        _pathIndex = 0;
        _visitedGrids.Clear();
    }

    public override void OnRecycle()
    {
        base.OnRecycle();
        _cachedPath.Clear();
        _pathIndex = 0;
        _visitedGrids.Clear();
    }

    public override bool CanMoveTo(Vector2Int gridPos)
    {
        return !_visitedGrids.Contains(gridPos);
    }

    /// <summary>
    /// 尝试获取缓存路径的下一个移动目标。
    /// 如果没有缓存路径或路径已走完，会尝试 BFS 计算新路径。
    /// </summary>
    public override bool TryGetCachedNextMove(out Vector3 nextTarget)
    {
        nextTarget = Vector3.zero;

        if (Level == null)
            return false;

        if (_pathIndex >= _cachedPath.Count || _cachedPath.Count == 0)
            RecalculatePath();

        if (_pathIndex >= _cachedPath.Count)
            return false;

        var dir = _cachedPath[_pathIndex];
        var prevPos = CurrentPos;

        if (!Level.CheckMoveTarget(this, dir, out nextTarget))
        {
            RecalculatePath();
            if (_pathIndex >= _cachedPath.Count)
                return false;

            dir = _cachedPath[_pathIndex];
            prevPos = CurrentPos;
            if (!Level.CheckMoveTarget(this, dir, out nextTarget))
                return false;
        }

        if (!CanMoveTo(CurrentPos))
        {
            RollbackCurrentPos(prevPos);
            return false;
        }

        _pathIndex++;
        return true;
    }

    /// <summary>
    /// 检查当前位置四个方向是否存在阻挡。
    /// 若存在至少一个方向被阻挡，则重新计算路径。
    /// </summary>
    public void CheckAndRecalculateIfBlocked()
    {
        if (Level == null)
            return;

        var directions = new[] { DirectionEnum.Up, DirectionEnum.Down, DirectionEnum.Left, DirectionEnum.Right };
        bool anyBlocked = false;

        foreach (var dir in directions)
        {
            if (!Level.CanMoveTo(this, dir))
            {
                anyBlocked = true;
                break;
            }
        }

        if (anyBlocked)
            RecalculatePath();
    }

    private void RecalculatePath()
    {
        _cachedPath = Level?.FindExitPath(this) ?? new List<DirectionEnum>();
        _pathIndex = 0;
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

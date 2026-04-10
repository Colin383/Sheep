using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关卡内动物运行时玩法辅助逻辑。
/// </summary>
public partial class LevelCtrl
{
    [Header("Gameplay")]
    [SerializeField] private Transform endPoint;

    // 世界坐标转格子时的吸附容差（相对格子尺寸）。
    private const float GridMatchEpsilon = 0.4f;

    /// <summary>
    /// 计算下一步目标点，并校验是否与其他动物占位冲突。
    /// 返回 true 表示允许移动。
    /// </summary>
    public bool CheckMoveTarget(BaseAnimal animal, out Vector3 nextTarget)
    {
        nextTarget = Vector3.zero;

        if (animal == null)
            return false;

        if (!TryGetConfigDimensions(out var gridW, out var gridH))
            return false;

        if (!TryGetAnimalAnchor(animal, gridW, gridH, out var anchorRow, out var anchorCol))
            return false;

        // 按当前朝向/移动方向，计算一步（1格）位移。
        var movingDirection = animal.GetMovableDirections();
        var step = DirectionToGridStep(movingDirection);
        if (step == Vector2Int.zero)
            return false;

        // 一步之后的锚点格子。
        int nextAnchorRow = anchorRow + step.y;
        int nextAnchorCol = anchorCol + step.x;
        nextTarget = GridToWorld(nextAnchorRow, nextAnchorCol, gridW, gridH);

        // 计算下一帧 footprint 占用格子；碰撞只统计棋盘内格子。
        var nextFootprintCells = new List<Vector2Int>(8);
        bool hasInsideGridCell = false;
        foreach (var offset in GetWorldFootprintOffsets(animal, movingDirection))
        {
            int row = nextAnchorRow + offset.y;
            int col = nextAnchorCol + offset.x;
            if (!IsInsideGrid(row, col, gridW, gridH))
                continue;

            hasInsideGridCell = true;
            nextFootprintCells.Add(new Vector2Int(col, row));
        }

        // 全部移出棋盘，视为“出界离场”移动，直接允许。
        if (!hasInsideGridCell)
            return true;

        // 收集其他存活动物占位；若重叠则禁止移动。
        var occupied = BuildOccupiedCellSet(animal, gridW, gridH);
        for (int i = 0; i < nextFootprintCells.Count; i++)
        {
            if (occupied.Contains(nextFootprintCells[i]))
                return false;
        }

        // 确认可以移动，立即更新动物的当前网格坐标。
        animal.SetCurrentGridPos(nextAnchorRow, nextAnchorCol);

        return true;
    }

    /// <summary>
    /// 判断目标位置是否满足“可回收（回农场）”条件。
    /// </summary>
    public bool IsAnimCanBack(Vector3 targetPos)
    {
        if (!TryGetConfigDimensions(out var gridW, out var gridH))
            return false;

        // 目标点不在棋盘内，表示已出界，可立即回收。
        if (!TryWorldToGrid(targetPos, gridW, gridH, out var row, out var col))
            return true;

        // 当前规则：到达边界格也视为可回收。
        return !IsInsideGrid(row, col, gridW, gridH) || IsBorderCell(row, col, gridW, gridH);
    }

    /// <summary>
    /// 返回农场。将 animal 切到 Back 状态并从缓存移除，但不立即销毁，由 Back 状态控制销毁时机。
    /// </summary>
    /// <param name="animal"></param>
    public void BackToFarm(BaseAnimal animal)
    {
        if (animal == null)
            return;

        // 先从运行时缓存移除，避免查询脏数据。
        spawned.Remove(animal);

        if (spawnedById.TryGetValue(animal.Id, out var cached) && cached == animal)
            spawnedById.Remove(animal.Id);

        if (spawnedByType.TryGetValue(animal.Type, out var list))
        {
            list.Remove(animal);
            if (list.Count == 0)
                // 该类型已无实例，移除空列表键。
                spawnedByType.Remove(animal.Type);
        }

        // 可选：回收前先挪到终点（视觉过渡点）。
        // if (endPoint != null)
        //     animal.transform.position = endPoint.position;

        // 切到 Back 状态，由状态机控制销毁时机（如动画播放完成后）。
        animal.EnterBackState();
    }

    private HashSet<Vector2Int> BuildOccupiedCellSet(BaseAnimal self, int gridW, int gridH)
    {
        // 占位键使用 (col,row)，与 GridToWorld 的坐标语义保持一致。
        var cells = new HashSet<Vector2Int>();
        for (int i = 0; i < spawned.Count; i++)
        {
            var other = spawned[i];
            if (other == null || other == self)
                continue;

            if (!TryGetAnimalAnchor(other, gridW, gridH, out var row, out var col))
                continue;

            // 占位方向取运行时当前朝向（含 footprint 旋转结果）。
            var direction = other.GetMovableDirections();
            foreach (var offset in GetWorldFootprintOffsets(other, direction))
            {
                int r = row + offset.y;
                int c = col + offset.x;
                if (!IsInsideGrid(r, c, gridW, gridH))
                    continue;

                cells.Add(new Vector2Int(c, r));
            }
        }

        return cells;
    }

    private bool TryGetAnimalAnchor(BaseAnimal animal, int gridW, int gridH, out int row, out int col)
    {
        // 使用动物自己维护的网格坐标，不再依赖 transform 实时反算。
        row = animal.CurrentPos.y;
        col = animal.CurrentPos.x;
        return IsInsideGrid(row, col, gridW, gridH);
    }

    private bool TryWorldToGrid(Vector3 worldPos, int width, int height, out int row, out int col)
    {
        row = 0;
        col = 0;

        // 防御：格子尺寸异常时直接失败。
        if (cellSize.x <= 1e-5f || cellSize.y <= 1e-5f)
            return false;

        var rootPos = instancesRoot != null ? instancesRoot.position : transform.position;
        float halfW = width * 0.5f * cellSize.x;
        float halfH = height * 0.5f * cellSize.y;

        float leftX = rootPos.x - halfW;
        float bottomZ = rootPos.z - halfH;

        // GridToWorld 的逆运算：世界坐标 -> 浮点格子索引。
        float colF = ((worldPos.x - origin.x) - leftX) / cellSize.x - 0.5f;
        float rowF = ((worldPos.z - origin.y) - bottomZ) / cellSize.y - 0.5f;

        col = Mathf.RoundToInt(colF);
        row = Mathf.RoundToInt(rowF);

        if (!IsInsideGrid(row, col, width, height))
            return false;

        // 与格心偏差过大，认为不在有效格子上。
        var snapped = GridToWorld(row, col, width, height);
        float dx = Mathf.Abs(snapped.x - worldPos.x);
        float dz = Mathf.Abs(snapped.z - worldPos.z);
        return dx <= cellSize.x * GridMatchEpsilon && dz <= cellSize.y * GridMatchEpsilon;
    }

    private static Vector2Int DirectionToGridStep(DirectionEnum direction)
    {
        // 将朝向映射为 row/col 的一步增量。
        return direction switch
        {
            DirectionEnum.Up => new Vector2Int(0, 1),
            DirectionEnum.Down => new Vector2Int(0, -1),
            DirectionEnum.Left => new Vector2Int(-1, 0),
            DirectionEnum.Right => new Vector2Int(1, 0),
            _ => Vector2Int.zero,
        };
    }

    private static bool IsInsideGrid(int row, int col, int gridW, int gridH)
    {
        // 合法索引区间：[0, width/height)。
        return row >= 0 && row < gridH && col >= 0 && col < gridW;
    }

    private static bool IsBorderCell(int row, int col, int gridW, int gridH)
    {
        // 当前玩法规则：碰到任意边界格即可回农场。
        return row == 0 || col == 0 || row == gridH - 1 || col == gridW - 1;
    }
}

using System.Collections.Generic;
using Bear.EventSystem;
using Bear.Fsm;
using Game.Events;
using Game.Play;
using GF;
using UnityEngine;

/// <summary>
/// 关卡内动物运行时玩法辅助逻辑。
/// </summary>
public partial class LevelCtrl : IEventSender
{
    [Header("Gameplay")]
    [SerializeField] private PathManager pathManager;

    // 世界坐标转格子时的吸附容差（相对格子尺寸）。
    private const float GridMatchEpsilon = 0.4f;

    // 正在返回农场的动物缓存（Back 状态）
    private readonly List<BaseAnimal> returningAnimals = new();

    /// <summary>
    /// 计算下一步目标点，并校验是否与其他动物占位冲突。
    /// 返回 true 表示允许移动。
    /// </summary>
    public bool CheckMoveTarget(BaseAnimal animal, out Vector3 nextTarget)
    {
        nextTarget = Vector3.zero;

        if (animal == null)
            return false;

        var directions = animal.GetMovableDirections();
        if (directions == null || directions.Count == 0)
            return false;

        foreach (var direction in directions)
        {
            if (CheckMoveTarget(animal, direction, out nextTarget))
                return true;
        }

        return false;
    }

    public bool CheckMoveTarget(BaseAnimal animal, DirectionEnum movingDirection, out Vector3 nextTarget)
    {
        nextTarget = Vector3.zero;

        if (animal == null)
            return false;

        if (!TryGetConfigDimensions(out var gridW, out var gridH))
            return false;

        if (!TryGetAnimalAnchor(animal, gridW, gridH, out var anchorRow, out var anchorCol))
            return false;

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

        // 全部移出棋盘，视为"出界离场"移动，直接允许。
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
    /// BFS 寻找从 animal 当前位置到最近出口（边界或可直接出界）的路径。
    /// 返回方向列表，找不到时返回 null。
    /// </summary>
    public List<DirectionEnum> FindExitPath(BaseAnimal animal)
    {
        if (animal == null)
            return null;

        if (!TryGetConfigDimensions(out var gridW, out var gridH))
            return null;

        var start = new Vector2Int(animal.CurrentPos.x, animal.CurrentPos.y);
        var occupied = BuildOccupiedCellSet(animal, gridW, gridH);

        var queue = new Queue<(Vector2Int pos, List<DirectionEnum> path)>();
        var visited = new HashSet<Vector2Int>();
        queue.Enqueue((start, new List<DirectionEnum>()));
        visited.Add(start);

        while (queue.Count > 0)
        {
            var (pos, path) = queue.Dequeue();

            // 当前位置已在边界，可直接离开
            if (IsBorderCell(pos.y, pos.x, gridW, gridH))
                return path;

            foreach (var dir in new[] { DirectionEnum.Up, DirectionEnum.Down, DirectionEnum.Left, DirectionEnum.Right })
            {
                var step = DirectionToGridStep(dir);
                var nextPos = pos + step;

                if (visited.Contains(nextPos))
                    continue;

                if (!CanAnimalAnchorMoveTo(animal, nextPos, dir, gridW, gridH, occupied, out bool allOutside))
                    continue;

                if (allOutside)
                {
                    var exitPath = new List<DirectionEnum>(path) { dir };
                    return exitPath;
                }

                visited.Add(nextPos);
                queue.Enqueue((nextPos, new List<DirectionEnum>(path) { dir }));
            }
        }

        return null;
    }

    /// <summary>
    /// 不修改 animal 状态的移动可行性检测。
    /// </summary>
    public bool CanMoveTo(BaseAnimal animal, DirectionEnum movingDirection)
    {
        if (animal == null)
            return false;

        if (!TryGetConfigDimensions(out var gridW, out var gridH))
            return false;

        if (!TryGetAnimalAnchor(animal, gridW, gridH, out var anchorRow, out var anchorCol))
            return false;

        var step = DirectionToGridStep(movingDirection);
        if (step == Vector2Int.zero)
            return false;

        var nextAnchor = new Vector2Int(anchorCol + step.x, anchorRow + step.y);
        var occupied = BuildOccupiedCellSet(animal, gridW, gridH);

        return CanAnimalAnchorMoveTo(animal, nextAnchor, movingDirection, gridW, gridH, occupied, out _);
    }

    private bool CanAnimalAnchorMoveTo(BaseAnimal animal, Vector2Int nextAnchor, DirectionEnum dir, int gridW, int gridH, HashSet<Vector2Int> occupied, out bool allOutside)
    {
        allOutside = true;
        bool hasInsideGridCell = false;

        foreach (var offset in GetWorldFootprintOffsets(animal, dir))
        {
            int r = nextAnchor.y + offset.y;
            int c = nextAnchor.x + offset.x;

            if (!IsInsideGrid(r, c, gridW, gridH))
                continue;

            allOutside = false;
            hasInsideGridCell = true;

            if (occupied.Contains(new Vector2Int(c, r)))
                return false;
        }

        if (allOutside)
            return true;

        return hasInsideGridCell;
    }

    /// <summary>
    /// 判断目标位置是否满足"可回收（回农场）"条件。
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

    public void UnregisterAnimalFromPathManager(BaseAnimal animal)
    {
        if (pathManager != null && animal != null)
            pathManager.UnregisterMoveHandle(animal.transform);
    }

    /// <summary>
    /// 返回农场。将 animal 切到 Back 状态并从缓存移除，交给 PathManager 控制移动到 EndPoint。
    /// </summary>
    public void BackToFarm(BaseAnimal animal)
    {
        if (animal == null)
            return;

        // 先从运行时缓存移除，避免查询脏数据
        spawned.Remove(animal);

        if (spawnedById.TryGetValue(animal.Id, out var cached) && cached == animal)
            spawnedById.Remove(animal.Id);

        if (spawnedByType.TryGetValue(animal.Type, out var list))
        {
            list.Remove(animal);
            if (list.Count == 0)
                spawnedByType.Remove(animal.Type);
        }

        if (animal is Chick chick)
            chicks.Remove(chick);

        if (animal is CdSheepAnimal cdSheep)
            cdSheeps.Remove(cdSheep);

        // 切到 Back 状态，并加入返回缓存
        animal.EnterBackState();
        returningAnimals.Add(animal);

        // 如果有 PathManager，将 animal 交给 PathManager 控制
        if (pathManager != null)
        {
            SetupBackPath(animal);
        }
        else
        {
            Debug.LogWarning("[LevelCtrl] PathManager 未设置，animal 将直接销毁");
            returningAnimals.Remove(animal);
            Destroy(animal.gameObject);
        }
    }

    /// <summary>
    /// 设置 animal 返回农场的路径（使用 A* 算法）
    /// </summary>
    private void SetupBackPath(BaseAnimal animal)
    {
        if (pathManager == null|| animal == null)
        {
            Debug.LogError($"[LevelCtrl] SetupBackPath 失败: pathManager={pathManager}, animal={animal}");
            return;
        }

        // 使用 A* 算法从 animal 当前位置到 PathManager.EndPoint 寻路
        // 起点：遍历 pathCells 找到离 animal 最近的一个
        // 终点：PathManager 预设的 endPointCell
        List<Vector2Int> path = pathManager.FindPathFromWorldPosition(animal.transform.position);

        if (path.Count == 0)
        {
            Debug.LogError($"[LevelCtrl] A* 无法找到从 {animal.transform.position} 到 EndPoint 的路径，直接销毁 animal");
            Destroy(animal.gameObject);
            return;
        }

        // 注册 animal 到 PathManager
        pathManager.RegisterMoveHandle(animal, animal.transform, path, () =>
        {
            OnAnimalReachEndPoint(animal);
        });

        // 开始移动
        pathManager.StartMove(animal.transform);
    }

    /// <summary>
    /// 直接销毁指定 animal（用于技能等即时销毁场景）。
    /// </summary>
    public void DestroyAnimal(BaseAnimal animal)
    {
        if (animal == null)
            return;

        spawned.Remove(animal);
        spawnedById.Remove(animal.Id);

        if (spawnedByType.TryGetValue(animal.Type, out var list))
        {
            list.Remove(animal);
            if (list.Count == 0)
                spawnedByType.Remove(animal.Type);
        }

        if (animal is Chick chick)
            chicks.Remove(chick);

        if (animal is CdSheepAnimal cdSheep)
            cdSheeps.Remove(cdSheep);

        RecycleAnimal(animal);

        CheckFinished();
    }

    /// <summary>
    /// Animal 到达终点后的回调
    /// </summary>
    private void OnAnimalReachEndPoint(BaseAnimal animal)
    {
        if (animal == null)
            return;

        // 从 PathManager 注销
        if (pathManager != null)
        {
            pathManager.UnregisterMoveHandle(animal.transform);
        }

        // 从返回缓存中移除
        returningAnimals.Remove(animal);

        RecycleAnimal(animal);

        // 检查是否所有动物都已处理完毕
        CheckFinished();
    }

    /// <summary>
    /// 检查是否所有 animals 都已处理完毕（spawned 和 returningAnimals 都为 0 表示游戏结束）
    /// </summary>
    /// <returns>是否游戏结束</returns>
    private bool CheckFinished()
    {
        int remainingCount = spawned.Count + returningAnimals.Count;
        if (remainingCount == 0)
        {
            Debug.Log("[LevelCtrl] 所有动物已处理完毕，游戏结束！");
            // TODO: 触发游戏结束逻辑（如显示胜利界面、上报关卡完成等）
            this.DispatchEvent(Witness<SwitchGameStateEvent>._, GamePlayStateName.SUCCESS);
            return true;
        }
        return false;
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
            var directions = other.GetMovableDirections();
            var direction = directions != null && directions.Count > 0 ? directions[0] : other.FacingDirection;
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

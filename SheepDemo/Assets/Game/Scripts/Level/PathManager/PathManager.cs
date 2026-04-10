using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 网格路径管理器：创建指定大小的 grids，cellSize 可以定义，用 Gizmos 显示 Grids。
/// 可以选择指定的 grids 算作 Paths 路径。未被选中的 grids 不在选择范围之内。
/// 指定 grid gridPosiiton 作为 endPoint
/// 驱动 IMovePathHandle，IMovePathHandle transform 会查找最近的 Path grid 顶点进行移动。
/// </summary>
public class PathManager : MonoBehaviour
{
    [Header("网格配置")]
    [Tooltip("网格宽度（单元格数量）")]
    [SerializeField] private int gridWidth = 10;

    [Tooltip("网格高度（单元格数量）")]
    [SerializeField] private int gridHeight = 10;

    [Tooltip("单元格大小")]
    [SerializeField] private float cellSize = 1f;

    [Tooltip("终点网格坐标，用于确定路径终点")]
    [SerializeField] private Vector2Int endPointCell;

    [Header("路径配置")]

    [Tooltip("到达目标点的距离阈值")]
    [SerializeField] private float reachThreshold = 0.1f;

    [Header("可视化")]
    [Tooltip("是否显示网格")]
    [SerializeField] private bool showGrid = true;

    [Tooltip("是否显示路径")]
    [SerializeField] private bool showPath = true;

    [Tooltip("网格颜色")]
    [SerializeField] private Color gridColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

    [Tooltip("路径单元格颜色")]
    [SerializeField] private Color pathCellColor = Color.green;

    [Tooltip("当前目标点颜色")]
    [SerializeField] private Color currentTargetColor = Color.red;

    [Tooltip("网格点大小")]
    [SerializeField] private float gridPointSize = 0.1f;

    [Tooltip("是否显示坐标标签")]
    [SerializeField] private bool showCoordinates = true;

    [Tooltip("坐标标签颜色")]
    [SerializeField] private Color coordinateColor = Color.white;

    // 路径单元格列表 (存储单元格坐标 x, y)
    [SerializeField] private List<Vector2Int> pathCells = new List<Vector2Int>();

    // 路径单元格缓存，用于 O(1) 快速查找
    private HashSet<Vector2Int> _pathCellCache = new HashSet<Vector2Int>();

    // 持有的移动控制器
    private IMovePathHandle _moveHandle;
    private Transform _moveTransform;

    // 移动状态
    private bool _isMoving = false;
    private int _currentPathIndex = 0;
    private bool _isCompleted = false;

    // 网格起点世界坐标（左下角）
    private Vector3 _gridOrigin;

    // Public properties
    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;
    public float CellSize => cellSize;
    public bool ShowGrid => showGrid;
    public bool ShowPath => showPath;
    public bool ShowCoordinates => showCoordinates;
    public IReadOnlyList<Vector2Int> PathCells => pathCells;

    #region 网格管理

    /// <summary>
    /// 初始化网格，根据 endPoint 确定网格位置
    /// </summary>
    public void InitializeGrid()
    {
        CalculateGridOrigin();
        RebuildPathCache();
    }

    /// <summary>
    /// 重建路径缓存，应在修改路径后调用
    /// </summary>
    private void RebuildPathCache()
    {
        _pathCellCache.Clear();
        foreach (var cell in pathCells)
        {
            _pathCellCache.Add(cell);
        }
    }

    /// <summary>
    /// 计算网格起点（左下角）
    /// </summary>
    private void CalculateGridOrigin()
    {
        // 以当前物体位置为网格原点（左下角对齐）
        _gridOrigin = transform.position - new Vector3(
            cellSize * 0.5f,
            0,
            cellSize * 0.5f
        );
    }

    /// <summary>
    /// 获取终点单元格的世界坐标
    /// </summary>
    public Vector3 GetEndPointPosition()
    {
        return GetCellCenter(endPointCell);
    }

    /// <summary>
    /// 设置终点网格坐标
    /// </summary>
    public void SetEndPointCell(int x, int y)
    {
        endPointCell = new Vector2Int(x, y);
    }

    /// <summary>
    /// 设置终点网格坐标
    /// </summary>
    public void SetEndPointCell(Vector2Int cell)
    {
        endPointCell = cell;
    }

    /// <summary>
    /// 获取单元格中心的世界坐标
    /// </summary>
    public Vector3 GetCellCenter(int x, int y)
    {
        return _gridOrigin + new Vector3(
            (x + 0.5f) * cellSize,
            0,
            (y + 0.5f) * cellSize
        );
    }

    /// <summary>
    /// 获取单元格中心的世界坐标
    /// </summary>
    public Vector3 GetCellCenter(Vector2Int cell)
    {
        return GetCellCenter(cell.x, cell.y);
    }

    /// <summary>
    /// 获取单元格的四个顶点世界坐标
    /// </summary>
    public Vector3[] GetCellCorners(int x, int y)
    {
        Vector3 center = GetCellCenter(x, y);
        float halfSize = cellSize * 0.5f;

        return new Vector3[]
        {
            center + new Vector3(-halfSize, 0, -halfSize), // 左下
            center + new Vector3(halfSize, 0, -halfSize),  // 右下
            center + new Vector3(halfSize, 0, halfSize),   // 右上
            center + new Vector3(-halfSize, 0, halfSize)   // 左上
        };
    }

    /// <summary>
    /// 将世界坐标转换为网格单元格坐标
    /// </summary>
    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - _gridOrigin;
        int x = Mathf.FloorToInt(localPos.x / cellSize);
        int y = Mathf.FloorToInt(localPos.z / cellSize);

        // 限制在网格范围内
        x = Mathf.Clamp(x, 0, gridWidth - 1);
        y = Mathf.Clamp(y, 0, gridHeight - 1);

        return new Vector2Int(x, y);
    }

    /// <summary>
    /// 检查单元格是否在网格范围内
    /// </summary>
    public bool IsValidCell(int x, int y)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }

    /// <summary>
    /// 检查单元格是否在网格范围内
    /// </summary>
    public bool IsValidCell(Vector2Int cell)
    {
        return IsValidCell(cell.x, cell.y);
    }

    #endregion

    #region 路径管理

    /// <summary>
    /// 添加单元格到路径
    /// </summary>
    public void AddPathCell(int x, int y)
    {
        if (!IsValidCell(x, y))
        {
            Debug.LogWarning($"[PathManager] 添加路径单元格失败: ({x}, {y}) 超出网格范围");
            return;
        }

        Vector2Int cell = new Vector2Int(x, y);
        if (!_pathCellCache.Contains(cell))
        {
            pathCells.Add(cell);
            _pathCellCache.Add(cell);
        }
    }

    /// <summary>
    /// 添加单元格到路径
    /// </summary>
    public void AddPathCell(Vector2Int cell)
    {
        AddPathCell(cell.x, cell.y);
    }

    /// <summary>
    /// 从路径中移除单元格
    /// </summary>
    public void RemovePathCell(int x, int y)
    {
        Vector2Int cell = new Vector2Int(x, y);
        if (pathCells.Remove(cell))
        {
            _pathCellCache.Remove(cell);
        }
    }

    /// <summary>
    /// 从路径中移除单元格
    /// </summary>
    public void RemovePathCell(Vector2Int cell)
    {
        pathCells.Remove(cell);
    }

    /// <summary>
    /// 切换单元格的路径状态
    /// </summary>
    public void TogglePathCell(int x, int y)
    {
        Vector2Int cell = new Vector2Int(x, y);
        if (_pathCellCache.Contains(cell))
        {
            pathCells.Remove(cell);
            _pathCellCache.Remove(cell);
        }
        else
        {
            AddPathCell(x, y);
        }
    }

    /// <summary>
    /// 检查单元格是否是路径 (O(1) 复杂度)
    /// </summary>
    public bool IsPathCell(int x, int y)
    {
        return _pathCellCache.Contains(new Vector2Int(x, y));
    }

    /// <summary>
    /// 检查单元格是否是路径 (O(1) 复杂度)
    /// </summary>
    public bool IsPathCell(Vector2Int cell)
    {
        return _pathCellCache.Contains(cell);
    }

    /// <summary>
    /// 清空路径
    /// </summary>
    public void ClearPath()
    {
        pathCells.Clear();
        _pathCellCache.Clear();
        _currentPathIndex = 0;
        _isCompleted = false;
    }

    /// <summary>
    /// 获取路径单元格数量
    /// </summary>
    public int PathCellCount => pathCells.Count;

    /// <summary>
    /// 获取指定索引的路径单元格
    /// </summary>
    public Vector2Int GetPathCell(int index)
    {
        if (index < 0 || index >= pathCells.Count)
        {
            Debug.LogWarning($"[PathManager] 获取路径单元格失败: 索引 {index} 超出范围");
            return Vector2Int.zero;
        }
        return pathCells[index];
    }

    /// <summary>
    /// 获取指定索引的路径单元格中心世界坐标
    /// </summary>
    public Vector3 GetPathCellPosition(int index)
    {
        return GetCellCenter(GetPathCell(index));
    }

    /// <summary>
    /// 查找最近的路径单元格（从世界坐标）
    /// </summary>
    public Vector2Int FindNearestPathCell(Vector3 worldPos)
    {
        if (pathCells.Count == 0)
        {
            return WorldToCell(worldPos);
        }

        Vector2Int nearestCell = pathCells[0];
        float nearestDistance = Vector3.Distance(worldPos, GetCellCenter(nearestCell));

        for (int i = 1; i < pathCells.Count; i++)
        {
            float distance = Vector3.Distance(worldPos, GetCellCenter(pathCells[i]));
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestCell = pathCells[i];
            }
        }

        return nearestCell;
    }

    /// <summary>
    /// 查找最近的路径单元格索引
    /// </summary>
    public int FindNearestPathCellIndex(Vector3 worldPos)
    {
        if (pathCells.Count == 0) return -1;

        int nearestIndex = 0;
        float nearestDistance = Vector3.Distance(worldPos, GetCellCenter(pathCells[0]));

        for (int i = 1; i < pathCells.Count; i++)
        {
            float distance = Vector3.Distance(worldPos, GetCellCenter(pathCells[i]));
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    #endregion

    #region 移动控制

    /// <summary>
    /// 设置要控制的移动句柄
    /// </summary>
    public void SetMoveHandle(IMovePathHandle handle, Transform moveTransform)
    {
        _moveHandle = handle;
        _moveTransform = moveTransform;
    }

    /// <summary>
    /// 开始沿路径移动
    /// </summary>
    public void StartMove()
    {
        if (_moveTransform == null)
        {
            Debug.LogWarning("[PathManager] 开始移动失败: 未设置移动 Transform");
            return;
        }

        if (pathCells.Count < 1)
        {
            Debug.LogWarning("[PathManager] 开始移动失败: 路径单元格数量不足（至少需要1个单元格）");
            return;
        }

        _isMoving = true;
        _isCompleted = false;
        _currentPathIndex = 0;

        // 将对象移动到第一个路径单元格
        _moveTransform.position = GetPathCellPosition(0);
    }

    /// <summary>
    /// 从最近的路径单元格开始移动
    /// </summary>
    public void StartMoveFromNearest()
    {
        if (_moveTransform == null)
        {
            Debug.LogWarning("[PathManager] 开始移动失败: 未设置移动 Transform");
            return;
        }

        if (pathCells.Count < 1)
        {
            Debug.LogWarning("[PathManager] 开始移动失败: 路径单元格数量不足");
            return;
        }

        _currentPathIndex = FindNearestPathCellIndex(_moveTransform.position);
        _isMoving = true;
        _isCompleted = false;
    }

    /// <summary>
    /// 从指定索引开始移动
    /// </summary>
    public void StartMoveFromIndex(int startIndex)
    {
        if (startIndex < 0 || startIndex >= pathCells.Count)
        {
            Debug.LogWarning($"[PathManager] StartMoveFromIndex 失败: 起始索引 {startIndex} 超出范围");
            return;
        }

        _currentPathIndex = startIndex;
        _isMoving = true;
        _isCompleted = false;

        if (_moveTransform != null)
        {
            _moveTransform.position = GetPathCellPosition(startIndex);
        }
    }

    /// <summary>
    /// 暂停移动
    /// </summary>
    public void PauseMove()
    {
        _isMoving = false;
    }

    /// <summary>
    /// 继续移动
    /// </summary>
    public void ResumeMove()
    {
        if (_moveTransform != null && pathCells.Count >= 1 && !_isCompleted)
        {
            _isMoving = true;
        }
    }

    /// <summary>
    /// 停止移动并重置
    /// </summary>
    public void StopMove()
    {
        _isMoving = false;
        _currentPathIndex = 0;
    }

    /// <summary>
    /// 是否正在移动
    /// </summary>
    public bool IsMoving => _isMoving;

    /// <summary>
    /// 是否已完成路径
    /// </summary>
    public bool IsCompleted => _isCompleted;

    /// <summary>
    /// 获取当前路径索引
    /// </summary>
    public int CurrentPathIndex => _currentPathIndex;

    /// <summary>
    /// 获取当前目标单元格
    /// </summary>
    public Vector2Int CurrentTargetCell => _currentPathIndex < pathCells.Count ? pathCells[_currentPathIndex] : Vector2Int.zero;

    /// <summary>
    /// 获取当前目标位置
    /// </summary>
    public Vector3 CurrentTargetPosition => _currentPathIndex < pathCells.Count ? GetPathCellPosition(_currentPathIndex) : Vector3.zero;

    #endregion

    #region MonoBehaviour

    private void Start()
    {
        InitializeGrid();
    }

    private void OnValidate()
    {
        // 编辑器修改时同步缓存
        if (!Application.isPlaying)
        {
            RebuildPathCache();
        }
    }

    private void Update()
    {
        if (_isMoving && _moveTransform != null)
        {
            UpdateMovement();
        }
    }

    private void OnDrawGizmos()
    {
        // 在编辑器模式下计算网格原点
        if (!Application.isPlaying)
        {
            CalculateGridOrigin();
        }

        DrawGridGizmos();
        DrawPathGizmos();
    }

    /// <summary>
    /// 绘制网格 Gizmos
    /// </summary>
    private void DrawGridGizmos()
    {
        if (!showGrid) return;

        Gizmos.color = gridColor;

        // 绘制网格线
        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 start = _gridOrigin + new Vector3(x * cellSize, 0, 0);
            Vector3 end = _gridOrigin + new Vector3(x * cellSize, 0, gridHeight * cellSize);
            Gizmos.DrawLine(start, end);
        }

        for (int y = 0; y <= gridHeight; y++)
        {
            Vector3 start = _gridOrigin + new Vector3(0, 0, y * cellSize);
            Vector3 end = _gridOrigin + new Vector3(gridWidth * cellSize, 0, y * cellSize);
            Gizmos.DrawLine(start, end);
        }

        // 绘制网格顶点
        Gizmos.color = new Color(gridColor.r, gridColor.g, gridColor.b, 0.8f);
        for (int x = 0; x <= gridWidth; x++)
        {
            for (int y = 0; y <= gridHeight; y++)
            {
                Vector3 point = _gridOrigin + new Vector3(x * cellSize, 0, y * cellSize);
                Gizmos.DrawSphere(point, gridPointSize);

                // 绘制坐标标签
                if (showCoordinates)
                {
                    DrawCoordinateLabel(point, $"({x},{y})");
                }
            }
        }
    }

    /// <summary>
    /// 绘制坐标标签
    /// </summary>
    private void DrawCoordinateLabel(Vector3 position, string text)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.color = coordinateColor;
        GUIStyle style = new GUIStyle();
        style.normal.textColor = coordinateColor;
        style.fontSize = 10;
        style.alignment = TextAnchor.MiddleCenter;
        style.fontStyle = FontStyle.Normal;

        // 在顶点上方显示坐标
        Vector3 labelPos = position + Vector3.up * cellSize * 0.3f;
        UnityEditor.Handles.Label(labelPos, text, style);
#endif
    }

    /// <summary>
    /// 绘制路径 Gizmos
    /// </summary>
    private void DrawPathGizmos()
    {
        if (!showPath || pathCells.Count == 0) return;

        // 绘制路径单元格
        Gizmos.color = pathCellColor;
        foreach (var cell in pathCells)
        {
            Vector3 center = GetCellCenter(cell);
            Vector3 size = new Vector3(cellSize * 0.8f, 0.05f, cellSize * 0.8f);
            Gizmos.DrawCube(center, size);

            // 在路径单元格上显示坐标
            if (showCoordinates)
            {
#if UNITY_EDITOR
                DrawCoordinateLabel(center + Vector3.up * 0.1f, $"[{cell.x},{cell.y}]");
#endif
            }
        }

        // 绘制当前目标点
        if (_isMoving && _currentPathIndex < pathCells.Count)
        {
            Gizmos.color = currentTargetColor;
            Vector3 currentTarget = GetPathCellPosition(_currentPathIndex);
            Gizmos.DrawSphere(currentTarget, gridPointSize * 2);

#if UNITY_EDITOR
            if (showCoordinates)
            {
                DrawCoordinateLabel(currentTarget + Vector3.up * 0.2f, $"→[{pathCells[_currentPathIndex].x},{pathCells[_currentPathIndex].y}]");
            }
#endif
        }

        // 绘制终点标记
        if (IsValidCell(endPointCell))
        {
            Gizmos.color = Color.magenta;
            Vector3 endPos = GetEndPointPosition();
            Gizmos.DrawWireCube(endPos, new Vector3(cellSize * 0.9f, 0.1f, cellSize * 0.9f));
#if UNITY_EDITOR
            if (showCoordinates)
            {
                DrawCoordinateLabel(endPos + Vector3.up * 0.3f, $"END[{endPointCell.x},{endPointCell.y}]");
            }
#endif
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 更新移动逻辑
    /// </summary>
    private void UpdateMovement()
    {
        if (_currentPathIndex >= pathCells.Count)
        {
            CompletePath();
            return;
        }

        Vector3 targetPoint = GetPathCellPosition(_currentPathIndex);
        Vector3 currentPos = _moveTransform.position;
        Vector3 direction = targetPoint - currentPos;
        direction.y = 0; // 保持在水平面移动

        float distance = direction.magnitude;

        // 检查是否到达目标点
        if (distance <= reachThreshold)
        {
            _moveTransform.position = targetPoint;
            _currentPathIndex++;

            // 检查是否到达最后一个点
            if (_currentPathIndex >= pathCells.Count)
            {
                CompletePath();
            }
            return;
        }

        // 归一化方向
        direction.Normalize();

        // 获取速度
        float moveSpeed = _moveHandle?.MoveSpeed ?? 5f;
        float rotateSpeed = _moveHandle?.RotateSpeed ?? 360f;

        // 平滑转向
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            _moveTransform.rotation = Quaternion.RotateTowards(
                _moveTransform.rotation,
                targetRotation,
                rotateSpeed * Time.deltaTime
            );
        }

        // 移动
        Vector3 moveDelta = direction * moveSpeed * Time.deltaTime;

        // 防止过冲
        if (moveDelta.magnitude > distance)
        {
            moveDelta = direction * distance;
        }

        _moveTransform.position += moveDelta;
    }

    /// <summary>
    /// 完成路径，触发回调
    /// </summary>
    private void CompletePath()
    {
        _isMoving = false;
        _isCompleted = true;

        // 触发 IMovePathHandle 的 OnComplete
        _moveHandle?.OnComplete();
    }

    #endregion
}

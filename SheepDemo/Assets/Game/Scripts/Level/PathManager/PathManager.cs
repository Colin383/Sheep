using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 网格路径管理器：使用 A* 寻路算法驱动 animal 沿路径移动。
/// </summary>
public class PathManager : MonoBehaviour
{
    [Header("网格配置")]
    [SerializeField] private int gridWidth = 10;
    [SerializeField] private int gridHeight = 10;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector2 originOffset = Vector2.zero;
    [SerializeField] private Vector2Int endPointCell;

    [Header("路径配置")]
    [SerializeField] private float reachThreshold = 0.1f;

    [Header("可视化")]
    [SerializeField] private bool showGrid = true;
    [SerializeField] private bool showPath = true;
    [SerializeField] private Color gridColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
    [SerializeField] private Color pathCellColor = Color.green;
    [SerializeField] private bool showCoordinates = true;
    [SerializeField] private Color coordinateColor = Color.white;

    [SerializeField] private List<Vector2Int> pathCells = new List<Vector2Int>();

    private bool[,] _pathGrid;
    private Vector3 _gridOrigin;

    private class MoveEntry
    {
        public IMovePathHandle Handle;
        public Transform Transform;
        public List<Vector2Int> PathCells = new List<Vector2Int>();
        public int CurrentPathIndex;
        public bool IsMoving;
        public bool IsCompleted;
        public System.Action OnCompleteCallback;
    }

    private Dictionary<Transform, MoveEntry> _moveEntries = new Dictionary<Transform, MoveEntry>();
    private List<Transform> _entryKeys = new List<Transform>();

    private List<AStarNode> _openList = new List<AStarNode>();
    private HashSet<Vector2Int> _closedSet = new HashSet<Vector2Int>();
    private Dictionary<Vector2Int, AStarNode> _nodeCache = new Dictionary<Vector2Int, AStarNode>();

    private static readonly Vector2Int[] NeighborOffsets = 
    { 
        new(0, 1), new(0, -1), new(-1, 0), new(1, 0),
        new(-1, 1), new(1, 1), new(-1, -1), new(1, -1)
    };

    private class AStarNode
    {
        public Vector2Int Position;
        public AStarNode Parent;
        public float GCost;
        public float HCost;
        public float FCost { get { return GCost + HCost; } }
    }

    public int GridWidth { get { return gridWidth; } }
    public int GridHeight { get { return gridHeight; } }
    public float CellSize { get { return cellSize; } }
    public IReadOnlyList<Vector2Int> PathCells { get { return pathCells; } }
    public Vector2Int EndPointCell { get { return endPointCell; } }
    public Vector2 OriginOffset { get { return originOffset; } }
    public int PathCellCount { get { return pathCells != null ? pathCells.Count : 0; } }

    public Vector2Int GetPathCell(int index)
    {
        if (pathCells != null && index >= 0 && index < pathCells.Count)
            return pathCells[index];
        return Vector2Int.zero;
    }

    private void Awake()
    {
        InitializeGrid();
    }

    private void Update()
    {
        if (_moveEntries.Count == 0) return;

        _entryKeys.Clear();
        _entryKeys.AddRange(_moveEntries.Keys);

        for (int i = 0; i < _entryKeys.Count; i++)
        {
            var key = _entryKeys[i];
            if (_moveEntries.TryGetValue(key, out var entry))
            {
                if (entry.IsMoving && entry.Transform != null)
                    UpdateMovement(entry);
            }
        }
    }

    private void OnDrawGizmos()
    {
        CalculateGridOrigin();
        DrawGridGizmos();
        DrawPathGizmos();
    }

    public void InitializeGrid()
    {
        CalculateGridOrigin();
        BuildPathGrid();
    }

    private void BuildPathGrid()
    {
        _pathGrid = new bool[gridWidth, gridHeight];
        if (pathCells == null) 
        {
            pathCells = new List<Vector2Int>();
            return;
        }
        
        for (int i = 0; i < pathCells.Count; i++)
        {
            var cell = pathCells[i];
            if (IsValidCell(cell))
                _pathGrid[cell.x, cell.y] = true;
        }
    }

    private void CalculateGridOrigin()
    {
        Vector3 rootPos = transform.position;
        float halfW = gridWidth * 0.5f * cellSize;
        float halfH = gridHeight * 0.5f * cellSize;
        float leftX = rootPos.x - halfW + originOffset.x;
        float bottomZ = rootPos.z - halfH + originOffset.y;
        _gridOrigin = new Vector3(leftX, rootPos.y, bottomZ);
    }

    public Vector3 GetCellCenter(int x, int y)
    {
        return _gridOrigin + new Vector3((x + 0.5f) * cellSize, 0, (y + 0.5f) * cellSize);
    }

    public Vector3 GetCellCenter(Vector2Int cell)
    {
        return GetCellCenter(cell.x, cell.y);
    }

    public bool IsValidCell(int x, int y)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }

    public bool IsValidCell(Vector2Int cell)
    {
        return IsValidCell(cell.x, cell.y);
    }

    public bool IsPathCell(int x, int y)
    {
        if (!IsValidCell(x, y)) return false;
        if (_pathGrid != null) return _pathGrid[x, y];
        // 编辑器模式下 _pathGrid 可能为 null，回退到检查 pathCells 列表
        return pathCells.Contains(new Vector2Int(x, y));
    }

    public bool IsPathCell(Vector2Int cell)
    {
        return IsPathCell(cell.x, cell.y);
    }

    public void AddPathCell(int x, int y)
    {
        if (!IsValidCell(x, y)) return;

        Vector2Int cell = new Vector2Int(x, y);
        if (!pathCells.Contains(cell))
        {
            pathCells.Add(cell);
            if (_pathGrid != null) _pathGrid[x, y] = true;
        }
    }

    public void AddPathCell(Vector2Int cell)
    {
        AddPathCell(cell.x, cell.y);
    }

    public void RemovePathCell(int x, int y)
    {
        if (pathCells.Remove(new Vector2Int(x, y)) && _pathGrid != null)
            _pathGrid[x, y] = false;
    }

    public void RemovePathCell(Vector2Int cell)
    {
        RemovePathCell(cell.x, cell.y);
    }

    public void TogglePathCell(int x, int y)
    {
        if (IsPathCell(x, y))
            RemovePathCell(x, y);
        else
            AddPathCell(x, y);
    }

    public void ClearPath()
    {
        pathCells.Clear();
        if (_pathGrid != null)
        {
            for (int x = 0; x < gridWidth; x++)
                for (int y = 0; y < gridHeight; y++)
                    _pathGrid[x, y] = false;
        }
    }

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
    {
        var result = new List<Vector2Int>();
        
        if (!IsValidCell(start) || !IsValidCell(end)) 
            return result;
        
        if (_pathGrid == null) BuildPathGrid();
        
        if (!IsPathCell(start) || !IsPathCell(end))
            return result;

        _openList.Clear();
        _closedSet.Clear();
        _nodeCache.Clear();

        var startNode = new AStarNode();
        startNode.Position = start;
        startNode.GCost = 0;
        startNode.HCost = Heuristic(start, end);
        _openList.Add(startNode);
        _nodeCache[start] = startNode;

        while (_openList.Count > 0)
        {
            var current = PopLowestFCostNode();
            _closedSet.Add(current.Position);

            if (current.Position == end)
            {
                return ReconstructPath(current);
            }

            for (int i = 0; i < 8; i++)
            {
                Vector2Int neighborPos = current.Position + NeighborOffsets[i];
                
                if (_closedSet.Contains(neighborPos) || !IsPathCell(neighborPos))
                    continue;

                float moveCost = (i < 4) ? 1f : 1.414f;
                float tentativeG = current.GCost + moveCost;

                AStarNode neighbor;
                if (!_nodeCache.TryGetValue(neighborPos, out neighbor))
                {
                    neighbor = new AStarNode();
                    neighbor.Position = neighborPos;
                    _nodeCache[neighborPos] = neighbor;
                }

                bool inOpenList = false;
                for (int j = 0; j < _openList.Count; j++)
                {
                    if (_openList[j].Position == neighborPos)
                    {
                        inOpenList = true;
                        break;
                    }
                }

                if (tentativeG < neighbor.GCost || !inOpenList)
                {
                    neighbor.GCost = tentativeG;
                    neighbor.HCost = Heuristic(neighborPos, end);
                    neighbor.Parent = current;

                    if (!inOpenList)
                        _openList.Add(neighbor);
                }
            }
        }

        return result;
    }

    public List<Vector2Int> FindPathFromWorldPosition(Vector3 worldPos)
    {
        if (pathCells.Count == 0)
        {
            Debug.LogError("[PathManager] pathCells 为空");
            return new List<Vector2Int>();
        }

        Vector2Int start = FindNearestPathCellByDistance(worldPos);
        
        if (!IsValidCell(endPointCell))
        {
            Debug.LogError("[PathManager] endPointCell 无效");
            return new List<Vector2Int>();
        }

        return FindPath(start, endPointCell);
    }

    private Vector2Int FindNearestPathCellByDistance(Vector3 worldPos)
    {
        if (pathCells.Count == 0)
            return new Vector2Int(-1, -1);

        Vector2Int nearest = pathCells[0];
        float nearestDistSq = Vector3.SqrMagnitude(GetCellCenter(nearest) - worldPos);

        for (int i = 1; i < pathCells.Count; i++)
        {
            float distSq = Vector3.SqrMagnitude(GetCellCenter(pathCells[i]) - worldPos);
            if (distSq < nearestDistSq)
            {
                nearestDistSq = distSq;
                nearest = pathCells[i];
            }
        }

        return nearest;
    }

    private float Heuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return Mathf.Max(dx, dy) + Mathf.Min(dx, dy) * 0.414f;
    }

    private AStarNode PopLowestFCostNode()
    {
        int bestIndex = 0;
        float bestF = _openList[0].FCost;

        for (int i = 1; i < _openList.Count; i++)
        {
            float f = _openList[i].FCost;
            if (f < bestF)
            {
                bestF = f;
                bestIndex = i;
            }
        }

        var node = _openList[bestIndex];
        _openList.RemoveAt(bestIndex);
        return node;
    }

    private List<Vector2Int> ReconstructPath(AStarNode endNode)
    {
        var path = new List<Vector2Int>();
        var current = endNode;
        while (current != null)
        {
            path.Add(current.Position);
            current = current.Parent;
        }
        path.Reverse();
        return path;
    }

    public void RegisterMoveHandle(IMovePathHandle handle, Transform moveTransform, List<Vector2Int> path, System.Action onComplete)
    {
        if (moveTransform == null || path == null || path.Count == 0)
            return;

        UnregisterMoveHandle(moveTransform);

        var entry = new MoveEntry();
        entry.Handle = handle;
        entry.Transform = moveTransform;
        entry.CurrentPathIndex = 0;
        entry.IsMoving = false;
        entry.IsCompleted = false;
        entry.OnCompleteCallback = onComplete;
        entry.PathCells.AddRange(path);
        _moveEntries[moveTransform] = entry;
    }

    public void UnregisterMoveHandle(Transform moveTransform)
    {
        if (moveTransform != null)
            _moveEntries.Remove(moveTransform);
    }

    public void StartMove(Transform moveTransform)
    {
        if (!_moveEntries.TryGetValue(moveTransform, out var entry))
            return;

        if (entry.PathCells.Count == 0)
            return;

        entry.IsMoving = true;
        entry.IsCompleted = false;
        entry.CurrentPathIndex = 0;
        moveTransform.position = GetCellCenter(entry.PathCells[0]);
    }

    private void UpdateMovement(MoveEntry entry)
    {
        if (entry.CurrentPathIndex >= entry.PathCells.Count)
        {
            CompletePath(entry);
            return;
        }

        Vector3 target = GetCellCenter(entry.PathCells[entry.CurrentPathIndex]);
        Vector3 current = entry.Transform.position;
        Vector3 dir = target - current;
        dir.y = 0;

        float dist = dir.magnitude;
        if (dist <= reachThreshold)
        {
            entry.Transform.position = target;
            entry.CurrentPathIndex++;

            if (entry.CurrentPathIndex >= entry.PathCells.Count)
                CompletePath(entry);
            return;
        }

        dir.Normalize();

        float moveSpeed = entry.Handle != null ? entry.Handle.MoveSpeed : 5f;
        float rotateSpeed = entry.Handle != null ? entry.Handle.RotateSpeed : 360f;

        if (dir != Vector3.zero)
        {
            entry.Transform.rotation = Quaternion.RotateTowards(
                entry.Transform.rotation,
                Quaternion.LookRotation(dir),
                rotateSpeed * Time.deltaTime
            );
        }

        float moveDist = Mathf.Min(moveSpeed * Time.deltaTime, dist);
        entry.Transform.position += dir * moveDist;
    }

    private void CompletePath(MoveEntry entry)
    {
        entry.IsMoving = false;
        entry.IsCompleted = true;
        if (entry.OnCompleteCallback != null)
            entry.OnCompleteCallback();
        if (entry.Handle != null)
            entry.Handle.OnComplete();
    }

    #region Gizmos

    private void DrawGridGizmos()
    {
        if (!showGrid) return;

        Gizmos.color = gridColor;
        float width = gridWidth * cellSize;
        float height = gridHeight * cellSize;

        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 start = _gridOrigin + new Vector3(x * cellSize, 0, 0);
            Gizmos.DrawLine(start, start + new Vector3(0, 0, height));
        }

        for (int y = 0; y <= gridHeight; y++)
        {
            Vector3 start = _gridOrigin + new Vector3(0, 0, y * cellSize);
            Gizmos.DrawLine(start, start + new Vector3(width, 0, 0));
        }
    }

    private void DrawPathGizmos()
    {
        if (!showPath || pathCells.Count == 0) return;

        Vector3 size = new Vector3(cellSize * 0.8f, 0.05f, cellSize * 0.8f);
        Gizmos.color = pathCellColor;

        for (int i = 0; i < pathCells.Count; i++)
        {
            Vector3 center = GetCellCenter(pathCells[i]);
            Gizmos.DrawCube(center, size);
        }

        if (IsValidCell(endPointCell))
        {
            Gizmos.color = Color.magenta;
            Vector3 endPos = GetCellCenter(endPointCell);
            Gizmos.DrawWireCube(endPos, new Vector3(cellSize * 0.9f, 0.1f, cellSize * 0.9f));
        }
    }

    #endregion
}

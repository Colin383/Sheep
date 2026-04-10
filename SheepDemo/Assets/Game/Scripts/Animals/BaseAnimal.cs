using System.Collections.Generic;
using Bear.Fsm;
using UnityEngine;

/// <summary>
/// 由关卡配置生成的实体基类。具体移动、交互等逻辑写在子类。
/// <para>
/// 与 <see cref="LevelGenerator"/> 约定：json 里的 (row,col) 为该实例 footprint 的<strong>锚定格</strong>
/// （row 较小表示更靠「上」，与 row0IsTop 一致）；默认将根 pivot 放在 footprint <strong>左下角格中心</strong>；
/// 若生成器开启「按 Renderer 对齐」会把合并包围盒中心挪到该格心，pivot 会随之偏移。
/// Gizmo 在物体<strong>本地 XZ 底面</strong>上铺设（列沿 local +X，行沿 local -Z；与 LevelGenerator 在世界坐标下、物体仅绕 Y 旋转时一致）。
/// 绘制时使用 <c>Gizmos.matrix = transform.localToWorldMatrix</c>，因此会随物体的<strong>旋转与缩放</strong>一起变换；棱柱厚度沿 local Y（一般即模型竖直方向）。
/// </para>
/// </summary>
public abstract class BaseAnimal : MonoBehaviour, IBearMachineOwner
{
    private const float GizmoPrismHeight = 0.01f;
    private StateMachine _machine;
    private bool _machineReady;

    public StateMachine Machine => _machine;
    public string CurrentState { get; private set; }

    protected virtual string DefaultState => AnimalStateName.IDLE;

    protected virtual void Awake()
    {
        InitStateMachine();
    }

    protected virtual void Update()
    {
        _machine?.Update();
    }

    protected virtual void OnDestroy()
    {
        _machine?.Dispose();
    }

    public void EnterState(string stateName)
    {
        if (!_machineReady || string.IsNullOrWhiteSpace(stateName))
            return;

        CurrentState = stateName;
        _machine.Enter(stateName);
    }

    public bool IsInState(string stateName)
    {
        return _machine != null && _machine.IsRunning(stateName);
    }

    public void EnterIdleState()
    {
        EnterState(AnimalStateName.IDLE);
    }

    public void EnterMovingState()
    {
        EnterState(AnimalStateName.MOVING);
    }

    public void EnterBackState()
    {
        EnterState(AnimalStateName.BACK);
    }

    private void InitStateMachine()
    {
        if (_machineReady)
            return;

        _machineReady = true;
        _machine = new StateMachine(this);
        _machine.Inject(typeof(Animal_Idle),
            typeof(Animal_Moving),
            typeof(Animal_Back));

        _machine.Apply(typeof(BaseAnimal));
        EnterState(DefaultState);
    }

    // 检测移动方向，每个动物的检测方式不一样。大多是都是朝向检测。
    public virtual DirectionEnum GetMovableDirections()
    {
        return FacingDirection;
    }

    // 进入移动模式
    public virtual void TryMoving()
    {
        EnterMovingState();
    }


    #region 关卡数据（由 Init 写入）

    public int Id { get; private set; }
    public int Row { get; private set; }
    public int Col { get; private set; }

    /// <summary>配置里的原始字符串（可能为空）。解析朝向请用 <see cref="FacingDirection"/>。</summary>
    public string Direction { get; private set; }

    /// <summary>由 json <c>direction</c> 解析；空或非法时为 <see cref="DirectionEnum.Down"/>（朝下）。</summary>
    public DirectionEnum FacingDirection { get; private set; } = DirectionEnum.Down;

    /// <summary>与配置 type / prefab 对应的动物种类。</summary>
    public abstract AnimalType Type { get; }

    public LevelCtrl Level { get; private set;}

    /// <summary>用关卡实例数据初始化，生成器在 Instantiate 后调用。</summary>
    public virtual void Init(int id, int row, int col, string direction)
    {
        Id = id;
        Row = row;
        Col = col;
        Direction = direction ?? string.Empty;
        FacingDirection = DirectionEnumUtility.ParseOrDefault(direction);
    }

    /// <summary>
    /// 用于监听 animal 状态，以及更新地图内容信息
    /// </summary>
    /// <param name="owner"></param>
    public virtual void SetLevelOwner(LevelCtrl owner)
    {
        Level = owner;
    }

    #endregion

    #region 占用格（与生成器一致，取 Prefab 上配置）

    /// <summary>
    /// footprint 占用格（相对“起始格”的偏移列表）。
    /// <para>
    /// 约定：以 <see cref="transform.position"/> 对应的格子为起始格（偏移 = (0,0)）。
    /// 本列表存放“额外占用”的格子偏移：x=列偏移（向右为 +），y=行偏移（向下为 +）。
    /// </para>
    /// <para>
    /// 注意：起始格 (0,0) 不需要填进列表；生成器会默认包含起始格。
    /// </para>
    /// </summary>
    public IReadOnlyList<Vector2Int> FootprintSizeCells => footprintExtraCells;

    #endregion

    #region Gizmo（仅编辑器）

    [Header("占用格子 Gizmo（仅 Scene 调试）")]
    [Tooltip("是否绘制占用区域")]
    [SerializeField] private bool drawFootprintGizmo = true;

    [Tooltip("勾选则仅在选中本物体时绘制")]
    [SerializeField] private bool drawFootprintWhenSelectedOnly = true;

    [Tooltip("额外占用格子的偏移（不包含起始格 (0,0)）。x=列偏移（向右 +），y=行偏移（向下 +）")]
    [SerializeField] private List<Vector2Int> footprintExtraCells = new List<Vector2Int>();

    [Tooltip("逐格绘制半透明块；关闭则只画整体线框")]
    [SerializeField] private bool drawCellsIndividually = true;

    [Tooltip("单格在物体本地底面上的尺寸：x=local X 宽度，y=local Z 深度（数值应与 LevelGenerator.cellSize 一致）")]
    [SerializeField] private Vector2 cellSizeXZ = Vector2.one;

    [SerializeField] private Color gizmoWireColor = new Color(0.2f, 1f, 0.35f, 0.95f);
    [SerializeField] private Color gizmoFillColor = new Color(0.2f, 1f, 0.35f, 0.28f);

    [Tooltip("几何中心所在格（离 anchor 最近的格子）的填充色")]
    [SerializeField] private Color gizmoCenterFillColor = new Color(1f, 0.15f, 0.15f, 0.4f);

    [Tooltip("几何中心所在格的线框颜色")]
    [SerializeField] private Color gizmoCenterWireColor = new Color(1f, 0.2f, 0.2f, 1f);

    [Tooltip("略微抬高，避免与地表 Z-fight")]
    [SerializeField] private float gizmoHeightBias = 0.02f;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!drawFootprintGizmo || drawFootprintWhenSelectedOnly)
            return;

        DrawFootprint();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawFootprintGizmo || !drawFootprintWhenSelectedOnly)
            return;

        DrawFootprint();
    }

    private void DrawFootprint()
    {
        float dx = Mathf.Max(1e-4f, cellSizeXZ.x);
        float dz = Mathf.Max(1e-4f, cellSizeXZ.y);

        // 本地空间：起始格中心为 (0,0)；列沿 local +X，行沿 local -Z
        Vector3 localAnchor = new Vector3(0f, gizmoHeightBias, 0f);
        Vector3 cellExtentLocal = new Vector3(dx, GizmoPrismHeight, dz);

        // 计算“所有占用格”（包含起始格）
        int occupiedCount = 1 + (footprintExtraCells?.Count ?? 0);
        var occupied = new List<Vector2Int>(occupiedCount) { Vector2Int.zero };
        if (footprintExtraCells != null)
        {
            for (int i = 0; i < footprintExtraCells.Count; i++)
            {
                var off = footprintExtraCells[i];
                if (off == Vector2Int.zero)
                    continue;
                occupied.Add(off);
            }
        }

        // 选“离起始格最近”的格子作为中心强调格（用于可视化，不影响逻辑）
        int centerIndex = 0;
        int bestSq = int.MaxValue;
        for (int i = 0; i < occupied.Count; i++)
        {
            var o = occupied[i];
            int dSq = o.x * o.x + o.y * o.y;
            if (dSq < bestSq)
            {
                bestSq = dSq;
                centerIndex = i;
            }
        }

        Matrix4x4 prevMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        if (drawCellsIndividually)
        {
            for (int i = 0; i < occupied.Count; i++)
            {
                var o = occupied[i];
                Vector3 localCell = localAnchor + new Vector3(o.x * dx, 0f, o.y * dz);
                bool isCenter = i == centerIndex;

                Gizmos.color = isCenter ? gizmoCenterFillColor : gizmoFillColor;
                Gizmos.DrawCube(localCell, cellExtentLocal);
                Gizmos.color = isCenter ? gizmoCenterWireColor : gizmoWireColor;
                Gizmos.DrawWireCube(localCell, cellExtentLocal);
            }
        }
        else
        {
            // 只画整体包围框（按占用格集合的 AABB）
            int minX = occupied[0].x;
            int maxX = occupied[0].x;
            int minY = occupied[0].y;
            int maxY = occupied[0].y;
            for (int i = 1; i < occupied.Count; i++)
            {
                var o = occupied[i];
                minX = Mathf.Min(minX, o.x);
                maxX = Mathf.Max(maxX, o.x);
                minY = Mathf.Min(minY, o.y);
                maxY = Mathf.Max(maxY, o.y);
            }

            float sizeX = (maxX - minX + 1) * dx;
            float sizeZ = (maxY - minY + 1) * dz;
            Vector3 boundsSizeLocal = new Vector3(sizeX, GizmoPrismHeight, sizeZ);
            Vector3 boundsCenterLocal = localAnchor + new Vector3(((minX + maxX) * 0.5f) * dx, 0f, -((minY + maxY) * 0.5f) * dz);

            Gizmos.color = gizmoWireColor;
            Gizmos.DrawWireCube(boundsCenterLocal, boundsSizeLocal);

            var centerOff = occupied[centerIndex];
            Vector3 localCenterCell = localAnchor + new Vector3(centerOff.x * dx, 0f, -centerOff.y * dz);
            Gizmos.color = gizmoCenterFillColor;
            Gizmos.DrawCube(localCenterCell, cellExtentLocal);
            Gizmos.color = gizmoCenterWireColor;
            Gizmos.DrawWireCube(localCenterCell, cellExtentLocal);
        }

        Gizmos.matrix = prevMatrix;
    }
#endif

    #endregion
}

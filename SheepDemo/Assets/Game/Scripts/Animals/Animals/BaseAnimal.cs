using System;
using System.Collections.Generic;
using Bear.Fsm;
using UnityEngine;

/// <summary>
/// 由关卡配置生成的实体基类。具体移动、交互等逻辑写在子类。
/// </summary>
public abstract class BaseAnimal : MonoBehaviour, IBearMachineOwner, IMovePathHandle
{
    private const float GizmoPrismHeight = 0.01f;
    private StateMachine _machine;
    private bool _machineReady;

    [Tooltip("动画控制器")]
    [SerializeField] private AnimAnimtorCtrl animAnimtorCtrl;

    [Header("移动配置")]
    [Tooltip("位移速度（单位/秒）")]
    [SerializeField] private float moveSpeed = 3f;

    [Tooltip("原地旋转速度（角度/秒）")]
    [SerializeField] private float rotateSpeed = 540f;

    /// <summary>
    /// 位移速度（单位/秒）
    /// </summary>
    public float MoveSpeed => moveSpeed;

    /// <summary>
    /// 原地旋转速度（角度/秒）
    /// </summary>
    public float RotateSpeed => rotateSpeed;

    /// <summary>
    /// 当前占用格（x=col, y=row）。
    /// </summary>
    public Vector2Int CurrentPos { get; protected set; }

    /// <summary>
    /// 上一次占用格（x=col, y=row）。
    /// </summary>
    public Vector2Int PreviousPos { get; private set; }

    public StateMachine Machine => _machine;
    public string CurrentState { get; private set; }

    protected virtual string DefaultState => AnimalStateName.IDLE;

    protected virtual void Awake()
    {
        ResolveAnimCtrl();
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

    public virtual void EnterIdleState()
    {
        EnterState(AnimalStateName.IDLE);
        // Idle 默认关闭 walk。
        SetWalkAnim(false);
        PlayKnockAnim();
        FailToMove();
    }

    /// <summary>
    /// 移动失败进入 Idle 时的回调，供子类补充逻辑。
    /// </summary>
    protected virtual void FailToMove()
    {
    }

    public virtual void EnterMovingState()
    {
        EnterState(AnimalStateName.MOVING);
        // Moving 默认开启 walk。
        SetWalkAnim(true);
    }

    public virtual void EnterBackState()
    {
        EnterState(AnimalStateName.BACK);
        // Back 默认关闭 walk。
        SetWalkAnim(true);
    }

    /// <summary>
    /// 播放受击动画（knock trigger）。
    /// </summary>
    public void PlayKnockAnim()
    {
        var ctrl = ResolveAnimCtrl();
        if (ctrl != null)
            ctrl.PlayKnock();
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

    // 检测移动方向，每个动物的检测方式不一样。大多是朝向检测。
    public virtual IReadOnlyList<DirectionEnum> GetMovableDirections()
    {
        return new[] { FacingDirection };
    }

    /// <summary>
    /// 校验是否允许移动到目标格子，供子类定制规则。
    /// </summary>
    public virtual bool CanMoveTo(Vector2Int gridPos)
    {
        return true;
    }

    /// <summary>
    /// 每确定一步移动后调用，供子类记录已走过的格子。
    /// </summary>
    public virtual void OnMoveStepConfirmed(Vector2Int gridPos)
    {
    }

    
    /// <summary>
    /// LevelCtrl 触发点击事件的时候触发
    /// </summary>
    public virtual void OnClickTrigger()
    {
        TryMoving();
    }

    // 进入移动模式。
    public virtual void TryMoving()
    {
        EnterMovingState();
    }

    /// <summary>
    /// 直接设置当前网格坐标（x=col, y=row）。
    /// </summary>
    public void SetCurrentGridPos(int row, int col)
    {
        PreviousPos = CurrentPos;
        CurrentPos = new Vector2Int(col, row);
    }

    /// <summary>
    /// 仅回滚 CurrentPos（不修改 PreviousPos）。
    /// </summary>
    public void RollbackCurrentPos(Vector2Int pos)
    {
        CurrentPos = pos;
    }

    /// <summary>
    /// 按方向推进一格并记录 PreviousPos。
    /// </summary>
    public void AdvanceCurrentGridPos(DirectionEnum direction)
    {
        PreviousPos = CurrentPos;

        switch (direction)
        {
            case DirectionEnum.Up:
                CurrentPos += new Vector2Int(0, 1);
                break;
            case DirectionEnum.Down:
                CurrentPos += new Vector2Int(0, -1);
                break;
            case DirectionEnum.Left:
                CurrentPos += new Vector2Int(-1, 0);
                break;
            case DirectionEnum.Right:
                CurrentPos += new Vector2Int(1, 0);
                break;
        }
    }

    private void SetWalkAnim(bool walking)
    {
        var ctrl = ResolveAnimCtrl();
        if (ctrl != null)
            ctrl.SetWalk(walking);
    }

    private AnimAnimtorCtrl ResolveAnimCtrl()
    {
        if (animAnimtorCtrl != null)
            return animAnimtorCtrl;

        animAnimtorCtrl = GetComponent<AnimAnimtorCtrl>();
        if (animAnimtorCtrl != null)
            return animAnimtorCtrl;

        animAnimtorCtrl = GetComponentInChildren<AnimAnimtorCtrl>(true);
        if (animAnimtorCtrl != null)
            return animAnimtorCtrl;

        // 缺少控制器时自动补齐，减少 prefab 手工配置成本。
        animAnimtorCtrl = gameObject.AddComponent<AnimAnimtorCtrl>();
        return animAnimtorCtrl;
    }


    #region 关卡数据（由 Init 写入）

    public int Id { get; private set; }
    public int Row { get; private set; }
    public int Col { get; private set; }

    /// <summary>配置里的原始字符串（可能为空）。解析朝向请用 <see cref="FacingDirection"/>。</summary>
    public string Direction { get; private set; }

    /// <summary>配置中的原始 param 字符串。</summary>
    public string Param { get; private set; }

    /// <summary>由 json <c>direction</c> 解析；空或非法时为 <see cref="DirectionEnum.Down"/>（朝下）。</summary>
    public DirectionEnum FacingDirection { get; private set; } = DirectionEnum.Down;

    /// <summary>与配置 type / prefab 对应的动物种类。</summary>
    public abstract AnimalType Type { get; }

    public LevelCtrl Level { get; private set; }

    /// <summary>用关卡实例数据初始化，生成器在 Instantiate 后调用。</summary>
    public virtual void Init(int id, int row, int col, string direction, string param = null)
    {
        Id = id;
        Row = row;
        Col = col;
        Direction = direction ?? string.Empty;
        Param = param ?? string.Empty;

        CurrentPos = new Vector2Int(Col, Row);
        PreviousPos = CurrentPos;

        FacingDirection = DirectionEnumUtility.ParseOrDefault(direction);
        try
        {
            ParseParam(Param);
        }
        catch (Exception e)
        {
            Debug.LogError($"[BaseAnimal] ParseParam failed for {GetType().Name}, param='{Param}': {e}");
        }
    }

    /// <summary>
    /// 解析关卡配置传入的 param 字段，供子类实现自定义参数逻辑。
    /// </summary>
    protected virtual void ParseParam(string param)
    {
    }

    /// <summary>
    /// 用于监听 animal 状态，以及更新地图内容信息。
    /// </summary>
    public virtual void SetLevelOwner(LevelCtrl owner)
    {
        Level = owner;
    }

    #endregion

    #region 占用格（与生成器一致，取 Prefab 上配置）

    /// <summary>
    /// footprint 占用格（相对“起始格”的偏移列表）。
    /// x=列偏移（向右 +），y=行偏移（向下 +）。
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

    [Tooltip("单格在物体本地底面上的尺寸：x=local X 宽度，y=local Z 深度（应与 LevelGenerator.cellSize 一致）")]
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
    protected virtual void OnDrawGizmos()
    {
        if (!drawFootprintGizmo || drawFootprintWhenSelectedOnly)
            return;

        DrawFootprint();
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (!drawFootprintGizmo || !drawFootprintWhenSelectedOnly)
            return;

        DrawFootprint();
    }

    protected virtual void DrawFootprint()
    {
        float dx = Mathf.Max(1e-4f, cellSizeXZ.x);
        float dz = Mathf.Max(1e-4f, cellSizeXZ.y);

        // 本地空间：起始格中心为 (0,0)；列沿 local +X，行沿 local -Z。
        Vector3 localAnchor = new Vector3(0f, gizmoHeightBias, 0f);
        Vector3 cellExtentLocal = new Vector3(dx, GizmoPrismHeight, dz);

        // 计算所有占用格（包含起始格）。
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

        // 选取距离起始格最近的格子作为中心强调格。
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
            // 只画整体包围框（占用格集合的 AABB）。
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

    public virtual void OnComplete()
    {
        // 路径移动完成回调，由 PathManager 调用
        // 具体的销毁逻辑已在 LevelCtrl.OnAnimalReachEndPoint 中处理
    }
}

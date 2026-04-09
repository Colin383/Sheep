using UnityEngine;

/// <summary>
/// 由关卡配置生成的实体基类。具体移动、交互等逻辑写在子类。
/// <para>
/// 与 <see cref="LevelGenerator"/> 约定：json 里的 (row,col) 为该实例 footprint 在网格中的<strong>左上角</strong>
/// （row 较小表示更靠“上”，与 row0IsTop 一致）；生成时 transform 放在整块占用的<strong>世界空间几何中心</strong>。
/// Gizmo 以当前 transform 为几何中心、按 <see cref="FootprintSizeCells"/> 向外画示意格子。
/// </para>
/// </summary>
public abstract class BaseAnimal : MonoBehaviour
{
    private const float GizmoPrismHeight = 0.01f;

    #region 关卡数据（由 Init 写入）

    public int Id { get; private set; }
    public int Row { get; private set; }
    public int Col { get; private set; }
    public string Direction { get; private set; }

    /// <summary>与配置 type / prefab 对应的动物种类。</summary>
    public abstract AnimalType Type { get; }

    /// <summary>用关卡实例数据初始化，生成器在 Instantiate 后调用。</summary>
    public virtual void Init(int id, int row, int col, string direction)
    {
        Id = id;
        Row = row;
        Col = col;
        Direction = direction;
    }

    #endregion

    #region 占用格（与生成器一致，取 Prefab 上配置）

    /// <summary> footprint 占用：x=列数，y=行数。json (row,col) 为左上角第一格。</summary>
    public Vector2Int FootprintSizeCells => footprintSize;

    #endregion

    #region Gizmo（仅编辑器）

    [Header("占用格子 Gizmo（仅 Scene 调试）")]
    [Tooltip("是否绘制占用区域")]
    [SerializeField] private bool drawFootprintGizmo = true;

    [Tooltip("勾选则仅在选中本物体时绘制")]
    [SerializeField] private bool drawFootprintWhenSelectedOnly = true;

    [Tooltip("占用列数 × 占用行数（与配置网格一致：x=列，y=行）")]
    [SerializeField] private Vector2Int footprintSize = Vector2Int.one;

    [Tooltip("相对几何中心的偏移，单位：格；x 沿世界 +X，y 沿“行”方向（对应世界 -Z 一侧）")]
    [SerializeField] private Vector2Int footprintCellOffset = Vector2Int.zero;

    [Tooltip("逐格绘制半透明块；关闭则只画整体线框")]
    [SerializeField] private bool drawCellsIndividually = true;

    [Tooltip("单格在世界空间中的尺寸：x=沿 X 的宽度，z=沿 Z 的深度（应与 LevelGenerator.cellSize 一致）")]
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
        int columns = Mathf.Max(1, footprintSize.x);
        int rows = Mathf.Max(1, footprintSize.y);
        float dx = Mathf.Max(1e-4f, cellSizeXZ.x);
        float dz = Mathf.Max(1e-4f, cellSizeXZ.y);

        // 几何中心 = transform，再按「格」做一次平移
        Vector3 anchor = transform.position;
        anchor.x += footprintCellOffset.x * dx;
        anchor.z -= footprintCellOffset.y * dz;
        anchor.y += gizmoHeightBias;

        float totalW = columns * dx;
        float totalD = rows * dz;

        float left = anchor.x - 0.5f * totalW + 0.5f * dx;
        float topZ = anchor.z + 0.5f * totalD - 0.5f * dz;
        Vector3 cellExtent = new Vector3(dx, GizmoPrismHeight, dz);

        // 与几何中心最近的格子视为「中心格」（1×1 时即该格；偶数尺寸时取距离最小的一个，平手取行小、列小）
        int centerR = 0;
        int centerC = 0;
        float bestSq = float.MaxValue;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                float cx = left + c * dx;
                float cz = topZ - r * dz;
                float dSq = (cx - anchor.x) * (cx - anchor.x) + (cz - anchor.z) * (cz - anchor.z);
                if (dSq < bestSq - 1e-8f || (Mathf.Abs(dSq - bestSq) <= 1e-8f && (r < centerR || (r == centerR && c < centerC))))
                {
                    bestSq = dSq;
                    centerR = r;
                    centerC = c;
                }
            }
        }

        if (drawCellsIndividually)
        {
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    Vector3 cellCenter = new Vector3(left + c * dx, anchor.y, topZ - r * dz);
                    bool isCenter = r == centerR && c == centerC;

                    Gizmos.color = isCenter ? gizmoCenterFillColor : gizmoFillColor;
                    Gizmos.DrawCube(cellCenter, cellExtent);
                    Gizmos.color = isCenter ? gizmoCenterWireColor : gizmoWireColor;
                    Gizmos.DrawWireCube(cellCenter, cellExtent);
                }
            }
        }
        else
        {
            Vector3 boundsExtent = new Vector3(totalW, GizmoPrismHeight, totalD);
            Gizmos.color = gizmoWireColor;
            Gizmos.DrawWireCube(anchor, boundsExtent);

            Vector3 pivotCenter = new Vector3(left + centerC * dx, anchor.y, topZ - centerR * dz);
            Gizmos.color = gizmoCenterFillColor;
            Gizmos.DrawCube(pivotCenter, cellExtent);
            Gizmos.color = gizmoCenterWireColor;
            Gizmos.DrawWireCube(pivotCenter, cellExtent);
        }
    }
#endif

    #endregion
}

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Bear.Logger;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 读取 LevelGameConfig，在 XZ 平面按格子生成实体并缓存。
/// 网格默认左下角为原点。
/// </summary>
public partial class LevelCtrl
{
    [Header("Config")]
    [SerializeField] private TextAsset levelJson;

    [Header("Grid")]
    [SerializeField] private Vector2 cellSize = Vector2.one;
    [SerializeField] private Vector2 origin = Vector2.zero;
    [SerializeField] private float spawnYOffset = 0f;

    [Header("Animal Prefabs")]
    [SerializeField]
    private readonly Dictionary<AnimalType, string> animalPrefabsPath = new Dictionary<AnimalType, string>()
    {
        {AnimalType.Sheep, "Animals/Sheep"},
        {AnimalType.BombSheep, "Animals/BombSheep"},
        {AnimalType.Chick, "Animals/Chick"},
        {AnimalType.Elephant, "Animals/Elephant"},
        {AnimalType.CdSheep, "Animals/CdSheep"}
    };


    [Header("Parents")]
    [SerializeField] private Transform instancesRoot;

    [Header("Spawn")]
    [Tooltip("footprint（Prefab 上列×行）是否随实例 facing 在世界格上旋转展开；关闭则始终沿「行 +、列 +」轴对齐矩形")]
    [SerializeField] private bool footprintFollowsFacing = true;

    #region Gizmos Config
    [SerializeField] private bool alignSpawnToRendererBoundsCenter = false;

    [FoldoutGroup("Gizmos", Expanded = false)]
    [SerializeField] private bool drawGridGizmos = true;

    [FoldoutGroup("Gizmos")]
    [SerializeField] private bool drawInstanceGizmos = true;

    [FoldoutGroup("Gizmos")]
    [SerializeField] private bool drawFootprintCellsGizmo = true;

    [FoldoutGroup("Gizmos")]
    [SerializeField] private bool drawCurrentPosGizmos = true;

    [FoldoutGroup("Gizmos")]
    [SerializeField] private bool drawInstanceGizmoLabels = true;

    [FoldoutGroup("Gizmos")]
    [SerializeField] private Color gizmoGridColor = new Color(0f, 0.8f, 1f, 0.7f);

    [FoldoutGroup("Gizmos")]
    [SerializeField] private Color gizmoBorderColor = new Color(1f, 1f, 1f, 0.9f);

    [FoldoutGroup("Gizmos")]
    [Tooltip("配置中锚点格（JSON row/col）线框")]
    [SerializeField] private Color gizmoAnchorCellColor = new Color(0.55f, 0.95f, 1f, 0.75f);

    [FoldoutGroup("Gizmos")]
    [Tooltip("footprint 左下角格（生成 pivot 对齐）强调线框")]
    [SerializeField] private Color gizmoBottomLeftCellWireColor = new Color(1f, 0.92f, 0.25f, 0.95f);

    [FoldoutGroup("Gizmos")]
    [Tooltip("生成目标点（footprint 左下角格中心）：球体 + 地面十字线")]
    [SerializeField] private Color gizmoInstanceColor = new Color(1f, 0.85f, 0.2f, 0.9f);

    [FoldoutGroup("Gizmos")]
    [SerializeField] private float gizmoInstanceRadius = 0.08f;

    [FoldoutGroup("Gizmos")]
    [Tooltip("footprint 占用的每一格的线框（青色半透明，不含单独加粗的左下角格）")]
    [SerializeField] private Color gizmoFootprintCellWireColor = new Color(0.25f, 0.95f, 1f, 0.55f);

    [FoldoutGroup("Gizmos")]
    [SerializeField] private Color gizmoCurrentPosColor = new Color(1f, 0.35f, 0.35f, 0.95f);

    [FoldoutGroup("Gizmos")]
    [SerializeField] private float gizmoCurrentPosYOffset = 0.03f;


    [FoldoutGroup("Gizmos")]
    [SerializeField] private bool drawGridCellLabels = true;

    [FoldoutGroup("Gizmos")]
    [SerializeField] private Color gizmoCellLabelColor = new Color(1f, 1f, 1f, 0.85f);

    [FoldoutGroup("Gizmos")]
    [SerializeField] private float gizmoCellLabelYOffset = 0.04f;

    #endregion

    private readonly Dictionary<AnimalType, BaseAnimal> prefabByType = new();
    private readonly List<BaseAnimal> spawned = new();
    private readonly Dictionary<int, BaseAnimal> spawnedById = new();
    private readonly Dictionary<AnimalType, List<BaseAnimal>> spawnedByType = new();

    private LevelGameConfig config;

    public IReadOnlyList<BaseAnimal> Spawned => spawned;

    private void Awake()
    {
        BuildPrefabMap();
        LoadConfig();
    }

    [Button("TestGenerate")]
    public void TestGenerate()
    {
        BuildPrefabMap();
        LoadConfig();
        Generate();
    }

    [Button("Clear")]
    public void TestClear()
    {
        ClearSpawned();
    }

    public void Generate()
    {
        ClearSpawned();

        if (config == null)
        {
            Debug.LogError("[LevelGenerator] Config is null.");
            return;
        }

        EnsureRoots();

        // 对齐 PathManager
        // AlignPathManager();

        if (config.instances == null || config.instances.Length == 0)
        {
            return;
        }

        for (int i = 0; i < config.instances.Length; i++)
        {
            var inst = config.instances[i];
            var instType = AnimalTypeUtils.Parse(inst.type);

            if (instType == AnimalType.Unknown)
            {
                Debug.LogWarning($"[LevelGenerator] Unknown type='{inst.type}', id={inst.id}.");
                continue;
            }

            if (!prefabByType.TryGetValue(instType, out var prefab) || prefab == null)
            {
                Debug.LogWarning($"[LevelGenerator] Prefab not found for type='{inst.type}', id={inst.id}.");
                continue;
            }

            if (prefab.Type != instType)
            {
                Debug.LogWarning(
                    $"[LevelGenerator] Prefab type mismatch. config='{inst.type}' parsed='{instType}', prefab.Type='{prefab.Type}'. id={inst.id}.");
                continue;
            }

            if (spawnedById.ContainsKey(inst.id))
            {
                Debug.LogWarning($"[LevelGenerator] Duplicate id={inst.id}, skip.");
                continue;
            }

            var facing = DirectionEnumUtility.ParseOrDefault(inst.direction);
            var spawnCellCenterWorld = GetFootprintBottomLeftWorld(inst.row, inst.col, facing, prefab, config.width, config.height);
            Quaternion worldRot = DirectionEnumUtility.ToWorldRotation(facing) * prefab.transform.localRotation;

            this.Log($"index: {i}, worldRot: {worldRot} | {facing}, posX: {inst.col}, posY: {inst.row}; spawnCellCenterWorld: {spawnCellCenterWorld}");

            // 先旋转再定位：无父级写世界旋转，再挂 instancesRoot，最后 pivot 落在左下角格心
            var animal = Instantiate(prefab);
            animal.transform.rotation = worldRot;
            animal.transform.SetParent(instancesRoot, worldPositionStays: true);
            animal.transform.position = spawnCellCenterWorld;

            animal.Init(inst.id, inst.row, inst.col, inst.direction, inst.param);
            animal.SetLevelOwner(this);

            CacheSpawned(animal, instType);
        }
    }

    [ContextMenu("Clear Spawned")]
    public void ClearSpawned()
    {
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null)
            {
                if (Application.isPlaying)
                    Destroy(spawned[i].gameObject);
                else
                    DestroyImmediate(spawned[i].gameObject);
            }
        }

        spawned.Clear();
        spawnedById.Clear();
        spawnedByType.Clear();
        chicks.Clear();
        cdSheeps.Clear();
        _cdStarted = false;
    }

    public bool TryGetSpawnedById(int id, out BaseAnimal animal)
    {
        return spawnedById.TryGetValue(id, out animal) && animal != null;
    }

    public IReadOnlyList<BaseAnimal> GetSpawnedByType(AnimalType type)
    {
        return spawnedByType.TryGetValue(type, out var list) ? list : Array.Empty<BaseAnimal>();
    }

    private void CacheSpawned(BaseAnimal animal, AnimalType type)
    {
        spawned.Add(animal);
        spawnedById[animal.Id] = animal;

        if (!spawnedByType.TryGetValue(type, out var list))
        {
            list = new List<BaseAnimal>();
            spawnedByType[type] = list;
        }

        list.Add(animal);

        if (animal is Chick chick)
            chicks.Add(chick);

        if (animal is CdSheepAnimal cdSheep)
            cdSheeps.Add(cdSheep);
    }

    private void BuildPrefabMap()
    {
        prefabByType.Clear();

        if (animalPrefabsPath == null || animalPrefabsPath.Count == 0)
        {
            return;
        }

        foreach (var kvp in animalPrefabsPath)
        {
            var type = kvp.Key;
            var path = kvp.Value;

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"[LevelGenerator] Prefab path is empty for type '{type}'.");
                continue;
            }

            var prefab = Resources.Load<BaseAnimal>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[LevelGenerator] Failed to load prefab for type '{type}' at path '{path}'.");
                continue;
            }

            if (prefab.Type != type)
            {
                Debug.LogWarning($"[LevelGenerator] Prefab type mismatch. Expected '{type}', prefab.Type='{prefab.Type}' at path '{path}'.");
                continue;
            }

            prefabByType[type] = prefab;
        }
    }

    private void LoadConfig()
    {
        config = null;

        if (levelJson == null || string.IsNullOrEmpty(levelJson.text))
        {
            Debug.LogWarning("[LevelGenerator] levelJson is null/empty.");
            return;
        }

        config = LevelGameConfig.FromJson(levelJson.text);
        if (config == null)
        {
            Debug.LogError("[LevelGenerator] Failed to parse json.");
        }
    }

    /// <summary>
    /// 设置关卡配置（用于动态加载配置）
    /// </summary>
    public void SetConfig(LevelGameConfig gameConfig)
    {
        config = gameConfig;
        if (config == null)
        {
            Debug.LogWarning("[LevelCtrl] SetConfig: gameConfig is null.");
        }
    }

    private bool TryGetConfigDimensions(out int width, out int height)
    {
        width = 0;
        height = 0;

        if (config != null && config.width > 0 && config.height > 0)
        {
            width = config.width;
            height = config.height;
            return true;
        }

        if (levelJson == null || string.IsNullOrEmpty(levelJson.text))
        {
            return false;
        }

        var parsed = LevelGameConfig.FromJson(levelJson.text);
        if (parsed == null || parsed.width <= 0 || parsed.height <= 0)
        {
            return false;
        }

        width = parsed.width;
        height = parsed.height;
        return true;
    }

#if UNITY_EDITOR
    #region Gizmos Editor

    private static GUIStyle s_gridCellLabelStyle;
    private static GUIStyle s_instanceGizmoLabelStyle;

    private void OnDrawGizmos()
    {
        if (!drawGridGizmos)
            return;

        if (!TryGetConfigDimensions(out int width, out int height))
            return;

        DrawGridGizmos(width, height);

        if (drawInstanceGizmos)
            DrawInstanceGizmos(width, height);

        if (drawCurrentPosGizmos)
            DrawCurrentPosGizmos(width, height);
    }

    private void DrawGridGizmos(int width, int height)
    {
        var rootPos = instancesRoot != null ? instancesRoot.position : transform.position;
        float y = rootPos.y + spawnYOffset;

        float halfW = width * 0.5f * cellSize.x;
        float halfH = height * 0.5f * cellSize.y;

        float leftX = rootPos.x - halfW + origin.x;
        float rightX = rootPos.x + halfW + origin.x;
        float topZ = rootPos.z + halfH + origin.y;
        float bottomZ = rootPos.z - halfH + origin.y;

        Gizmos.color = gizmoBorderColor;
        Gizmos.DrawLine(new Vector3(leftX, y, topZ), new Vector3(rightX, y, topZ));
        Gizmos.DrawLine(new Vector3(rightX, y, topZ), new Vector3(rightX, y, bottomZ));
        Gizmos.DrawLine(new Vector3(rightX, y, bottomZ), new Vector3(leftX, y, bottomZ));
        Gizmos.DrawLine(new Vector3(leftX, y, bottomZ), new Vector3(leftX, y, topZ));

        Gizmos.color = gizmoGridColor;

        for (int c = 0; c <= width; c++)
        {
            float x = leftX + c * cellSize.x;
            Gizmos.DrawLine(new Vector3(x, y, topZ), new Vector3(x, y, bottomZ));
        }

        for (int r = 0; r <= height; r++)
        {
            float z = topZ - r * cellSize.y;
            Gizmos.DrawLine(new Vector3(leftX, y, z), new Vector3(rightX, y, z));
        }

        if (drawGridCellLabels)
            DrawGridCellLabels(width, height);
    }

    private void DrawGridCellLabels(int width, int height)
    {
        if (s_gridCellLabelStyle == null)
        {
            s_gridCellLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
            };
        }

        s_gridCellLabelStyle.normal.textColor = gizmoCellLabelColor;

        for (int jsonRow = 0; jsonRow < height; jsonRow++)
        {
            for (int col = 0; col < width; col++)
            {
                Vector3 center = GridToWorld(jsonRow, col, width, height);
                center.y += gizmoCellLabelYOffset;
                int labelRow = jsonRow;
                Handles.Label(center, $"{col},{labelRow}", s_gridCellLabelStyle);
            }
        }
    }

    private void DrawInstanceGizmos(int width, int height)
    {
        if (levelJson == null || string.IsNullOrEmpty(levelJson.text))
        {
            return;
        }

        var parsed = LevelGameConfig.FromJson(levelJson.text);
        if (parsed == null || parsed.instances == null || parsed.instances.Length == 0)
        {
            return;
        }

        BuildPrefabMap();

        if (drawInstanceGizmoLabels)
        {
            if (s_instanceGizmoLabelStyle == null)
            {
                s_instanceGizmoLabelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                };
            }

            s_instanceGizmoLabelStyle.normal.textColor = gizmoCellLabelColor;
        }

        float footprintPrismY = 0.02f;
        Vector3 footprintExtent = new Vector3(
            cellSize.x * 0.88f,
            footprintPrismY,
            cellSize.y * 0.88f);
        Vector3 anchorExtent = new Vector3(
            cellSize.x * 0.96f,
            footprintPrismY * 1.25f,
            cellSize.y * 0.96f);
        float crossHx = cellSize.x * 0.42f;
        float crossHz = cellSize.y * 0.42f;
        float labelDy = gizmoCellLabelYOffset + 0.06f;

        for (int i = 0; i < parsed.instances.Length; i++)
        {
            var inst = parsed.instances[i];
            var instType = AnimalTypeUtils.Parse(inst.type);

            Vector3 anchorWorld = GridToWorld(inst.row, inst.col, width, height);
            var facing = DirectionEnumUtility.ParseOrDefault(inst.direction);

            if (instType != AnimalType.Unknown && prefabByType.TryGetValue(instType, out var p) && p != null)
            {
                Vector3 bottomLeftWorld = GridToWorld(inst.row, inst.col, width, height);

                if (drawFootprintCellsGizmo)
                    DrawFootprintOccupiedWireGizmo(inst.row, inst.col, facing, p, width, height, footprintExtent, inst.row, inst.col);

                Gizmos.color = gizmoAnchorCellColor;
                Gizmos.DrawWireCube(anchorWorld, anchorExtent);

                // 左下角格：与 GetFootprintBottomLeftWorld / 生成 pivot 一致（加粗线框 + 黄球十字）
                Gizmos.color = gizmoBottomLeftCellWireColor;
                Vector3 bottomLeftWireExtent = new Vector3(
                    footprintExtent.x * 1.06f,
                    footprintExtent.y * 1.75f,
                    footprintExtent.z * 1.06f);
                Gizmos.DrawWireCube(bottomLeftWorld, bottomLeftWireExtent);

                Gizmos.color = gizmoInstanceColor;
                Gizmos.DrawSphere(bottomLeftWorld, gizmoInstanceRadius);
                Gizmos.DrawLine(bottomLeftWorld + new Vector3(-crossHx, 0f, 0f), bottomLeftWorld + new Vector3(crossHx, 0f, 0f));
                Gizmos.DrawLine(bottomLeftWorld + new Vector3(0f, 0f, -crossHz), bottomLeftWorld + new Vector3(0f, 0f, crossHz));

                if (drawInstanceGizmoLabels)
                {
                    Handles.Label(
                           anchorWorld + Vector3.up * labelDy,
                           $"锚 = 左下角（生成）\n({inst.col},{inst.row})",
                           s_instanceGizmoLabelStyle);
                }
            }
            else
            {
                Gizmos.color = gizmoAnchorCellColor;
                Gizmos.DrawWireCube(anchorWorld, anchorExtent);

                Vector3 bottomLeftWorld = anchorWorld;
                Gizmos.color = gizmoBottomLeftCellWireColor;
                Gizmos.DrawWireCube(
                    bottomLeftWorld,
                    new Vector3(footprintExtent.x * 1.06f, footprintExtent.y * 1.75f, footprintExtent.z * 1.06f));

                Gizmos.color = gizmoInstanceColor;
                Gizmos.DrawSphere(bottomLeftWorld, gizmoInstanceRadius);
                Gizmos.DrawLine(bottomLeftWorld + new Vector3(-crossHx, 0f, 0f), bottomLeftWorld + new Vector3(crossHx, 0f, 0f));
                Gizmos.DrawLine(bottomLeftWorld + new Vector3(0f, 0f, -crossHz), bottomLeftWorld + new Vector3(0f, 0f, crossHz));

                if (drawInstanceGizmoLabels)
                {
                    Handles.Label(
                        anchorWorld + Vector3.up * labelDy,
                        $"锚 = 左下角（生成）\n({inst.col},{inst.row}) · 无 prefab",
                        s_instanceGizmoLabelStyle);
                }
            }
        }
    }

    /// <param name="skipRow">与 <paramref name="skipCol"/> 同时为占用格索引时，跳过该格线框（由左下角强调框单独绘制）。无跳过时设为 -1。</param>
    /// <param name="skipCol">见 <paramref name="skipRow"/>。</param>
    private void DrawFootprintOccupiedWireGizmo(
        int anchorRow,
        int anchorCol,
        DirectionEnum facing,
        BaseAnimal prefab,
        int gridW,
        int gridH,
        Vector3 cellExtent,
        int skipRow = -1,
        int skipCol = -1)
    {
        Gizmos.color = gizmoFootprintCellWireColor;
        foreach (var o in GetWorldFootprintOffsets(prefab, facing))
        {
            int gr = anchorRow + o.y;
            int gc = anchorCol + o.x;
            if (gr < 0 || gc < 0 || gr >= gridH || gc >= gridW)
                continue;
            if (gr == skipRow && gc == skipCol)
                continue;
            Gizmos.DrawWireCube(GridToWorld(gr, gc, gridW, gridH), cellExtent);
        }
    }

    private void DrawCurrentPosGizmos(int width, int height)
    {
        // 只使用 CurrentPos，不从 transform 反推格子。
        if (spawned == null || spawned.Count == 0)
            return;

        if (drawInstanceGizmoLabels && s_instanceGizmoLabelStyle == null)
        {
            s_instanceGizmoLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
            };
        }

        if (s_instanceGizmoLabelStyle != null)
            s_instanceGizmoLabelStyle.normal.textColor = gizmoCurrentPosColor;

        Vector3 currentPosExtent = new Vector3(cellSize.x * 0.55f, 0.03f, cellSize.y * 0.55f);
        float labelDy = gizmoCellLabelYOffset + 0.08f;

        for (int i = 0; i < spawned.Count; i++)
        {
            var animal = spawned[i];
            if (animal == null)
                continue;

            int row = animal.CurrentPos.y;
            int col = animal.CurrentPos.x;
            Vector3 world = GridToWorld(row, col, width, height);
            world.y += gizmoCurrentPosYOffset;

            Gizmos.color = gizmoCurrentPosColor;
            Gizmos.DrawWireCube(world, currentPosExtent);
            Gizmos.DrawSphere(world, gizmoInstanceRadius * 0.55f);

            if (drawInstanceGizmoLabels && s_instanceGizmoLabelStyle != null)
                Handles.Label(world + Vector3.up * labelDy, $"CurrentPos ({col},{row})", s_instanceGizmoLabelStyle);
        }
    }

    #endregion
#endif

    private void EnsureRoots()
    {
        if (instancesRoot != null)
        {
            return;
        }

        var go = new GameObject("InstancesRoot");
        go.transform.SetParent(transform, false);
        instancesRoot = go.transform;
    }

    /// <summary>
    /// 对齐 PathManager 到当前关卡的网格设置
    /// </summary>
    private void AlignPathManager()
    {
        if (pathManager == null) return;
        if (!TryGetConfigDimensions(out int width, out int height)) return;

        // 使 PathManager 的位置和设置与 LevelCtrl 一致
        // pathManager.transform.position = transform.position;

        // 计算 originOffset 使网格对齐
        // LevelCtrl 的 GridToWorld: leftX = rootPos.x - halfW + origin.x
        // PathManager 的 _gridOrigin: leftX = rootPos.x - halfW + originOffset.x
        // 所以我们需要 originOffset = origin

        // 通过反射或直接设置（暂时用简单方法）
        /*         Debug.Log($"[LevelCtrl] AlignPathManager: 请手动设置 PathManager 的 Origin Offset 为 ({origin.x}, {origin.y})");
                Debug.Log($"[LevelCtrl] 当前 PathManager GridOrigin={pathManager.GridOrigin}"); */

        // 尝试自动对齐
        // pathManager.AlignToGrid(transform.position, cellSize.x, width, height);
    }

    /// <summary>
    /// 单格 (row,col) 中心点的世界坐标（格子顶点为边界，中心 = 格中心）。
    /// </summary>
    private Vector3 GridToWorld(int row, int col, int width, int height)
    {
        var rootPos = instancesRoot != null ? instancesRoot.position : transform.position;

        float halfW = width * 0.5f * cellSize.x;
        float halfH = height * 0.5f * cellSize.y;

        float leftX = rootPos.x - halfW;
        float topZ = rootPos.z + halfH;
        float bottomZ = rootPos.z - halfH;

        float x = leftX + ((col + 0.5f) * cellSize.x) + origin.x;
        float z = (bottomZ + ((row + 0.5f) * cellSize.y)) + origin.y;

        float y = rootPos.y + spawnYOffset;
        return new Vector3(x, y, z);
    }

    private static Vector2Int RotateOffset(Vector2Int offset, DirectionEnum facing)
    {
        return facing switch
        {
            // 以左下角格为锚点：默认 footprint 朝 Up；Down 为 180°。
            DirectionEnum.Down => new Vector2Int(-offset.x, -offset.y),
            DirectionEnum.Up => offset,
            DirectionEnum.Left => new Vector2Int(-offset.y, offset.x),
            DirectionEnum.Right => new Vector2Int(offset.y, -offset.x),
            _ => offset,
        };
    }

    private IEnumerable<Vector2Int> GetWorldFootprintOffsets(BaseAnimal prefab, DirectionEnum facing)
    {
        yield return Vector2Int.zero;

        var extras = prefab.FootprintSizeCells;
        if (extras == null)
            yield break;

        for (int i = 0; i < extras.Count; i++)
        {
            var off = extras[i];
            if (off == Vector2Int.zero)
                continue;
            yield return footprintFollowsFacing ? RotateOffset(off, facing) : off;
        }
    }

    private Vector3 GetFootprintBottomLeftWorld(
        int anchorRow,
        int anchorCol,
        DirectionEnum facing,
        BaseAnimal prefab,
        int gridW,
        int gridH)
    {
        return GridToWorld(anchorRow, anchorCol, gridW, gridH);
    }
}

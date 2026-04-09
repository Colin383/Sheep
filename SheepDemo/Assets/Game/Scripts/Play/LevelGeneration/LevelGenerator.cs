using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 读取 LevelGameConfig，在 XZ 平面按格子生成实体并缓存。
/// 约定：json 中每个 instance 的 (row,col) 为该对象 footprint 的<strong>左上角</strong>格；
/// 目标对齐点为 footprint 在世界空间中的<strong>几何中心</strong>。
/// Prefab 的 pivot 往往不在模型几何中心（尤其左右朝向下更明显），开启 <see cref="alignSpawnToRendererBoundsCenter"/> 时
/// 会在生成后平移物体，使<strong>子节点 Renderer 包围盒世界中心</strong>落在该目标点上。
/// </summary>
public class LevelGenerator : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private TextAsset levelJson;

    [Header("Grid")]
    [SerializeField] private Vector2 cellSize = Vector2.one;
    [SerializeField] private Vector2 origin = Vector2.zero;
    [SerializeField] private float spawnYOffset = 0f;
    [SerializeField] private bool row0IsTop = true;

    [Header("Animal Prefabs")]
    [SerializeField] private BaseAnimal[] animalPrefabs;

    [Header("Parents")]
    [SerializeField] private Transform instancesRoot;

    [Header("Spawn")]
    [Tooltip("生成后平移物体，使所有子 Renderer 的合并 bounds.center 落在关卡计算出的格中心（pivot 可在尾部等任意处）")]
    [SerializeField] private bool alignSpawnToRendererBoundsCenter = true;

    [Header("Gizmos")]
    [SerializeField] private bool drawGridGizmos = true;
    [SerializeField] private bool drawInstanceGizmos = true;
    [SerializeField] private Color gizmoGridColor = new Color(0f, 0.8f, 1f, 0.7f);
    [SerializeField] private Color gizmoBorderColor = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] private Color gizmoInstanceColor = new Color(1f, 0.85f, 0.2f, 0.9f);
    [SerializeField] private float gizmoInstanceRadius = 0.08f;

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

            var pos = GetFootprintCenterWorld(inst.row, inst.col, prefab, config.width, config.height);
            // 世界朝向往：在 XZ 平面上朝 grid 的 up/right/down/left；先套美术在 prefab 根上的 localRotation，再挂到父节点并保持世界变换（避免父级旋转/缩放导致「某一朝向」偏移）
            Quaternion worldRot = DirectionToRotation(inst.direction) * prefab.transform.localRotation;
            var animal = Instantiate(prefab, pos, worldRot);
            animal.transform.SetParent(instancesRoot, worldPositionStays: true);

            animal.Init(inst.id, inst.row, inst.col, inst.direction);

            if (alignSpawnToRendererBoundsCenter)
                AlignSpawnToRendererBoundsCenter(animal.transform, pos);

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
    }

    private void BuildPrefabMap()
    {
        prefabByType.Clear();

        if (animalPrefabs == null || animalPrefabs.Length == 0)
        {
            return;
        }

        for (int i = 0; i < animalPrefabs.Length; i++)
        {
            var prefab = animalPrefabs[i];
            if (prefab == null)
            {
                continue;
            }

            var type = prefab.Type;
            if (type == AnimalType.Unknown)
            {
                Debug.LogWarning($"[LevelGenerator] Prefab '{prefab.name}' has Unknown type, skip.");
                continue;
            }

            if (prefabByType.TryGetValue(type, out var existed) && existed != null)
            {
                Debug.LogWarning($"[LevelGenerator] Duplicate prefab type '{type}'. '{existed.name}' will be replaced by '{prefab.name}'.");
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
    private void OnDrawGizmos()
    {
        if (!drawGridGizmos)
            return;

        if (!TryGetConfigDimensions(out int width, out int height))
            return;

        DrawGridGizmos(width, height);

        if (drawInstanceGizmos)
            DrawInstanceGizmos(width, height);
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

        Gizmos.color = gizmoInstanceColor;

        for (int i = 0; i < parsed.instances.Length; i++)
        {
            var inst = parsed.instances[i];
            var instType = AnimalTypeUtils.Parse(inst.type);
            if (instType != AnimalType.Unknown && prefabByType.TryGetValue(instType, out var p) && p != null)
            {
                var pos = GetFootprintCenterWorld(inst.row, inst.col, p, width, height);
                Gizmos.DrawSphere(pos, gizmoInstanceRadius);
            }
            else
            {
                var pos = GridToWorld(inst.row, inst.col, width, height);
                Gizmos.DrawSphere(pos, gizmoInstanceRadius);
            }
        }
    }
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
        float z = (row0IsTop
                ? (topZ - ((row + 0.5f) * cellSize.y))
                : (bottomZ + ((row + 0.5f) * cellSize.y)))
            + origin.y;

        float y = rootPos.y + spawnYOffset;
        return new Vector3(x, y, z);
    }

    /// <summary>
    /// (anchorRow, anchorCol) 为 footprint 左上角；取覆盖 w 列、h 行后的矩形中心。
    /// </summary>
    private Vector3 GetFootprintCenterWorld(int anchorRow, int anchorCol, BaseAnimal prefab, int gridW, int gridH)
    {
        var fs = prefab.FootprintSizeCells;
        int w = Mathf.Max(1, fs.x);
        int h = Mathf.Max(1, fs.y);

        if (anchorRow < 0 || anchorCol < 0 || anchorRow + h > gridH || anchorCol + w > gridW)
        {
            Debug.LogWarning(
                $"[LevelGenerator] footprint 越界：anchor=({anchorRow},{anchorCol})，占用 {w}×{h}，地图 {gridW}×{gridH}，prefab={prefab.name}");
        }

        Vector3 a = GridToWorld(anchorRow, anchorCol, gridW, gridH);
        Vector3 b = GridToWorld(anchorRow + h - 1, anchorCol + w - 1, gridW, gridH);
        return (a + b) * 0.5f;
    }

    /// <summary>
    /// 平移 root，使子层级合并后的 Renderer.bounds.center 与目标世界点重合（用于 pivot 不在视觉中心时对齐格子）。
    /// </summary>
    private static void AlignSpawnToRendererBoundsCenter(Transform root, Vector3 targetWorldCenter)
    {
        if (!TryGetCombinedRendererBounds(root, out Bounds worldBounds))
            return;

        Vector3 delta = targetWorldCenter - worldBounds.center;
        root.position += delta;
    }

    private static bool TryGetCombinedRendererBounds(Transform root, out Bounds worldBounds)
    {
        worldBounds = default;
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
            return false;

        int start = 0;
        while (start < renderers.Length && renderers[start] is ParticleSystemRenderer)
            start++;
        if (start >= renderers.Length)
            return false;

        worldBounds = renderers[start].bounds;
        for (int i = start + 1; i < renderers.Length; i++)
        {
            if (renderers[i] is ParticleSystemRenderer)
                continue;
            worldBounds.Encapsulate(renderers[i].bounds);
        }

        return true;
    }

    /// <summary>
    /// 地图含义：up = 世界 +Z，down = -Z，right = +X，left = -X（Y 轴朝上）。与 <see cref="GridToWorld"/> 一致。
    /// </summary>
    private static Quaternion DirectionToRotation(string direction)
    {
        if (string.IsNullOrEmpty(direction))
            return Quaternion.identity;

        Vector3 forward;
        switch (direction.Trim().ToLowerInvariant())
        {
            case "up":
                forward = Vector3.forward;
                break;
            case "down":
                forward = Vector3.back;
                break;
            case "right":
                forward = Vector3.right;
                break;
            case "left":
                forward = Vector3.left;
                break;
            default:
                return Quaternion.identity;
        }

        return Quaternion.LookRotation(forward, Vector3.up);
    }
}


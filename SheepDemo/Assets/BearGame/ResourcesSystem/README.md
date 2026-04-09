# Bear ResourceSystem 模块文档

## 概述

Bear ResourceSystem 是一个灵活的资源管理模块，用于 Unity 游戏开发。该模块支持多种资源加载策略，包括 Unity 内置的 Resources 系统和 YooAsset 资源包系统，同时提供同步和异步加载方式。

**版本**: 0.0.1  
**Unity 版本**: 6000.0.59f2  
**命名空间**: `Bear.ResourceSystem`

## 目录结构

```
ResourcesSystem/
├── Runtime/                    # 运行时核心代码
│   ├── Core/                  # 核心接口和实现
│   │   ├── IResourceLoader.cs              # 资源加载器接口（基础）
│   │   ├── IResourceLoader.UniTask.cs      # 资源加载器接口（UniTask 扩展）
│   │   ├── ResourceManager.cs              # 资源管理器主类（基础）
│   │   └── ResourceManager.UniTask.cs      # 资源管理器（UniTask 扩展）
│   ├── Loaders/               # 加载器实现
│   │   ├── ResourcesLoader.cs              # Resources 加载策略（基础）
│   │   └── ResourcesLoader.UniTask.cs      # Resources 加载策略（UniTask 扩展）
│   ├── External/              # 外部库适配
│   │   ├── YooAssetLoader.cs               # YooAsset 加载策略（基础）
│   │   └── YooAssetLoader.UniTask.cs       # YooAsset 加载策略（UniTask 扩展）
│   ├── Utils/                 # 工具类
│   │   └── ConditionalWeakTable.cs         # 弱引用表
│   ├── ResourceSystemInitializer.cs        # 初始化器（基础）
│   ├── ResourceSystemInitializer.UniTask.cs# 初始化器（UniTask 扩展）
│   └── com.bear.resourcesystem.asmdef      # 程序集定义
├── package.json               # 包配置
├── CHANGELOG.md               # 更新日志
└── README.md                  # 本文档
```

## 模块架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                        ResourceManager                          │
│                   (资源管理器 - 统一入口)                         │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  - 加载器管理 (优先级排序)                                │   │
│  │  - 资源缓存 (线程安全)                                    │   │
│  │  - 同步加载 Load<T>()                                    │   │
│  │  - 异步加载 LoadAsync<T>()     [需要 UniTask]            │   │
│  │  - 预加载 PreloadAsync<T>()    [需要 UniTask]            │   │
│  │  - 资源释放 Release()                                     │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┼───────────────┐
              │               │               │
              ▼               ▼               ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│  IResourceLoader │  │  IResourceLoader │  │  IResourceLoader │
│     接口定义      │  │   UniTask 扩展   │  │  (自定义加载器)  │
└─────────────────┘  └─────────────────┘  └─────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        │                     │                     │
        ▼                     ▼                     ▼
┌───────────────┐     ┌───────────────┐     ┌───────────────┐
│ ResourcesLoader│     │ YooAssetLoader │     │  CustomLoader  │
│               │     │               │     │               │
│ 基础优先级: 100 │     │  高优先级: 10  │     │   自定义优先级  │
│ 总是可用       │     │ 需要 YooAsset  │     │               │
│ Resources.Load │     │ 包已初始化      │     │               │
└───────────────┘     └───────────────┘     └───────────────┘
        │                     │
        └─────────────────────┘
                  │
                  ▼
        ┌─────────────────┐
        │  条件编译符号     │
        │  UNITASK         │ → 启用 UniTask 异步支持
        │  YOOASSET        │ → 启用 YooAsset 支持
        └─────────────────┘
```

## 核心组件

### 1. ResourceManager（资源管理器）

**位置**: `Runtime/Core/ResourceManager.cs` (基础)  
**位置**: `Runtime/Core/ResourceManager.UniTask.cs` (UniTask 扩展)

资源管理器的核心控制器，负责统一管理所有资源加载策略。

#### 主要功能

- **加载器管理**: 注册、注销多个加载器，按优先级排序
- **资源缓存**: 线程安全的资源缓存机制
- **同步加载**: 通过 `Load<T>()` 方法同步加载资源
- **异步加载**: 通过 `LoadAsync<T>()` 方法异步加载资源（需要 UniTask）
- **预加载**: 支持单个和批量预加载
- **资源释放**: 支持按路径、按实例或全部释放

#### 关键方法

```csharp
// 获取单例实例
public static ResourceManager Instance { get; }

// 注册/注销加载器
public void RegisterLoader(IResourceLoader loader)
public void UnregisterLoader(string loaderName)

// 同步加载
public T Load<T>(string path) where T : UnityEngine.Object

// 异步加载（需要定义 UNITASK）
public async UniTask<T> LoadAsync<T>(string path, Action<float> onProgress = null, CancellationToken cancellationToken = default)

// 异步加载并实例化（需要 UniTask）
public async UniTask<T> LoadAndInstantiateAsync<T>(string path, Transform parent = null, Action<float> onProgress = null, CancellationToken cancellationToken = default) where T : Component

// 预加载（需要 UniTask）
public async UniTask<bool> PreloadAsync<T>(string path)
public async UniTask PreloadBatchAsync<T>(IEnumerable<string> paths, Action<float> onProgress = null)

// 资源释放
public void Release(string path)
public void ReleaseInstance(UnityEngine.Object instance)
public void ReleaseAll()

// 查询
public bool IsLoaded(string path)
public IReadOnlyCollection<string> GetLoadedAssets()
```

### 2. IResourceLoader（资源加载器接口）

**位置**: `Runtime/Core/IResourceLoader.cs` (基础)  
**位置**: `Runtime/Core/IResourceLoader.UniTask.cs` (UniTask 扩展)

定义资源加载策略的接口，所有自定义加载器都需要实现此接口。

#### 接口成员

```csharp
// 基础接口 (IResourceLoader.cs)
string LoaderName { get; }      // 加载器名称
int Priority { get; }           // 优先级（数字越小优先级越高）
bool IsAvailable { get; }       // 是否可用

T Load<T>(string path) where T : UnityEngine.Object;
void Release(string path);
void ReleaseAll();

// UniTask 扩展 (IResourceLoader.UniTask.cs) - 需要 UNITASK
UniTask<T> LoadAsync<T>(string path, Action<float> onProgress, CancellationToken cancellationToken) where T : UnityEngine.Object;
UniTask<bool> PreloadAsync<T>(string path) where T : UnityEngine.Object;
```

### 3. ResourcesLoader（Resources 加载策略）

**位置**: `Runtime/Loaders/ResourcesLoader.cs` (基础)  
**位置**: `Runtime/Loaders/ResourcesLoader.UniTask.cs` (UniTask 扩展)

使用 Unity 内置 Resources 系统的加载策略，作为兜底方案。

#### 特点

- 总是可用（`IsAvailable = true`）
- 优先级较低（默认 100）
- 支持基础路径配置
- 资源释放需要通过 `Resources.UnloadUnusedAssets()`

### 4. YooAssetLoader（YooAsset 加载策略）

**位置**: `Runtime/External/YooAssetLoader.cs` (基础)  
**位置**: `Runtime/External/YooAssetLoader.UniTask.cs` (UniTask 扩展)

适配 YooAsset 资源包系统的加载策略（需要定义 YOOASSET 或 YOOASSET_ENABLED）。

#### 特点

- 需要 YooAsset 包已初始化
- 优先级较高（默认 10）
- 支持 Handle 缓存（UniTask 版本）
- 支持异步加载

## 使用示例

### 1. 初始化资源系统

```csharp
using Bear.ResourceSystem;

public class GameLauncher : MonoBehaviour
{
    void Start()
    {
        // 基础初始化（仅使用 Resources）
        ResourceSystemInitializer.Initialize(useYooAsset: false);
        
        // 或启用 YooAsset（需要 YooAsset 包已安装并初始化）
        // ResourceSystemInitializer.Initialize(useYooAsset: true);
    }
}
```

### 2. 同步加载资源

```csharp
// 加载 Sprite
Sprite icon = ResourceManager.Instance.Load<Sprite>("Icons/PlayerIcon");

// 加载 GameObject
GameObject prefab = ResourceManager.Instance.Load<GameObject>("Prefabs/Player");

// 加载 AudioClip
AudioClip bgm = ResourceManager.Instance.Load<AudioClip>("Audio/BGM/MainTheme");
```

### 3. 异步加载资源（需要 UniTask）

```csharp
using Cysharp.Threading.Tasks;

// 基础异步加载
Sprite icon = await ResourceManager.Instance.LoadAsync<Sprite>("Icons/PlayerIcon");

// 带进度回调
Sprite icon = await ResourceManager.Instance.LoadAsync<Sprite>(
    "Icons/PlayerIcon",
    progress => Debug.Log($"Loading: {progress:P}")
);

// 带取消令牌
var cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromSeconds(10)); // 10秒超时
Sprite icon = await ResourceManager.Instance.LoadAsync<Sprite>(
    "Icons/PlayerIcon",
    cancellationToken: cts.Token
);
```

### 4. 异步加载并实例化（需要 UniTask）

```csharp
// 加载并获取组件
PlayerController player = await ResourceManager.Instance
    .LoadAndInstantiateAsync<PlayerController>("Prefabs/Player", parentTransform);

// 加载 UI
GameObject uiPanel = await ResourceManager.Instance
    .LoadAndInstantiateAsync<GameObject>("UI/MainMenu", uiCanvas);
```

### 5. 预加载资源（需要 UniTask）

```csharp
// 单个预加载
bool success = await ResourceManager.Instance.PreloadAsync<Sprite>("Icons/PlayerIcon");
if (success)
{
    Debug.Log("预加载成功");
}

// 批量预加载
var paths = new[] { "Icons/Icon1", "Icons/Icon2", "Icons/Icon3" };
await ResourceManager.Instance.PreloadBatchAsync<Sprite>(paths, progress =>
{
    Debug.Log($"Preloading: {progress:P}");
});
```

### 6. 释放资源

```csharp
// 按路径释放（从缓存中移除）
ResourceManager.Instance.Release("Icons/PlayerIcon");

// 按实例释放（销毁实例并清理引用）
GameObject instance = Instantiate(prefab);
// ... 使用实例 ...
ResourceManager.Instance.ReleaseInstance(instance);

// 释放所有资源（场景切换时使用）
ResourceManager.Instance.ReleaseAll();
```

### 7. 自定义加载器

```csharp
using Bear.ResourceSystem;

public class CustomLoader : IResourceLoader
{
    public string LoaderName => "CustomLoader";
    public int Priority { get; set; } = 50;
    public bool IsAvailable => true;

    private readonly string _rootPath;
    
    public CustomLoader(string rootPath)
    {
        _rootPath = rootPath;
    }

    public T Load<T>(string path) where T : UnityEngine.Object
    {
        // 自定义加载逻辑
        string fullPath = $"{_rootPath}/{path}";
        // ... 加载逻辑 ...
        return null;
    }

    public void Release(string path) 
    { 
        // 自定义释放逻辑
    }
    
    public void ReleaseAll() 
    { 
        // 释放所有资源
    }
}

// 注册自定义加载器
ResourceManager.Instance.RegisterLoader(new CustomLoader("CustomAssets"));
```

### 8. 与 UI 模块集成

```csharp
using Bear.UI;
using Bear.ResourceSystem;

// 使用 ResourceSystem 作为 UI 加载器
public class UIManager : MonoBehaviour
{
    void Start()
    {
        // 初始化资源系统
        ResourceSystemInitializer.Initialize();
        
        // 设置 UI 加载器
        var uiLoader = new ResourceSystemUILoader();
        UIManager.Instance.SetLoader(uiLoader);
    }
}

// 加载 UI
UIManager.Instance.OpenUI("MainMenu");
UIManager.Instance.OpenUIAsync("Settings", onComplete: panel =>
{
    Debug.Log("Settings panel loaded");
});
```

## 条件编译符号

| 符号 | 说明 | 自动检测 |
|------|------|----------|
| `UNITASK` | 启用 UniTask 异步加载功能 | ✓ (通过 versionDefines) |
| `YOOASSET` / `YOOASSET_ENABLED` | 启用 YooAsset 加载策略 | ✗ (需手动定义) |

### 自动配置说明

从 v0.0.2 版本开始，UniTask 支持通过 `versionDefines` 自动配置：

```json
// com.bear.resourcesystem.asmdef
{
    "versionDefines": [
        {
            "name": "com.cysharp.unitask",
            "expression": "",
            "define": "UNITASK"
        }
    ]
}
```

当 UniTask 包安装后，`UNITASK` 符号会自动定义，无需手动配置。

### 手动配置（可选）

如需手动配置符号：
1. Edit → Project Settings → Player
2. Other Settings → Scripting Define Symbols
3. 添加需要的符号

## 工作流程

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   初始化     │ ──→ │   加载资源   │ ──→ │   使用资源   │
│             │     │             │     │             │
│ Initialize()│     │ Load()      │     │ Instantiate │
│ 注册加载器   │     │ LoadAsync() │     │ 直接使用    │
└─────────────┘     └─────────────┘     └──────┬──────┘
                                               │
                                               ▼
                                        ┌─────────────┐
                                        │   释放资源   │
                                        │             │
                                        │ Release()   │
                                        │ ReleaseAll()│
                                        └─────────────┘
```

### 详细流程

1. **初始化阶段**
   - 调用 `ResourceSystemInitializer.Initialize()`
   - 系统按优先级注册可用的加载器
   - 优先级数字越小，优先级越高（YooAsset: 10, Resources: 100）

2. **加载阶段**
   - 调用 `Load<T>()` 或 `LoadAsync<T>()`
   - 系统优先检查缓存（线程安全）
   - 使用优先级最高的可用加载器加载资源
   - 自动缓存加载的资源

3. **使用阶段**
   - 直接使用加载的资源
   - 或通过 `LoadAndInstantiateAsync()` 实例化后使用
   - 实例与路径的映射会被自动记录

4. **释放阶段**
   - 调用 `Release()` 释放指定路径的资源
   - 调用 `ReleaseInstance()` 销毁实例并清理引用
   - 场景切换时调用 `ReleaseAll()` 清理所有资源

## 架构特点

- **多策略支持**: 支持多种资源加载策略，可按优先级自动选择
- **线程安全**: 所有对外接口可在任意线程调用
- **防止重复加载**: 自动检测并复用正在进行的加载任务
- **资源缓存**: 自动缓存已加载资源，提高重复访问性能
- **弱引用映射**: 使用 ConditionalWeakTable 管理实例到路径的映射
- **可选依赖**: UniTask 和 YooAsset 为可选依赖，按需启用
- **Partial 类设计**: UniTask 功能通过 partial 类分离，代码结构清晰

## 性能优化建议

1. **预加载常用资源**: 在游戏启动或场景切换时预加载常用资源
2. **合理使用缓存**: 频繁使用的资源保持缓存，不频繁使用的及时释放
3. **异步加载大资源**: 大资源（如场景、高清贴图）使用异步加载避免卡顿
4. **批量加载**: 使用 `PreloadBatchAsync` 批量预加载相关资源
5. **取消无用加载**: 使用 `CancellationToken` 取消不再需要的加载任务

## 注意事项

1. 确保在首次使用资源系统前调用 `ResourceSystemInitializer.Initialize()`
2. 异步方法需要 UniTask 包，会自动通过 versionDefines 启用
3. YooAsset 支持需要手动定义 `YOOASSET` 或 `YOOASSET_ENABLED` 符号
4. Resources 加载的资源需要通过 `Resources.UnloadUnusedAssets()` 释放
5. 建议在游戏启动时预加载常用资源，避免运行时卡顿

## 更新日志

详见 [CHANGELOG.md](./CHANGELOG.md)

---

**最后更新**: 2026-03-31  
**维护者**: Bear Game

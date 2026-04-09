# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.0.1] - 2026-03-31

### Added
- 初始版本发布
- 资源管理器核心功能
  - 支持多加载器策略（优先级管理）
  - 线程安全的资源缓存
  - 防止重复加载机制
- Resources 加载策略（兜底方案）
- YooAsset 加载策略（可选扩展）
- UniTask 异步加载支持（可选）
- 资源预加载和批量预加载
- 资源释放管理

### Features
- **同步加载**: `Load<T>(string path)`
- **异步加载**: `LoadAsync<T>(string path, Action<float> onProgress, CancellationToken cancellationToken)`
- **加载并实例化**: `LoadAndInstantiateAsync<T>(string path, Transform parent)`
- **预加载**: `PreloadAsync<T>(string path)` 和 `PreloadBatchAsync<T>(IEnumerable<string> paths)`
- **资源释放**: `Release(string path)`, `ReleaseInstance(Object instance)`, `ReleaseAll()`
- **查询接口**: `IsLoaded(string path)`, `GetLoadedAssets()`

### Dependencies
- Unity 6000.0.59f2 或更高版本
- UniTask (可选): 用于异步加载功能
- YooAsset (可选): 用于资源包加载

### Notes
- 定义 `UNITASK_ENABLED` 或 `UNITASK` 启用 UniTask 支持
- 定义 `YOOASSET` 或 `YOOASSET_ENABLED` 启用 YooAsset 支持

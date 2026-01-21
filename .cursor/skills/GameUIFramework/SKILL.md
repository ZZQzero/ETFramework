---
name: game-ui-framework
description: 基于ET框架和YooAsset的UI管理系统，提供完整的UI生命周期管理、层级控制、缓存机制和异步加载功能。
---

# GameUIFramework - UI管理系统

## 📋 概述

GameUIFramework 是基于 ET 框架和 YooAsset 的 UI 管理系统，提供 UI 生命周期管理、层级控制、缓存机制和异步加载功能。

**技术栈**：Unity + ET框架 + YooAsset + UniTask

---

## 🏗️ 核心架构

### 1. UI层级系统 (EGameUILayer)

```csharp
public enum EGameUILayer
{
    Main = 0,      // 主界面层
    Normal,        // 普通界面层
    Popup,         // 弹窗层
    Tips,          // 提示层
    Loading,       // 加载层
    SystemNotify,  // 系统提示
    Mask,          // UI遮罩层
}
```

### 2. UI显示模式 (EGameUIMode)

```csharp
public enum EGameUIMode
{
    Normal,             // 普通
    HideOther,         // 隐藏其他
    ReverseChange,     // 反向切换
}
```

### 3. 核心数据结构

```csharp
// UI实例管理
Dictionary<string, GameUIBase> _allOpenGameUIDic       // 已打开UI
Dictionary<string, GameUIBase> _allCloseGameUIDic      // 已关闭UI
Dictionary<string, AssetHandle> _allAssetHandleDic     // 资源句柄

// 层级和栈管理
Dictionary<int, Transform> _uiLayerDic                 // UI层级容器
Dictionary<int, Stack<GameUIBase>> _reverseUIStack      // 反向切换栈
Dictionary<int, string> _finallyOpenUINameByLayer      // 每层最后打开UI

// 状态控制
HashSet<string> _loadingUIHash                         // 正在加载的UI
HashSet<string> _notCloseUIFilterSet                   // 不关闭UI过滤器
```

---

## 🚀 核心功能

### 1. UI生命周期管理

#### 打开UI (OpenUI)
```csharp
public async UniTask OpenUI(string uiName, object data)
```

**执行流程**：
1. 参数校验：检查UI名称和初始化状态
2. 加载防重：防止同一UI重复加载
3. 缓存复用：优先从关闭缓存恢复UI
4. 数据更新：如果UI已打开，仅更新数据
5. 异步加载：从资源包异步加载新UI
6. 初始化设置：设置层级、模式、生命周期回调

#### 关闭UI (CloseUI)
```csharp
public void CloseUI(string uiName)
```

**执行流程**：
1. 状态检查：确认UI已打开
2. 反向栈处理：处理反向切换逻辑
3. 生命周期回调：调用OnCloseUI()
4. 缓存管理：移至关闭缓存
5. 层级清理：清理最后打开UI标记

#### 销毁UI (CloseAndDestroyUI)
```csharp
public void CloseAndDestroyUI(string uiName)
```

**执行流程**：
1. 强制清理：清理反向栈和缓存
2. 生命周期回调：调用OnDestroyUI()
3. 资源释放：销毁GameObject，释放AssetHandle
4. 状态清理：移除所有相关记录

### 2. 缓存机制

#### 缓存策略
- 关闭缓存：关闭的UI进入缓存池，可快速恢复
- 资源缓存：AssetHandle缓存，避免重复加载

#### 缓存清理 (ClearClosedUICache)
```csharp
public void ClearClosedUICache()
```

### 3. 反向切换系统

#### 栈管理
- 每个层级维护独立的UI栈
- ReverseChange模式UI关闭时自动恢复上一个UI

#### 实现方法
```csharp
private void ReverseStackPush(GameUIBase uiBase)  // 压栈
private void ReverseStackPop(GameUIBase uiBase)   // 出栈
```

### 4. 异步加载系统

#### YooAsset集成
```csharp
AssetHandle handle = _package.LoadAssetAsync(uiName);
await handle;
```

**加载控制**：
- 并发控制：_loadingUIHash防止重复加载
- 错误处理：完整的异常捕获和资源清理
- 状态同步：加载完成后自动设置UI状态

---

## 🎮 主要API接口

### 核心管理接口

#### 初始化和配置
```csharp
public void Init()                                    // 初始化UI管理器
public void SetPackage(ResourcePackage package)        // 设置资源包
```

#### UI操作接口
```csharp
public async UniTask OpenUI(string uiName, object data)           // 打开UI
public void CloseUI(string uiName)                                // 关闭UI
public void CloseAndDestroyUI(string uiName)                     // 关闭并销毁UI
public void RefreshUI(GameUIBase uiBase, object data)            // 刷新UI数据
public void RefreshUI(string uiName, object data)                // 通过名称刷新UI
```

#### 批量操作
```csharp
public void CloseAllUI()                             // 关闭所有UI
public void CloseAllAndDestroyUI()                   // 销毁所有UI
public void ClearClosedUICache()                     // 清理关闭UI缓存
```

#### 查询和配置
```csharp
public GameUIBase GetOpenUI(string uiName)           // 获取已打开UI
public Camera GetUICamera()                          // 获取UI相机
public void AddNotCloseFilter(string uiName)         // 添加不关闭过滤器
public void RemoveNotCloseFilter(string uiName)      // 移除不关闭过滤器
public bool IsInNotCloseFilter(string uiName)        // 检查是否在过滤器中
```

#### 层级管理
```csharp
public void AddUILayer(int layer, Transform transform)     // 添加UI层级
public Transform GetUILayer(EGameUILayer layer)           // 获取UI层级
```

---

## ⚡ 实现细节

### 内存优化
- **对象池**：复用List减少GC (_tempUINameList)
- **缓存复用**：关闭UI缓存，避免重复实例化
- **资源管理**：AssetHandle缓存和及时释放

### 错误处理
- **异常捕获**：完善的异常捕获和日志记录
- **状态检查**：多重校验防止状态不一致
- **资源清理**：异常时正确清理资源和状态

---

## 🚀 总结

GameUIManager 提供了完整的UI管理功能：

1. **异步加载**：基于YooAsset的非阻塞UI加载
2. **智能缓存**：多级缓存机制提高响应速度
3. **层级管理**：7层UI层级系统管理渲染顺序
4. **模式切换**：3种显示模式支持不同交互需求
5. **反向导航**：栈式UI管理提供导航体验
6. **资源管理**：高效的资源加载和释放
7. **异常处理**：完善的错误处理确保稳定运行
---
name: event-system
description: ET框架事件分发系统，提供Event（事件）和Invoke（调用）两种消息分发机制，支持场景类型过滤和异步处理。
---

# EventSystem - 事件分发系统

## 📋 概述

EventSystem 是 ET 框架的核心事件分发系统，提供两种不同的消息分发机制：
- **Event（事件）**：观察者模式，可有可无订阅者
- **Invoke（调用）**：函数调用模式，必须有处理器

**技术栈**：C# + ET框架 + 泛型 + 反射

---

## 🏗️ 核心架构

### 1. 两种分发机制

#### Event（事件）
```csharp
// 数据结构
Dictionary<Type, List<EventInfo>> _allEventDic

// EventInfo 结构
private class EventInfo
{
    public IEvent IEvent { get; }
    public int SceneType { get; }
}
```

#### Invoke（调用）
```csharp
// 数据结构
Dictionary<Type, Dictionary<long, IInvoke>> _allInvokeDic
```

### 2. 注册接口

#### 注册Event
```csharp
public void RegisterEvent<T>(int sceneType) where T : IEvent, new()
```

#### 注册Invoke
```csharp
public void RegisterInvoke<T>(long attributeType = 0) where T : IInvoke, new()
```

---

## 🚀 核心功能

### 1. 自动注册机制

#### 标签自动注册
Event和Invoke处理器通过标签自动注册，无需手动调用注册方法：

```csharp
// Invoke标签自动注册
[Invoke(TimerInvokeType.AttackComboTimeout)]
public class AttackComboTimeoutInvoke: ATimer<AttackComponent>
{
    protected override void Run(AttackComponent self)
    {
        if (self == null || self.IsDisposed)
            return;
        self.OnComboTimeout();
    }
}

// Event标签自动注册
[Event(SceneType.Main)]
public class AppStartInitFinish_CreateLoginUI: AEvent<Scene, AppStartInitFinish>
{
    protected override async ETTask Run(Scene root, AppStartInitFinish args)
    {
        EventSystem.Instance.PublishAsync(root, new SceneChangeStart(){SceneName = UnityScene.Login}).NoContext();
        GameUIManager.Instance.CloseAndDestroyUI(GameUIName.UIHelp);
        await GameUIManager.Instance.OpenUI(GameUIName.UILogin, root);
    }
}
```

#### 自动注册流程
- 编译时通过代码生成工具扫描标签
- 运行时通过`GameRegisterHotfix.RegisterEventAuto()`和`GameRegisterHotfix.RegisterInvokeAuto()`完成注册

### 2. Event分发机制

#### 异步事件分发 (PublishAsync)
```csharp
public async ETTask PublishAsync<S, T>(S scene, T a) where S: class, IScene where T : struct
```

**执行流程**：
1. 根据事件类型T查找所有已注册的处理器
2. 过滤匹配场景类型的处理器
3. 并发执行所有处理器（ETTaskHelper.WaitAll）
4. 异常处理和日志记录

#### 同步事件分发 (Publish)
```csharp
public void Publish<S, T>(S scene, T a) where S: class, IScene where T : struct
```

**执行流程**：
1. 根据事件类型T查找所有已注册的处理器
2. 过滤匹配场景类型的处理器
3. 依次执行所有处理器（.NoContext()）
4. 不等待执行完成

### 3. Invoke调用机制

#### 无返回值调用
```csharp
public void Invoke<A>(long type, A args) where A: struct
public void Invoke<A>(A args) where A: struct  // type默认为0
```

#### 有返回值调用
```csharp
public T Invoke<A, T>(long type, A args) where A: struct
public T Invoke<A, T>(A args) where A: struct  // type默认为0
```

**执行流程**：
1. 根据参数类型A查找处理器集合
2. 根据type值查找具体处理器
3. 类型转换和参数传递
4. 执行处理器并返回结果
5. 未找到处理器时抛出异常

---

## 🎮 使用接口

### 核心API

#### 事件发布
```csharp
// 异步事件（等待所有处理器完成）
await EventSystem.Instance.PublishAsync(scene, new LoginFinish());

// 同步事件（fire-and-forget）
EventSystem.Instance.Publish(scene, new SceneChangeStart(){SceneName = "Map1"});
```

#### 调用执行
```csharp
// 无返回值调用
EventSystem.Instance.Invoke(timerCallback);

// 有返回值调用
var result = EventSystem.Instance.Invoke<NetComponentOnRead, ETTask>(sceneType, onReadArgs);
```

---

## 📊 设计特点

### Event vs Invoke 区别

| 特性 | Event（事件） | Invoke（调用） |
|------|---------------|---------------|
| **订阅者要求** | 可有可无 | 必须有处理器 |
| **执行方式** | 并发/顺序执行 | 单一处理器 |
| **返回值** | 无返回值 | 可有返回值 |
| **异常处理** | 记录日志继续 | 抛出异常 |
| **使用场景** | 模块间解耦通知 | 模块内功能调用 |

### 场景类型过滤

- **SceneType匹配**：通过SceneTypeSingleton.IsSame进行场景类型匹配
- **跨场景支持**：支持在不同场景类型间分发事件
- **性能优化**：避免不必要的处理器执行

### 异步处理机制

- **PublishAsync**：使用ETTaskHelper.WaitAll等待所有异步处理器完成
- **Publish**：使用.NoContext()实现fire-and-forget模式
- **异常隔离**：单个处理器异常不影响其他处理器

---

## ⚡ 实现细节

### 内存管理
- **对象池**：使用ListComponent<ETTask>对象池减少GC
- **字典缓存**：事件和调用处理器缓存避免重复查找

### 错误处理
- **异常记录**：PublishAsync和Publish记录异常但不中断执行
- **类型检查**：运行时类型转换检查和异常抛出
- **调试信息**：详细的错误信息便于问题定位

### 性能优化
- **字典查找**：O(1)时间复杂度的事件处理器查找
- **场景过滤**：提前过滤不匹配的场景处理器
- **异步并发**：PublishAsync支持并发执行提高性能

---

## 🎯 总结

EventSystem 提供了灵活的事件分发机制：

1. **Event机制**：观察者模式，支持异步并发处理，适用于模块间解耦通信
2. **Invoke机制**：函数调用模式，强类型检查，适用于模块内功能调用
3. **场景过滤**：支持按场景类型过滤，提高性能和精确度
4. **异步支持**：完善的异步处理机制，支持等待和fire-and-forget模式
5. **异常处理**：健壮的错误处理，不因单个处理器异常影响整体执行

这个事件系统是ET框架的核心组件，为整个框架提供了统一的消息分发基础设施。
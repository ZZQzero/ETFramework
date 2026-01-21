---
name: entity-system
description: ET框架实体系统，提供组件生命周期管理和帧更新分发机制，支持自动注册和类型安全的系统调用。
---

# EntitySystem - 实体系统

## 📋 概述

EntitySystem 是 ET 框架的核心组件系统，提供组件生命周期管理和帧更新分发机制。通过标签自动注册和类型安全的设计，实现高效的组件管理。

**技术栈**：C# + ET框架 + 泛型 + 反射 + 位标记优化

---

## 🏗️ 核心架构

### 1. 组件队列管理 (EntitySystem)

```csharp
// 核心数据结构
private readonly Dictionary<Type, Queue<EntityRef<Entity>>> queues = new();

// 获取组件队列
public Queue<EntityRef<Entity>> GetQueue(Type type)
{
    if (!this.queues.TryGetValue(type, out var queue))
    {
        queue = new Queue<EntityRef<Entity>>();
        this.queues.Add(type, queue);
    }
    return queue;
}
```

### 2. 类型系统管理 (TypeSystems)

```csharp
// OneTypeSystems 结构
public class OneTypeSystems
{
    public readonly UnOrderMultiMap<Type, SystemObject> Map = new();  // 系统映射
    public readonly List<Type> ClassType = new();                     // 事件类型列表
    public SystemFlags Capabilities = SystemFlags.None;              // 能力位标记
}
```

### 3. 系统能力标记 (SystemFlags)

```csharp
// 位标记枚举
public enum SystemFlags : byte
{
    None = 0,
    Awake = 1,
    Update = 2,
    LateUpdate = 4,
    FixedUpdate = 8,
    OnAnimatorMove = 16,
    Destroy = 32,
}
```

---

## 🚀 核心功能

### 1. 自动注册机制

#### 标签声明系统
```csharp
[EntitySystemOf(typeof(AttackComponent))]  // 声明所属组件
[FriendOf(typeof(AttackComponent))]        // 声明友元关系
public static partial class AttackComponentSystem
{
    [EntitySystem]  // 自动注册标签
    private static void Awake(this AttackComponent self, string configPath)
    {
        // Awake逻辑
    }

    [EntitySystem]
    private static void Update(this AttackComponent self)
    {
        // Update逻辑
    }

    [EntitySystem]
    private static void Destroy(this AttackComponent self)
    {
        // Destroy逻辑
    }
}
```

#### 编译时自动注册
- 通过`[EntitySystem]`标签标记方法
- 编译时生成注册代码
- 运行时通过`EntitySystemSingleton.RegisterEntitySystem<T>()`注册

### 2. 组件注册流程

#### RegisterSystem 方法
```csharp
public virtual void RegisterSystem(Entity component)
{
    Type type = component.GetType();
    TypeSystems.OneTypeSystems oneTypeSystems = EntitySystemSingleton.TypeSystems.GetOneTypeSystems(type);
    if (oneTypeSystems == null) return;

    // 将组件加入对应的事件队列
    foreach (Type queueType in oneTypeSystems.ClassType)
    {
        var queue = this.GetQueue(queueType);
        queue.Enqueue(component);
    }
}
```

**注册流程**：
1. 获取组件类型
2. 获取对应的TypeSystems
3. 将组件加入所有相关的事件队列

### 3. 帧更新分发

#### Publish 方法
```csharp
public void Publish<T>(T t) where T: struct
{
    Type systemType = typeof(AClassEventSystem<T>);
    if (!this.queues.TryGetValue(systemType, out var queue))
        return;

    // 遍历队列中的所有组件
    int count = queue.Count;
    while (count-- > 0)
    {
        Entity component = queue.Dequeue();
        if (component == null || component.IsDisposed)
            continue;

        // 执行对应的系统方法
        List<SystemObject> systems = EntitySystemSingleton.TypeSystems.GetSystems(componentType, systemType);
        foreach (AClassEventSystem<T> classSystem in systems)
        {
            classSystem.Run(component, t);
        }

        queue.Enqueue(component); // 重新入队
    }
}
```

#### 内置事件类型
```csharp
// Fiber.cs 中的调用
this.EntitySystem.Publish(new UpdateEvent());         // 每帧Update
this.EntitySystem.Publish(new LateUpdateEvent());     // 每帧LateUpdate
this.EntitySystem.Publish(new FixedUpdateEvent());    // 物理帧FixedUpdate
this.EntitySystem.Publish(new OnAnimatorMoveEvent()); // 动画移动事件
```

### 4. 生命周期管理

#### Awake 系统
```csharp
// EntitySystemSingleton.Awake 方法
public void Awake(Entity component)
public void Awake<P1>(Entity component, P1 p1)
public void Awake<P1, P2>(Entity component, P1 p1, P2 p2)
public void Awake<P1, P2, P3>(Entity component, P1 p1, P2 p2, P3 p3)
```

**执行流程**：
1. 检查Capabilities是否有Awake标记
2. 获取对应的IAwakeSystem列表
3. 依次执行所有Awake方法
4. 异常隔离，单个失败不影响其他

#### Destroy 系统
```csharp
public void Destroy(Entity component)
```

**执行流程**：
1. 检查Capabilities是否有Destroy标记
2. 获取对应的IDestroySystem列表
3. 依次执行所有Destroy方法
4. 异常隔离处理

### 5. 能力位标记优化

#### SetSystemCapability 方法
```csharp
private static void SetSystemCapability(TypeSystems.OneTypeSystems oneTypeSystems, SystemObject obj)
{
    if (obj is IAwakeSystemMarker) oneTypeSystems.Capabilities |= SystemFlags.Awake;
    if (obj is IDestroySystemMarker) oneTypeSystems.Capabilities |= SystemFlags.Destroy;
    if (obj is AClassEventSystem<UpdateEvent>) oneTypeSystems.Capabilities |= SystemFlags.Update;
    // ... 其他标记
}
```

**优化效果**：
- 运行时快速检查，避免无效查询
- 减少不必要的系统调用
- 提高组件创建/销毁性能

---

## 🎮 使用指南

### 1. 创建组件系统

#### 基础结构
```csharp
[EntitySystemOf(typeof(YourComponent))]  // 声明系统所属组件
[FriendOf(typeof(YourComponent))]        // 声明友元关系（可选）
public static partial class YourComponentSystem
{
    [EntitySystem]  // Awake系统，支持0-3个参数
    private static void Awake(this YourComponent self) { }

    [EntitySystem]  // Update系统，每帧调用
    private static void Update(this YourComponent self) { }

    [EntitySystem]  // LateUpdate系统，每帧晚期调用
    private static void LateUpdate(this YourComponent self) { }

    [EntitySystem]  // Destroy系统，组件销毁时调用
    private static void Destroy(this YourComponent self) { }

    // 其他自定义方法...
}
```

#### 支持的生命周期方法
- **Awake**: 组件创建时调用，支持0-3个参数
- **Update**: 每帧Update时调用
- **LateUpdate**: 每帧LateUpdate时调用
- **FixedUpdate**: 物理帧FixedUpdate时调用
- **Destroy**: 组件销毁时调用

### 2. 组件创建和销毁

#### 创建组件
```csharp
// 创建组件时自动调用Awake系统
var component = entity.AddComponent<YourComponent>();
// 或
var component = entity.AddComponent<YourComponent, P1>(param1);
```

#### 销毁组件
```csharp
// 销毁组件时自动调用Destroy系统
entity.RemoveComponent<YourComponent>();
```

### 3. 自定义事件

#### 定义事件结构体
```csharp
public struct CustomEvent
{
    public int Value;
    public string Message;
}
```

#### 创建事件系统
```csharp
[EntitySystemOf(typeof(YourComponent))]
public static partial class YourComponentSystem
{
    [EntitySystem]
    private static void CustomEvent(this YourComponent self, CustomEvent e)
    {
        // 处理自定义事件
    }
}
```

#### 触发事件
```csharp
// 在代码中触发
entity.Scene().EntitySystem.Publish(new CustomEvent { Value = 1, Message = "Hello" });
```

---

## ⚡ 性能优化

### 1. 队列管理优化
- **循环队列**：Publish时出队处理后重新入队，保证顺序
- **引用检查**：跳过已销毁的组件，避免无效调用
- **异常隔离**：单个组件异常不影响其他组件

### 2. 类型系统优化
- **位标记检查**：运行时快速判断是否有对应系统
- **字典查找**：O(1)时间复杂度获取系统列表
- **延迟初始化**：队列按需创建，减少内存占用

### 3. 内存管理
- **对象池**：复用队列和列表对象
- **引用管理**：EntityRef防止野指针问题
- **GC优化**：避免在热点路径分配对象

---

## 🎯 设计特点

| 特性 | 说明 | 优势 |
|------|------|------|
| **自动注册** | 通过标签声明，无需手动注册 | 开发效率高，类型安全 |
| **生命周期管理** | 完整的组件生命周期 | 资源管理规范 |
| **帧更新分发** | 基于队列的事件分发 | 性能可控，顺序保证 |
| **能力位优化** | 位标记快速检查 | 运行时性能优化 |
| **异常隔离** | 单个组件异常不影响整体 | 系统稳定性高 |
| **类型安全** | 泛型约束和编译时检查 | 减少运行时错误 |

---

## 🚀 总结

EntitySystem 提供了完整的组件生命周期和事件分发机制：

1. **声明式编程**：通过标签自动注册，简化开发流程
2. **性能优化**：能力位标记和队列管理，高效的事件分发
3. **生命周期管理**：从Awake到Destroy的完整生命周期支持
4. **类型安全**：泛型约束和编译时检查，确保代码正确性
5. **异常隔离**：单个组件失败不影响其他组件的正常运行
6. **扩展性强**：支持自定义事件和系统扩展

这个实体系统是ET框架组件模式的核心，为游戏开发提供了高效、可靠的组件管理基础设施。
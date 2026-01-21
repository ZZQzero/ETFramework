---
name: fiber-system
description: ET框架Fiber系统，提供轻量级协程并发编程，支持多线程调度和异步任务管理。
---

# Fiber系统 (协程并发框架)

## 核心概念

**Fiber** 是ET框架的执行单元，实现Actor模型的轻量级协程并发。每个Fiber拥有独立的Scene、EntitySystem和消息队列，支持在不同线程中并行执行，同时保证线程安全的通信。

### 主要特性
- **轻量级并发**: 在Unity单线程环境实现多线程效果
- **Actor模型**: 通过消息通信保证线程安全
- **多调度器**: 支持主线程、独立线程、线程池三种调度模式
- **平台适配**: 自动适配Unity、WebGL等平台的线程限制

### Fiber组成
```csharp
public class Fiber: IDisposable
{
    public int Id { get; }                          // 唯一标识
    public Scene Root { get; }                      // 根Scene
    public EntitySystem EntitySystem { get; }       // 实体系统
    public Mailboxes Mailboxes { get; }             // 消息队列
    public ThreadSynchronizationContext ThreadSynchronizationContext { get; }
}
```

### 生命周期方法
- `Update()`: 发布UpdateEvent
- `LateUpdate()`: 发布LateUpdateEvent，处理异步任务
- `FixedUpdate()`: Unity物理更新
- `OnAnimatorMove()`: Unity动画回调

## 调度器系统

ET框架提供三种调度器，适应不同并发需求：

| 调度器 | 执行方式 | 适用场景 | 特点 |
|--------|----------|----------|------|
| **Main** | Unity主线程 | 客户端UI/网络 | 支持Unity API，串行执行 |
| **Thread** | 独立线程 | 服务端高并发 | 真正并行，资源占用高 |
| **ThreadPool** | .NET线程池 | 服务端临时任务 | 资源复用，适合短任务 |

### 平台适配
- **Unity/WebGL**: Thread和ThreadPool调度器自动降级为主线程
- **服务端**: 支持所有调度器类型，根据业务选择

## 线程安全通信

**核心原则：** 禁止直接访问其他Fiber对象，必须通过消息通信。

```csharp
// ❌ 错误：直接访问
var otherEntity = fiberManager.Get(id).Root.GetComponent<SomeComponent>();

// ✅ 正确：消息通信
await MessageSender.Call(actorId, request);
```

每个Fiber拥有独立的Mailboxes和ThreadSynchronizationContext用于安全通信。

## 服务端Fiber使用

服务端使用Fiber实现分布式多线程架构：

| Fiber类型 | 调度器 | 用途 | 关键组件 |
|-----------|--------|------|----------|
| **Main** | Main | 服务器启动 | 基础服务管理 |
| **NetInner** | ThreadPool | 内部网络 | ProcessOuterSender |
| **Map** | ThreadPool | 游戏世界 | UnitComponent、AOIManager |
| **Gate** | Main | 外部网络 | 网络连接管理 |
| **Robot** | ThreadPool | 测试工具 | AIComponent |

### 服务端启动流程
1. 创建Main Fiber处理服务器初始化
2. 创建NetInner Fiber处理进程间通信
3. 根据配置动态创建业务Fiber（Map、Gate等）
4. 每个Fiber通过FiberInit进行场景初始化

## 核心API用法

### 创建Fiber
```csharp
// 客户端UI Fiber
await FiberManager.Instance.Create(SchedulerType.Main, SceneType.Main, 0, SceneType.Main, "Main");

// 服务端业务Fiber
await FiberManager.Instance.Create(SchedulerType.ThreadPool, zone, SceneType.Map, "Map");
```

### 帧同步等待
```csharp
// 等待当前帧完成后再执行
await this.Fiber().WaitFrameFinish();
ContinueExecution();
```

## 架构优势

- **轻量级并发**：在Unity单线程环境实现多线程效果
- **Actor模型**：通过消息通信保证线程安全
- **平台适配**：自动适配Unity、WebGL等平台的线程限制
- **生命周期管理**：完整的异步任务和同步机制

这个Fiber系统为ET框架提供了强大的并发编程能力，既保持了Unity开发的便利性，又提供了服务端级别的并发性能。
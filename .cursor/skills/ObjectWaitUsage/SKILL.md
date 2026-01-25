---
name: object-wait-usage
description: ET框架 ObjectWait 的正确用法、与 ETTask 上下文/取消的关系、以及常见竞态与排障方法（严格先 Wait 后 Notify）。
---

# ObjectWaitUsage - ObjectWait 使用指南（严格模式）

## 适用场景

`ObjectWait` 是一个 **一次性（one-shot）等待器**：用于在同一 `Scene/Fiber` 内，把“事件到达（Notify）”与“协程等待（Wait）”解耦。

典型场景：
- **场景切换流程**：等待 Unity 场景加载完成、等待服务器下发 Unit 数据
- **异步步骤串联**：某个 handler 收到消息后唤醒业务协程继续执行

> 本项目当前实现为 **严格模式：必须先 Wait 后 Notify**。`Notify<T>` 未命中 waiter 会记录错误日志并丢弃。

---

## 核心接口

### 1) 等待：`Wait<T>()`

- 语义：注册一个 `T` 类型的等待，并返回 `ETTask<T>`（完成后得到结果 `T`）
- 约束：
  - 同一个 `ObjectWait` 上 **同一 `T` 只能同时存在一个 waiter**（否则返回 `Error=Cancel` 并记录日志）
  - **必须先调用 Wait，再触发会导致 Notify 的流程**

### 2) 通知：`Notify<T>(T value)`

- 语义：如果存在同类型 waiter，则完成它；否则记录错误日志（严格模式下不缓存）。

---

## 与 ETTask 的关键关系（必须理解）

### 1) 为什么 `Wait<T>()` 里要先“注册”，再 `await`？

为了避免竞态：如果 `Wait<T>()` 内部先 `await`（让出执行权），消息可能提前到达，`Notify<T>()` 会找不到 waiter，从而导致业务协程永久卡住。

当前实现使用 `TryAdd` 在任何 `await` 前把 waiter 放入 `tcss`，从根上消除“注册空窗”。

### 2) 为什么要缓存 `ETTask<T> task = tcs.Task`，然后 `await task`？

因为 `Notify<T>()` 可能在 `Wait<T>()` 内部的 `await` 期间“抢跑完成”，此时 `ResultCallback<T>` 会把内部字段置空。若后续再访问 `tcs.Task` 可能触发 `NullReferenceException`。

缓存局部变量 `task` 可以保证 `await` 的对象引用稳定。

### 3) `GetContextAsync<ETCancellationToken>()` 是干什么的？

它从 ETTask 的上下文链里获取取消令牌，使得 **外层流程取消**时，`Wait<T>()` 能被取消并返回（`Error=Cancel`），避免永久等待。

注意：这是“协程上下文取消”机制，与 C# `CancellationToken` 不同。

---

## 正确用法（推荐模式）

### 模式 A：先注册 Wait，再触发异步操作

这是严格模式下最重要的操作规范：

1. **先创建 waiter**
2. 再触发任何可能导致 `Notify<T>` 的操作（包括网络请求、事件发布、异步加载）
3. `await waiter` 继续流程

以切场景为例（伪代码）：

```csharp
ETTask<Wait_UnitySceneLoaded> loadedTask = root.GetComponent<ObjectWait>().Wait<Wait_UnitySceneLoaded>();
EventSystem.Instance.Publish(root, new SceneChangeStart(){ SceneName = "Map1" });
Wait_UnitySceneLoaded loaded = await loadedTask;

ETTask<Wait_CreateMyUnit> unitTask = root.GetComponent<ObjectWait>().Wait<Wait_CreateMyUnit>();
await clientSender.Call(C2M_SceneLoadFinish.Create());
Wait_CreateMyUnit unitMsg = await unitTask;
```

### 模式 B：在 Notify 前确保是在同一个 `ObjectWait` 上

`Wait/Notify` 必须作用于 **同一个 Entity 实例**（通常是 `root.GetComponent<ObjectWait>()`）。
不要在子 `Scene`、`Unit` 上新增一个 `ObjectWait` 然后去 Notify root 的 waiter（或反过来）。

---

## 常见坑与排障

### 1) 日志：`ObjectWait.Notify 未命中 waiter`

说明 **Notify 发生时没有对应 Wait**。在严格模式下，这会导致业务协程卡住。

排查顺序：
- 是否先 `Wait<T>` 再触发会产生 Notify 的流程？
- 是否 Wait/Notify 作用在同一个 `ObjectWait` 实例上（同一个 root/scene）？
- 是否同类型 waiter 被重复注册导致提前返回（看 `ObjectWait 重复等待同一类型`）？

### 2) 业务卡住：一直停在“等待服务端发送Unit数据...”

说明 `Wait_CreateMyUnit` 没有被完成，常见原因：
- `M2C_CreateMyUnitHandler` 没有触发（网络断线 / 未发送 / 路由错误）
- `Notify<Wait_CreateMyUnit>` 未命中 waiter（严格模式下会打 `Notify 未命中` 日志）

### 3) 重复 Wait：`ObjectWait 重复等待同一类型`

说明同一个 `ObjectWait` 上并发调用了两次 `Wait<T>`（T 相同）。

建议：
- 把等待逻辑整理为“单入口状态机”
- 或在上层加互斥（例如 `CoroutineLock`）保证同一流程不会并发执行

---

## 工程建议（商业级约束）

- **关键 Wait 必须加超时/断线兜底**：严格模式下任何丢 Notify 都会卡死。建议上层流程（例如切场景）对关键等待做超时处理（例如 10~30 秒），并在断线时主动 Notify 相关 WaitType 结束流程。
- **不要缓存 MessageObject 到长期结构**：很多消息对象可能来自对象池或在 handler 结束时 Dispose；如果必须跨帧保存，请保存必要字段而不是保存整个消息对象引用。
- **只用在同 Fiber 单线程语义下**：`ObjectWait` 的实现假设同一 `Scene/Fiber` 内调用，不提供跨线程并发安全保证。


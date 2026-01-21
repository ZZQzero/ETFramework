---
name: timer-system
description: ET框架定时器系统，提供高性能的时间管理、异步等待和定时任务调度功能。
---

# ET Framework Timer System

ET框架的定时器系统基于TimerComponent实现，支持一次性定时器、重复定时器、异步等待等多种时间管理功能。

## 核心组件

### TimerComponent
定时器组件，挂载在Scene上，负责管理所有定时器任务。

## 定时器类型

### 1. OnceTimer (一次性定时器)
执行一次后自动移除的回调定时器。

### 2. OnceWaitTimer (一次性等待定时器)
用于异步等待的定时器，返回ETTask。

### 3. RepeatedTimer (重复定时器)
按固定间隔重复执行的定时器。

## 核心API

### 异步等待方法

#### WaitTillAsync(long tillTime)
等待到指定的绝对时间点。
```csharp
// 等待到服务器时间1000
await timerComponent.WaitTillAsync(1000);
```

#### WaitAsync(long time)
等待指定的相对时间长度。
```csharp
// 等待100毫秒
await timerComponent.WaitAsync(100);

// 立即返回（time=0）
await timerComponent.WaitAsync(0);
```

#### WaitFrameAsync()
等待一帧的时间。
```csharp
// 等待下一帧
await timerComponent.WaitFrameAsync();
```

### 回调定时器方法

#### NewOnceTimer(long tillTime, int type, object args)
创建一次性回调定时器，返回定时器ID。
```csharp
// 5秒后执行回调
long timerId = timerComponent.NewOnceTimer(
    TimeInfo.Instance.ServerFrameTime() + 5000,
    (int)TimerType.TimeoutCallback,
    callbackArgs
);
```

#### NewRepeatedTimer(long time, int type, object args)
创建重复执行的定时器，返回定时器ID。
```csharp
// 每秒执行一次
long timerId = timerComponent.NewRepeatedTimer(
    1000, // 间隔时间（毫秒）
    (int)TimerType.Heartbeat,
    heartbeatArgs
);
```

#### NewFrameTimer(int type, object args)
创建每帧执行的定时器。
```csharp
// 每帧更新
long timerId = timerComponent.NewFrameTimer(
    (int)TimerType.FrameUpdate,
    updateArgs
);
```

### 定时器管理

#### Remove(ref long id)
移除指定的定时器。
```csharp
long timerId = timerComponent.NewOnceTimer(...);
// 移除定时器
bool removed = timerComponent.Remove(ref timerId);
```

## 内部实现机制

### 时间轮盘算法
- **timeId字典**：按执行时间分组存储定时器ID列表
- **minTime优化**：记录最近的执行时间，避免无效遍历
- **批量处理**：Update中批量处理到期的定时器

### 执行流程
```
Update()每帧执行：
1. 检查当前时间 >= minTime
2. 遍历timeId字典找到已到期的时间点
3. 将到期的定时器ID加入timeOutTimerIds队列
4. 执行所有到期的定时器回调
5. 更新minTime为下一个最近的时间点
```

### 取消机制
```csharp
// 异步等待支持取消
ETCancellationToken cancellationToken = await ETTaskHelper.GetContextAsync<ETCancellationToken>();
try
{
    cancellationToken?.Add(CancelAction);
    await tcs;
}
finally
{
    cancellationToken?.Remove(CancelAction);
}
```

## 实际使用示例

### 连击超时定时器
```csharp
// 在技能系统中设置连击超时
private long comboTimeoutTimer;

public void StartComboTimer(long timeoutMs)
{
    // 取消之前的定时器
    this.Root().TimerComponent.Remove(ref this.comboTimeoutTimer);

    // 设置新的超时定时器
    long timeoutTime = TimeInfo.Instance.ServerFrameTime() + timeoutMs;
    this.comboTimeoutTimer = this.Root().TimerComponent.NewOnceTimer(
        timeoutTime,
        TimerInvokeType.AttackComboTimeout,
        this
    );
}

public void CancelComboTimer()
{
    // 取消连击超时定时器
    this.Root().TimerComponent.Remove(ref this.comboTimeoutTimer);
}
```

### 异步等待模式
```csharp
public async ETTask DoSomethingAsync()
{
    // 执行一些操作
    await timerComponent.WaitAsync(1000); // 等待1秒

    // 继续执行
    await timerComponent.WaitTillAsync(targetTime);
}
```

### 回调模式
```csharp
public void StartTimer()
{
    // 创建定时器
    long timerId = timerComponent.NewRepeatedTimer(
        5000, // 5秒间隔
        (int)TimerType.CustomEvent,
        new CustomArgs { Data = "test" }
    );

    // 存储timerId以便后续取消
    this.timerId = timerId;
}

public void StopTimer()
{
    // 取消定时器
    timerComponent.Remove(ref this.timerId);
}
```

### 帧同步模式
```csharp
public void InitFrameUpdate()
{
    // 每帧执行更新逻辑
    timerComponent.NewFrameTimer(
        (int)TimerType.FrameUpdate,
        new UpdateArgs { Component = this }
    );
}
```

## 实现机制

### 时间轮盘算法
- **timeId字典**：按执行时间分组存储定时器ID列表
- **minTime优化**：记录最近的执行时间，避免无效遍历
- **批量处理**：Update中批量处理到期的定时器

## 最佳实践

### 选择合适的定时器类型

基于代码注释的建议：
- **WaitTillAsync/WaitAsync**：时间短且逻辑需要连贯的场景
- **NewOnceTimer**：时间长且不需要逻辑连贯的场景，支持热更但回调式写法

### 定时器生命周期管理
```csharp
public class GameManager : IDisposable
{
    private long updateTimerId;
    private long heartbeatTimerId;

    public void Start()
    {
        // 启动定时器
        updateTimerId = timerComponent.NewFrameTimer(...);
        heartbeatTimerId = timerComponent.NewRepeatedTimer(...);
    }

    public void Dispose()
    {
        // 清理所有定时器
        timerComponent.Remove(ref updateTimerId);
        timerComponent.Remove(ref heartbeatTimerId);
    }
}
```



这个定时器系统为ET框架提供了灵活高效的时间管理能力，支持从毫秒级精确控制到帧级同步的各种时间需求。
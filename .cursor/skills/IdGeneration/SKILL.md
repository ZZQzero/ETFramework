---
name: id-generation-system
description: ET框架ID生成系统，提供高性能、线程安全的全局唯一标识符生成服务，支持分布式环境下的ID唯一性保证。
---

# ID生成系统 (GenerateIdManager)

## 概述

ET框架的ID生成系统提供高性能、线程安全的全局唯一标识符生成服务。支持两种类型的ID：全局唯一ID和实例ID。采用时间戳、进程ID和递增值的组合方式，确保在分布式环境下ID的唯一性。

## 核心组件

### GenerateIdManager类

单例管理类，负责ID的生成和管理。

```csharp
public class GenerateIdManager: Singleton<GenerateIdManager>, ISingletonAwake
{
    // 常量定义
    public const int MaxZone = 1024;
    public const int Mask14bit = 0x3fff;      // 14位掩码
    public const int Mask30bit = 0x3fffffff;  // 30位掩码
    public const int Mask20bit = 0xfffff;     // 20位掩码
}
```

## 数据结构

### IdStruct - 全局唯一ID结构

64位ID结构，由三部分组成：

| 字段 | 位数 | 说明 |
|------|------|------|
| Process | 14bit | 进程标识 (0-16383) |
| Time | 30bit | 时间戳 (从2022年1月1日开始的秒数) |
| Value | 20bit | 递增序列号 (0-1048575) |

```csharp
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct IdStruct
{
    public short Process;  // 14bit
    public uint Time;      // 30bit
    public uint Value;     // 20bit

    public long ToLong();  // 转换为64位long
    public IdStruct(uint time, short process, uint value);  // 构造函数
    public IdStruct(long id);  // 从long构造
}
```

### InstanceIdStruct - 实例ID结构

64位实例ID结构，由两部分组成：

| 字段 | 位数 | 说明 |
|------|------|------|
| Time | 32bit | 时间戳 (从2022年1月1日开始的秒数) |
| Value | 32bit | 递增序列号 |

```csharp
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct InstanceIdStruct
{
    public uint Time;   // 32bit
    public uint Value;  // 32bit

    public long ToLong();  // 转换为64位long
    public InstanceIdStruct(uint time, uint value);  // 构造函数
    public InstanceIdStruct(long id);  // 从long构造
}
```

## 时间基准

系统使用2022年1月1日作为时间基准(epoch)：

```csharp
private long epoch2022;  // 1970年到2022年的毫秒数差值

public void Awake()
{
    long epoch1970tick = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks / 10000;
    this.epoch2022 = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks / 10000 - epoch1970tick;
}
```

**时间戳计算：**
```csharp
private uint TimeSince2022()
{
    return (uint)((TimeInfo.Instance.FrameTime - this.epoch2022) / 1000);
}
```

## ID生成算法

### GenerateId() - 生成全局唯一ID

```csharp
public long GenerateId()
{
    uint time = TimeSince2022();

    // 原子递增并处理溢出
    int newValue = Interlocked.Increment(ref this.value);
    uint v = (uint)(newValue & Mask20bit);  // 确保不超过20bit

    IdStruct idStruct = new(time, (short)Options.Instance.Process, v);
    return idStruct.ToLong();
}
```

**算法特点：**
- **线程安全**：使用`Interlocked.Increment`原子操作
- **防溢出**：通过掩码运算确保值在有效范围内
- **分布式唯一**：结合进程ID和时间戳保证全局唯一性

### GenerateInstanceId() - 生成实例ID

```csharp
public long GenerateInstanceId()
{
    uint time = this.TimeSince2022();
    uint v = (uint)Interlocked.Increment(ref this.instanceIdValue);

    InstanceIdStruct instanceIdStruct = new(time, v);
    return instanceIdStruct.ToLong();
}
```

**特点：**
- 不包含进程ID，适用于单进程内的实例标识
- 32bit时间 + 32bit值，总共64bit

## 位运算逻辑

### ID转换为二进制结构

**全局唯一ID (64bit):**
```
PPPPPPPPPPPPPP TTTTTTTTTTTTTTTTTTTTTTTTTTTTTT VVVVVVVVVVVVVVVVVV
│              │                              │
├─ Process (14bit) ─┼─ Time (30bit) ──────────┼─ Value (20bit) ───┤
```

**实例ID (64bit):**
```
TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT VVVVVVVVVVVVVVVVVVVVVVVVVVVVVVVV
│                                │
├─ Time (32bit) ────────────────┼─ Value (32bit) ──────────────────┤
```

### 数值范围

| 类型 | 字段 | 范围 |
|------|------|------|
| 全局ID | Process | 0 - 16,383 (2^14 - 1) |
| 全局ID | Time | 0 - 1,073,741,823 (2^30 - 1) |
| 全局ID | Value | 0 - 1,048,575 (2^20 - 1) |
| 实例ID | Time | 0 - 4,294,967,295 (2^32 - 1) |
| 实例ID | Value | 0 - 4,294,967,295 (2^32 - 1) |

## 使用示例

### 生成全局唯一ID
```csharp
// 获取单例实例
var idManager = GenerateIdManager.Instance;

// 生成全局唯一ID
long globalId = idManager.GenerateId();

// ID结构分析
IdStruct idStruct = new IdStruct(globalId);
Log.Info($"Process: {idStruct.Process}, Time: {idStruct.Time}, Value: {idStruct.Value}");
```

### 生成实例ID
```csharp
// 生成实例ID
long instanceId = idManager.GenerateInstanceId();

// ID结构分析
InstanceIdStruct instanceStruct = new InstanceIdStruct(instanceId);
Log.Info($"Time: {instanceStruct.Time}, Value: {instanceStruct.Value}");
```

### 在UnitFactory中的使用
```csharp
// 创建Unit时生成唯一ID
long monsterId = GenerateIdManager.Instance.GenerateId();
Unit monster = UnitFactory.Create(scene, monsterId, UnitType.Monster);
```

## 性能特性

- **高性能**：使用原子操作，无锁竞争
- **内存友好**：结构体紧凑布局，无额外开销
- **扩展性好**：支持分布式部署，多进程唯一
- **容错性强**：时间戳回退保护，数值溢出处理

## 注意事项

1. **时间同步**：分布式环境下各服务器时间需要同步
2. **进程ID唯一性**：确保不同进程的Options.Instance.Process不重复
3. **ID溢出**：Value字段达到最大值后会循环使用
4. **时间基准**：2022年后约34年时间戳才会溢出

### 调试技巧
- 通过`IdStruct.ToString()`查看ID组成
- 使用ID解析函数验证ID结构正确性
- 监控ID生成速率评估系统负载
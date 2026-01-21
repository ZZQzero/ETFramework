---
name: proto-generation-system
description: ET框架Proto消息协议生成系统，使用Protocol Buffers定义消息，通过Proto2CS工具自动生成C#代码，支持Nino序列化。
---

# Proto消息协议生成系统 (Proto2CS)

## 概述

ET框架使用Protocol Buffers (protobuf)定义消息协议，通过自定义的Proto2CS工具自动生成C#代码。生成的代码支持Nino序列化库，自动集成对象池管理，并生成唯一的Opcode常量。

## 目录结构

```
G:\UnityProject\ETFramework\
├── Config\Proto\                           # Proto定义文件目录
│   ├── ActorLocation_S_20100.proto         # Actor定位相关消息
│   ├── LoginOuter_C_1000.proto             # 外部登录消息
│   └── ...
├── DotNet\ET.Proto2CS\                     # Proto生成工具项目
│   ├── Proto2CS.cs                         # 核心生成逻辑
│   └── Init.cs                             # 程序入口
└── Unity\Assets\Scripts\Model\Core\Share\Proto\ClientServer\  # 生成的C#代码
    ├── ActorLocation_S_20100.cs
    ├── LoginOuter_C_1000.cs
    └── ...
```

## Proto文件规范

### 文件命名规则

Proto文件的命名格式为：`{模块名}_{类型}_{起始Opcode}.proto`

- **模块名**：功能模块标识，如`ActorLocation`、`LoginOuter`、`StateSync`
- **类型**：消息类型标识
  - `S` = 服务端消息
  - `C` = 客户端消息
  - `Inner` = 内部消息
  - `Outer` = 外部消息
- **起始Opcode**：起始操作码（实际生成时基于消息名哈希）

**示例：**
- `ActorLocation_S_20100.proto` - Actor定位模块服务端消息
- `LoginOuter_C_1000.proto` - 登录模块外部客户端消息

### Proto语法格式

```protobuf
syntax = "proto3";
package ET;

// 普通消息（单向）
message MessageName
{
    int32 Field1 = 1;
    string Field2 = 2;
    repeated int64 Field3 = 3;  // List<T>
    map<int32, string> Field4 = 4;  // Dictionary<TKey, TValue>
}

// RPC请求消息
// ResponseType ResponseMessageName
message RequestMessageName // IRequest
{
    int32 RpcId = 1;  // RPC消息必须包含RpcId
    // ... 其他字段
}

// RPC响应消息
message ResponseMessageName // IResponse
{
    int32 RpcId = 1;
    int32 Error = 2;      // 错误码
    string Message = 3;   // 错误消息
    // ... 业务字段
}

// 会话消息（网络通信）
message SessionMessageName // ISessionRequest 或 ISessionMessage
{
    int32 RpcId = 1;  // ISessionRequest需要
    // ... 其他字段
}
```

### 接口标记

通过注释标记消息接口类型：

- `// IRequest` - Actor RPC请求
- `// IResponse` - Actor RPC响应
- `// IActorMessage` - Actor单向消息
- `// IActorRequest` - Actor RPC请求（显式）
- `// IActorResponse` - Actor RPC响应（显式）
- `// ISessionRequest` - 会话RPC请求
- `// ISessionResponse` - 会话RPC响应
- `// ISessionMessage` - 会话单向消息

**ResponseType标记：**
```protobuf
// ResponseType ResponseMessageName
message RequestMessageName // IRequest
{
    // ...
}
```

### 类型映射

Proto类型自动映射到C#类型：

| Proto类型 | C#类型 | 说明 |
|-----------|--------|------|
| `int32` | `int` | 32位整数 |
| `int64` | `long` | 64位整数 |
| `uint32` | `uint` | 无符号32位整数 |
| `uint64` | `ulong` | 无符号64位整数 |
| `int16` | `short` | 16位整数 |
| `uint16` | `ushort` | 无符号16位整数 |
| `string` | `string` | 字符串 |
| `bytes` | `byte[]` | 字节数组 |
| `bool` | `bool` | 布尔值 |
| `float` | `float` | 单精度浮点 |
| `double` | `double` | 双精度浮点 |
| `repeated T` | `List<T>` | 列表 |
| `map<K,V>` | `Dictionary<K,V>` | 字典 |

## 生成流程

### 1. 入口调用

```csharp
// 程序化调用
Proto2CS.Export();

// 或命令行执行
dotnet run --project ET.Proto2CS/ET.Proto2CS.csproj
```

### 2. 目录定位

工具按优先级自动查找Proto目录：

1. `AppContext.BaseDirectory/Config/Proto/`
2. `AppContext.BaseDirectory/../Config/Proto/`
3. `Directory.GetCurrentDirectory()/Config/Proto/`

### 3. 解决方案根目录定位

自动向上查找包含Unity和DotNet目录的根目录：

```
Solution Root/
├── Unity/
└── DotNet/
```

### 4. 生成步骤

#### 第一遍：收集消息类型
遍历所有proto文件，收集所有`message`类型名，用于后续的Dispose代码生成。

#### 第二遍：生成代码
对每个proto文件执行：
1. **解析文件名**：提取模块名和类型标识
2. **读取proto内容**：逐行解析消息定义
3. **生成C#类**：为每个message生成对应的C#类
4. **生成Opcode常量**：为每个消息生成唯一的操作码

## 生成的C#代码结构

### 类定义结构

```csharp
using Nino.Core;
using System.Collections.Generic;

namespace ET
{
    // 消息注释
    [NinoType(false)]
    [Message(ModuleName.MessageName)]
    [ResponseType(nameof(ResponseType))]  // RPC请求消息
    public partial class MessageName : MessageObject, IInterface
    {
        public static MessageName Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<MessageName>(isFromPool);
        }

        // 字段定义
        [NinoMember(0)]
        public FieldType FieldName { get; set; }

        // Dispose方法
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            // 清理代码...
            ObjectPool.Recycle(this);
        }
    }

    // Opcode常量类
    public static class ModuleName
    {
        public const ushort MessageName1 = 12345;
        public const ushort MessageName2 = 67890;
        // ...
    }
}
```

### 特性说明

- **`[NinoType(false)]`**：禁用Nino类型标记，消息类不参与自动序列化
- **`[Message(ModuleName.MessageName)]`**：标记消息类型和Opcode
- **`[ResponseType(nameof(ResponseType))]`**：指定响应消息类型（RPC请求）
- **`[NinoMember(n)]`**：指定字段的序列化顺序

### 对象池集成

生成的代码自动集成对象池管理：

```csharp
// 创建对象
var message = MessageName.Create();  // 从对象池获取

// 使用后回收
message.Dispose();  // 自动清理字段并回收到对象池
```

### Dispose逻辑

根据字段类型生成相应的清理代码：

- **MessageObject子类**：调用`Dispose()`后设为`null`
- **基础类型**：设为默认值（0或false）
- **引用类型**：设为`null`
- **结构体**：设为`default`

## Opcode生成算法

### 哈希算法

使用多重哈希确保稳定性：

1. **主哈希**：FNV-1a算法生成基础Opcode
2. **冲突解决**：DJB哈希生成步长，线性探测解决冲突

### 范围限制

- **最小值**：4（保留0-3给特殊用途）
- **最大值**：65535（ushort最大值）
- **冲突处理**：自动寻找下一个可用位置

### 稳定性保证

- 同一消息名始终生成相同的Opcode
- 添加新消息不会影响现有消息的Opcode
- 跨文件全局唯一，不重复

## 命令行执行

### 方式一：直接运行项目

```bash
cd DotNet
dotnet run --project ET.Proto2CS/ET.Proto2CS.csproj
```

### 方式二：编译后执行

```bash
cd DotNet
dotnet build ET.Proto2CS/ET.Proto2CS.csproj
dotnet ET.Proto2CS/bin/Debug/net8.0/ET.Proto2CS.dll
```

### 方式三：发布后执行

```bash
cd DotNet
dotnet publish ET.Proto2CS/ET.Proto2CS.csproj -c Release -o publish
./publish/ET.Proto2CS.exe
```

## 使用示例

### 定义Proto消息

```protobuf
// LoginOuter_C_1000.proto
syntax = "proto3";
package ET;

// ResponseType LoginResponse
message LoginRequest // IRequest
{
    int32 RpcId = 1;
    string Account = 2;
    string Password = 3;
}

message LoginResponse // IResponse
{
    int32 RpcId = 1;
    int32 Error = 2;
    string Message = 3;
    int64 UserId = 4;
}
```

### 生成并使用

运行Proto2CS工具后，自动生成对应的C#类和Opcode常量：

```csharp
// 使用示例
var request = LoginRequest.Create();
request.Account = "user";
request.Password = "pass";

var response = await session.Call(request);
if (response.Error == 0)
{
    // 登录成功，使用response.UserId
}

// 自动回收到对象池
request.Dispose();
response.Dispose();

// Opcode常量使用
ushort opcode = LoginOuter.LoginRequest;  // 获取操作码
```

## 注意事项

1. **文件位置**：Proto文件必须放在`Config/Proto/`目录
2. **命名规范**：严格遵循`{模块}_{类型}_{起始码}.proto`格式
3. **RPC消息**：必须包含`RpcId`字段且为第一个字段
4. **错误处理**：Response消息通常包含`Error`和`Message`字段
5. **对象池**：使用`Create()`方法创建，使用`Dispose()`回收
6. **接口继承**：通过注释标记正确的接口类型
7. **跨平台**：生成的代码在Unity和.NET Core中通用

## 故障排除

### 常见错误

1. **找不到Proto目录**
   - 检查`Config/Proto/`是否存在
   - 确认工作目录正确

2. **Opcode冲突**
   - 通常自动解决，如频繁冲突考虑调整哈希算法

3. **类型映射错误**
   - 检查proto文件中使用的类型是否标准

4. **接口标记错误**
   - 确保RPC消息正确标记了Request/Response接口

### 调试技巧

1. **查看生成的代码**：检查`Unity/Assets/Scripts/Model/Core/Share/Proto/ClientServer/`
2. **检查Opcode**：确认常量类中的值是否正确
3. **验证序列化**：测试消息的序列化/反序列化是否正常
4. **对象池监控**：检查对象是否正确回收到池中
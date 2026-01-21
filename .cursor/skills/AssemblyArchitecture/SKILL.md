---
name: Assembly-Architecture
description: ET框架程序集架构分析，详细说明服务端和客户端各程序集的作用、职责划分和依赖关系。
---

# ET框架程序集架构

ET框架采用分层架构设计，将代码按照功能和平台划分为多个程序集，实现清晰的职责分离和模块化管理。本文档详细分析各程序集的作用和架构设计。

## 架构设计原则

### 1. 平台分离
- **服务端（DotNet）**：纯C#环境，无Unity依赖
- **客户端（Unity）**：Unity环境，支持热更新

### 2. 代码分层
- **Model层**：数据定义和纯逻辑实现
- **Hotfix层**：业务逻辑和系统实现
- **View层**：视图相关的数据和逻辑（仅客户端）

### 3. 热更新支持
- Model层不可热更新（需重启）
- Hotfix层支持热更新
- View层支持热更新

---

## 服务端程序集（DotNet）

### ET.Core - 核心框架
**位置**：`DotNet/ET.Core/`  
**作用**：
- 框架核心代码（Entity、Component、EventSystem等）
- 基础工具类（序列化、对象池、定时器等）
- 网络通信基础组件
- 不包含业务逻辑

**依赖关系**：被所有程序集依赖

### ET.Model - 服务端数据模型
**位置**：`DotNet/ET.Model/`  
**作用**：
- 定义服务端使用的Entity和Component
- 包含事件定义和消息协议
- 配置表数据结构定义
- 纯数据和逻辑，不包含Unity相关代码

**代码示例**：
```csharp
// 定义Unit实体（数据结构）
[ChildOf(typeof(UnitComponent))]
public partial class Unit: Entity, IAwake<int>
{
    public int ConfigId { get; set; }
    public float3 Position { get; set; }
    // ... 数据字段定义
}
```

**依赖关系**：依赖ET.Core，被ET.Hotfix依赖

### ET.Hotfix - 服务端业务逻辑
**位置**：`DotNet/ET.Hotfix/`  
**作用**：
- 实现Component的System（生命周期方法）
- 消息处理器（RPC、单向消息）
- 业务逻辑实现
- 支持热更新

**代码示例**：
```csharp
[EntitySystemOf(typeof(Unit))]
public static partial class UnitSystem
{
    [EntitySystem]
    private static void Awake(this Unit self, int configId)
    {
        self.ConfigId = configId;
        // 初始化逻辑
    }
    
    // 扩展方法
    public static UnitTable Config(this Unit self)
    {
        return UnitConfig.Instance.Get(self.ConfigId);
    }
}
```

**依赖关系**：依赖ET.Model和ET.Core

### ET.App - 服务端应用入口
**位置**：`DotNet/ET.App/`  
**作用**：
- 服务端程序主入口
- 初始化各种管理器和服务
- 场景创建和启动逻辑

### 其他工具程序集
- **ET.Loader**：代码加载和反射工具
- **ET.SourceGenerator**：编译时代码生成（分析器和生成器）
- **ET.SourceGeneratorAttribute**：生成器使用的属性
- **ET.Mathematics**：数学运算库
- **ET.Recast**：寻路算法库
- **ET.Proto2CS**：Proto协议文件生成工具

---

## 客户端程序集（Unity）

### ETClient.Core - 客户端核心框架
**位置**：`Unity/Assets/Scripts/Core/`  
**作用**：
- 客户端框架核心代码
- 基础组件和工具类
- 与服务端ET.Core共享大部分代码

**依赖关系**：基础依赖，不依赖Unity引擎

### ETClient.Model - 客户端数据模型
**位置**：`Unity/Assets/Scripts/Model/`  
**作用**：
- 客户端使用的Entity和Component定义
- 事件和消息协议定义
- 配置表数据结构
- 不包含Unity相关代码

**依赖关系**：依赖ETClient.Core

### ETClient.Hotfix - 客户端业务逻辑
**位置**：`Unity/Assets/Scripts/Hotfix/`  
**作用**：
- 客户端Component的System实现
- 消息处理器和网络逻辑
- 游戏逻辑处理
- 支持热更新

**依赖关系**：依赖ETClient.Model

### ETClient.ModelView - 客户端视图数据模型
**位置**：`Unity/Assets/Scripts/ModelView/`  
**作用**：
- 视图层的数据模型定义
- Unity相关组件（不含MonoBehaviour）
- 动画、UI、输入等数据结构
- 可被热更新

**代码示例**：
```csharp
// 视图组件定义（不含MonoBehaviour）
public class AnimatorComponent : Entity, IAwake, IUpdate, IDestroy
{
    public LinearMixerTransition MoveMixer;
    public LinearMixerTransition JumpMixer;
    public AnimancerLayer AttackLayer;
    public AnimancerComponent Animancer { get; set; }
    // ... 数据字段，不包含Unity对象初始化
}
```

**依赖关系**：依赖ETClient.Model，可访问Unity API但不直接操作GameObject

### ETClient.HotfixView - 客户端视图逻辑
**位置**：`Unity/Assets/Scripts/HotfixView/`  
**作用**：
- 视图组件的System实现
- Unity API调用和GameObject操作
- UI逻辑和渲染处理
- 支持热更新

**代码示例**：
```csharp
[EntitySystemOf(typeof(AnimatorComponent))]
public static partial class AnimatorComponentSystem
{
    [EntitySystem]
    private static void Awake(this AnimatorComponent self)
    {
        var unit = self.GetParent<Unit>();
        var obj = unit.GetComponent<GameObjectComponent>().GameObject;
        
        // Unity API调用
        self.Animancer = obj.GetComponent<AnimancerComponent>();
        self.Animancer.Layers.SetMinCount(2);
        // ... 初始化逻辑
    }
    
    [EntitySystem]
    private static void Update(this AnimatorComponent self)
    {
        // 每帧更新逻辑，包含Unity API调用
    }
}
```

**依赖关系**：依赖ETClient.ModelView和ETClient.Hotfix，完全访问Unity API

### GameEntry - 客户端应用入口
**位置**：`Unity/Assets/Scripts/GameEntry/`  
**作用**：
- Unity程序入口
- 初始化客户端各种系统
- 场景管理和生命周期

### 其他工具程序集
- **ETClient.Loader**：客户端代码加载器
- **ETClient.ThirdParty**：第三方库封装
- **ETClient.Editor**：Unity编辑器扩展工具

---

## 程序集依赖关系图

```
服务端架构：
ET.App → ET.Hotfix → ET.Model → ET.Core
                    ↘        ↘
                     ET.Loader  ET.SourceGenerator

客户端架构：
GameEntry → ETClient.HotfixView → ETClient.ModelView → ETClient.Hotfix → ETClient.Model → ETClient.Core
```

## 热更新策略

### 可热更新的程序集
- **ET.Hotfix**（服务端业务逻辑）
- **ETClient.Hotfix**（客户端业务逻辑）
- **ETClient.HotfixView**（客户端视图逻辑）

### 不可热更新的程序集
- **Model层**（数据定义，如ET.Model、ETClient.Model）
- **Core层**（框架核心，如ET.Core、ETClient.Core）
- **App层**（应用程序入口）

## 开发规范

### Model层开发
- 只定义数据结构和纯逻辑方法
- 禁止直接Unity API调用
- 扩展方法放在对应的System类中

### Hotfix层开发
- 实现Component的生命周期方法
- 处理消息和RPC调用
- 实现业务逻辑

### View层开发（仅客户端）
- ModelView：定义视图数据结构
- HotfixView：实现视图逻辑和Unity API调用

## 跨平台共享

### 共享代码位置
- **Model/Core/Share/**：跨平台共享的核心代码
- **Model/*/Share/**：各模块的共享代码

### 平台特定代码
- 使用条件编译（`#if DOTNET`、`#if UNITY_EDITOR`等）
- 平台相关的实现在对应平台目录下

## 最佳实践

1. **职责分离**：严格按照分层架构组织代码
2. **依赖管理**：遵循程序集依赖关系，避免循环依赖
3. **热更新考虑**：频繁变动的逻辑放在Hotfix层
4. **性能优化**：合理使用对象池和缓存机制
5. **代码复用**：通过Share目录实现跨平台代码共享
---
name: code-structure
description: ET框架代码目录结构说明，提供客户端和服务端代码组织架构的详细文档。
---

# ET框架代码目录结构说明

## 概述

ET框架采用前后端分离架构，前后端都是使用C#语言开发。客户端基于Unity引擎，服务端基于.NET 8.0。框架采用模块化设计，核心代码在`Core/Share`目录下被客户端和服务端共享。

## 客户端代码结构 (G:\UnityProject\ETFramework\Unity\Assets\Scripts)

### Core/
框架核心代码目录，包含客户端和服务端共享的核心模块。

#### Core/Share/
共享的核心框架代码，被客户端和服务端同时引用。

- **Config/** - 配置文件相关
- **Entity/** - Entity实体系统核心实现
- **ETTask/** - 异步任务框架ETTask的实现
- **Helper/** - 通用工具类和辅助方法
- **Method/** - 方法扩展和工具方法
- **Network/** - 网络通信核心组件
- **Object/** - 对象管理相关
- **Serialize/** - 序列化相关
- **World/**
  - **EventSystem/** - 事件系统实现
  - **Fiber/** - Fiber调度器和线程模型
  - **IdGenerater/** - ID生成器
  - **Log/** - 日志系统
  - **ObjectPool/** - 对象池管理
  - **Options/** - 配置选项
  - **TimeInfo/** - 时间信息管理
  - **World.cs** - 世界管理器

### Editor/
Unity编辑器扩展和工具。

### GameEntry/
游戏入口点和初始化逻辑。

### Hotfix/
热更新代码目录，包含具体的业务逻辑实现。

- **AI/** - AI相关逻辑
- **Core/** - 核心系统扩展
- **Login/** - 登录系统
- **Move/** - 移动系统
- **Numeric/** - 数值系统
- **Router/** - 路由系统
- **Scene/** - 场景管理
- **StateSync/**
  - **Share/** - 状态同步共享逻辑
- **Unit/** - 单位管理

### HotfixView/
视图层热更新代码，包含Unity相关的UI和渲染逻辑。

- **Camera/** - 相机控制
- **GameUI/** - 游戏UI管理
- **Opera/** - 操作相关
- **Scene/** - 场景视图
- **UI/** - UI组件
- **Unit/**
  - **Attack/** - 攻击相关视图逻辑
- **YooAssets/** - 资源管理

### Loader/
资源加载和初始化相关代码。

- **Console/** - 控制台相关
- **Core/** - 核心加载逻辑
- **ResourceLoad/** - 资源加载管理
- **UI/** - UI加载面板

### Model/
数据模型定义目录，包含数据结构和接口定义。

- **ActorLocation/** - 位置服务相关
- **AI/** - AI数据模型
- **Core/** - 核心数据模型
- **Login/** - 登录相关数据模型
- **Move/** - 移动相关数据模型
- **Numeric/** - 数值相关数据模型
- **Router/** - 路由相关数据模型
- **StateSync/** - 状态同步数据模型
- **Unit/** - 单位数据模型

### ModelView/
模型视图层，连接数据模型和视图显示。

### ThirdParty/
第三方库和工具。

- **Recast/** - 寻路库RecastNavigation
  - **Core/** - 核心组件
  - **Detour/** - Detour寻路
  - **Detour.Crowd/** - 人群模拟
  - **Detour.Dynamic/** - 动态寻路
  - **Detour.Extras/** - 额外工具
  - **Detour.TileCache/** - 瓦片缓存
  - **Recast/** - Recast网格生成
- **SourceGeneratorAttribute/** - 源码生成器属性

## 服务端代码结构 (G:\UnityProject\ETFramework\DotNet)

### ET.App/
服务端应用程序入口。

- **GameRegister/** - 游戏注册和初始化
- **GameServer.cs** - 游戏服务器主类
- **ServerLauncher.cs** - 服务器启动器

### ET.Core/
服务端核心项目，通过项目引用包含客户端的`Core/Share`目录。

**项目引用配置 (ET.Core.csproj):**
```xml
<ItemGroup>
    <Compile Include="..\..\Unity\Assets\Scripts\Core\Share\**\*.cs">
        <Link>Core\Share\%(RecursiveDir)%(FileName)%(Extension)</Link>
    </Compile>
    <Folder Include="Core\" />
</ItemGroup>
```

### ET.GenerateEntity/
实体生成工具。

### ET.Hotfix/
服务端热更新代码，包含服务端特有的业务逻辑。

- **ActorLocation/** - 位置服务实现
- **AOI/** - 视野管理(Area Of Interest)
- **Console/** - 控制台命令处理
- **DB/** - 数据库操作
- **HTTP/** - HTTP服务
- **Login/** - 登录服务
- **Map/** - 地图服务
  - **AOI/** - 地图视野管理
  - **Handler/** - 地图消息处理器
  - **Move/** - 地图移动逻辑
  - **Transfer/** - 地图切换逻辑
  - **Unit/** - 地图单位管理
- **MongoDBSerializer/** - MongoDB序列化
- **Move/** - 移动服务
- **NetInner/** - 内部网络通信
- **Recast/** - 寻路服务
- **Robot/** - 机器人测试相关
- **Router/** - 路由服务
- **StateSync/** - 状态同步服务
- **Watcher/** - 监控服务

### ET.Loader/
代码加载器。

### ET.Mathematics/
数学库。

### ET.Model/
服务端数据模型。

- **Actorlocation/** - 位置服务模型
- **AOI/** - 视野管理模型
- **Console/** - 控制台模型
- **DB/** - 数据库模型
- **HTTP/** - HTTP模型
- **Login/** - 登录模型
- **NetInner/** - 内部网络模型
- **Recast/** - 寻路模型
- **Router/** - 路由模型
- **StateSync/** - 状态同步模型
- **Watcher/** - 监控模型

### ET.Proto2CS/
协议生成工具。

### ET.Recast/
寻路库项目。

### ET.SourceGenerator/
源码生成器。

- **Analyzer/** - 代码分析器
- **CodeFixer/** - 代码修复器
- **Config/** - 配置定义
- **Generator/** - 代码生成器

### ET.SourceGeneratorAttribute/
源码生成器属性定义。

## 代码共享机制

### 客户端服务端共享代码
- **位置**: `Unity/Assets/Scripts/Core/Share/`
- **共享方式**: 服务端通过项目文件引用包含客户端共享代码
- **作用**: 核心框架代码（如Entity系统、ETTask、网络组件等）被前后端共享，避免代码重复

### 代码组织原则
1. **Core/Share**: 纯逻辑代码，无Unity相关代码，可被服务端共享
2. **Model**: 数据定义和接口，前后端共享
3. **Hotfix**: 具体业务逻辑实现，客户端和服务端分离
4. **HotfixView**: Unity视图层代码，仅客户端使用

### 编译和构建
- 客户端: Unity项目编译
- 服务端: .NET 8.0项目编译，通过`ET.Core.csproj`引用共享代码
- 共享代码: 通过`<Compile Include>`指令在服务端项目中链接
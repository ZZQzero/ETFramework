---
name: message-handling-and-login-flow
description: ET框架“客户端登录到服务器”完整链路梳理，并给出 [Message]/[MessageHandler]/[MessageSessionHandler] 与 MessageHandler/MessageSessionHandler/MessageLocationHandler 的选型规则与最佳实践。
---

# MessageHandlingAndLoginFlow - 登录链路与消息处理器选型

## 目标

本 Skill 用于回答两个问题：

- **客户端从 Unity 登录到服务器的完整流程是怎样的？**（涉及 Main Fiber、NetClient Fiber、Router/Realm/Gate/Map）
- **消息“标签/基类”什么时候用哪个？**
  - 消息类型：`[Message]`（含 `ResponseType`）
  - Actor Handler：`[MessageHandler]` + `MessageHandler<...>`
  - Session Handler：`[MessageSessionHandler]` + `MessageSessionHandler<...>`
  - Location Handler：`MessageLocationHandler<...>`（仍配合 `[MessageHandler]` 注册到 Actor Dispatcher）

---

## 三套“消息/分发”系统（不要混用）

### 1) Actor 消息（进程内 / Fiber 间）

- **发送端**：`ProcessInnerSender.Send/Call`（同进程），或 `MessageSender.Send/Call`（跨进程会转 NetInner/Inner 网络）
- **分发器**：`MessageDispatcher`
- **Handler 基类**：`MessageHandler<E, Message>` 或 `MessageHandler<E, Request, Response>`
- **标记**：`[MessageHandler(SceneType.XXX)]`
- **典型用途**：
  - Unity 客户端 **Main Fiber ↔ NetClient Fiber** 通信（登录、断线等）
  - 服务端 **Realm ↔ Gate ↔ LoginCenter ↔ Map** 等 Scene 之间进程内/跨进程通信（通过 ActorId）

关键点：Actor 消息通常 **不经过网络序列化**（同进程）；但依然要求消息类型有 `[Message]`，以便生成器构建 `RequestResponseType` 等映射，支持 RPC 响应创建/校验。

### 2) Session 消息（网络通信 / Client ↔ Server）

- **发送端**：`Session.Send/Call`
- **分发器**：`MessageSessionDispatcher`
- **Handler 基类**：`MessageSessionHandler<Message>` 或 `MessageSessionHandler<Request, Response>`
- **标记**：`[MessageSessionHandler(SceneType.XXX)]`
- **典型用途**：
  - `C2R_LoginAccount`（客户端 → Realm）
  - `C2G_LoginGameGate`、`C2G_EnterGame`（客户端 → Gate）

关键点：Session 消息一定会序列化/反序列化，**必须有非 0 的 opcode**（来自 `[Message(xxx)]`）。

### 3) Location 消息（“按 UnitId 路由”/ Gate 转发到 Map）

- **网络入口（Gate）**：`NetComponentOnReadInvoker_Gate` 对 `ILocationMessage/ILocationRequest` 分支做特殊处理：
  - `ILocationMessage`：直接 `Send(unitId, msg)` 转发到 Map/Unit
  - `ILocationRequest`：`Call(unitId, req)` 转发并把响应再回给客户端
- **Map 侧 Handler 基类**：`MessageLocationHandler<Unit, ...>`
- **标记**：仍然使用 `[MessageHandler(SceneType.Map)]`（注册到 Actor Dispatcher）

关键点：Location 消息的“网络入口”在 Gate，但**真正业务处理在 Map 的 Unit 上**；因此 Map 侧应继承 `MessageLocationHandler`，而不是 `MessageSessionHandler`。

---

## 三个 Attribute 到底分别贴在哪里？

### `[Message]`：贴在“消息类型”上（MessageObject）

用途：
- 让 **Opcode 生成器**收集该消息类型
- 生成 `MessageOpcodeTypeMap`：
  - `OpcodeToMessage`（反序列化工厂）
  - `TypeToOpcode`（序列化写 opcode）
  - `RequestResponseType` / `RequestResponse`（RPC 响应类型映射/工厂）

规则：
- **网络消息（Session/Inner 网络）**：`[Message(非0 opcode)]` 必须唯一且一致（通常由 proto 常量生成）。
- **纯进程内 Actor 消息**：可以使用 `[Message]`（opcode 默认 0），不参与网络映射，但仍参与 `RequestResponseType` 等映射生成。

RPC 约束（生成器强约束）：
- 任何实现 `IRequest` 的消息，若 **不是** `ILocationMessage`，必须有 `ResponseType`，否则会编译期报错（生成器诊断 `ETOP002`）。

### `[MessageHandler]`：贴在“Actor Handler 类”上

用途：
- 被 `ETMessageHandlerRegisterGenerator` 扫描，生成 `GameRegister*.RegisterMessageAuto()` 注册到 `MessageDispatcher`

典型：
- Unity 客户端 NetClient Fiber 的 `Main2NetClient_LoginHandler`
- 服务端 Gate 的 `R2G_GetLoginKeyHandler`
- Map 的 `C2M_TransferMapHandler`（虽然是 Location 消息，但仍走 Actor Dispatcher）

### `[MessageSessionHandler]`：贴在“Session Handler 类”上

用途：
- 被生成器扫描，生成 `GameRegister*.RegisterMessageSessionAuto()` 注册到 `MessageSessionDispatcher`

典型：
- 服务端 Realm 的 `C2R_LoginAccountHandler`
- 服务端 Gate 的 `C2G_LoginGameGateHandler`

注意：
- 生成器里对 `MessageHandlerAttribute` 有一层“基类判断”，理论上你误贴 `[MessageHandler]` 但继承了 `MessageSessionHandler`，也可能被归类到 Session 注册里；**不建议依赖这个容错**，请用正确标签提升可读性。

---

## 三个 Handler 基类什么时候继承？

### 1) `MessageHandler<...>`（Actor 消息）

#### 单向 Actor 消息

- 继承：`MessageHandler<E, Message>`
- 消息接口：`IMessage`
- 示例：NetClient 断线通知（Main → NetClient 或反向）

#### Actor RPC（带 Response）

- 继承：`MessageHandler<E, Request, Response>`
- 消息接口：`IRequest` / `IResponse`
- 特征：
  - 基类会捕获异常并自动 `Reply(fromAddress, response)`
  - `MessageDispatcher` 里会校验 handler 的 `Response` 类型与 `MessageOpcodeTypeMap.RequestResponseType` 一致

适用：**同进程 Fiber 间 RPC**，或 **跨进程的 Actor RPC**（通过 `MessageSender/ProcessOuterSender` 走 Inner 网络中转）。

### 2) `MessageSessionHandler<...>`（网络 Session 消息）

#### 单向 Session 消息

- 继承：`MessageSessionHandler<Message>`
- 消息接口：推荐 `ISessionMessage`

#### Session RPC（带 Response）

- 继承：`MessageSessionHandler<Request, Response>`
- 消息接口：推荐 `ISessionRequest` / `ISessionResponse`
- 特征：
  - 基类会 `session.Send(response)`，并保护 `session.InstanceId` 防止断线后发送
  - Request/Response 会在处理完成后 `Dispose()`（网络反序列化对象必须回收）

适用：**客户端/服务器跨进程通信**（Realm/Gate/Map 对客户端的入口通常都是 Session）。

### 3) `MessageLocationHandler<...>`（Location 路由后的 Actor 处理）

它是 Actor Handler（实现 `IMHandler`），但专门用于：
- 消息接口是 `ILocationMessage / ILocationRequest`（本质也是 `IRequest`）
- 发送端往往不知道目标 ActorId，只知道“Key（如 UnitId）”，由 Location 系统找到 ActorId 再转发

两种形态：
- `MessageLocationHandler<E, Message>`：用于 `ILocationMessage`（基类会**先回一个 MessageResponse**，再异步执行 Run）
- `MessageLocationHandler<E, Request, Response>`：用于 `ILocationRequest`（基类会在发送 response 前用 `CoroutineLockType.MessageLocationSender` 保序）

适用：**客户端发给 Gate，但需要由 Gate 转发给 Map/Unit 处理** 的请求。

---

## 客户端登录到服务器：完整链路（按真实代码走）

### 0) UI 入口（Unity Main Fiber）

- `UILoginPanel.OnLoginClick()` 调用 `LoginHelper.Login(root, account, password)`

### 1) Main Fiber → NetClient Fiber：Actor RPC（登录）

- Main Fiber 调 `ClientSenderComponentSystem.LoginAsync`
  - 构造 `Main2NetClient_Login`（`IRequest`）
  - `ProcessInnerSender.Call(self.netClientActorId, main2NetClientLogin)` 发到 NetClient Scene
- NetClient Scene 处理：
  - `Main2NetClient_LoginHandler : MessageHandler<Scene, Main2NetClient_Login, NetClient2Main_Login>`
  - 拉 Router 列表、创建 `NetComponent`
  - `CreateRouterSession(realmAddress, account, password)`
  - `session.Call(C2R_LoginAccount)`（网络 Session RPC 到 Realm）
  - 保存 `SessionComponent.Session = session`，把 `Token/UserInfo` 回填到 Actor Response

### 2) Main Fiber → NetClient Fiber：Actor RPC（转发任意 Session RPC）

Main Fiber 通过 `ClientSenderComponentSystem.Call(IRequest request)` 走一层包装：

- 包装消息：`A2NetClient_Request : IRequest`，字段 `MessageObject = request`
- NetClient 处理：`A2NetClient_RequestHandler`
  - `res = await root.GetComponent<SessionComponent>().Session.Call(request.MessageObject)`
  - 把 `res.RpcId` 设置回 Main 的 rpcId，并塞进 `A2NetClient_Response.MessageObject`

在登录流程里，这一层用于：
- `C2R_GetRealmKey`（客户端向 Realm 换取 GateKey + GateAddress）

### 3) NetClient Session → Realm：`[MessageSessionHandler]`

服务端 Realm：
- `C2R_LoginAccountHandler : MessageSessionHandler<C2R_LoginAccount, R2C_LoginAccount>`
- `C2R_GetRealmKeyHandler : MessageSessionHandler<C2R_GetRealmKey, R2C_GetRealmKey>`
  - 通过 `MessageSender.Call(gateActorId, R2G_GetLoginKey)` 向 Gate 请求 key

### 4) Realm → Gate：Actor RPC（跨进程也按 Actor 语义）

服务端 Gate：
- `R2G_GetLoginKeyHandler : MessageHandler<Scene, R2G_GetLoginKey, G2R_GetLoginKey>`

`MessageSender/ProcessOuterSender` 会根据 `actorId.Process` 判断是否同进程：
- 同进程：直接 `ProcessInnerSender.Call`
- 跨进程：发到 `NetInner`，由 `ProcessOuterSender` 走 Inner 网络把 Actor RPC 中转过去

### 5) Client → Gate：Session RPC（登录 Gate + EnterGame）

NetClient 的 `Main2NetClient_LoginGameHandler`：
- `gateSession = netComponent.CreateRouterSession(gateEndPoint, account, account)`
- `gateSession.Call(C2G_LoginGameGate)` → Gate `C2G_LoginGameGateHandler`
- `gateSession.Call(C2G_EnterGame)` → Gate `C2G_EnterGameHandler`

### 6) Gate → Map：Location RPC（按 UserId/UnitId 路由）

Gate 在网络读入时：
- `NetComponentOnReadInvoker_Gate`：
  - `ILocationRequest`：`locationSender.Call(unitId, req)`，再把 response 回给客户端

Map 上处理：
- `C2M_TransferMapHandler : MessageLocationHandler<Unit, C2M_TransferMap, M2C_TransferMap>`
- `G2M_SecondLoginHandler : MessageLocationHandler<Unit, G2M_SecondLogin, M2G_SecondLogin>`

---

## 选型速查表（最常用）

| 需求 | 消息接口 | 发送 API | Handler 标记 | Handler 基类 | 典型例子 |
|---|---|---|---|---|---|
| Main ↔ NetClient（同进程 Fiber） | `IMessage/IRequest` | `ProcessInnerSender.Send/Call` | `[MessageHandler]` | `MessageHandler<...>` | `Main2NetClient_LoginHandler` |
| Client ↔ Realm/Gate（网络） | `ISessionMessage/ISessionRequest`（推荐） | `Session.Send/Call` | `[MessageSessionHandler]` | `MessageSessionHandler<...>` | `C2R_LoginAccountHandler` |
| Realm ↔ Gate（跨进程 Actor RPC） | `IRequest/IResponse` | `MessageSender.Call(actorId, req)` | `[MessageHandler]` | `MessageHandler<...>` | `R2G_GetLoginKeyHandler` |
| Client 发到 Gate，但业务在 Map/Unit | `ILocationRequest/ILocationMessage` | 客户端 `Session.Call/Send`；Gate 自动转发 | Map 侧 `[MessageHandler]` | `MessageLocationHandler<...>` | `C2M_TransferMapHandler` |

---

## 自动注册（你不需要手写 Register）

运行时注册入口（已在工程里调用）：

- Unity：`GameEntry.Awake()` 会调用：
  - `GameRegisterHotfix.RegisterMessageAuto()`
  - `GameRegisterHotfix.RegisterMessageSessionAuto()`
- Server：`GameServer.Register()` 会调用：
  - `GameRegisterHotfix.RegisterMessageAuto()`
  - `GameRegisterHotfix.RegisterMessageSessionAuto()`

因此：你只要保证 Handler 类贴对 Attribute（并继承对的基类），就会被源生成器自动注册到对应 Dispatcher。

---

## 常见坑（真实会踩）

1) **把网络消息写成 Actor Handler**
- 现象：客户端 `session.Call()` 发送后，服务端 `MessageSessionDispatcher` 找不到 handler
- 原因：你写了 `[MessageHandler] + MessageHandler<...>`，但网络读入只会分发到 `MessageSessionDispatcher`
- 修复：改为 `[MessageSessionHandler] + MessageSessionHandler<...>`，并确保消息实现 `ISessionMessage/ISessionRequest`

2) **Location 消息被 MessageHandler 处理**
- `MessageHandler.GetRequestType()` 会对 `ILocationMessage` 打 Log Error（提示你该用 `MessageLocationHandler`）
- 修复：Map 侧 handler 改继承 `MessageLocationHandler`

3) **IRequest 没写 ResponseType**
- 生成器会报错：`ETOP002 Missing ResponseType`
- 修复：给 request 加 `[ResponseType(nameof(XXXResponse))]`

4) **不该用 opcode=0 的消息走网络**
- `MessageSessionDispatcher.RegisterMessageSession` 会用 `TypeToOpcode` 找 opcode，若为 0 会抛异常
- 修复：网络消息必须有非 0 opcode（一般由 proto 生成，不要手写乱填）

---

## 最简结论（写新消息时按这个做）

- **消息类型**：一律继承 `MessageObject`，贴 `[Message(...)]`；如果是 `IRequest`，贴 `ResponseType`
- **进程内 Actor/Fiber 通信**：用 `MessageHandler<...>` + `[MessageHandler(SceneType.X)]`
- **网络 Session 通信**：用 `MessageSessionHandler<...>` + `[MessageSessionHandler(SceneType.X)]`，消息推荐实现 `ISessionMessage/ISessionRequest`
- **Gate 转发到 Map/Unit 的业务**：消息实现 `ILocationRequest/ILocationMessage`，Map 侧用 `MessageLocationHandler<...>`（仍贴 `[MessageHandler(SceneType.Map)]`）


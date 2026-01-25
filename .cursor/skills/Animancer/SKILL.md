---
name: Animancer-Usage
description: Animancer（PlayableGraph）在本项目中的工程级用法：AnimancerComponent/Graph/Layer/State 的关系、Layer0/Layer1 分层架构、事件绑定、淡入淡出、速度与暂停（HitStop）等最佳实践与常见坑。
---

# Animancer 工程级使用规范（结合本项目）

## 目标

本项目客户端动画使用 **Animancer（PlayableGraph）**，以代码驱动方式替代 Animator Controller 状态机。

本文档整理：
- **核心对象模型**：`AnimancerComponent` / `AnimancerGraph` / `AnimancerLayer` / `AnimancerState`
- **Layer 分层策略**：Layer0（Move/Jump）+ Layer1（Attack 覆盖）
- **播放/切换/淡入淡出**：`Play`、`StartFade`、`FadeMode`、`Weight`
- **事件系统（Animancer Events）**：用事件驱动判定窗口与轨道（HitBox/VFX/SFX/Active）
- **暂停/顿帧（HitStop）**：正确冻结/恢复方式（Graph Pause/Unpause）与时间源
- **常见坑**：Animator.speed 无效、EndEvent 重复触发、State 复用事件残留、Layer 权重为 0 等

---

## 1. Animancer 的核心对象关系（Runtime 源码结论）

### 1.1 `AnimancerComponent`：MonoBehaviour 持有 Graph

- `AnimancerComponent.Graph` 是内部核心（会懒加载初始化 Graph）。
- `AnimancerComponent.Layers`、`States`、`Parameters`、`Events` 都是 `Graph` 的便捷入口。

源码位置：
- `Unity/Packages/com.kybernetik.animancer/Runtime/AnimancerComponent.cs`

### 1.2 `AnimancerGraph`：PlayableGraph 的管理器

- Graph 管理 PlayableGraph 的 **播放/暂停/评估**：
  - `PauseGraph()`：底层 `PlayableGraph.Stop()`，冻结图在当前状态
  - `UnpauseGraph()`：底层 `PlayableGraph.Play()`，继续播放
  - `IsGraphPlaying`：判断图是否在 Play 状态

源码位置：
- `Unity/Packages/com.kybernetik.animancer/Runtime/Core/AnimancerGraph.cs`

### 1.3 `AnimancerLayer`：每层一个 `AnimationMixerPlayable`，独立管理 State

Animancer 的 Layer 不是 Unity Animator Controller 那套 Layer，而是 PlayableGraph 的分层输出。

关键点（来自 `AnimancerLayer` 源码）：
- 每个 Layer 内部是一个 `AnimationMixerPlayable`，子输入是各个 `AnimancerState`
- `CurrentState` 表示该 Layer 最近一次通过 Layer 的 `Play/CrossFade` 系列启动的 State（不包含“只控制 state 自己”的 Play）
- `Mask`（Pro-Only）用于 AvatarMask（只影响部分骨骼）
- `Weight` 决定该 Layer 对最终输出的贡献；若 `Weight==0`，Layer 的事件更新也会跳过（内部 `UpdateEvents` 有 `Weight<=0` 的早退）
- `SetLayerWeightOnPlay` 默认 true：当 Layer weight 为 0 且播放时，会自动把 Layer weight 拉到 1（除非你关闭它）

源码位置：
- `Unity/Packages/com.kybernetik.animancer/Runtime/Core/Nodes/AnimancerLayer.cs`

### 1.4 `AnimancerState`：一条动画/混合节点的运行时状态

核心能力：
- `Time / NormalizedTime / Length`：播放进度
- `IsPlaying`：可暂停/恢复单个 state（内部会调用 `Playable.Pause/Play`）
- `Weight`：混合权重（淡入淡出由 `FadeGroup` 驱动）
- `Events`：每个 state 的事件序列（可绑定命中窗口、段结束等）

源码位置：
- `Unity/Packages/com.kybernetik.animancer/Runtime/Core/Nodes/AnimancerState.cs`

---

## 2. 本项目的 Animancer 分层架构（强约束）

### 2.1 `AnimatorComponentSystem`：初始化 Layer0/Layer1

文件：
- `Unity/Assets/Scripts/HotfixView/Unit/AnimatorComponentSystem.cs`

工程约束：
- **Layer0：Move/Jump 基础层**（由 `MoveMixer`/`JumpMixer` 驱动）
- **Layer1：Attack 覆盖层**（`AttackLayer`，权重默认 0，使用 AvatarMask）
- 禁用 RootMotion：由 `CharacterControllerComponent` 自己控制移动（避免 RootMotion 与逻辑移动打架）

### 2.2 Layer0（Move/Jump）如何驱动

特点：
- 每帧在 `AnimatorComponentSystem.Update` 根据 Ground 状态切换 MoveMixer/JumpMixer
- 通过 `Mixer.State.Parameter` 输入速度参数（推荐：只改参数，不频繁重新 Play）

### 2.3 Layer1（Attack）如何驱动

特点：
- Attack 播放走 `AttackComponentSystem.StartAttack`，在 `AttackLayer` 播放 `ClipTransition`
- 起手（segmentIndex==0）通常 0 秒淡入，立刻把 Layer1 置权重为 1（确保覆盖立即生效）
- 后续段走 `FadeDuration`，同时 `StartFade(1, fadeIn)` 保持权重收敛

---

## 3. 播放、淡入淡出、权重：必须理解的语义

### 3.1 `AnimancerLayer.Play(state)` 的强语义

来自 `AnimancerLayer` 源码的关键行为：
- **会 Stop 同层其它 ActiveStates**
- **会把目标 state 直接设为 Weight=1 并 IsPlaying=true**
- 若 `SetLayerWeightOnPlay` 且 Layer `Weight==0` 且没有 FadeGroup，会把 Layer Weight 自动设为 1

结论：
- 想“同层互斥、只有一个 state 播放” → 用 `layer.Play(...)`
- 想“同层多个 state 共存（例如手动 Mixer）” → 不要直接用 Layer.Play；应使用 `ManualMixerState` / `LinearMixerState` 之类混合节点

### 3.2 `StartFade`/`FadeMode`（淡入淡出）语义

在 Layer.Play(state, fadeDuration, mode) 里：
- 会创建/复用 `FadeGroup`：对目标 state FadeIn，其他 state FadeOut
- **如果 Layer TargetWeight==0**，它会先把 Layer 自己 Fade 到 1（SetLayerWeightOnPlay 开启时）
- `FadeMode.FromStart/NormalizedFromStart` 会触发“克隆 weightless state”逻辑（用于从头淡入，避免状态复用造成时间/权重干扰）

工程建议：
- 本项目 Segment 切段建议保持 `FadeDuration >= 0.05f`，避免突变带来的抖动
- 需要“从头开始”时，不要手写 stop+play，优先用 `FadeMode.FromStart`（但注意会触发 clone 机制）

---

## 4. Animancer Events：本项目的“时间轴驱动”核心

### 4.1 为什么要用 Events（而不是 Update 里比时间）

工程价值：
- 减少每帧判断，逻辑集中在动画时间点
- 同 Clip 状态复用时更可控（前提是每次绑定前 `events.Clear()`）
- 更贴近“技能编辑器时间轴”工作流

### 4.2 本项目 `AttackComponentSystem.BindAnimancerEvents` 的规范

文件：
- `Unity/Assets/Scripts/HotfixView/Unit/Attack/AttackComponentSystem.cs`

核心约束（建议保持）：
- **每次 StartAttack 后都重新绑定事件，并清理旧事件**：避免同 clip 复用 state 导致残留
- **不要用 EndEvent（events.OnEnd）**：EndEvent 超过结束点后会“每帧触发”，容易重复逻辑；本项目改用“普通事件触发一次”是正确做法
- HitBox/VFX/SFX/Active 轨道都由事件触发，避免逻辑散落

---

## 5. 速度与暂停：Animancer 与 Animator 的差异（必须遵守）

### 5.1 `Animator.speed` 对 Animancer 无效

Animancer Runtime 明确提示：
- `Animator.speed` 只影响 Animator Controller，不影响 Playables API
- 若需要控制整体速度，应使用 `AnimancerGraph.Speed`（或对具体 Node/State 设置 Speed）

源码位置：
- `Unity/Packages/com.kybernetik.animancer/Runtime/Editor/OptionalWarning.cs`（`OptionalWarning.AnimatorSpeed`）

### 5.2 HitStop（顿帧）正确做法

优先级（建议）：
1) **冻结整个 Graph（推荐用于 HitStop）**：`animancer.Graph.PauseGraph()` / `UnpauseGraph()`
2) 冻结单个 state：`state.IsPlaying = false/true`（适用于只想冻结某个层或某个状态）

为什么不建议改 `Playable.Speed = 0`：
- Speed=0 仍可能存在评估/事件边界等复杂情况
- Animancer 源码提供了明确的 PauseGraph/UnpauseGraph 语义，且更接近“冻结图”

### 5.3 HitStop 计时使用 `Time.realtimeSinceStartupAsDouble`

工程理由：
- 单调递增，不受系统时间校正影响
- 与网络校时（ServerMinusClientTime）无关
- 适合“真实经过 N ms 就结束”的表现型计时

> 注意：HitStop 的“结束计时”与战斗逻辑的“combat-time”可以分离：前者用 realtime，后者可用“顿帧期间不推进”的自定义时间域。

---

## 6. 与 ET 系统协作的工程约束（本项目建议）

### 6.1 谁可以控制动画

建议的控制权边界（避免系统打架）：
- `AnimatorComponentSystem`：只负责 Layer0 的 Move/Jump 驱动与基础资源加载
- `AttackComponentSystem`：只负责 AttackLayer（Layer1）播放与事件绑定
- `CombatFeedbackComponentSystem`（HitStop）：只负责“暂停/恢复 AnimancerGraph”与时间域，不参与具体动画选择
- `HitReactionComponentSystem`：负责受击动画/位移逻辑，但若需要暂停动画，应通过 CombatFeedback 请求（不要自己 PauseGraph）

### 6.2 Layer0 与 Layer1 的交互（关键）

你项目里攻击时把 Layer0State.Speed = 0，这是合理的“锁基础动作”，但必须保证：
- 退出攻击/进入 Recovery 淡出时恢复 Layer0 的 Speed（否则角色会卡在移动层停止）
- AttackLayer fade out 结束后 Layer0 能继续接管表现

---

## 7. 常见坑清单（排障用）

- **攻击段卡死/切段不触发**
  - 检查是否用了 EndEvent（每帧触发导致重复/中断）
  - 检查 state 复用时是否 `events.Clear()` 后重绑
  - 检查 `AttackLayer.Weight` 是否为 0（Layer 权重为 0 时事件更新会被跳过）

- **以为 `Animator.speed=0` 能顿帧**
  - Animancer 无效（看 OptionalWarning.AnimatorSpeed）
  - 正确：Graph.PauseGraph 或 state.IsPlaying=false

- **多系统暂停/恢复互相覆盖**
  - 必须集中在一个系统执行 Pause/Unpause（建议 CombatFeedback）
  - 恢复时记录“之前是否在播放”，避免强行 Unpause

---

## 8. 快速索引（源码/项目入口）

### Animancer Runtime
- `AnimancerComponent`：`Unity/Packages/com.kybernetik.animancer/Runtime/AnimancerComponent.cs`
- `AnimancerGraph`：`Unity/Packages/com.kybernetik.animancer/Runtime/Core/AnimancerGraph.cs`
- `AnimancerLayer`：`Unity/Packages/com.kybernetik.animancer/Runtime/Core/Nodes/AnimancerLayer.cs`
- `AnimancerState`：`Unity/Packages/com.kybernetik.animancer/Runtime/Core/Nodes/AnimancerState.cs`
- `OptionalWarning.AnimatorSpeed`：`Unity/Packages/com.kybernetik.animancer/Runtime/Editor/OptionalWarning.cs`

### 本项目用法
- `AnimatorComponentSystem`：`Unity/Assets/Scripts/HotfixView/Unit/AnimatorComponentSystem.cs`
- `AttackComponentSystem`：`Unity/Assets/Scripts/HotfixView/Unit/Attack/AttackComponentSystem.cs`
- HitStop：`Unity/Assets/Scripts/HotfixView/Combat/CombatFeedbackComponentSystem.cs`


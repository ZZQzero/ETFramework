---
name: skill-editor
description: Unity技能编辑器窗口，支持多轨道技能动画编辑和实时预览。提供时间轴控制、属性面板编辑、播放预览等技能开发工具。
---

# Unity Skill Editor Window

Unity技能编辑器是一个专业的技能开发工具，支持可视化编辑技能的各个组成部分。

## 核心功能模块

### 1. UI界面管理
- **CreateGUI()** - 初始化UI布局，加载UXML和USS文件
- **InitData()** - 初始化数据绑定和事件监听
- **InitRightPanel()** - 初始化右侧属性面板

### 2. 轨道数据管理
- **InitTrackData()** - 初始化轨道数据结构
- **CreateTrack()** - 创建轨道UI元素
- **RefreshTrackContent()** - 刷新轨道内容显示

### 3. 时间轴控制
- **InitTimelineRuler()** - 初始化时间轴标尺
- **DrawTimelineRulerMarks()** - 绘制时间刻度线
- **UpdateTimelineContentWidth()** - 更新时间轴内容宽度

### 4. 播放预览系统 (Playback.cs)
- **OnPlayButtonClicked()** - 开始播放预览
- **OnStopButtonClicked()** - 停止播放预览
- **OnLoopButtonClicked()** - 切换循环播放
- **UpdatePlayback()** - 更新播放状态（每帧调用）

### 5. 时间轴交互 (Timeline.cs)
- **OnClipMouseDown()** - Clip鼠标按下事件
- **OnClipMouseMove()** - Clip拖拽移动
- **OnClipMouseUp()** - Clip鼠标释放
- **HandleClipResize()** - 处理Clip大小调整

### 6. 视图模式切换
- **OnToggleViewModeClicked()** - 切换全局/聚焦视图模式
- **UpdateViewModeButtonText()** - 更新视图模式按钮文本
- **ApplyViewMode()** - 应用视图模式变化

## 主要数据结构

### 配置数据
- **AttackConfig** - 技能配置数据
- **AttackConfigAsset** - Unity资源形式的配置

### 轨道类型
- **AnimationTrack** - 动画轨道
- **EffectTrack** - 特效轨道
- **SoundTrack** - 音效轨道
- **HitBoxTrack** - 碰撞箱轨道
- **ActiveTrack** - 激活轨道

### Clip类型
- **AnimationClipItem** - 动画片段
- **EffectClipItem** - 特效片段
- **SoundClipItem** - 音效片段
- **HitBoxClipItem** - 碰撞箱片段
- **ActiveClipItem** - 激活片段

## 关键交互

### 选择和编辑
- **OnObjectFieldChanged()** - 配置或对象选择变化时重新初始化
- **OnTrackSelected()** - 轨道选中事件
- **OnClipSelected()** - Clip选中事件
- **UpdateTrackInfo()** - 更新右侧面板轨道信息

### 键盘快捷键
- **Delete键** - 删除选中的Clip
- **全局键盘事件监听** - 在root元素上注册

## 视图模式

### Global模式
- 显示所有轨道
- 完整的时间轴视图
- 适合整体技能编辑

### ClipFocus模式
- 只显示选中AnimationClip相关的轨道
- 聚焦特定动画片段
- 适合精细化编辑

## 播放控制

### 基本控制
- **播放按钮** - 开始/继续播放
- **停止按钮** - 停止播放并回到起点
- **循环按钮** - 切换循环播放模式

### 速度控制
- **InitPlaybackSpeedControl()** - 初始化速度控制UI
- **播放速度范围** - 0-6倍速可调

### 进度控制
- **InitPlayHead()** - 初始化播放进度条
- **拖拽播放头** - 手动调整播放进度

## Clip操作功能

### 添加Clip
- **OnAddEffectButtonClicked()** - 添加特效Clip到选中动画片段
- **OnAddSoundButtonClicked()** - 添加音效Clip到选中动画片段
- **OnAddHitboxButtonClicked()** - 添加碰撞箱Clip到选中动画片段
- **OnAddActiveButtonClicked()** - 添加激活Clip到选中动画片段
- **GetOrCreateTrackForAnimationClip()** - 为动画片段创建或获取对应轨道

### 删除Clip
- **DeleteClip()** - 删除指定的Clip
- **CleanupEmptyNonAnimationTracks()** - 清理空的非动画轨道

### 数据同步
- **SyncDraggedClipToConfig()** - 将拖拽后的Clip数据同步到配置
- **SyncOwnerChildClipsToOwner()** - 同步子Clip与父动画Clip的时间关系
- **RecomputeAnimationEndsAround()** - 重算动画结束时间

## 数据持久化

### 保存功能
- **SaveDirtyAssets()** - 保存已修改的资源（窗口关闭时自动调用）
- **SaveConfigAsset()** - 手动保存当前配置
- **MarkAssetDirty()** - 标记资源为已修改状态

### 数据验证
- **数据完整性检查** - 确保配置数据的有效性
- **引用关系验证** - 验证轨道和Clip的引用关系

## 预览系统扩展

### Scene视图预览
- **OnSceneGUI()** - 在Scene视图中绘制预览元素
- **DrawMovementTrajectoryForCurrentContext()** - 绘制移动轨迹
- **DrawPreviewHitBoxesAtCurrentTime()** - 绘制当前时间的HitBox预览
- **DrawPreviewMovementProgress()** - 绘制移动进度

### VFX预览系统
- **PreviewVfxInstance** - 特效预览实例管理
- **特效生命周期管理** - 预览特效的创建、更新、销毁
- **特效跟随和偏移** - 支持特效相对于角色的位置调整

### SFX预览系统
- **PreviewSfxInstance** - 音效预览实例管理
- **音频播放控制** - 使用Unity Editor音频API播放预览音效
- **音量控制** - 预览时支持音量调整

### 激活状态预览
- **previewOriginalActiveStates** - 保存原始激活状态
- **对象激活控制** - 预览时动态控制对象的激活状态
- **相对路径解析** - 支持基于角色子对象的相对路径激活

### 命中检测预览
- **previewHitTargets** - 命中目标管理
- **previewActiveHitBoxes** - 激活的HitBox及其命中目标
- **previewTriggeredKeyFrameHitBoxes** - 关键帧HitBox触发去重

## 数据验证

### 完整性检查
- **配置数据验证** - 确保AttackConfig数据的有效性
- **轨道引用验证** - 验证轨道和Clip的引用关系正确性
- **时间范围检查** - 确保Clip时间在有效范围内

## 键盘快捷键

### 编辑操作
- **Delete键** - 删除选中的Clip

## 高级编辑功能

### 拖拽操作
- **拖拽Clip移动** - 在时间轴上拖拽Clip改变开始时间
- **拖拽Clip缩放** - 拖拽Clip边缘调整持续时间
- **拖拽资源创建** - 从Project窗口拖拽资源到轨道自动创建Clip
- **多选拖拽铺开** - 多选Clip拖拽时自动依次排列避免重叠

### 轨道管理
- **轨道自动创建** - 当添加Clip时自动创建对应的轨道
- **空轨道清理** - 删除最后一个Clip后自动清理空轨道
- **轨道类型区分** - 不同轨道类型有不同的视觉样式和行为

### 时间同步
- **归一化时间转换** - 在动画片段内的相对时间与绝对时间的自动转换
- **父子关系同步** - AnimationClip移动时子Clip自动同步位置
- **时间约束检查** - 确保Clip时间在有效范围内

## 资源管理

### 拖拽创建
- **动画Clip拖拽** - 从Project拖拽AnimationClip创建动画轨道
- **音频Clip拖拽** - 从Project拖拽AudioClip创建音效Clip
- **预制体拖拽** - 从Project拖拽Prefab创建特效Clip
- **子对象拖拽** - 从Hierarchy拖拽角色子对象设置激活目标

### 引用管理
- **资源引用跟踪** - 自动跟踪和管理所有资源引用
- **引用有效性检查** - 检测并警告无效的资源引用
- **资源替换** - 支持更换Clip引用的资源

## 可视化反馈

### 高亮显示
- **选中状态高亮** - 选中的轨道和Clip有视觉高亮
- **拖拽预览** - 拖拽时显示目标位置预览
- **约束边界提示** - 拖拽到边界时显示红色警告线
- **类型颜色区分** - 不同类型的Clip使用不同颜色

### 状态指示
- **播放进度条** - 实时显示当前播放位置
- **循环状态指示** - 循环按钮的视觉状态变化
- **时间显示** - 当前时间和总时长的实时显示
- **缩放倍数显示** - 当前时间轴缩放倍数的显示
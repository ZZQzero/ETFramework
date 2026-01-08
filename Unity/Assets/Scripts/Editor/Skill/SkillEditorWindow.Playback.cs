using System;
using System.Collections.Generic;
using System.Reflection;
using Animancer;
using ET;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public partial class SkillEditorWindow : EditorWindow
{
    #region 播放预览：公共状态（按播放类型分类）

    private const string LOOPING_CLASS = "is-looping";

    #region 播放驱动（EditorApplication.update）
    // Editor 下的播放驱动：用 EditorApplication.update 推进（不依赖 PlayMode）
    private double lastEditorUpdateTime;
    private bool hasLastEditorUpdateTime;
    #endregion

    // 预览状态缓存：减少重复 Play（尤其在拖拽/播放过程中）
    // 注意：编辑器预览采用“确定性采样”，每次采样都会 Stop Graph 并重新应用当前时间点的姿态，
    // 因此不依赖缓存状态来减少 Play（避免跨段残留权重/过渡混合导致变形）。

    #region Animation 预览设置（Animator/Animancer）
    // 预览期间关闭 RootMotion，避免编辑器场景中的对象被“推走”
    private bool? cachedApplyRootMotion;
    private AnimatorCullingMode? cachedCullingMode;
    private bool? cachedFireEvents;
    private AnimatorUpdateMode? cachedUpdateMode;

    // 预览融合：使用 Animancer 的 ManualMixerState（Pro-Only）实现确定性的两段权重混合。
    // 这样拖拽时间轴/播放时看到的融合与运行时 FadeDuration 语义一致。
    private ManualMixerState previewBlendMixer;
    #endregion

    #region HitStop 预览（顿帧）
    // === 预览：顿帧（HitStop）===
    // 运行时：命中时把 CurrentAnimState.Speed 置 0，并在 durationMs 后恢复。
    // 编辑器：用“冻结 currentPlaybackTime”的方式模拟（播放状态下才有时间流动，拖拽采样不需要模拟顿帧时间流）。
    private int previewHitStopRemainingMs;
    private float previewLastPlaybackTime;
    private bool hasPreviewLastPlaybackTime;
    #endregion

    #region VFX 预览（特效）
    // === 预览：VFX ===
    // 说明：
    // - 运行时 VFX 由 AttackComponentSystem.BindAnimancerEvents 驱动。
    // - 编辑器预览不走运行时消息/组件，因此在 EditorApplication.update 中按时间区间触发。
    // - 生命周期以“预览时间轴 globalTimeSeconds”为准，而不是 wall-clock，保证暂停/顿帧/倍速下确定性一致。
    private readonly List<PreviewVfxInstance> previewVfxInstances = new List<PreviewVfxInstance>(32);
    private int previewVfxSpawnCounter;

    private sealed class PreviewVfxInstance
    {
        public GameObject GameObject;
        public VisualEffectData Data;
        public float EndTime; // globalTimeSeconds；<=0 或 Infinity 表示不自动结束
        public float StartTime; // globalTimeSeconds
        public AnimationClip LegacyAnimationClip; // 若该特效对象带 Animation(legacy)，用于编辑器预览采样

        // SceneView 交互编辑：用于检测“用户直接用 Unity Transform Gizmo 改了实例 Transform”
        public bool HasSyncedPose;
        public Vector3 LastSyncedPosition;
        public Quaternion LastSyncedRotation;
    }
    #endregion

    #region SFX 预览（音效）
    // === 预览：SFX（Editor 预览播放）===
    // Unity 在非 PlayMode 下不建议使用 AudioSource 播放，因此这里采用 UnityEditor 内部的 AudioUtil（反射调用）。
    // 约束：AudioUtil API 在不同 Unity 版本/平台可能变化，因此要做兼容性兜底（找不到方法就静默跳过）。
    private readonly List<PreviewSfxInstance> previewSfxInstances = new List<PreviewSfxInstance>(32);

    private sealed class PreviewSfxInstance
    {
        public AudioClip Clip;
        public float EndTime; // globalTimeSeconds；主要用于未来做精细停止，这里先用于统计/兜底
    }

    private static Type audioUtilType;
    private static MethodInfo audioUtilPlayPreviewClip;
    private static MethodInfo audioUtilStopAllPreviewClips;
    private static MethodInfo audioUtilSetPreviewVolume;
    private static bool audioUtilReflectionInited;
    #endregion

    #region Active 预览（挂载对象显隐/启用）
    // 说明：
    // - 对齐 Unity Timeline 的 Activation Track 语义：在区间内把目标 SetActive(Active)。
    // - 目标对象来自角色层级内的“相对路径”，不实例化。
    // - 预览期间需要记录被修改对象的原始 activeSelf，并在 Stop/Loop/关闭窗口时恢复。
    private readonly Dictionary<GameObject, bool> previewOriginalActiveStates = new Dictionary<GameObject, bool>(32);
    #endregion

    #endregion

    #region PreviewObject（最小：定位 Animancer + 绑定 Animator）

    // 约束：场景实例上存在 AnimancerComponent（或其子节点上）。
    // 目标：保持简单，只做“找到可用的 AnimancerComponent，并确保它的 Animator 绑定正确”。
    private GameObject previewSource;

    private void EnsurePreviewObject()
    {
        var src = selectObj != null ? selectObj.value as GameObject : null;
        if (src == null)
        {
            DestroyPreviewObject();
            return;
        }

        // 已绑定且目标未变：避免每帧重复 GetComponentInChildren
        if (ReferenceEquals(previewSource, src) && animancer != null)
        {
            EnsurePreviewAnimatorSettings();
            return;
        }

        previewSource = src;

        // 直接复用场景对象（或其子节点）上的 AnimancerComponent
        animancer = src.GetComponentInChildren<AnimancerComponent>(true);
        if (animancer == null)
        {
            return;
        }

        // 确保 AnimancerComponent 绑定了正确的 Animator（常见：Animator 在子节点）
        if (animancer.Animator == null)
        {
            var animator = animancer.GetComponent<Animator>();
            animator ??= src.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                animancer.Animator = animator;
            }
        }

        EnsurePreviewAnimatorSettings();
    }

    private void DestroyPreviewObject()
    {
        CleanupPreviewVfx();
        CleanupPreviewSfx();
        CleanupPreviewAttachedActives();
        previewSource = null;
        animancer = null;

        RestorePreviewAnimatorSettingsIfNeeded();
    }

    #endregion

    #region 播放预览：控制（按钮/AnimationMode/Animator设置）

    // 编辑器预览应使用 AnimationMode 进行“可回滚采样”，避免任何姿态/scale 等属性残留到场景对象上。
    // 注意：AnimationMode 是全局状态，必须只在本窗口拥有时停止。
    private bool ownsAnimationMode;

    private void EnsureAnimationMode()
    {
        if (AnimationMode.InAnimationMode())
        {
            return;
        }

        AnimationMode.StartAnimationMode();
        ownsAnimationMode = true;
    }

    private void StopAnimationModeIfOwned()
    {
        if (ownsAnimationMode && AnimationMode.InAnimationMode())
        {
            AnimationMode.StopAnimationMode();
        }

        ownsAnimationMode = false;
    }

    private void UpdateLoopButtonVisual()
    {
        var loopBtn = root?.Q<Button>("Loop");
        loopBtn?.EnableInClassList(LOOPING_CLASS, isLooping);
    }

    private void UpdatePlayButtonText()
    {
        var playBtn = root?.Q<Button>("Play");
        if (playBtn != null)
        {
            playBtn.text = isPlaying ? "Pause" : "Play";
        }
    }

    /// <summary>
    /// Play按钮点击事件
    /// </summary>
    private void OnPlayButtonClicked()
    {
        if (isPlaying)
        {
            PausePreviewPlayback();
            return;
        }

        StartPreviewPlayback();
    }

    /// <summary>
    /// Stop按钮点击事件
    /// </summary>
    private void OnStopButtonClicked()
    {
        // Stop：回到默认姿态（而不是采样到第 0 秒攻击姿态）
        StopPreviewPlayback(resetTime: true, sampleAfterStop: false);
    }

    /// <summary>
    /// Loop按钮点击事件
    /// </summary>
    private void OnLoopButtonClicked()
    {
        isLooping = !isLooping;
        UpdateLoopButtonVisual();
        
        // Loop 对预览的影响体现在“时间映射策略”（Repeat/Clamp），切换后立刻重采样一帧即可生效
        UpdateAnimationPreview();
    }
    #endregion

    #region Editor Update 驱动（播放推进）

    private void OnEditorUpdate()
    {
        // 非播放状态时不做任何事
        if (!isPlaying)
        {
            hasLastEditorUpdateTime = false;
            return;
        }

        // 没有可预览对象/配置时，直接停止播放
        if (config == null || animancer == null)
        {
            StopPreviewPlayback(resetTime: false);
            return;
        }

        double now = EditorApplication.timeSinceStartup;
        if (!hasLastEditorUpdateTime)
        {
            hasLastEditorUpdateTime = true;
            lastEditorUpdateTime = now;
            return;
        }

        float deltaTime = (float)(now - lastEditorUpdateTime);
        if (deltaTime <= 0f)
        {
            return;
        }
        lastEditorUpdateTime = now;

        AdvancePlayback(deltaTime);
    }

    private void AdvancePlayback(float deltaTime)
    {
        float maxTime = GetMaxPlaybackTime();
        if (maxTime <= 0f)
        {
            StopPreviewPlayback(resetTime: false);
            return;
        }

        // 顿帧：冻结时间推进（但仍保持预览刷新）
        if (previewHitStopRemainingMs > 0)
        {
            previewHitStopRemainingMs = Mathf.Max(0, previewHitStopRemainingMs - Mathf.RoundToInt(deltaTime * 1000f));
        }

        float speed = Mathf.Max(0f, playbackSpeed);
        float prevTime = currentPlaybackTime;
        if (previewHitStopRemainingMs <= 0)
        {
            currentPlaybackTime += deltaTime * speed;
        }

        bool wrapped = false;
        if (currentPlaybackTime >= maxTime)
        {
            if (isLooping)
            {
                currentPlaybackTime = Mathf.Repeat(currentPlaybackTime, maxTime);
                wrapped = true;
            }
            else
            {
                currentPlaybackTime = maxTime;
                isPlaying = false;
                hasLastEditorUpdateTime = false;
                UpdatePlayButtonText();
            }
        }

        // 仅在“播放”且时间确实前进时，模拟命中触发顿帧（对齐运行时：顿帧是时间效果，不在拖拽采样时触发）
        if (previewHitStopRemainingMs <= 0)
        {
            TryTriggerPreviewHitStop(prevTime, currentPlaybackTime, maxTime);
            // VFX：同样只在播放推进时触发；拖拽采样不触发（避免生成大量临时对象）。
            TryTriggerPreviewVfx(prevTime, currentPlaybackTime, maxTime, wrapped);
            // SFX：按轨道触发播放（编辑器预览）
            TryTriggerPreviewSfx(prevTime, currentPlaybackTime, maxTime, wrapped);
        }

        UpdatePlayheadPosition();
        UpdateAnimationPreview();

        // Active 预览：在采样完角色姿态后再更新显隐（避免 AnimationMode 未开启时采样失败）
        UpdatePreviewAttachedActives(currentPlaybackTime, wrapped);

        // VFX：legacy Animation 帧动画采样（在本帧姿态确定后采样）
        UpdatePreviewVfxLegacyAnimations(currentPlaybackTime);

        // VFX 生命周期更新：按预览时间轴销毁，保证暂停/倍速/顿帧一致
        UpdatePreviewVfxLifetime(currentPlaybackTime, wrapped);

        // 刷新窗口与场景视图（用于 HitBox 可视化跟随）
        Repaint();
        SceneView.RepaintAll();
    }

    private float GetMaxPlaybackTime()
    {
        // 使用 config 作为“真实时间轴”的来源（避免 viewMode 切换导致 trackDataList 缺失）
        if (config == null || config.Segments == null || config.Segments.Count == 0)
        {
            return 0f;
        }

        float max = 0f;
        foreach (var seg in config.Segments)
        {
            if (seg == null)
            {
                continue;
            }

            float duration = seg.Duration;
            if (duration <= 0f && seg.AnimationClipTrans != null && seg.AnimationClipTrans.Clip != null)
            {
                float s = Mathf.Max(0.01f, seg.AnimationClipTrans.Speed);
                duration = seg.AnimationClipTrans.Clip.length / s;
            }

            max = Mathf.Max(max, seg.StartTime + Mathf.Max(0f, duration));
        }
        return max;
    }

    private void StartPreviewPlayback()
    {
        if (config == null)
        {
            return;
        }

        EnsurePreviewObject();
        if (animancer == null) return;

        EnsurePreviewRootMotionDisabled();
        EnsurePreviewAnimatorSettings();

        previewHitStopRemainingMs = 0;
        hasPreviewLastPlaybackTime = false;

        isPlaying = true;
        hasLastEditorUpdateTime = false; // 让下一帧重新取时间基准
        UpdatePlayButtonText();

        // 立刻采样一帧，确保按当前 playhead 起播
        UpdateAnimationPreview();
        Repaint();
        SceneView.RepaintAll();
    }

    private void PausePreviewPlayback()
    {
        isPlaying = false;
        hasLastEditorUpdateTime = false;
        UpdatePlayButtonText();
    }

    private void StopPreviewPlayback(bool resetTime, bool sampleAfterStop = true)
    {
        isPlaying = false;
        hasLastEditorUpdateTime = false;
        UpdatePlayButtonText();

        previewHitStopRemainingMs = 0;
        hasPreviewLastPlaybackTime = false;
        CleanupPreviewVfx();
        CleanupPreviewSfx();
        CleanupPreviewAttachedActives();

        if (resetTime)
        {
            currentPlaybackTime = 0f;
            UpdatePlayheadPosition();
        }

        StopAnimancerPreview();

        // Stop 按钮语义：恢复默认姿态（Animator 初始/绑定姿态）
        if (resetTime)
        {
            ResetPreviewToInitialPose();
        }
        else if (sampleAfterStop)
        {
            // 非 reset 的 stop（例如窗口关闭）才需要按当前时间补采样
            UpdateAnimationPreview();
        }
        Repaint();
        SceneView.RepaintAll();
    }

    private void ResetPreviewToInitialPose()
    {
        if (animancer == null || animancer.Animator == null)
        {
            return;
        }

        // 清空图，避免残留姿态影响 Rebind
        if (animancer.IsGraphInitialized)
        {
            animancer.Graph.Stop();
        }

        // 回到默认姿态（绑定/默认值），对齐“停止后回到初始状态”的预期
        animancer.Animator.Rebind();
        animancer.Animator.Update(0f);
    }

    private void EnsurePreviewRootMotionDisabled()
    {
        if (animancer == null || animancer.Animator == null)
        {
            return;
        }

        if (!cachedApplyRootMotion.HasValue)
        {
            cachedApplyRootMotion = animancer.Animator.applyRootMotion;
        }

        animancer.Animator.applyRootMotion = false;
    }

    private void EnsurePreviewAnimatorSettings()
    {
        if (animancer == null || animancer.Animator == null)
        {
            return;
        }

        var a = animancer.Animator;

        // 对齐 Animancer.Editor.Previews.AnimancerPreviewObject 的预览 Animator 参数：
        // - AlwaysAnimate：避免视锥/禁用导致不更新
        // - fireEvents=false：避免 AnimationEvent 影响编辑器/游戏逻辑
        // - updateMode=Normal：避免 Physics/Unscaled 等差异导致预览不一致
        if (!cachedCullingMode.HasValue) cachedCullingMode = a.cullingMode;
        if (!cachedFireEvents.HasValue) cachedFireEvents = a.fireEvents;
        if (!cachedUpdateMode.HasValue) cachedUpdateMode = a.updateMode;

        a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        a.fireEvents = false;
        a.updateMode = AnimatorUpdateMode.Normal;
    }

    private void RestorePreviewAnimatorSettingsIfNeeded()
    {
        // animancer/Animator 可能在编辑器中被替换/销毁：这里做防御并清空缓存
        if (animancer == null || animancer.Animator == null)
        {
            cachedCullingMode = null;
            cachedFireEvents = null;
            cachedUpdateMode = null;
            return;
        }

        var a = animancer.Animator;
        if (cachedCullingMode.HasValue) a.cullingMode = cachedCullingMode.Value;
        if (cachedFireEvents.HasValue) a.fireEvents = cachedFireEvents.Value;
        if (cachedUpdateMode.HasValue) a.updateMode = cachedUpdateMode.Value;

        cachedCullingMode = null;
        cachedFireEvents = null;
        cachedUpdateMode = null;
    }

    private void RestorePreviewRootMotionIfNeeded()
    {
        if (!cachedApplyRootMotion.HasValue)
        {
            return;
        }

        if (animancer != null && animancer.Animator != null)
        {
            animancer.Animator.applyRootMotion = cachedApplyRootMotion.Value;
        }

        cachedApplyRootMotion = null;
    }

    private void StopAnimancerPreview()
    {
        if (animancer != null && animancer.IsGraphInitialized)
        {
            animancer.Graph.Stop();
        }

        CleanupPreviewVfx();
        CleanupPreviewSfx();
        CleanupPreviewAttachedActives();
        RestorePreviewRootMotionIfNeeded();
        RestorePreviewAnimatorSettingsIfNeeded();
        StopAnimationModeIfOwned();
    }

    #endregion

    #region Animancer 预览采样（按全局时间）

    private void SamplePreviewAnimation(float globalTimeSeconds)
    {
        if (config == null)
        {
            return;
        }

        // 采样前确保 animancer 指向选中对象的 AnimancerComponent
        EnsurePreviewObject();
        if (animancer == null)
        {
            return;
        }

        // 编辑器预览：进入 AnimationMode，确保采样对场景对象是可回滚的。
        EnsureAnimationMode();

        // 拖拽 playhead 也需要禁用 RootMotion，避免编辑场景对象被挪动
        EnsurePreviewRootMotionDisabled();
        EnsurePreviewAnimatorSettings();

        animancer.enabled = true;

        AttackSegmentData seg = FindSegmentAtTime(globalTimeSeconds);
        var trans = seg?.AnimationClipTrans;
        AnimationClip clip = trans?.Clip;

        // 按 Animancer 的预览实现：先暂停并清空图，确保每次采样都是确定性的“单一真相姿态”
        // 这样段切换不会受上一段残留状态/权重/过渡影响（常见的“人物变形”根因之一）。
        if (animancer.IsGraphInitialized)
        {
            animancer.Graph.PauseGraph();
            animancer.Graph.Stop();
        }

        if (seg == null || trans == null || clip == null)
        {
            // 不在任何 segment 范围内：保持图已停止即可
            return;
        }

        // === 融合（FadeDuration）===
        // 运行时 AttackComponentSystem：
        // - fadeIn = Mathf.Max(0.05f, segment.AnimationClipTrans.FadeDuration)
        // - attackLayer.Play(segment.AnimationClipTrans) 触发同层 CrossFade
        //
        // 编辑器预览需要支持“任意时间采样”（拖拽），因此采用确定性采样：
        // - 在 fade window 内同时采样 prev/curr 两段，并用权重 t 混合
        // - 不依赖 Graph 内部的 fade 进度，避免拖拽时出现不可重复的权重状态

        float fadeDuration = Mathf.Max(0.05f, trans.FadeDuration);
        AttackSegmentData prev = FindPreviousSegment(seg);
        bool canBlend = prev != null &&
                        fadeDuration > 0f &&
                        globalTimeSeconds >= seg.StartTime &&
                        globalTimeSeconds < seg.StartTime + fadeDuration &&
                        prev.AnimationClipTrans != null &&
                        prev.AnimationClipTrans.Clip != null &&
                        prev.AnimationClipTrans.IsValid();

        if (canBlend)
        {
            // 计算权重：在 [seg.StartTime, seg.StartTime + fadeDuration] 内从 0 -> 1
            float t = Mathf.InverseLerp(seg.StartTime, seg.StartTime + fadeDuration, globalTimeSeconds);
            t = Mathf.Clamp01(t);

            // Mixer：两段权重混合
            previewBlendMixer ??= new ManualMixerState();
            previewBlendMixer.DestroyChildren();

            // 让 Mixer 挂到 Layer0（预览用）。我们只做姿态采样，不依赖 Layer 结构。
            var layer = animancer.Layers[0];
            layer.Play(previewBlendMixer);

            // 添加两个 child（按 transition 创建 ClipState 并应用参数）
            var fromState = previewBlendMixer.Add(prev.AnimationClipTrans);
            var toState = previewBlendMixer.Add(trans);

            // local time：使用各自 Speed，把全局时间映射到 clip time
            ApplyStateAtGlobalTime(fromState, prev, globalTimeSeconds, 1f - t);
            ApplyStateAtGlobalTime(toState, seg, globalTimeSeconds, t);

            AnimationMode.BeginSampling();
            try
            {
                animancer.Graph.Evaluate();
            }
            finally
            {
                AnimationMode.EndSampling();
            }
            return;
        }

        // === 非融合：单段采样 ===
        float segmentSpeed = Mathf.Max(0.01f, trans.Speed);
        float localTime = (globalTimeSeconds - seg.StartTime) * segmentSpeed;

        // safety：避免越界
        float clipLength = Mathf.Max(0.0001f, clip.length);
        if (isLooping)
        {
            localTime = Mathf.Repeat(localTime, clipLength);
        }
        else
        {
            localTime = Mathf.Clamp(localTime, 0f, clipLength);
        }

        // 编辑器采样要确定性：使用 0 fade 硬切，避免过渡混合引入骨骼插值畸变
        var state = animancer.Play(trans, 0f);
        if (state == null)
        {
            return;
        }

        state.Speed = 0f; // 由全局时间驱动，保持确定性（播放/拖拽统一）
        state.Weight = 1f;
        state.Time = localTime;

        // 编辑器模式下：使用 AnimationMode 采样 PlayableGraph（不推进时间），并确保采样范围正确记录/回滚
        // 这里不直接手工回滚 scale，而是让 AnimationMode 负责记录/恢复所有动画属性（更健壮、维护成本更低）。
        AnimationMode.BeginSampling();
        try
        {
            // 走 AnimancerGraph.Evaluate，避免 Unity 的 SamplePlayableGraph API 版本差异。
            // 注：state.Speed = 0 且显式写入 state.Time，因此 Evaluate 不会推进时间，只会把姿态应用到 Animator。
            animancer.Graph.Evaluate();
        }
        finally
        {
            AnimationMode.EndSampling();
        }
    }

    #region 播放预览：HitStop（顿帧）

    private void TryTriggerPreviewHitStop(float prevTime, float currentTime, float maxTime)
    {
        // 时间倒退（例如手动拖拽到更小时间、或 Stop 重置）时不触发
        if (currentTime < prevTime)
        {
            hasPreviewLastPlaybackTime = false;
            return;
        }

        // 记录上一次播放时间（用于未来更精细的跨帧检测扩展）
        previewLastPlaybackTime = currentTime;
        hasPreviewLastPlaybackTime = true;

        if (config == null || config.Segments == null || config.Segments.Count == 0)
        {
            return;
        }

        // Loop wrap：把区间拆成两段检查
        bool wrapped = isLooping && prevTime > currentTime;
        if (wrapped)
        {
            // [prevTime, maxTime]
            TryTriggerPreviewHitStopInRange(prevTime, maxTime, includeStart: false, includeEnd: true);
            // [0, currentTime]
            TryTriggerPreviewHitStopInRange(0f, currentTime, includeStart: false, includeEnd: true);
        }
        else
        {
            TryTriggerPreviewHitStopInRange(prevTime, currentTime, includeStart: false, includeEnd: true);
        }
    }

    private void TryTriggerPreviewHitStopInRange(float fromTime, float toTime, bool includeStart, bool includeEnd)
    {
        if (config == null || config.Segments == null)
        {
            return;
        }

        int bestMs = 0;

        foreach (var seg in config.Segments)
        {
            if (seg?.HitBoxes == null || seg.HitBoxes.Count == 0)
            {
                continue;
            }

            // 段时长：使用 segment.Duration（运行时的归一化时间也是按全段 0-1 计算，再用 AnimationEnd 终止段）
            float duration = Mathf.Max(0f, seg.Duration);
            if (duration <= 0f && seg.AnimationClipTrans != null && seg.AnimationClipTrans.Clip != null)
            {
                float s = Mathf.Max(0.01f, seg.AnimationClipTrans.Speed);
                duration = seg.AnimationClipTrans.Clip.length / s;
            }

            if (duration <= 0f)
            {
                continue;
            }

            // 段提前结束阈值：运行时超过 AnimationEnd 就会结束，不会再触发后续 hitbox
            float endNorm = seg.TimeWindow != null ? seg.TimeWindow.AnimationEnd : 1f;
            if (endNorm <= 0f) endNorm = 1f;
            endNorm = Mathf.Clamp01(endNorm);

            foreach (var hb in seg.HitBoxes)
            {
                if (hb == null)
                {
                    continue;
                }

                // 只用 NormalizedStart 作为“命中触发点”（方案B：模拟命中）
                float n = Mathf.Clamp01(hb.NormalizedStart);
                if (n > endNorm)
                {
                    continue;
                }

                float triggerTime = seg.StartTime + duration * n;
                bool inRange =
                    (includeStart ? triggerTime >= fromTime : triggerTime > fromTime) &&
                    (includeEnd ? triggerTime <= toTime : triggerTime < toTime);

                if (!inRange)
                {
                    continue;
                }

                int ms = hb.Feedback != null ? hb.Feedback.HitStopMs : 0;
                if (ms <= 0)
                {
                    ms = config.DefaultHitStopMs;
                }
                ms = Mathf.Max(0, ms);
                bestMs = Mathf.Max(bestMs, ms);
            }
        }

        if (bestMs > 0)
        {
            previewHitStopRemainingMs = bestMs;
        }
    }

    #endregion

    #region 播放预览：VFX（特效）

    private void TryTriggerPreviewVfx(float prevTime, float currentTime, float maxTime, bool wrapped)
    {
        if (config == null || animancer == null)
        {
            return;
        }

        // Loop wrap：为保持确定性，跨回环时先清理上一轮残留（尤其是 FollowTarget 的挂载特效）
        if (wrapped)
        {
            CleanupPreviewVfx();
            CleanupPreviewSfx();
        }

        // 时间倒退（例如手动拖拽到更小时间、或 Stop 重置）时不触发
        // 注意：循环回环时 currentTime 会小于 prevTime，但这不是“倒退”，需要允许触发（靠区间拆分处理）。
        if (!wrapped && currentTime < prevTime)
        {
            return;
        }

        // wrapped 由调用方提供；这里仍按区间拆分保证不漏触发
        if (wrapped && maxTime > 0f)
        {
            // includeStart=true：避免触发点刚好落在区间起点（例如 trigger==0）时被漏掉
            TryTriggerPreviewVfxInRange(prevTime, maxTime, includeStart: true, includeEnd: true);
            TryTriggerPreviewVfxInRange(0f, currentTime, includeStart: true, includeEnd: true);
        }
        else
        {
            // includeStart=true：避免 trigger==prevTime 时漏触发（尤其是第一次从0开始播放）
            TryTriggerPreviewVfxInRange(prevTime, currentTime, includeStart: true, includeEnd: true);
        }
    }

    private void TryTriggerPreviewVfxInRange(float fromTime, float toTime, bool includeStart, bool includeEnd)
    {
        if (config == null || config.Segments == null || config.Segments.Count == 0 || animancer == null)
        {
            return;
        }

        Transform owner = animancer.transform;

        // 遍历所有段：VFX 的 NormalizedStart 相对于段（0-1），触发点 = seg.StartTime + normalized * segDuration
        foreach (var seg in config.Segments)
        {
            if (seg?.VisualEffects == null || seg.VisualEffects.Count == 0)
            {
                continue;
            }

            float segDuration = seg.Duration;
            if (segDuration <= 0f && seg.AnimationClipTrans != null && seg.AnimationClipTrans.Clip != null)
            {
                float s = Mathf.Max(0.01f, seg.AnimationClipTrans.Speed);
                segDuration = seg.AnimationClipTrans.Clip.length / s;
            }
            segDuration = Mathf.Max(0f, segDuration);

            for (int i = 0; i < seg.VisualEffects.Count; ++i)
            {
                var vfx = seg.VisualEffects[i];
                if (vfx == null || vfx.Prefab == null)
                {
                    continue;
                }

                float t = Mathf.Clamp01(vfx.NormalizedStart);
                float trigger = seg.StartTime + t * segDuration;

                bool afterStart = includeStart ? trigger >= fromTime : trigger > fromTime;
                bool beforeEnd = includeEnd ? trigger <= toTime : trigger < toTime;
                if (!afterStart || !beforeEnd)
                {
                    continue;
                }

                PlayPreviewVisualEffect(owner, vfx, trigger);
            }
        }
    }

    private void PlayPreviewVisualEffect(Transform owner, VisualEffectData vfx, float triggerTime)
    {
        if (owner == null || vfx == null || vfx.Prefab == null)
        {
            return;
        }

        try
        {
            GameObject instance;

            // Prefab 优先用 PrefabUtility.InstantiatePrefab（保持 Prefab 语义，且不污染场景保存）
            if (UnityEditor.EditorUtility.IsPersistent(vfx.Prefab))
            {
                instance = PrefabUtility.InstantiatePrefab(vfx.Prefab) as GameObject;
            }
            else
            {
                instance = Object.Instantiate(vfx.Prefab);
            }

            if (instance == null)
            {
                return;
            }

            // 很多项目的特效 Prefab 根节点默认是 inactive（配合对象池/脚本 OnEnable 播放）。
            // 预览必须强制激活，否则既看不到渲染，也不会触发 OnEnable（例如 TimeEffect）。
            if (!instance.activeSelf)
            {
                instance.SetActive(true);
            }

            // Debug：让用户在 Hierarchy 里可见，便于检查是否重复实例化。
            // 注意：仍会在 Stop/回环/离开窗口时 DestroyImmediate 清理，避免污染场景。
            instance.hideFlags = HideFlags.None;
            previewVfxSpawnCounter++;
            instance.name = $"[PreviewVFX #{previewVfxSpawnCounter}] {(vfx.Prefab != null ? vfx.Prefab.name : instance.name)}";

            // FollowTarget 语义（与运行时一致）：
            // - true：特效跟随角色（作为子物体），Offset/RotationEuler 为 local pose。
            // - false：特效生成后定格在世界，Offset/RotationEuler 仍以“相对角色”描述，但在生成时烘焙为 world pose。
            if (vfx.FollowTarget)
            {
                instance.transform.SetParent(owner, worldPositionStays: false);
                instance.transform.localPosition = vfx.Offset;
                instance.transform.localRotation = Quaternion.Euler(vfx.RotationEuler);
            }
            else
            {
                instance.transform.SetParent(null, worldPositionStays: false);
                instance.transform.position = owner.TransformPoint(vfx.Offset);
                instance.transform.rotation = owner.rotation * Quaternion.Euler(vfx.RotationEuler);
            }
            instance.transform.localScale = Vector3.one;

            float end = float.PositiveInfinity;
            if (vfx.Length > 0f)
            {
                end = triggerTime + vfx.Length;
            }

            // legacy Animation：用于编辑器预览采样（不依赖 Animation.Play）
            AnimationClip legacyClip = null;
            var legacyAnim = instance.GetComponentInChildren<Animation>(true);
            if (legacyAnim != null)
            {
                legacyClip = GetFirstLegacyAnimationClip(legacyAnim);
                // 若未配置 Length，则默认使用动画自身时长作为生命周期
                if (float.IsInfinity(end) && legacyClip != null)
                {
                    end = triggerTime + Mathf.Max(0.01f, legacyClip.length);
                }
            }

            previewVfxInstances.Add(new PreviewVfxInstance
            {
                GameObject = instance,
                Data = vfx,
                EndTime = end,
                StartTime = triggerTime,
                LegacyAnimationClip = legacyClip,
                HasSyncedPose = true,
                // 注意：为兼容 FollowTarget=false（world 定格），这里记录“当前使用空间”的 pose：
                // - FollowTarget=true 记录 local；FollowTarget=false 记录 world。
                LastSyncedPosition = vfx.FollowTarget ? instance.transform.localPosition : instance.transform.position,
                LastSyncedRotation = vfx.FollowTarget ? instance.transform.localRotation : instance.transform.rotation,
            });

            // 立刻采样一次第 0 帧，避免"触发当帧看不到任何变化"的错觉
            if (legacyClip != null)
            {
                EnsureAnimationMode();
                AnimationMode.BeginSampling();
                try
                {
                    AnimationMode.SampleAnimationClip(instance, legacyClip, 0f);
                }
                finally
                {
                    AnimationMode.EndSampling();
                }
            }

            // ParticleSystem：需要调用Play()方法才能开始播放（可以与legacy Animation共存）
            var ps = instance.GetComponentInChildren<ParticleSystem>(true);
            if (ps != null)
            {
                ps.Play(true); // true表示包含子粒子系统
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SkillEditorWindow] PlayPreviewVisualEffect failed: {e.Message}");
        }
    }

    private static AnimationClip GetFirstLegacyAnimationClip(Animation anim)
    {
        if (anim == null)
        {
            return null;
        }

        if (anim.clip != null)
        {
            return anim.clip;
        }

        // 取第一个 state 的 clip（保持最小可用）
        foreach (AnimationState state in anim)
        {
            if (state?.clip != null)
            {
                return state.clip;
            }
        }

        return null;
    }

    private void UpdatePreviewVfxLegacyAnimations(float currentTime)
    {
        if (previewVfxInstances.Count == 0)
        {
            return;
        }

        // 采样需要 AnimationMode
        EnsureAnimationMode();

        AnimationMode.BeginSampling();
        try
        {
            for (int i = 0; i < previewVfxInstances.Count; ++i)
            {
                var inst = previewVfxInstances[i];
                var go = inst?.GameObject;
                var clip = inst?.LegacyAnimationClip;
                if (go == null || clip == null)
                {
                    continue;
                }

                float t = currentTime - inst.StartTime;
                if (t < 0f)
                {
                    continue;
                }

                // 默认不循环：超过长度就 clamp 到最后一帧（配合 EndTime 会很快销毁/隐藏）
                float local = Mathf.Clamp(t, 0f, Mathf.Max(0.0001f, clip.length));
                AnimationMode.SampleAnimationClip(go, clip, local);
            }
        }
        finally
        {
            AnimationMode.EndSampling();
        }
    }

    /// <summary>
    /// 拖拽预览（scrub）用：按“当前时间点”重建应该可见的 VFX 实例，并采样到对应帧。
    /// - 目标：拖动 playhead 时也能看到特效（而不是只在 Play 时触发一次）。
    /// - 策略：为确定性，直接清理旧实例，然后对所有 VFX 计算窗口 [trigger, trigger+Length]，
    ///   若当前时间落在窗口内则实例化并采样（legacy Animation/ParticleSystem）。
    /// </summary>
    private void EvaluatePreviewVfxAtTime(float globalTimeSeconds)
    {
        if (config == null || animancer == null)
        {
            return;
        }

        // 重建：先清理旧实例，避免拖动过程中重复堆叠
        CleanupPreviewVfx();

        if (config.Segments == null || config.Segments.Count == 0)
        {
            return;
        }

        EnsurePreviewObject();
        if (animancer == null)
        {
            return;
        }

        EnsureAnimationMode();
        EnsurePreviewRootMotionDisabled();
        EnsurePreviewAnimatorSettings();

        Transform owner = animancer.transform;

        for (int s = 0; s < config.Segments.Count; ++s)
        {
            var seg = config.Segments[s];
            if (seg?.VisualEffects == null || seg.VisualEffects.Count == 0)
            {
                continue;
            }

            float segDuration = seg.Duration;
            if (segDuration <= 0f && seg.AnimationClipTrans != null && seg.AnimationClipTrans.Clip != null)
            {
                float spd = Mathf.Max(0.01f, seg.AnimationClipTrans.Speed);
                segDuration = seg.AnimationClipTrans.Clip.length / spd;
            }
            segDuration = Mathf.Max(0f, segDuration);

            for (int i = 0; i < seg.VisualEffects.Count; ++i)
            {
                var vfx = seg.VisualEffects[i];
                if (vfx == null || vfx.Prefab == null)
                {
                    continue;
                }

                float tNorm = Mathf.Clamp01(vfx.NormalizedStart);
                float trigger = seg.StartTime + tNorm * segDuration;
                float len = Mathf.Max(0f, vfx.Length);
                if (len <= 0f)
                {
                    // 若配置里 Length 还未写回，scrub 预览兜底用 Prefab 自身推导长度：
                    // - legacy Animation：clip.length
                    // - ParticleSystem：main.duration
                    var anim = vfx.Prefab.GetComponentInChildren<Animation>(true);
                    var legacy = anim != null ? GetFirstLegacyAnimationClip(anim) : null;
                    if (legacy != null)
                    {
                        len = Mathf.Max(0.01f, legacy.length);
                    }
                    else
                    {
                        var ps = vfx.Prefab.GetComponentInChildren<ParticleSystem>(true);
                        if (ps != null)
                        {
                            len = Mathf.Max(0.01f, ps.main.duration);
                        }
                    }
                }
                float end = trigger + len;

                if (globalTimeSeconds < trigger || globalTimeSeconds > end)
                {
                    continue;
                }

                // 复用已有的实例化逻辑（会记录 StartTime/LegacyAnimationClip 等）
                PlayPreviewVisualEffect(owner, vfx, trigger);

                // 采样到当前帧（对刚创建的最后一个实例）
                if (previewVfxInstances.Count == 0)
                {
                    continue;
                }

                var inst = previewVfxInstances[previewVfxInstances.Count - 1];
                var go = inst?.GameObject;
                if (go == null)
                {
                    continue;
                }

                float localT = Mathf.Max(0f, globalTimeSeconds - trigger);

                // legacy Animation：确定性采样
                if (inst.LegacyAnimationClip != null)
                {
                    AnimationMode.BeginSampling();
                    try
                    {
                        float local = Mathf.Clamp(localT, 0f, Mathf.Max(0.0001f, inst.LegacyAnimationClip.length));
                        AnimationMode.SampleAnimationClip(go, inst.LegacyAnimationClip, local);
                    }
                    finally
                    {
                        AnimationMode.EndSampling();
                    }
                }
                else
                {
                    // ParticleSystem：确定性模拟（若存在）
                    var ps = go.GetComponentInChildren<ParticleSystem>(true);
                    if (ps != null)
                    {
                        // 确定性：从 0 模拟到 localT
                        ps.Simulate(0f, true, true, true);
                        ps.Simulate(localT, true, false, true);
                    }
                }
            }
        }
    }

    private void UpdatePreviewVfxLifetime(float currentTime, bool wrapped)
    {
        if (previewVfxInstances.Count == 0)
        {
            return;
        }

        // wrapped 时已清理过，这里只做兜底
        if (wrapped)
        {
            return;
        }

        for (int i = previewVfxInstances.Count - 1; i >= 0; --i)
        {
            var inst = previewVfxInstances[i];
            var go = inst?.GameObject;
            if (go == null)
            {
                previewVfxInstances.RemoveAt(i);
                continue;
            }

            if (float.IsInfinity(inst.EndTime))
            {
                continue;
            }

            if (currentTime >= inst.EndTime)
            {
                Object.DestroyImmediate(go);
                previewVfxInstances.RemoveAt(i);
            }
        }
    }

    private void CleanupPreviewVfx()
    {
        if (previewVfxInstances.Count == 0)
        {
            return;
        }

        for (int i = previewVfxInstances.Count - 1; i >= 0; --i)
        {
            var go = previewVfxInstances[i]?.GameObject;
            if (go != null)
            {
                Object.DestroyImmediate(go);
            }
        }

        previewVfxInstances.Clear();
        previewVfxSpawnCounter = 0;
    }

    #endregion

    #region 播放预览：SFX（音效）

    private void TryTriggerPreviewSfx(float prevTime, float currentTime, float maxTime, bool wrapped)
    {
        if (config == null)
        {
            return;
        }

        // 时间倒退不触发
        // 注意：循环回环时 currentTime 会小于 prevTime，但这不是“倒退”，需要允许触发（靠区间拆分处理）。
        if (!wrapped && currentTime < prevTime)
        {
            return;
        }

        // wrapped 时调用方已经 Cleanup 过，这里按区间拆分确保不漏触发
        if (wrapped && maxTime > 0f)
        {
            TryTriggerPreviewSfxInRange(prevTime, maxTime, includeStart: true, includeEnd: true);
            TryTriggerPreviewSfxInRange(0f, currentTime, includeStart: true, includeEnd: true);
        }
        else
        {
            TryTriggerPreviewSfxInRange(prevTime, currentTime, includeStart: true, includeEnd: true);
        }
    }

    private void TryTriggerPreviewSfxInRange(float fromTime, float toTime, bool includeStart, bool includeEnd)
    {
        if (config == null || config.Segments == null || config.Segments.Count == 0)
        {
            return;
        }

        foreach (var seg in config.Segments)
        {
            if (seg?.SoundEffects == null || seg.SoundEffects.Count == 0)
            {
                continue;
            }

            float segDuration = seg.Duration;
            if (segDuration <= 0f && seg.AnimationClipTrans != null && seg.AnimationClipTrans.Clip != null)
            {
                float s = Mathf.Max(0.01f, seg.AnimationClipTrans.Speed);
                segDuration = seg.AnimationClipTrans.Clip.length / s;
            }
            segDuration = Mathf.Max(0f, segDuration);

            for (int i = 0; i < seg.SoundEffects.Count; ++i)
            {
                var sfx = seg.SoundEffects[i];
                if (sfx == null || sfx.Clip == null)
                {
                    continue;
                }

                float t = Mathf.Clamp01(sfx.NormalizedStart);
                float trigger = seg.StartTime + t * segDuration;

                bool afterStart = includeStart ? trigger >= fromTime : trigger > fromTime;
                bool beforeEnd = includeEnd ? trigger <= toTime : trigger < toTime;
                if (!afterStart || !beforeEnd)
                {
                    continue;
                }

                PlayPreviewSoundEffect(sfx, trigger);
            }
        }
    }

    private void EnsureAudioUtilReflection()
    {
        if (audioUtilReflectionInited)
        {
            return;
        }

        audioUtilReflectionInited = true;
        try
        {
            // AudioUtil 位于 UnityEditor 程序集内部类型
            audioUtilType = typeof(Editor).Assembly.GetType("UnityEditor.AudioUtil");
            if (audioUtilType == null)
            {
                return;
            }

            // 常见签名：PlayPreviewClip(AudioClip clip, int startSample, bool loop)
            audioUtilPlayPreviewClip = audioUtilType.GetMethod(
                "PlayPreviewClip",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] { typeof(AudioClip), typeof(int), typeof(bool) },
                null);

            // StopAllPreviewClips()
            audioUtilStopAllPreviewClips = audioUtilType.GetMethod(
                "StopAllPreviewClips",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null,
                Type.EmptyTypes,
                null);

            // SetPreviewVolume(float volume)（部分版本存在）
            audioUtilSetPreviewVolume = audioUtilType.GetMethod(
                "SetPreviewVolume",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] { typeof(float) },
                null);
        }
        catch
        {
            // 反射失败则保持 null，后续静默跳过
            audioUtilType = null;
            audioUtilPlayPreviewClip = null;
            audioUtilStopAllPreviewClips = null;
            audioUtilSetPreviewVolume = null;
        }
    }

    private void PlayPreviewSoundEffect(SoundEffectData sfx, float triggerTime)
    {
        if (sfx == null || sfx.Clip == null)
        {
            return;
        }

        EnsureAudioUtilReflection();
        if (audioUtilPlayPreviewClip == null)
        {
            // 找不到 AudioUtil：静默跳过（避免在编辑器刷屏报错）
            return;
        }

        try
        {
            // 注意：SetPreviewVolume 是全局的；这里只做最小可用实现，设置一次后可能影响其他预览播放。
            // 若未来需要“每条SFX独立音量”，建议接入项目统一音频预览系统。
            if (audioUtilSetPreviewVolume != null)
            {
                float volume = Mathf.Clamp01(sfx.Volume);
                audioUtilSetPreviewVolume.Invoke(null, new object[] { volume });
            }

            audioUtilPlayPreviewClip.Invoke(null, new object[] { sfx.Clip, 0, false });

            previewSfxInstances.Add(new PreviewSfxInstance
            {
                Clip = sfx.Clip,
                EndTime = triggerTime + Mathf.Max(0.01f, sfx.Clip.length),
            });
        }
        catch
        {
            // 静默：避免版本差异导致预览报错干扰工作流
        }
    }

    private void CleanupPreviewSfx()
    {
        previewSfxInstances.Clear();

        EnsureAudioUtilReflection();
        if (audioUtilStopAllPreviewClips == null)
        {
            return;
        }

        try
        {
            audioUtilStopAllPreviewClips.Invoke(null, null);
        }
        catch
        {
            // ignore
        }
    }

    #endregion

    #region 播放预览：Active（挂载对象显隐/启用）

    private void UpdatePreviewAttachedActives(float currentTime, bool wrapped)
    {
        if (wrapped)
        {
            // 回环时先恢复上一轮对对象的修改，避免“永久隐藏/永久显示”的残留
            CleanupPreviewAttachedActives();
        }

        if (config == null || config.Segments == null || config.Segments.Count == 0)
        {
            CleanupPreviewAttachedActives();
            return;
        }

        // 路径解析根：优先使用选中对象（更符合“角色根”语义），否则退回 animancer 节点
        var rootGo = selectObj != null ? selectObj.value as GameObject : null;
        Transform root = rootGo != null ? rootGo.transform : animancer != null ? animancer.transform : null;
        if (root == null)
        {
            CleanupPreviewAttachedActives();
            return;
        }

        // 语义（满足你的“第四段进入后，0.5s 前也要隐藏”的预期）：
        // - 只由“当前所在段（当前动画段）”的 AttachedActives 控制，不跨段影响。
        // - 若当前段里引用了某个对象，则该对象在本段内默认隐藏（false），
        //   只有落在某条 ActiveClip 区间内才显示（true）。
        // - 离开该段后恢复原始 activeSelf（不影响其他段）。

        AttackSegmentData segNow = FindSegmentAtTime(currentTime);
        if (segNow == null || segNow.AttachedActives == null || segNow.AttachedActives.Count == 0)
        {
            // 当前不在任何“带 Active 控制”的段：恢复所有被我们改过的对象
            CleanupPreviewAttachedActives();
            return;
        }

        // 段时长：与 Active 的归一化时间一致（使用 segNow.Duration；若缺失则从 Clip 推导）
        float segDuration = segNow.Duration;
        if (segDuration <= 0f && segNow.AnimationClipTrans != null && segNow.AnimationClipTrans.Clip != null)
        {
            float s = Mathf.Max(0.01f, segNow.AnimationClipTrans.Speed);
            segDuration = segNow.AnimationClipTrans.Clip.length / s;
        }
        segDuration = Mathf.Max(0f, segDuration);

        var controlled = new HashSet<GameObject>();
        var desired = new Dictionary<GameObject, bool>(16); // 当前时间命中区间的值（后者覆盖前者）
        var desiredStart = new Dictionary<GameObject, float>(16); // 用于 legacy Animation 采样：区间起点

        foreach (var a in segNow.AttachedActives)
        {
            if (a == null)
            {
                continue;
            }

            // 空路径不允许控制（避免误操作 root）
            string path = a.RelativePath ?? string.Empty;
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            Transform target = root.Find(path);
            if (target == null)
            {
                continue;
            }

            var go = target.gameObject;
            if (go == null)
            {
                continue;
            }

            controlled.Add(go);

            float ns = Mathf.Clamp01(a.NormalizedStart);
            float ne = Mathf.Clamp01(a.NormalizedEnd);
            if (ne < ns)
            {
                (ns, ne) = (ne, ns);
            }

            float start = segNow.StartTime + ns * segDuration;
            float end = segNow.StartTime + ne * segDuration;

            // 区间包含语义：start<=t<=end
            if (currentTime < start || currentTime > end)
            {
                continue;
            }

            desired[go] = true;
            desiredStart[go] = start;
        }

        // 本段内：对受控对象应用
        // - 命中区间 => desired
        // - 未命中区间（但在本段且被引用）=> 默认隐藏 false
        foreach (var go in controlled)
        {
            if (go == null)
            {
                continue;
            }

            if (!previewOriginalActiveStates.ContainsKey(go))
            {
                previewOriginalActiveStates[go] = go.activeSelf;
            }

            bool targetActive = desired.TryGetValue(go, out bool v) ? v : false;
            if (go.activeSelf != targetActive)
            {
                go.SetActive(targetActive);
            }

            // 若该对象在区间内显示，并且带 legacy Animation，则按区间内时间采样（帧动画预览）
            if (targetActive && desiredStart.TryGetValue(go, out float startTime))
            {
                var anim = go.GetComponentInChildren<Animation>(true);
                var clip = anim != null ? GetFirstLegacyAnimationClip(anim) : null;
                if (clip != null)
                {
                    EnsureAnimationMode();
                    float local = Mathf.Clamp(currentTime - startTime, 0f, Mathf.Max(0.0001f, clip.length));
                    AnimationMode.BeginSampling();
                    try
                    {
                        AnimationMode.SampleAnimationClip(go, clip, local);
                    }
                    finally
                    {
                        AnimationMode.EndSampling();
                    }
                }
            }
        }

        // 段切换/路径失效：把之前改过但本段不再控制的对象恢复并移除
        if (previewOriginalActiveStates.Count > 0)
        {
            var toRestore = new List<GameObject>(16);
            foreach (var kv in previewOriginalActiveStates)
            {
                var go = kv.Key;
                if (go == null || !controlled.Contains(go))
                {
                    toRestore.Add(go);
                }
            }

            foreach (var go in toRestore)
            {
                if (go == null)
                {
                    previewOriginalActiveStates.Remove(go);
                    continue;
                }

                if (previewOriginalActiveStates.TryGetValue(go, out bool original))
                {
                    if (go.activeSelf != original)
                    {
                        go.SetActive(original);
                    }
                    previewOriginalActiveStates.Remove(go);
                }
            }
        }
    }

    private void CleanupPreviewAttachedActives()
    {
        if (previewOriginalActiveStates.Count == 0)
        {
            return;
        }

        foreach (var kv in previewOriginalActiveStates)
        {
            var go = kv.Key;
            if (go == null)
            {
                continue;
            }

            bool original = kv.Value;
            if (go.activeSelf != original)
            {
                go.SetActive(original);
            }
        }

        previewOriginalActiveStates.Clear();
    }

    #endregion

    private void ApplyStateAtGlobalTime(AnimancerState state, AttackSegmentData segment, float globalTimeSeconds, float weight)
    {
        if (state == null || segment == null || segment.AnimationClipTrans == null || segment.AnimationClipTrans.Clip == null)
        {
            return;
        }

        float speed = Mathf.Max(0.01f, segment.AnimationClipTrans.Speed);
        float localTime = (globalTimeSeconds - segment.StartTime) * speed;

        // safety：避免越界
        float clipLength = Mathf.Max(0.0001f, segment.AnimationClipTrans.Clip.length);
        localTime = isLooping ? Mathf.Repeat(localTime, clipLength) : Mathf.Clamp(localTime, 0f, clipLength);

        state.Speed = 0f;
        state.Weight = Mathf.Clamp01(weight);
        state.Time = localTime;
    }

    private AttackSegmentData FindPreviousSegment(AttackSegmentData current)
    {
        if (config == null || current == null || config.Segments == null || config.Segments.Count == 0)
        {
            return null;
        }

        float currentStart = current.StartTime;
        AttackSegmentData best = null;
        float bestStart = float.NegativeInfinity;

        foreach (var seg in config.Segments)
        {
            if (seg == null || ReferenceEquals(seg, current))
            {
                continue;
            }

            if (seg.StartTime < currentStart && seg.StartTime >= bestStart && seg.AnimationClipTrans != null && seg.AnimationClipTrans.IsValid())
            {
                bestStart = seg.StartTime;
                best = seg;
            }
        }

        return best;
    }

    private AttackSegmentData FindSegmentAtTime(float globalTimeSeconds)
    {
        if (config == null || config.Segments == null || config.Segments.Count == 0)
        {
            return null;
        }

        AttackSegmentData best = null;
        float bestStart = float.NegativeInfinity;

        foreach (var seg in config.Segments)
        {
            if (seg == null)
            {
                continue;
            }

            // 运行时段结束判定基于 TimeWindow.AnimationEnd（NormalizedTime 阈值），
            // 编辑器预览需对齐：把“可播放有效区间”裁到该阈值对应的绝对时长。
            float endNorm = seg.TimeWindow != null ? seg.TimeWindow.AnimationEnd : 1f;
            if (endNorm <= 0f) endNorm = 1f;
            endNorm = Mathf.Clamp01(endNorm);

            float duration = seg.Duration * endNorm;
            if (duration <= 0f && seg.AnimationClipTrans != null && seg.AnimationClipTrans.Clip != null)
            {
                float s = Mathf.Max(0.01f, seg.AnimationClipTrans.Speed);
                duration = (seg.AnimationClipTrans.Clip.length / s) * endNorm;
            }

            float start = seg.StartTime;
            float end = start + Mathf.Max(0f, duration);

            if (globalTimeSeconds < start || globalTimeSeconds > end)
            {
                continue;
            }

            // 重叠时选择“起始时间更靠后”的段（更贴近时间轴覆盖/插入语义）
            if (start >= bestStart)
            {
                bestStart = start;
                best = seg;
            }
        }

        return best;
    }

    #endregion

    #region 播放预览：UI（速度控制）

    // 初始化播放速度控制（Top Bar）
    private void InitPlaybackSpeedControl()
    {
        // 找到Speed标签后面的Slider（在Top容器中查找）
        var topContainer = root.Q<VisualElement>("Top");
        if (topContainer != null)
        {
            // 查找Slider（在Speed标签后面）
            var speedLabel = root.Q<Label>("Speed");
            if (speedLabel != null)
            {
                // 查找Speed标签后面的Slider
                var elements = topContainer.Children();
                bool foundSpeed = false;
                foreach (var element in elements)
                {
                    if (element == speedLabel)
                    {
                        foundSpeed = true;
                        continue;
                    }
                    if (foundSpeed && element is Slider slider)
                    {
                        speedSlider = slider;
                        break;
                    }
                }
            }

            // 获取SpeedNum（TextField，可直接编辑）
            speedNumField = root.Q<TextField>("SpeedNum");
        }

        // 设置Slider范围（0-6）
        if (speedSlider != null)
        {
            speedSlider.lowValue = 0f;
            speedSlider.highValue = 6f;
            speedSlider.value = playbackSpeed; // 初始值1.0

            // 监听Slider值变化
            speedSlider.RegisterValueChangedCallback(evt =>
            {
                playbackSpeed = evt.newValue;
                UpdateSpeedDisplay();
            });
        }

        // 监听SpeedNum值变化（用户直接编辑时）
        if (speedNumField != null)
        {
            speedNumField.RegisterValueChangedCallback(evt =>
            {
                string input = evt.newValue.Trim();

                // 移除"x"后缀（如果有）
                if (input.EndsWith("x", StringComparison.OrdinalIgnoreCase))
                {
                    input = input.Substring(0, input.Length - 1);
                }

                if (float.TryParse(input, out float speedValue))
                {
                    speedValue = Mathf.Clamp(speedValue, 0f, 6f);
                    playbackSpeed = speedValue;

                    // 更新Slider位置（使用SetValueWithoutNotify避免触发回调）
                    if (speedSlider != null)
                    {
                        speedSlider.SetValueWithoutNotify(playbackSpeed);
                    }

                    UpdateSpeedDisplay();
                }
                else
                {
                    UpdateSpeedDisplay();
                }
            });
        }
        
        // 初始化显示
        UpdateSpeedDisplay();
    }

    // 更新速度显示
    private void UpdateSpeedDisplay()
    {
        if (speedNumField == null) return;

        if (Mathf.Approximately(playbackSpeed, Mathf.Round(playbackSpeed)))
        {
            speedNumField.value = $"{(int)playbackSpeed}x";
        }
        else
        {
            speedNumField.value = $"{playbackSpeed:F1}x";
        }
    }

    #endregion
}
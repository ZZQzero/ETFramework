using System;
using System.Collections.Generic;
using System.Reflection;
using Animancer;
using ET;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public partial class SkillEditorWindow : EditorWindow
{
    #region 播放预览：公共状态

    private const string LOOPING_CLASS = "is-looping";

    #region 播放驱动
    private double lastEditorUpdateTime;
    private bool hasLastEditorUpdateTime;
    #endregion

    #region Animation 预览设置
    private bool? cachedApplyRootMotion;
    private AnimatorCullingMode? cachedCullingMode;
    private bool? cachedFireEvents;
    private AnimatorUpdateMode? cachedUpdateMode;
    
    private Vector3? previewMovementStartPosition;
    private Vector3? previewMovementTargetPosition;
    private AttackSegmentData previewMovementSegment;
    private ManualMixerState previewBlendMixer;
    #endregion

    #region HitStop 预览
    private int previewHitStopRemainingMs;
    #endregion

    #region 命中检测预览
    /// <summary>
    /// 预览命中目标（按段分组，key: SegmentIndex, value: 命中的目标列表）
    /// </summary>
    private readonly Dictionary<int, HashSet<GameObject>> previewHitTargets = new Dictionary<int, HashSet<GameObject>>();
    
    /// <summary>
    /// 当前激活的 HitBox 及其命中的目标（用于可视化）
    /// </summary>
    private readonly Dictionary<HitBoxData, List<GameObject>> previewActiveHitBoxes = new Dictionary<HitBoxData, List<GameObject>>();

    /// <summary>
    /// 关键帧 HitBox（NormalizedStart==NormalizedEnd）在编辑器预览中的触发去重：
    /// - key: seg.Id
    /// - value: 本段已触发过的关键帧 HitBox 集合
    /// 说明：拖拽 playhead 时会频繁调用命中检测预览，必须去重避免重复触发。
    /// </summary>
    private readonly Dictionary<int, HashSet<HitBoxData>> previewTriggeredKeyFrameHitBoxes = new Dictionary<int, HashSet<HitBoxData>>();

    private float previewLastHitDetectionTime;
    private bool hasPreviewLastHitDetectionTime;
    #endregion

    #region VFX 预览
    private readonly List<PreviewVfxInstance> previewVfxInstances = new List<PreviewVfxInstance>(32);
    private int previewVfxSpawnCounter;

    private sealed class PreviewVfxInstance
    {
        public GameObject GameObject;
        public VisualEffectData Data;
        public float EndTime;
        public float StartTime;
        public AnimationClip LegacyAnimationClip;

        public bool HasSyncedPose;
        public Vector3 LastSyncedPosition;
        public Quaternion LastSyncedRotation;
    }
    #endregion

    #region SFX 预览
    private readonly List<PreviewSfxInstance> previewSfxInstances = new List<PreviewSfxInstance>(32);

    private sealed class PreviewSfxInstance
    {
        public AudioClip Clip;
        public float EndTime;
    }

    private static Type audioUtilType;
    private static MethodInfo audioUtilPlayPreviewClip;
    private static MethodInfo audioUtilStopAllPreviewClips;
    private static MethodInfo audioUtilSetPreviewVolume;
    private static bool audioUtilReflectionInited;
    #endregion

    #region Active 预览
    private readonly Dictionary<GameObject, bool> previewOriginalActiveStates = new Dictionary<GameObject, bool>(32);
    #endregion

    #endregion

    #region PreviewObject
    private GameObject previewSource;

    private void EnsurePreviewObject()
    {
        var src = selectObj != null ? selectObj.value as GameObject : null;
        if (src == null)
        {
            DestroyPreviewObject();
            return;
        }

        if (ReferenceEquals(previewSource, src) && animancer != null)
        {
            EnsurePreviewAnimatorSettings();
            return;
        }

        previewSource = src;
        animancer = src.GetComponentInChildren<AnimancerComponent>(true);
        if (animancer == null)
        {
            return;
        }

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
        previewMovementStartPosition = null;
        previewMovementTargetPosition = null;
        previewMovementSegment = null;

        RestorePreviewAnimatorSettingsIfNeeded();
    }

    #endregion

    #region 播放控制
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
        
        UpdateAnimationPreview();
    }
    #endregion

    #region Editor Update 驱动

    private void OnEditorUpdate()
    {
        if (!isPlaying)
        {
            hasLastEditorUpdateTime = false;
            return;
        }

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

        if (previewHitStopRemainingMs <= 0)
        {
            TryTriggerPreviewHitStop(prevTime, currentPlaybackTime, maxTime);
            TryTriggerPreviewVfx(prevTime, currentPlaybackTime, maxTime, wrapped);
            TryTriggerPreviewSfx(prevTime, currentPlaybackTime, maxTime, wrapped);
            TryTriggerPreviewHitDetection(prevTime, currentPlaybackTime, maxTime, wrapped);
        }

        UpdatePlayheadPosition();
        UpdateAnimationPreview();
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
        // 播放预览的最大范围：取“轨道内容最大结束时间”（动画段 + VFX/SFX/HitBox/Active 等）
        return GetMaxClipEndTime();
    }

    private void StartPreviewPlayback()
    {
        if (config == null)
        {
            return;
        }

        EnsurePreviewObject();
        if (animancer == null) return;

        // 预览期间始终禁用 RootMotion
        EnsurePreviewRootMotionDisabled();
        EnsurePreviewAnimatorSettings();

        previewHitStopRemainingMs = 0;

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
        CleanupPreviewVfx();
        CleanupPreviewSfx();
        CleanupPreviewAttachedActives();
        CleanupPreviewHitDetection();

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

        if (animancer.IsGraphInitialized)
        {
            animancer.Graph.Stop();
        }

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

        if (!cachedCullingMode.HasValue) cachedCullingMode = a.cullingMode;
        if (!cachedFireEvents.HasValue) cachedFireEvents = a.fireEvents;
        if (!cachedUpdateMode.HasValue) cachedUpdateMode = a.updateMode;

        a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        a.fireEvents = false;
        a.updateMode = AnimatorUpdateMode.Normal;
    }

    private void RestorePreviewAnimatorSettingsIfNeeded()
    {
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
        // 清除程序化位移数据
        previewMovementStartPosition = null;
        previewMovementTargetPosition = null;
        previewMovementSegment = null;
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

        // 预览期间始终禁用 RootMotion
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
            return;
        }

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
            float endNorm = GetSegmentAnimationEndNorm(seg);

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

    #region 播放预览：命中检测

    private void TryTriggerPreviewHitDetection(float prevTime, float currentTime, float maxTime, bool wrapped)
    {
        if (config == null || animancer == null || animancer.transform == null)
        {
            return;
        }

        // 内部时间倒退检测（用于 playhead 拖拽：调用方传入的 prevTime 可能是“伪上一帧”）
        if (hasPreviewLastHitDetectionTime && !wrapped && currentTime + 1e-6f < previewLastHitDetectionTime)
        {
            CleanupPreviewHitDetection();
        }
        previewLastHitDetectionTime = currentTime;
        hasPreviewLastHitDetectionTime = true;

        if (wrapped)
        {
            CleanupPreviewHitDetection();
        }

        // 时间倒退时清理命中检测缓存
        if (!wrapped && currentTime < prevTime)
        {
            CleanupPreviewHitDetection();
            return;
        }

        if (wrapped && maxTime > 0f)
        {
            TryTriggerPreviewHitDetectionInRange(prevTime, maxTime, includeStart: true, includeEnd: true);
            TryTriggerPreviewHitDetectionInRange(0f, currentTime, includeStart: true, includeEnd: true);
        }
        else
        {
            TryTriggerPreviewHitDetectionInRange(prevTime, currentTime, includeStart: true, includeEnd: true);
        }
    }

    private void TryTriggerPreviewHitDetectionInRange(float fromTime, float toTime, bool includeStart, bool includeEnd)
    {
        if (config == null || config.Segments == null || config.Segments.Count == 0 || animancer == null || animancer.transform == null)
        {
            return;
        }

        Transform player = animancer.transform;
        int layerMask = previewHitTargetLayerMask.value;

        foreach (var seg in config.Segments)
        {
            if (seg?.HitBoxes == null || seg.HitBoxes.Count == 0)
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

            if (segDuration <= 0f)
            {
                continue;
            }

            // 段提前结束阈值：运行时超过 AnimationEnd 就会结束，不会再触发后续 hitbox
            float endNorm = GetSegmentAnimationEndNorm(seg);

            // 检查当前时间是否在当前段内
            float segmentStartTime = seg.StartTime;
            float segmentEffectiveEndTime = seg.StartTime + segDuration * endNorm;
            
            // 如果区间与该段没有交集，清理该段的命中目标（段切换时）
            // 注意：这里必须用“区间交集”判断，而不是仅用 toTime。
            // 否则当 currentTime 因浮点误差略微超过 segmentEffectiveEndTime 时，会把 AnimationEnd 那一帧的关键帧 HitBox 也跳过。
            float evalFromTime = Mathf.Max(fromTime, segmentStartTime);
            float evalToTime = Mathf.Min(toTime, segmentEffectiveEndTime);
            if (evalToTime < evalFromTime)
            {
                if (previewHitTargets.ContainsKey(seg.Id))
                {
                    previewHitTargets[seg.Id].Clear();
                }
                if (previewTriggeredKeyFrameHitBoxes.ContainsKey(seg.Id))
                {
                    previewTriggeredKeyFrameHitBoxes[seg.Id].Clear();
                }
                continue;
            }

            // 获取或创建当前段的命中目标集合
            if (!previewHitTargets.TryGetValue(seg.Id, out var hitTargets))
            {
                hitTargets = new HashSet<GameObject>();
                previewHitTargets[seg.Id] = hitTargets;
            }

            // 清空当前激活的 HitBox 列表（用于可视化）
            previewActiveHitBoxes.Clear();

            foreach (var hb in seg.HitBoxes)
            {
                if (hb == null)
                {
                    continue;
                }

                float start = Mathf.Min(Mathf.Clamp01(hb.NormalizedStart), endNorm);
                float end = Mathf.Min(Mathf.Clamp01(hb.NormalizedEnd), endNorm);

                // 关键帧 HitBox：NormalizedStart == NormalizedEnd，跨越触发点时执行一次检测并可视化
                if (Mathf.Abs(end - start) < 1e-6f)
                {
                    float triggerTime = seg.StartTime + segDuration * start;
                    bool inRange =
                        (includeStart ? triggerTime >= evalFromTime : triggerTime > evalFromTime) &&
                        (includeEnd ? triggerTime <= evalToTime : triggerTime < evalToTime);

                    if (!inRange)
                    {
                        continue;
                    }

                    if (!previewTriggeredKeyFrameHitBoxes.TryGetValue(seg.Id, out var triggered))
                    {
                        triggered = new HashSet<HitBoxData>();
                        previewTriggeredKeyFrameHitBoxes[seg.Id] = triggered;
                    }

                    if (triggered.Contains(hb))
                    {
                        continue;
                    }

                    PerformPreviewHitDetection(player, seg, hb, hitTargets, layerMask);
                    triggered.Add(hb);
                    continue;
                }

                if (end <= start)
                {
                    continue;
                }

                // 区间 HitBox：检查 HitBox 是否在当前时间范围内激活
                float startTime = seg.StartTime + segDuration * start;
                float endTime = seg.StartTime + segDuration * end;

                // 检查时间范围是否有重叠（HitBox 激活窗口与检测时间范围有交集）
                // 只有当 evalToTime（当前时间，已夹到 AnimationEnd）在 HitBox 激活窗口内时才检测
                bool isActive = evalToTime >= startTime && evalToTime <= endTime;
                
                if (!isActive)
                {
                    continue;
                }

                // 如果 HitBox 在当前时间激活，进行检测
                PerformPreviewHitDetection(player, seg, hb, hitTargets, layerMask);
            }
        }
    }

    private void PerformPreviewHitDetection(Transform player, AttackSegmentData seg, HitBoxData hitBox, HashSet<GameObject> hitTargets, int layerMask)
    {
        if (player == null || hitBox == null)
        {
            return;
        }

        // 计算判定框世界坐标（与运行时一致）
        Vector3 worldPosition = player.position + player.rotation * hitBox.Offset;
        Quaternion worldRotation = player.rotation * Quaternion.Euler(hitBox.RotationEuler);

        // 根据形状类型进行检测
        List<GameObject> detectedTargets = new List<GameObject>();

        try
        {
            // 使用 Unity 的 Physics API 直接检测（编辑器不需要使用 ListComponent）
            Collider[] colliders = new Collider[64];
            int count = 0;
            
            switch (hitBox.ShapeType)
            {
                case HitShapeType.Box:
                    count = Physics.OverlapBoxNonAlloc(worldPosition, hitBox.Size * 0.5f, colliders, worldRotation, layerMask);
                    break;
                case HitShapeType.Sphere:
                    count = Physics.OverlapSphereNonAlloc(worldPosition, hitBox.Size.x, colliders, layerMask);
                    break;
                case HitShapeType.Fan:
                    // 扇形检测：先用球体检测，再过滤角度和高度（与 PhysicsHelper.OverlapFan 逻辑完全一致）
                    int sphereCount = Physics.OverlapSphereNonAlloc(worldPosition, hitBox.Size.x, colliders, layerMask);
                    Vector3 forward = worldRotation * Vector3.forward;
                    float halfAngle = hitBox.Size.y * 0.5f;
                    float halfHeight = hitBox.Size.z > 0f ? hitBox.Size.z * 0.5f : 0f;
                    count = 0;
                    
                    for (int i = 0; i < sphereCount; i++)
                    {
                        var collider = colliders[i];
                        if (collider == null)
                            continue;
                        
                        // 高度过滤：检查目标的 Collider 边界是否与扇形高度范围有重叠
                        if (halfHeight > 0f)
                        {
                            float fanBottom = worldPosition.y - halfHeight;
                            float fanTop = worldPosition.y + halfHeight;
                            Bounds colliderBounds = collider.bounds;
                            
                            // 检查是否有重叠：目标的底部在扇形顶部之上，或目标的顶部在扇形底部之下，则无重叠
                            if (colliderBounds.min.y > fanTop || colliderBounds.max.y < fanBottom)
                            {
                                continue;
                            }
                        }
                        
                        // 角度过滤（水平扇形检测）
                        Vector3 directionToTarget = collider.transform.position - worldPosition;
                        directionToTarget.y = 0;
                        
                        // 如果水平距离为 0（目标在正上方或正下方），跳过
                        if (directionToTarget.sqrMagnitude < 1e-6f)
                        {
                            continue;
                        }
                        directionToTarget.Normalize();
                        
                        // forward 的水平方向向量
                        Vector3 forwardFlat = forward;
                        forwardFlat.y = 0;
                        
                        // 如果 forward 的水平分量为 0（forward 垂直向上或向下），跳过
                        if (forwardFlat.sqrMagnitude < 1e-6f)
                        {
                            continue;
                        }
                        forwardFlat.Normalize();

                        // 计算角度并判断
                        float angleToTarget = Vector3.Angle(forwardFlat, directionToTarget);
                        if (angleToTarget <= halfAngle)
                        {
                            colliders[count] = collider;
                            count++;
                        }
                    }
                    break;
                case HitShapeType.Capsule:
                    // 计算胶囊体两端点
                    float capsuleHalfHeight = Mathf.Max(0, (hitBox.Size.y - hitBox.Size.x * 2) * 0.5f);
                    Vector3 up = worldRotation * Vector3.up;
                    Vector3 point0 = worldPosition - up * capsuleHalfHeight;
                    Vector3 point1 = worldPosition + up * capsuleHalfHeight;
                    count = Physics.OverlapCapsuleNonAlloc(point0, point1, hitBox.Size.x, colliders, layerMask);
                    break;
            }
            
            // 转换为 GameObject 列表
            for (int i = 0; i < count; i++)
            {
                if (colliders[i] != null && colliders[i].gameObject != null)
                {
                    detectedTargets.Add(colliders[i].gameObject);
                }
            }

            // 处理命中目标
            List<GameObject> newHits = new List<GameObject>();
            List<GameObject> allActiveHits = new List<GameObject>(); // 当前激活的所有命中目标（包括之前命中的）
            
            foreach (var target in detectedTargets)
            {
                if (target == null)
                    continue;

                // 检查是否已命中过该目标（同一段中只能命中一次）
                bool isNewHit = !hitTargets.Contains(target);
                if (isNewHit)
                {
                    // 记录新命中
                    hitTargets.Add(target);
                    newHits.Add(target);
                }
                
                // 无论新旧，只要当前检测到，就添加到可视化列表（HitBox 激活时显示所有命中的目标）
                allActiveHits.Add(target);
            }

            // 保存当前激活的 HitBox 及其命中的目标（用于可视化）
            // 注意：即使没有新命中，只要 HitBox 激活且之前命中过目标，也应该显示
            if (allActiveHits.Count > 0)
            {
                previewActiveHitBoxes[hitBox] = allActiveHits;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SkillEditor: Hit detection failed - {e.Message}");
        }
    }

    private void CleanupPreviewHitDetection()
    {
        previewHitTargets.Clear();
        previewActiveHitBoxes.Clear();
        previewTriggeredKeyFrameHitBoxes.Clear();
        hasPreviewLastHitDetectionTime = false;
    }

    #endregion

    #region 播放预览：VFX（特效）

    private void TryTriggerPreviewVfx(float prevTime, float currentTime, float maxTime, bool wrapped)
    {
        if (config == null || animancer == null)
        {
            return;
        }

        if (wrapped)
        {
            CleanupPreviewVfx();
            CleanupPreviewSfx();
        }

        if (!wrapped && currentTime < prevTime)
        {
            return;
        }

        if (wrapped && maxTime > 0f)
        {
            TryTriggerPreviewVfxInRange(prevTime, maxTime, includeStart: true, includeEnd: true);
            TryTriggerPreviewVfxInRange(0f, currentTime, includeStart: true, includeEnd: true);
        }
        else
        {
            TryTriggerPreviewVfxInRange(prevTime, currentTime, includeStart: true, includeEnd: true);
        }
    }

    private void TryTriggerPreviewVfxInRange(float fromTime, float toTime, bool includeStart, bool includeEnd)
    {
        if (config == null || config.Segments == null || config.Segments.Count == 0 || animancer == null)
        {
            return;
        }

        Transform owner = animancer != null ? animancer.transform : null;

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

            // 段提前结束阈值：运行时超过 AnimationEnd 就会结束，编辑器预览需对齐
            float endNorm = GetSegmentAnimationEndNorm(seg);

            for (int i = 0; i < seg.VisualEffects.Count; ++i)
            {
                var vfx = seg.VisualEffects[i];
                if (vfx == null || vfx.Prefab == null)
                {
                    continue;
                }

                float t = Mathf.Clamp01(vfx.NormalizedStart);
                if (t > endNorm)
                {
                    continue;
                }
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

            if (!instance.activeSelf)
            {
                instance.SetActive(true);
            }

            instance.hideFlags = HideFlags.None;
            previewVfxSpawnCounter++;
            instance.name = $"[PreviewVFX #{previewVfxSpawnCounter}] {(vfx.Prefab != null ? vfx.Prefab.name : instance.name)}";

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
                LastSyncedPosition = vfx.FollowTarget ? instance.transform.localPosition : instance.transform.position,
                LastSyncedRotation = vfx.FollowTarget ? instance.transform.localRotation : instance.transform.rotation,
            });

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

            var ps = instance.GetComponentInChildren<ParticleSystem>(true);
            if (ps != null)
            {
                ps.Play(true); // true 表示包含子粒子系统
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

        Transform owner = animancer != null ? animancer.transform : null;
        if (owner == null)
        {
            return;
        }

        EnsureAnimationMode();

        AnimationMode.BeginSampling();
        try
        {
            for (int i = 0; i < previewVfxInstances.Count; ++i)
            {
                var inst = previewVfxInstances[i];
                var go = inst?.GameObject;
                if (go == null)
                {
                    continue;
                }

                float t = currentTime - inst.StartTime;
                if (t < 0f)
                {
                    continue;
                }

                // 跟随特效位置同步：每帧更新位置/旋转，确保跟随角色移动（处理攻击位移等情况）
                // 说明：运行时跟随特效作为角色的子物体会自动跟随，但编辑器预览中需要手动同步
                if (inst.Data != null && inst.Data.FollowTarget)
                {
                    go.transform.SetParent(owner, worldPositionStays: false);
                    go.transform.localPosition = inst.Data.Offset;
                    go.transform.localRotation = Quaternion.Euler(inst.Data.RotationEuler);
                    go.transform.localScale = Vector3.one;
                }

                var clip = inst?.LegacyAnimationClip;
                if (clip != null)
                {
                    float local = Mathf.Clamp(t, 0f, Mathf.Max(0.0001f, clip.length));
                    AnimationMode.SampleAnimationClip(go, clip, local);
                }

                var ps = go.GetComponentInChildren<ParticleSystem>(true);
                if (ps != null)
                {
                    float localT = Mathf.Max(0f, t);
                    ps.Simulate(0f, true, true, true);
                    ps.Simulate(localT, true, false, true);
                }
            }
        }
        finally
        {
            AnimationMode.EndSampling();
        }
    }

    private void EvaluatePreviewVfxAtTime(float globalTimeSeconds)
    {
        if (config == null || animancer == null)
        {
            return;
        }

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

        Transform owner = animancer != null ? animancer.transform : null;

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
                    var anim = vfx.Prefab.GetComponentInChildren<Animation>(true);
                    var legacy = anim != null ? GetFirstLegacyAnimationClip(anim) : null;
                    if (legacy != null)
                    {
                        len = Mathf.Max(0.01f, legacy.length);
                    }
                    else
                    {
                        var prefabPs = vfx.Prefab.GetComponentInChildren<ParticleSystem>(true);
                        if (prefabPs != null)
                        {
                            len = Mathf.Max(0.01f, prefabPs.main.duration);
                        }
                    }
                }
                float end = trigger + len;

                if (globalTimeSeconds < trigger || globalTimeSeconds > end)
                {
                    continue;
                }

                PlayPreviewVisualEffect(owner, vfx, trigger);

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

                var instancePs = go.GetComponentInChildren<ParticleSystem>(true);
                if (instancePs != null)
                {
                    instancePs.Simulate(0f, true, true, true);  // 重置到初始状态
                    instancePs.Simulate(localT, true, false, true);  // 模拟到当前时间点
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

            // 段提前结束阈值：运行时超过 AnimationEnd 就会结束，编辑器预览需对齐
            float endNorm = GetSegmentAnimationEndNorm(seg);

            for (int i = 0; i < seg.SoundEffects.Count; ++i)
            {
                var sfx = seg.SoundEffects[i];
                if (sfx == null || sfx.Clip == null)
                {
                    continue;
                }

                float t = Mathf.Clamp01(sfx.NormalizedStart);
                if (t > endNorm)
                {
                    continue;
                }
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
            audioUtilType = typeof(Editor).Assembly.GetType("UnityEditor.AudioUtil");
            if (audioUtilType == null)
            {
                return;
            }

            audioUtilPlayPreviewClip = audioUtilType.GetMethod(
                "PlayPreviewClip",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] { typeof(AudioClip), typeof(int), typeof(bool) },
                null);

            audioUtilStopAllPreviewClips = audioUtilType.GetMethod(
                "StopAllPreviewClips",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null,
                Type.EmptyTypes,
                null);

            audioUtilSetPreviewVolume = audioUtilType.GetMethod(
                "SetPreviewVolume",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] { typeof(float) },
                null);
        }
        catch
        {
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
            return;
        }

        try
        {
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
            CleanupPreviewAttachedActives();
        }

        if (config == null || config.Segments == null || config.Segments.Count == 0)
        {
            CleanupPreviewAttachedActives();
            return;
        }

        var rootGo = selectObj != null ? selectObj.value as GameObject : null;
        Transform root = rootGo != null ? rootGo.transform : animancer != null ? animancer.transform : null;
        if (root == null)
        {
            CleanupPreviewAttachedActives();
            return;
        }

        AttackSegmentData segNow = FindSegmentAtTime(currentTime);
        if (segNow == null || segNow.AttachedActives == null || segNow.AttachedActives.Count == 0)
        {
            CleanupPreviewAttachedActives();
            return;
        }

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
            float endNorm = GetSegmentAnimationEndNorm(seg);

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

    #region 程序化位移预览

    /// <summary>
    /// 更新编辑器预览中的程序化位移
    /// </summary>
    private void UpdatePreviewMovement(float globalTimeSeconds)
    {
        if (animancer == null || animancer.transform == null || config == null)
        {
            return;
        }

        AttackSegmentData seg = FindSegmentAtTime(globalTimeSeconds);
        
        if (seg != null && seg.Movement != null && seg.Movement.EnableMovement)
        {
            if (previewMovementSegment != seg || !previewMovementStartPosition.HasValue)
            {
                InitializePreviewMovement(seg, seg.StartTime);
            }
        }
        else
        {
            previewMovementSegment = null;
            previewMovementStartPosition = null;
            previewMovementTargetPosition = null;
            return;
        }

        float segmentLocalTime = globalTimeSeconds - seg.StartTime;
        float segmentDuration = seg.Duration > 0f ? seg.Duration : 
            (seg.AnimationClipTrans?.Clip != null ? seg.AnimationClipTrans.Clip.length / Mathf.Max(0.01f, seg.AnimationClipTrans.Speed) : 0f);
        
        if (segmentDuration <= 0f)
        {
            return;
        }

        float normalizedTime = segmentLocalTime / segmentDuration;
        normalizedTime = Mathf.Clamp01(normalizedTime);

        var movement = seg.Movement;
        
        if (normalizedTime < movement.NormalizedStart || normalizedTime > movement.NormalizedEnd)
        {
            return;
        }

        float moveProgress = (normalizedTime - movement.NormalizedStart) / (movement.NormalizedEnd - movement.NormalizedStart);
        moveProgress = Mathf.Clamp01(moveProgress);
        float curveValue = movement.MoveCurve.Evaluate(moveProgress);

        if (previewMovementStartPosition.HasValue && previewMovementTargetPosition.HasValue)
        {
            Vector3 targetPos = Vector3.Lerp(previewMovementStartPosition.Value, previewMovementTargetPosition.Value, curveValue);
            
            AnimationMode.BeginSampling();
            try
            {
                Vector3 currentPos = animancer.transform.position;
                animancer.transform.position = new Vector3(targetPos.x, currentPos.y, targetPos.z);
            }
            finally
            {
                AnimationMode.EndSampling();
            }
        }
    }

    /// <summary>
    /// 初始化编辑器预览中的程序化位移
    /// </summary>
    private void InitializePreviewMovement(AttackSegmentData seg, float globalTimeSeconds)
    {
        if (animancer == null || animancer.transform == null || seg?.Movement == null)
        {
            return;
        }

        previewMovementSegment = seg;
        var movement = seg.Movement;
        previewMovementStartPosition = animancer.transform.position;

        Vector3 direction = animancer.transform.forward;
        if (movement.TrackTarget)
        {
            direction = animancer.transform.forward;
        }

        direction.y = 0;
        direction.Normalize();
        
        previewMovementTargetPosition = previewMovementStartPosition.Value + direction * movement.Distance;
    }

    #endregion

    #region 播放预览：UI

    private void InitPlaybackSpeedControl()
    {
        var topContainer = root.Q<VisualElement>("Top");
        if (topContainer != null)
        {
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
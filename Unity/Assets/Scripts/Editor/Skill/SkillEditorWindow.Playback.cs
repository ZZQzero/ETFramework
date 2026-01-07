using System;
using System.Collections.Generic;
using Animancer;
using ET;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public partial class SkillEditorWindow : EditorWindow
{
    #region 播放控制按钮事件

    private const string LOOPING_CLASS = "is-looping";

    // Editor 下的播放驱动：用 EditorApplication.update 推进（不依赖 PlayMode）
    private double lastEditorUpdateTime;
    private bool hasLastEditorUpdateTime;

    // 预览状态缓存：减少重复 Play（尤其在拖拽/播放过程中）
    // 注意：编辑器预览采用“确定性采样”，每次采样都会 Stop Graph 并重新应用当前时间点的姿态，
    // 因此不依赖缓存状态来减少 Play（避免跨段残留权重/过渡混合导致变形）。

    // 预览期间关闭 RootMotion，避免编辑器场景中的对象被“推走”
    private bool? cachedApplyRootMotion;
    private AnimatorCullingMode? cachedCullingMode;
    private bool? cachedFireEvents;
    private AnimatorUpdateMode? cachedUpdateMode;

    // 预览融合：使用 Animancer 的 ManualMixerState（Pro-Only）实现确定性的两段权重混合。
    // 这样拖拽时间轴/播放时看到的融合与运行时 FadeDuration 语义一致。
    private ManualMixerState previewBlendMixer;

    // === 预览：顿帧（HitStop）===
    // 运行时：命中时把 CurrentAnimState.Speed 置 0，并在 durationMs 后恢复。
    // 编辑器：用“冻结 currentPlaybackTime”的方式模拟（播放状态下才有时间流动，拖拽采样不需要模拟顿帧时间流）。
    private int previewHitStopRemainingMs;
    private float previewLastPlaybackTime;
    private bool hasPreviewLastPlaybackTime;

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
        previewSource = null;
        animancer = null;

        RestorePreviewAnimatorSettingsIfNeeded();
    }

    #endregion

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
        StopPreviewPlayback(resetTime: true);
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

        if (currentPlaybackTime >= maxTime)
        {
            if (isLooping)
            {
                currentPlaybackTime = Mathf.Repeat(currentPlaybackTime, maxTime);
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
        }

        UpdatePlayheadPosition();
        UpdateAnimationPreview();

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

        if (resetTime)
        {
            currentPlaybackTime = 0f;
            UpdatePlayheadPosition();
        }

        StopAnimancerPreview();

        // 停止后根据当前时间（通常为 0）再采样一次，保证姿态一致
        if (sampleAfterStop)
        {
            UpdateAnimationPreview();
        }
        Repaint();
        SceneView.RepaintAll();
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
}
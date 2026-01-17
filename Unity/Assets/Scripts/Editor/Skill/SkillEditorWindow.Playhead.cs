using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public partial class SkillEditorWindow : EditorWindow
{
    #region 播放进度条

    // 当ScrollView滚动时更新playhead位置
    private void OnTimelineScrollChanged(float value)
    {
        UpdatePlayheadPosition();
    }

     // 初始化播放进度条
    private void InitPlayHead()
    {
        if (timelineContent == null) return;

        // 如果已经存在播放进度条，先移除（避免重复创建）
        if (playheadElement != null)
        {
            playheadElement.RemoveFromHierarchy();
            playheadElement = null;
        }
        
        // 检查 timelineContent 中是否已经有 Playhead 元素（通过名称查找）
        var existingPlayhead = timelineContent.Q<VisualElement>("Playhead");
        if (existingPlayhead != null)
        {
            existingPlayhead.RemoveFromHierarchy();
        }

        // 创建播放进度条容器（只保留红色进度线，去掉箭头）
        playheadElement = new VisualElement();
        playheadElement.name = "Playhead";
        playheadElement.AddToClassList("timeline-playhead");
        playheadElement.style.position = Position.Absolute;
        playheadElement.style.top = 0;
        // 固定为线宽，确保不会出现负坐标/越界几何导致 ScrollView 误判需要横向滚动条
        playheadElement.style.width = PLAYHEAD_WIDTH;
        playheadElement.style.alignItems = Align.Center;
        playheadElement.pickingMode = PickingMode.Position; // 允许接收鼠标事件

        // 创建进度线（垂直红线）- 从顶部开始，连接到轨道底部
        var playheadLine = new VisualElement();
        playheadLine.name = "PlayheadLine";
        playheadLine.AddToClassList("timeline-playhead-line");
        playheadLine.style.position = Position.Absolute;
        playheadLine.style.top = 0f;
        playheadLine.style.left = 0f;
        playheadLine.style.width = PLAYHEAD_LINE_WIDTH;
        playheadLine.style.backgroundColor = new Color(1f, 0.3f, 0.3f, 1f);

        // 将线添加到播放进度条容器
        playheadElement.Add(playheadLine);

        // 添加到内容容器，确保在最上层
        timelineContent.Add(playheadElement);

        // 设置初始位置
        UpdatePlayheadPosition();

        // 监听内容容器大小变化，更新播放进度条位置
        timelineContent.RegisterCallback<GeometryChangedEvent>(evt => {
            UpdatePlayheadPosition();
        });

        // 初始更新一次大小和位置
        UpdatePlayheadSize();

        // 设置拖动交互（点击/拖动进度线即可）
        SetupPlayheadInteractions();
    }

    // 更新播放进度条的高度（从Ruler顶部到最后一个轨道底部）
    private void UpdatePlayheadSize()
    {
        if (playheadElement == null || timelineContent == null) return;

        // 计算高度：Ruler高度 + (轨道数量 * 轨道高度)
        float height = RULER_HEIGHT;
        
        if (trackContainer != null && trackDataList != null && trackDataList.Count > 0)
        {
            // 使用trackContainer的实际高度，确保覆盖所有轨道
            float trackContainerHeight = trackContainer.layout.height;
            if (trackContainerHeight > 0)
            {
                height += trackContainerHeight;
            }
            else
            {
                // 如果layout还未计算，使用估算值
                height += trackDataList.Count * TRACK_ITEM_HEIGHT;
            }
        }

        playheadElement.style.height = height;

        // 更新进度线的高度（从顶部到轨道底部）
        var playheadLine = playheadElement.Q<VisualElement>("PlayheadLine");
        if (playheadLine != null)
        {
            playheadLine.style.height = height;
            playheadLine.style.top = 0f;
        }
    }

    // 更新播放进度条的位置
    private void UpdatePlayheadPosition()
    {
        if (playheadElement == null) return;

        // playhead在内容容器内的绝对位置（不需要减去滚动偏移）
        float xPosition = currentPlaybackTime * pixelsPerSecond;
        // 红线居中在时间位置，所以需要减去线宽的一半；同时 clamp 避免越界触发 ScrollView 横向滚动条。
        float contentWidth = GetContentWidth();
        float desiredLeft = xPosition - PLAYHEAD_HALF_WIDTH;
        playheadElement.style.left = Mathf.Clamp(desiredLeft, 0f, Mathf.Max(0f, contentWidth - PLAYHEAD_WIDTH));
        
        // 更新时间显示
        UpdateTimeLengthDisplay();
    }
    
    // 更新时间长度显示
    private void UpdateTimeLengthDisplay()
    {
        if (timeLengthLabel == null) return;
        
        // 获取clip的最大结束时间
        float maxClipTime = GetMaxClipEndTime();
        float displayMaxTime = maxClipTime > 0 ? maxClipTime : 0f;
        
        // 格式化显示：当前播放时间s / clip最大时间s
        timeLengthLabel.text = $"{currentPlaybackTime:F2}s / {displayMaxTime:F2}s";
    }

    // 设置播放进度条的交互
    private void SetupPlayheadInteractions()
    {
        if (playheadElement == null) return;

        float prevScrubTime = 0f;

        playheadElement.RegisterCallback<MouseDownEvent>(evt => {
            // 点击播放条即可开始拖动
            if (evt.button == 0)
            {
                // 点击时间轴/拖动 playhead 时自动暂停播放
                if (isPlaying)
                {
                    PausePreviewPlayback();
                }
                isDraggingPlayhead = true;
                prevScrubTime = currentPlaybackTime;
                playheadElement.CaptureMouse();
                evt.StopPropagation();
            }
        });

        // 鼠标移动 - 拖动过程
        playheadElement.RegisterCallback<MouseMoveEvent>(evt => {
            if (isDraggingPlayhead && playheadElement.HasMouseCapture())
            {
                // 获取相对于内容容器的鼠标位置
                Vector2 localMousePos = timelineContent.WorldToLocal(evt.mousePosition);
                float newX = localMousePos.x;

                // 计算实际播放时间
                currentPlaybackTime = newX / pixelsPerSecond;
                // 限制在clip的最大时间范围内
                float maxClipTime = GetMaxClipEndTime();
                currentPlaybackTime = Mathf.Clamp(currentPlaybackTime, 0f, maxClipTime);

                // 更新位置
                UpdatePlayheadPosition();

                // 更新动画预览
                UpdateAnimationPreview();

                // 拖拽预览：VFX 需要"按时间点求值"，否则只会在 Play 时触发一次
                EvaluatePreviewVfxAtTime(currentPlaybackTime);

                // 拖拽预览：SFX（可选）——只在向前拖动时按跨越区间触发一次，避免来回拖动爆音
                float from = prevScrubTime;
                float to = currentPlaybackTime;
                if (to > from)
                {
                    TryTriggerPreviewSfx(from, to, maxClipTime, wrapped: false);
                }
                prevScrubTime = currentPlaybackTime;

                evt.StopPropagation();
            }
        });

        // 鼠标释放 - 结束拖动
        playheadElement.RegisterCallback<MouseUpEvent>(evt => {
            if (isDraggingPlayhead)
            {
                isDraggingPlayhead = false;
                playheadElement.ReleaseMouse();
                evt.StopPropagation();
            }
        });

        // 在内容容器上也添加点击来移动播放进度条
        if (timelineContent != null)
        {
            timelineContent.RegisterCallback<MouseDownEvent>(evt => {
                // 如果点击的是轨道空白区域（不是clip），移动播放进度条
                if (evt.button == 0 && !isDragging && !isDraggingPlayhead)
                {
                    // 点击时间轴时自动暂停播放（保持当前帧/便于调参）
                    if (isPlaying)
                    {
                        PausePreviewPlayback();
                    }
                    Vector2 localMousePos = timelineContent.WorldToLocal(evt.mousePosition);
                    float newX = localMousePos.x;
                    
                    // 检查是否点击在Ruler或轨道区域
                    VisualElement target = evt.target as VisualElement;
                    bool isOnRuler = target == timelineRuler;
                    bool isOnTrack = false;
                    
                    // 检查是否在轨道区域内（通过向上查找父元素）
                    if (target != null && trackContainer != null)
                    {
                        VisualElement parent = target.parent;
                        while (parent != null && parent != timelineContent)
                        {
                            if (parent == trackContainer || parent.name == "track")
                            {
                                isOnTrack = true;
                                break;
                            }
                            parent = parent.parent;
                        }
                    }
                    
                    if (isOnRuler || isOnTrack)
                    {
                        // 计算实际播放时间
                        currentPlaybackTime = newX / pixelsPerSecond;
                        // 限制在clip的最大时间范围内
                        float maxClipTime = GetMaxClipEndTime();
                        currentPlaybackTime = Mathf.Clamp(currentPlaybackTime, 0f, maxClipTime);

                        UpdatePlayheadPosition();
                        UpdateAnimationPreview();

                        // 拖拽预览：VFX 需要"按时间点求值"，否则只会在 Play 时触发一次
                        EvaluatePreviewVfxAtTime(currentPlaybackTime);

                        // 拖拽预览：SFX（可选）——只在向前拖动时按跨越区间触发一次，避免来回拖动爆音
                        float from = prevScrubTime;
                        float to = currentPlaybackTime;
                        if (to > from)
                        {
                            TryTriggerPreviewSfx(from, to, maxClipTime, wrapped: false);
                        }
                        prevScrubTime = currentPlaybackTime;
                    }
                }
            });
        }
    }
    
    // 更新动画预览（根据当前播放时间）
    private void UpdateAnimationPreview()
    {
        SamplePreviewAnimation(currentPlaybackTime);
        // 程序化位移预览：拖拽 playhead 时也要即时更新位移
        UpdatePreviewMovement(currentPlaybackTime);
        // Active 预览：拖拽 playhead 时也要即时更新显隐（否则会出现"到达区间前不隐藏"的错觉）
        UpdatePreviewAttachedActives(currentPlaybackTime, wrapped: false);
        // 命中检测预览：拖拽 playhead 时也要即时更新命中检测
        if (isDraggingPlayhead || isPlaying)
        {
            float maxTime = GetMaxPlaybackTime();
            float prevTime = Mathf.Max(0f, currentPlaybackTime - 0.016f); // 假设约一帧时间
            TryTriggerPreviewHitDetection(prevTime, currentPlaybackTime, maxTime, wrapped: false);
        }
        // 注意：TimeScale 只在自动播放时生效，拖拽时不需要（因为拖拽是手动控制时间，不依赖播放速度）
    }

    #endregion
}

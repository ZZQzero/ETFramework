using System;
using System.Collections.Generic;
using ET;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public partial class SkillEditorWindow : EditorWindow
{
    // Timeline UI：构建轨道与 Clip 视图 + 交互（点击/拖拽/重叠提示）

    // 仅清理轨道 UI（用于重新初始化数据前）
    private void ClearTrackUI()
    {
        trackContainer?.Clear();
    }

    // 初始化Config提示标签（时间轴空态提示）
    private void InitConfigHint()
    {
        if (timelineContent == null) return;

        // 创建提示标签
        configHintLabel = new Label("请选择Config和角色");
        configHintLabel.name = "ConfigHint";
        configHintLabel.style.position = Position.Absolute;
        configHintLabel.style.left = 0;
        configHintLabel.style.top = 0;
        configHintLabel.style.width = Length.Percent(100);
        configHintLabel.style.height = Length.Percent(100);
        configHintLabel.style.fontSize = 20;
        configHintLabel.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        configHintLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        configHintLabel.style.display = DisplayStyle.Flex;
        timelineContent.Add(configHintLabel);
    }

    // 更新Config提示显示状态
    private void UpdateConfigHint()
    {
        if (configHintLabel == null) return;

        configHintLabel.style.display = config == null ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // 获取时间轴可视区域宽度
    private float GetTimelineViewWidth()
    {
        if (timelineScrollView == null)
        {
            return 800f;
        }
        float viewWidth = timelineScrollView.layout.width;
        return viewWidth > 0 ? viewWidth : 800f;
    }

    // 获取当前水平滚动偏移量
    private float GetScrollOffset()
    {
        if (timelineScrollView == null) return 0f;
        return timelineScrollView.horizontalScroller.value;
    }

    // 获取所有clip中最大的结束时间（用于内容宽度、进度条范围等）
    private float GetMaxClipEndTime()
    {
        float maxEndTime = 0f;
        foreach (var trackItem in trackDataList)
        {
            if (trackItem is AnimationTrack animTrack)
            {
                foreach (var clip in animTrack.ClipList)
                {
                    float endTime = clip.StartTime + clip.Duration;
                    if (endTime > maxEndTime) maxEndTime = endTime;
                }
            }
            else if (trackItem is EffectTrack effectTrack)
            {
                foreach (var clip in effectTrack.ClipList)
                {
                    float endTime = clip.StartTime + clip.Duration;
                    if (endTime > maxEndTime) maxEndTime = endTime;
                }
            }
            else if (trackItem is SoundTrack soundTrack)
            {
                foreach (var clip in soundTrack.ClipList)
                {
                    float endTime = clip.StartTime + clip.Duration;
                    if (endTime > maxEndTime) maxEndTime = endTime;
                }
            }
            else if (trackItem is HitBoxTrack hitBoxTrack)
            {
                foreach (var clip in hitBoxTrack.ClipList)
                {
                    float endTime = clip.StartTime + clip.Duration;
                    if (endTime > maxEndTime) maxEndTime = endTime;
                }
            }
        }
        return maxEndTime;
    }

    // 获取内容宽度
    private float GetContentWidth()
    {
        float viewWidth = GetTimelineViewWidth();

        if (config == null)
        {
            return viewWidth;
        }
        // 基础内容宽度 = 可视区域宽度 × 缩放倍数
        float baseContentWidth = viewWidth * zoomScale;

        // 内容宽度 = max(缩放后的宽度, clip最大位置)
        float maxClipEndTime = GetMaxClipEndTime();
        float clipBasedWidth = maxClipEndTime > 0 ? maxClipEndTime * pixelsPerSecond : 0f;
        return Mathf.Max(baseContentWidth, clipBasedWidth);
    }

    // 更新时间轴内容容器的宽度
    private void UpdateTimelineContentWidth()
    {
        if (timelineContent == null) return;

        float contentWidth = GetContentWidth();

        timelineContent.style.width = contentWidth;
        timelineContent.style.minWidth = contentWidth;
        // 更新标尺的宽度
        if (timelineRuler != null)
        {
            timelineRuler.style.width = contentWidth;
            timelineRuler.style.minWidth = contentWidth;
        }
        // 同时更新所有轨道的宽度
        UpdateAllTrackWidths();
    }

    // 更新所有轨道的宽度（含视觉缓冲，拖动时不穿帮）
    private void UpdateAllTrackWidths()
    {
        if (trackContainer == null) return;

        float trackWidth = GetContentWidth();

        var trackElements = trackContainer.Query<VisualElement>(name: "track").ToList();
        foreach (var trackElement in trackElements)
        {
            trackElement.style.width = trackWidth;
            trackElement.style.minWidth = trackWidth;
        }
    }
    private void CreateTrack()
    {
        // 创建轨道元素
        for (int i = 0; i < trackDataList.Count; i++)
        {
            var trackElement = CreateTrackElement(trackDataList[i], i);
            trackContainer.Add(trackElement);
        }

        // 更新播放进度条高度（轨道数量可能改变）
        if (playheadElement != null)
        {
            UpdatePlayheadSize();
        }

        // 如果有选中的轨道，更新轨道信息显示
        if (selectedTrack != null)
        {
            // 检查选中的轨道是否仍然存在
            bool trackStillExists = trackDataList.Contains(selectedTrack);
            if (trackStillExists)
            {
                UpdateTrackInfo(selectedTrack);
            }
            else
            {
                // 轨道已不存在，清空选择
                ClearSelection();
            }
        }
        else if (trackDataList.Count > 0)
        {
            // 如果没有选中轨道，默认选中第一个轨道
            UpdateTrackInfo(trackDataList[0]);
        }
    }

    // 创建轨道元素
    private VisualElement CreateTrackElement(ITrackItem trackData, int index)
    {
        var trackElement = new VisualElement();
        trackElement.name = "track";
        trackElement.AddToClassList("timeline-track");
        trackElement.style.position = Position.Relative;
        trackElement.style.height = TRACK_ITEM_HEIGHT;
        trackElement.style.overflow = Overflow.Visible; // 允许clip超出显示（ScrollView会处理滚动）
        trackElement.userData = trackData;
        trackData.Index = index;

        // 设置轨道宽度
        float trackWidth = GetContentWidth();
        trackElement.style.width = trackWidth;
        trackElement.style.minWidth = trackWidth;

        // 给轨道容器添加点击事件
        SetupTrackInteractions(trackElement);

        // 创建轨道中的clip
        List<VisualElement> clipElements = new List<VisualElement>();

        if (trackData is AnimationTrack animationTrack)
        {
            foreach (var clipItem in animationTrack.ClipList)
            {
                var clipElement = SetClipItem(trackElement, clipItem);
                if (clipItem.SegmentData != null && clipItem.SegmentData.AnimationClipTrans != null)
                {
                    if (clipElement is Label label)
                    {
                        label.text = clipItem.Name;
                    }
                }
                clipElements.Add(clipElement);
            }
        }
        else if (trackData is EffectTrack effectTrack)
        {
            foreach (var clipItem in effectTrack.ClipList)
            {
                var clipElement = SetClipItem(trackElement, clipItem);
                clipElements.Add(clipElement);
            }
        }
        else if (trackData is SoundTrack soundTrack)
        {
            foreach (var clipItem in soundTrack.ClipList)
            {
                var clipElement = SetClipItem(trackElement, clipItem);
                clipElements.Add(clipElement);
            }
        }
        else if (trackData is HitBoxTrack hitBoxTrack)
        {
            foreach (var clipItem in hitBoxTrack.ClipList)
            {
                var clipElement = SetClipItem(trackElement, clipItem);
                clipElements.Add(clipElement);
            }
        }

        // 检测并高亮显示重叠区域
        HighlightOverlappingClips(trackElement, clipElements);

        return trackElement;
    }

    // 刷新轨道内容
    private void RefreshTrackContent()
    {
        if (trackContainer == null) return;

        trackContainer.Clear();

        for (int i = 0; i < trackDataList.Count; i++)
        {
            var trackElement = CreateTrackElement(trackDataList[i], i);
            trackContainer.Add(trackElement);
        }

        // 更新播放进度条高度
        UpdatePlayheadSize();

        // 选中对象可能在刷新前已被删除/过滤（尤其是局部视图过滤空子轨道），这里要先做存在性校验，避免“看起来没刷新”。
        if (selectedTrack != null && !trackDataList.Contains(selectedTrack))
        {
            selectedTrack = null;
            UpdateTrackInfo(null);
        }
        else if (selectedTrack != null)
        {
            UpdateTrackInfo(selectedTrack);
        }

        // Clip 不一定存在于当前视图（比如全局/局部切换、或数据被删除），这里用 owner 反推校验更稳妥。
        if (selectedClip != null)
        {
            var owner = GetOwnerAnimationClipItem(selectedClip);
            if (owner == null)
            {
                selectedClip = null;
                UpdateClipProperties(null);
            }
            else
            {
                UpdateClipProperties(selectedClip);
            }
        }

        RefreshSelectionHighlight();
    }

    private VisualElement SetClipItem(VisualElement element, IClipItem clipItem)
    {
        var clipElement = new Label(clipItem.Name);
        clipElement.text = clipItem.Name;
        clipElement.AddToClassList("timeline-clip");
        string clipTypeClass = GetTypeClass(clipItem.Type);
        if (!string.IsNullOrEmpty(clipTypeClass))
        {
            clipElement.AddToClassList(clipTypeClass);
        }
        clipElement.style.position = Position.Absolute; // 绝对定位以支持拖拽

        // 根据开始时间和时长计算位置和宽度
        float xPosition = clipItem.StartTime * pixelsPerSecond;
        float clipWidth = Mathf.Max(clipItem.Duration * pixelsPerSecond, 20f); // 最小宽度20像素
        clipElement.style.left = xPosition;
        clipElement.style.top = GetClipTopOffset(); // 垂直居中（整数像素）
        clipElement.style.height = CLIP_ITEM_HEIGHT;
        clipElement.style.width = clipWidth;
        clipElement.text = clipItem.Name;

        SetupClipInteractions(clipElement);
        element.Add(clipElement);

        // 存储clip数据到元素中，方便后续重叠检测
        clipElement.userData = clipItem;

        return clipElement;
    }

    // 检测并高亮显示重叠的clip - 只高亮重叠部分
    private void HighlightOverlappingClips(VisualElement trackElement, List<VisualElement> clipElements)
    {
        // 清除之前的所有高亮覆盖层
        var existingHighlights = trackElement.Query<VisualElement>(className: "overlap-highlight").ToList();
        foreach (var highlight in existingHighlights)
        {
            trackElement.Remove(highlight);
        }

        // 检测所有clip之间的重叠
        for (int i = 0; i < clipElements.Count; i++)
        {
            var clip1 = clipElements[i];
            var clipItem1 = clip1.userData as IClipItem;
            if (clipItem1 == null) continue;

            float clip1Start = clipItem1.StartTime;
            float clip1End = clipItem1.StartTime + clipItem1.Duration;

            for (int j = i + 1; j < clipElements.Count; j++)
            {
                var clip2 = clipElements[j];
                var clipItem2 = clip2.userData as IClipItem;
                if (clipItem2 == null) continue;

                float clip2Start = clipItem2.StartTime;
                float clip2End = clipItem2.StartTime + clipItem2.Duration;

                // 检查是否重叠
                if (clip1Start < clip2End && clip2Start < clip1End)
                {
                    // 计算重叠区域
                    float overlapStart = Mathf.Max(clip1Start, clip2Start);
                    float overlapEnd = Mathf.Min(clip1End, clip2End);
                    float overlapDuration = overlapEnd - overlapStart;

                    if (overlapDuration > 0)
                    {
                        // 计算重叠程度 (0-1)
                        float totalDuration = Mathf.Max(clip1End, clip2End) - Mathf.Min(clip1Start, clip2Start);
                        float overlapRatio = overlapDuration / totalDuration;

                        // 创建高亮覆盖层，只覆盖重叠部分
                        var highlightElement = new VisualElement();
                        highlightElement.AddToClassList("overlap-highlight");
                        highlightElement.style.position = Position.Absolute;

                        float highlightX = overlapStart * pixelsPerSecond;
                        float highlightWidth = overlapDuration * pixelsPerSecond;
                        float highlightY = GetClipTopOffset();
                        float highlightHeight = CLIP_ITEM_HEIGHT;

                        highlightElement.style.left = highlightX;
                        highlightElement.style.top = highlightY;
                        highlightElement.style.width = highlightWidth;
                        highlightElement.style.height = highlightHeight;

                        // 根据重叠程度调整颜色深度
                        Color highlightColor = Color.Lerp(
                            new Color(1f, 1f, 0f, 0.3f),  // 浅黄色（轻微重叠）
                            new Color(1f, 0.4f, 0f, 0.7f), // 深橙色（高度重叠）
                            overlapRatio
                        );

                        highlightElement.style.backgroundColor = highlightColor;
                        highlightElement.style.borderTopWidth = 2;
                        highlightElement.style.borderTopColor = new Color(1f, 0.6f, 0f, 0.9f);
                        highlightElement.style.borderBottomWidth = 2;
                        highlightElement.style.borderBottomColor = new Color(1f, 0.6f, 0f, 0.9f);

                        // 确保高亮层在clip之上但低于鼠标事件层级
                        highlightElement.pickingMode = PickingMode.Ignore; // 不接收鼠标事件，让下面的clip能正常交互

                        trackElement.Add(highlightElement);
                    }
                }
            }
        }
    }

    // 拖拽过程中实时检测当前clip与其他clip的重叠
    private void HighlightDraggingClipOverlap(VisualElement trackElement, List<VisualElement> clipElements, VisualElement draggingClip)
    {
        // 清除之前的所有高亮覆盖层
        var existingHighlights = trackElement.Query<VisualElement>(className: "overlap-highlight").ToList();
        foreach (var highlight in existingHighlights)
        {
            trackElement.Remove(highlight);
        }

        var draggingClipItem = draggingClip.userData as IClipItem;
        if (draggingClipItem == null) return;

        // 计算拖拽clip的当前时间位置
        float draggingClipStart = draggingClip.style.left.value.value / pixelsPerSecond;
        float draggingClipEnd = draggingClipStart + draggingClipItem.Duration;

        // 检测所有clip之间的重叠（包括被拖动clip与其他clip的重叠，以及其他clip之间的重叠）
        for (int i = 0; i < clipElements.Count; i++)
        {
            var clip1 = clipElements[i];
            var clipItem1 = clip1.userData as IClipItem;
            if (clipItem1 == null) continue;

            // 对于被拖动的clip，使用当前位置；对于其他clip，使用存储的StartTime
            float clip1Start, clip1End;
            if (clip1 == draggingClip)
            {
                clip1Start = draggingClipStart;
                clip1End = draggingClipEnd;
            }
            else
            {
                clip1Start = clipItem1.StartTime;
                clip1End = clipItem1.StartTime + clipItem1.Duration;
            }

            for (int j = i + 1; j < clipElements.Count; j++)
            {
                var clip2 = clipElements[j];
                var clipItem2 = clip2.userData as IClipItem;
                if (clipItem2 == null) continue;

                // 对于被拖动的clip，使用当前位置；对于其他clip，使用存储的StartTime
                float clip2Start, clip2End;
                if (clip2 == draggingClip)
                {
                    clip2Start = draggingClipStart;
                    clip2End = draggingClipEnd;
                }
                else
                {
                    clip2Start = clipItem2.StartTime;
                    clip2End = clipItem2.StartTime + clipItem2.Duration;
                }

                // 检查是否重叠
                if (clip1Start < clip2End && clip2Start < clip1End)
                {
                    // 计算重叠区域
                    float overlapStart = Mathf.Max(clip1Start, clip2Start);
                    float overlapEnd = Mathf.Min(clip1End, clip2End);
                    float overlapDuration = overlapEnd - overlapStart;

                    if (overlapDuration > 0)
                    {
                        // 计算重叠程度 (0-1)
                        float totalDuration = Mathf.Max(clip1End, clip2End) - Mathf.Min(clip1Start, clip2Start);
                        float overlapRatio = overlapDuration / totalDuration;

                        // 判断是否涉及被拖动的clip，使用不同的颜色
                        bool isDraggingRelated = (clip1 == draggingClip || clip2 == draggingClip);

                        // 创建高亮覆盖层，只覆盖重叠部分
                        var highlightElement = new VisualElement();
                        highlightElement.AddToClassList("overlap-highlight");
                        highlightElement.style.position = Position.Absolute;

                        float highlightX = overlapStart * pixelsPerSecond;
                        float highlightWidth = overlapDuration * pixelsPerSecond;
                        float highlightY = GetClipTopOffset();
                        float highlightHeight = CLIP_ITEM_HEIGHT;

                        highlightElement.style.left = highlightX;
                        highlightElement.style.top = highlightY;
                        highlightElement.style.width = highlightWidth;
                        highlightElement.style.height = highlightHeight;

                        // 根据是否涉及被拖动clip和重叠程度调整颜色深度
                        Color highlightColor;
                        if (isDraggingRelated)
                        {
                            // 被拖动clip的重叠 - 使用更明显的颜色（红色系）
                            highlightColor = Color.Lerp(
                                new Color(1f, 1f, 0f, 0.5f),  // 浅黄色（轻微重叠）
                                new Color(1f, 0f, 0f, 0.8f),   // 红色（高度重叠）- 拖拽时更明显
                                overlapRatio
                            );
                            highlightElement.style.borderTopWidth = 3;
                            highlightElement.style.borderTopColor = new Color(1f, 0.2f, 0f, 1f);
                            highlightElement.style.borderBottomWidth = 3;
                            highlightElement.style.borderBottomColor = new Color(1f, 0.2f, 0f, 1f);
                        }
                        else
                        {
                            // 其他clip之间的重叠 - 使用普通颜色（橙色系）
                            highlightColor = Color.Lerp(
                                new Color(1f, 1f, 0f, 0.3f),  // 浅黄色（轻微重叠）
                                new Color(1f, 0.4f, 0f, 0.7f), // 深橙色（高度重叠）
                                overlapRatio
                            );
                            highlightElement.style.borderTopWidth = 2;
                            highlightElement.style.borderTopColor = new Color(1f, 0.6f, 0f, 0.9f);
                            highlightElement.style.borderBottomWidth = 2;
                            highlightElement.style.borderBottomColor = new Color(1f, 0.6f, 0f, 0.9f);
                        }

                        highlightElement.style.backgroundColor = highlightColor;

                        // 确保高亮层在clip之上但低于鼠标事件层级
                        highlightElement.pickingMode = PickingMode.Ignore;

                        trackElement.Add(highlightElement);
                    }
                }
            }
        }
    }

    // 拖拽结束后自动调整位置消除重叠
    private void ResolveOverlapOnDragEnd(VisualElement trackElement, List<VisualElement> clipElements, VisualElement draggedClip)
    {
        var draggedClipItem = draggedClip.userData as IClipItem;
        if (draggedClipItem == null) return;

        // 使用已经同步的StartTime
        float draggedClipStart = draggedClipItem.StartTime;
        float draggedClipEnd = draggedClipStart + draggedClipItem.Duration;

        // 检测与其他clip的重叠
        List<(VisualElement clip, float overlapStart, float overlapEnd)> overlappingClips = new List<(VisualElement, float, float)>();

        foreach (var otherClip in clipElements)
        {
            if (otherClip == draggedClip) continue;

            var otherClipItem = otherClip.userData as IClipItem;
            if (otherClipItem == null) continue;

            float otherClipStart = otherClipItem.StartTime;
            float otherClipEnd = otherClipItem.StartTime + otherClipItem.Duration;

            if (draggedClipStart < otherClipEnd && otherClipStart < draggedClipEnd)
            {
                float overlapStart = Mathf.Max(draggedClipStart, otherClipStart);
                float overlapEnd = Mathf.Min(draggedClipEnd, otherClipEnd);
                overlappingClips.Add((otherClip, overlapStart, overlapEnd));
            }
        }

        // 拖拽结束，不自动调整位置，保持用户拖拽的确切位置；仅输出提示并重绘高亮
        if (overlappingClips.Count > 0)
        {
            float totalOverlapDuration = 0f;
            foreach (var (_, overlapStart, overlapEnd) in overlappingClips)
            {
                totalOverlapDuration += (overlapEnd - overlapStart);
            }

            float overlapRatio = totalOverlapDuration / draggedClipItem.Duration;
            Debug.Log($"clip重叠检测 - 重叠占比: {overlapRatio:P1}, 位置保持不变");
        }

        HighlightOverlappingClips(trackElement, clipElements);
    }

    private void SetupClipInteractions(VisualElement clipElement)
    {
        // 允许Clip元素接收鼠标事件
        clipElement.pickingMode = PickingMode.Position;

        // 鼠标悬停效果
        clipElement.RegisterCallback<MouseEnterEvent>(evt =>
        {
            if (!isDragging)
            {
                clipElement.AddToClassList("hovered");
            }
        });

        clipElement.RegisterCallback<MouseLeaveEvent>(evt =>
        {
            clipElement.RemoveFromClassList("hovered");
            if (!isDragging)
            {
                clipElement.RemoveFromClassList("dragging");
            }
        });

        // 鼠标按下 - 开始拖拽
        clipElement.RegisterCallback<MouseDownEvent>(evt =>
        {
            if (evt.button == 0) // 左键
            {
                // 选中Clip并更新显示（Selection 统一管理）
                if (clipElement.userData is IClipItem clipItem)
                {
                    var trackElement = clipElement.parent;
                    ITrackItem trackItem = null;
                    if (trackElement != null && trackElement.userData is ITrackItem t)
                    {
                        trackItem = t;
                    }

                    SelectClip(clipItem, trackItem);
                }

                isDragging = true;
                dragStartPosition = evt.mousePosition;

                // 计算鼠标点击位置相对于 clip 左侧的偏移量
                float currentLeft = clipElement.layout.x;
                dragOffset = evt.mousePosition.x - currentLeft;
                clipElement.AddToClassList("dragging"); // 添加拖拽样式类
                clipElement.CaptureMouse(); // 捕获鼠标，确保能接收鼠标移动事件
                evt.StopPropagation(); // 阻止事件冒泡到轨道
            }
        });

        // 鼠标移动 - 拖拽过程
        clipElement.RegisterCallback<MouseMoveEvent>(evt =>
        {
            if (isDragging && clipElement.HasMouseCapture())
            {
                float newLeft = evt.mousePosition.x - dragOffset;
                newLeft = Mathf.Max(0f, newLeft);
                clipElement.style.left = newLeft;

                // 实时更新高亮区域
                var trackElement = clipElement.parent;
                if (trackElement != null)
                {
                    var clipElements = trackElement.Query<VisualElement>().Where(x => x is Label && x.userData is IClipItem).ToList();
                    HighlightDraggingClipOverlap(trackElement, clipElements, clipElement);
                }

                evt.StopPropagation();
            }
        });

        // 鼠标释放 - 结束拖拽
        clipElement.RegisterCallback<MouseUpEvent>(evt =>
        {
            if (isDragging)
            {
                clipElement.ReleaseMouse();
                clipElement.RemoveFromClassList("dragging");

                // 拖拽结束后，同步更新数据
                var draggedClipItem = clipElement.userData as IClipItem;
                if (draggedClipItem != null)
                {
                    isDragging = false;
                    float currentStartTime = clipElement.style.left.value.value / pixelsPerSecond;
                    draggedClipItem.StartTime = Mathf.Max(0f, currentStartTime);

                    // 写回Config/数据结构
                    SyncDraggedClipToConfig(draggedClipItem);

                    clipElement.style.top = GetClipTopOffset();
                    UpdateClipProperties(draggedClipItem);
                }

                // 重绘重叠高亮
                var trackElement = clipElement.parent;
                if (trackElement != null)
                {
                    var clipElements = trackElement.Query<VisualElement>().Where(x => x is Label && x.userData is IClipItem).ToList();
                    ResolveOverlapOnDragEnd(trackElement, clipElements, clipElement);
                }

                UpdateTimelineContentWidth();
                DrawTimelineRulerMarks();

                evt.StopPropagation();
            }
        });
    }

    private void SetupTrackInteractions(VisualElement trackElement)
    {
        // 轨道点击事件（点击轨道空白区域）
        trackElement.RegisterCallback<MouseDownEvent>(evt =>
        {
            if (isDragging) return;

            if (trackElement.userData is ITrackItem trackInfo)
            {
                if (evt.button == 0)
                {
                    SelectTrack(trackInfo);
                    evt.StopPropagation();
                }
                else if (evt.button == 1)
                {
                    // 已移除右键菜单逻辑，仅阻止冒泡
                    evt.StopPropagation();
                }
            }
        });

        // 轨道悬停效果
        trackElement.RegisterCallback<MouseEnterEvent>(evt =>
        {
            if (!isDragging)
            {
                trackElement.AddToClassList("hovered");
            }
        });

        trackElement.RegisterCallback<MouseLeaveEvent>(evt =>
        {
            if (!isDragging)
            {
                trackElement.RemoveFromClassList("hovered");
            }
        });

        trackElement.pickingMode = PickingMode.Position;
    }

    // 更新所有clip的位置和宽度（用于缩放后统一刷新）
    private void UpdateAllClipPositions()
    {
        if (trackContainer == null) return;

        // 遍历所有轨道元素
        var trackElements = trackContainer.Query<VisualElement>(name: "track").ToList();
        foreach (var trackElement in trackElements)
        {
            if (trackElement.userData is ITrackItem trackData)
            {
                // 获取轨道中的所有clip元素
                var clipElements = trackElement.Query<VisualElement>().Where(x => x is Label && x.userData is IClipItem).ToList();

                // 根据轨道类型获取对应的clip列表
                List<IClipItem> clipList = null;
                if (trackData is AnimationTrack animationTrack)
                {
                    clipList = animationTrack.ClipList.ConvertAll(x => (IClipItem)x);
                }
                else if (trackData is EffectTrack effectTrack)
                {
                    clipList = effectTrack.ClipList.ConvertAll(x => (IClipItem)x);
                }
                else if (trackData is SoundTrack soundTrack)
                {
                    clipList = soundTrack.ClipList.ConvertAll(x => (IClipItem)x);
                }
                else if (trackData is HitBoxTrack hitBoxTrack)
                {
                    clipList = hitBoxTrack.ClipList.ConvertAll(x => (IClipItem)x);
                }

                if (clipList != null)
                {
                    // 更新每个clip的位置和宽度
                    for (int i = 0; i < Mathf.Min(clipElements.Count, clipList.Count); i++)
                    {
                        var clipElement = clipElements[i];
                        var clipItem = clipList[i];

                        float xPosition = clipItem.StartTime * pixelsPerSecond;
                        float clipWidth = clipItem.Duration * pixelsPerSecond;

                        clipElement.style.left = xPosition;
                        clipElement.style.width = clipWidth;
                        clipElement.style.top = GetClipTopOffset(); // 确保垂直居中（整数像素）
                    }

                    // 重新检测并高亮显示重叠区域
                    HighlightOverlappingClips(trackElement, clipElements);
                }
            }
        }
    }
    
    private static float GetClipTopOffset()
    {
        return Mathf.RoundToInt((TRACK_ITEM_HEIGHT - CLIP_ITEM_HEIGHT) * 0.5f);
    }
    // Selection 高亮已抽到 SkillEditorWindow.Selection.cs
}


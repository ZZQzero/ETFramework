using System;
using System.Collections.Generic;
using ET;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public partial class SkillEditorWindow : EditorWindow
{
    private const float LANE_PADDING_Y = 2f;
    private const float LANE_GAP_Y = 4f;
    private const float LANE_ROW_HEIGHT = CLIP_ITEM_HEIGHT + LANE_GAP_Y;
    private const float RESIZE_HANDLE_HIT_WIDTH_PX = 6f; // 右侧拖拽缩放热区宽度（像素）
    private const string RESIZE_CURSOR_CLASS = "cursor-resize-h";

    // 动画结束时间黄色竖线相关常量
    private const float ANIMATION_END_LINE_WIDTH = 2f; // 黄色竖线宽度
    private const float ANIMATION_END_HALF_WIDTH = ANIMATION_END_LINE_WIDTH * 0.5f; // 半宽用于定位

    private readonly Dictionary<IClipItem, int> laneIndexByClip = new();
    private int draggingLaneIndex = -1;

    // AnimationClip 拖拽时：缓存其子 clip 的 UI 元素，便于实时同步位置
    private AnimationClipItem draggingOwnerAnimationClipItem;
    private List<(IClipItem item, VisualElement element)> draggingOwnerChildClips;

    // 子 clip（HitBox/Active）拖拽到 AnimationEnd 边界时：显示红线提示
    private VisualElement dragAnimationEndLineElement;
    private VisualElement dragAnimationEndLineOwnerTrackElement; // AnimationClip 所在轨道行
    private VisualElement dragAnimationEndLineChildTrackElement; // 当前被拖动子 clip 所在轨道行

    // 动画结束时间黄色竖线
    private readonly Dictionary<AnimationClipItem, VisualElement> animationEndLineElements = new();

    // 右侧拖拽缩放（Active/Effect/HitBox）
    private bool isResizingClip;
    private VisualElement resizingClipElement;
    private IClipItem resizingClipItem;
    private float resizeStartMouseX;
    private float resizeStartWidthPx;
    private float resizeClipLeftPx; // 固定左侧（用于计算最大可用宽度）
    private AnimationClipItem resizingOwnerClipItem; // 所属 AnimationClip（用于 Active/HitBox 约束 AnimationEnd）

    private static bool IsLaneTrack(ITrackItem track)
    {
        return track is EffectTrack || track is SoundTrack || track is HitBoxTrack || track is ActiveTrack;
    }

    private static bool IsResizableClip(IClipItem clipItem)
    {
        // 右侧拖拽改变长度：Active / Effect / HitBox
        return clipItem is ActiveClipItem || clipItem is EffectClipItem || clipItem is HitBoxClipItem;
    }

    private static bool IsMouseNearRightEdge(VisualElement el, Vector2 localMousePos)
    {
        if (el == null)
        {
            return false;
        }

        float w = el.resolvedStyle.width;
        if (w <= 0f)
        {
            w = el.layout.width;
        }
        if (w <= 0f)
        {
            return false;
        }

        return localMousePos.x >= (w - RESIZE_HANDLE_HIT_WIDTH_PX);
    }

    private static float GetElementLeftPx(VisualElement el)
    {
        if (el == null)
        {
            return 0f;
        }

        var left = el.style.left;
        if (left.keyword == StyleKeyword.Auto)
        {
            return el.layout.x;
        }

        return left.value.value;
    }

    private static float GetElementWidthPx(VisualElement el)
    {
        if (el == null)
        {
            return 0f;
        }

        float w = el.resolvedStyle.width;
        if (w <= 0f)
        {
            w = el.layout.width;
        }
        if (w <= 0f)
        {
            var sw = el.style.width;
            if (sw.keyword != StyleKeyword.Auto)
            {
                w = sw.value.value;
            }
        }
        return w;
    }

    private void EnsureDragAnimationEndLine()
    {
        if (timelineContent == null)
        {
            return;
        }

        if (dragAnimationEndLineElement != null)
        {
            return;
        }

        // 兜底：避免热重载/重复创建导致残留
        var existing = timelineContent.Q<VisualElement>("DragAnimationEndLine");
        if (existing != null)
        {
            existing.RemoveFromHierarchy();
        }

        var line = new VisualElement();
        line.name = "DragAnimationEndLine";
        line.style.position = Position.Absolute;
        line.style.top = 0f;
        line.style.left = 0f;
        line.style.width = PLAYHEAD_LINE_WIDTH;
        line.style.height = 0f;
        // 与播放轴一致的红色
        line.style.backgroundColor = new Color(1f, 0.3f, 0.3f, 1f);
        line.style.display = DisplayStyle.None;
        line.pickingMode = PickingMode.Ignore;

        timelineContent.Add(line); // 保持在最上层
        dragAnimationEndLineElement = line;
    }

    private void HideDragAnimationEndLine()
    {
        if (dragAnimationEndLineElement == null)
        {
            return;
        }

        dragAnimationEndLineElement.style.display = DisplayStyle.None;
        dragAnimationEndLineOwnerTrackElement = null;
        dragAnimationEndLineChildTrackElement = null;
    }

    private void ShowDragAnimationEndLine(float xPositionPx)
    {
        if (timelineContent == null)
        {
            return;
        }

        EnsureDragAnimationEndLine();
        if (dragAnimationEndLineElement == null)
        {
            return;
        }

        // 没有轨道范围信息就不画（避免画满屏）
        if (dragAnimationEndLineOwnerTrackElement == null || dragAnimationEndLineChildTrackElement == null)
        {
            dragAnimationEndLineElement.style.display = DisplayStyle.None;
            return;
        }

        // 计算从 AnimationClip 轨道到子轨道的纵向范围（timelineContent 本地坐标）
        float ownerTopWorld = dragAnimationEndLineOwnerTrackElement.worldBound.yMin;
        float ownerBottomWorld = dragAnimationEndLineOwnerTrackElement.worldBound.yMax;
        float childTopWorld = dragAnimationEndLineChildTrackElement.worldBound.yMin;
        float childBottomWorld = dragAnimationEndLineChildTrackElement.worldBound.yMax;

        float topWorld = Mathf.Min(ownerTopWorld, childTopWorld);
        float bottomWorld = Mathf.Max(ownerBottomWorld, childBottomWorld);

        float topPx = timelineContent.WorldToLocal(new Vector2(0f, topWorld)).y;
        float bottomPx = timelineContent.WorldToLocal(new Vector2(0f, bottomWorld)).y;

        dragAnimationEndLineElement.style.display = DisplayStyle.Flex;
        // 红线居中在 AnimationEnd 的 x 位置
        dragAnimationEndLineElement.style.left = Mathf.Max(0f, xPositionPx - PLAYHEAD_HALF_WIDTH);
        dragAnimationEndLineElement.style.top = Mathf.Max(0f, topPx);
        dragAnimationEndLineElement.style.height = Mathf.Max(0f, bottomPx - topPx);
        dragAnimationEndLineElement.BringToFront();
    }

    private VisualElement FindClipElement(IClipItem clipItem)
    {
        if (clipItem == null || trackContainer == null)
        {
            return null;
        }

        // 这里只在拖拽开始时调用一次，性能足够；后续通过缓存 trackElement 来计算红线高度
        var clipElements = trackContainer.Query<VisualElement>(className: "timeline-clip").ToList();
        for (int i = 0; i < clipElements.Count; ++i)
        {
            var el = clipElements[i];
            if (el?.userData is IClipItem c && ReferenceEquals(c, clipItem))
            {
                return el;
            }
        }

        return null;
    }

    private static float GetLaneTopPx(int laneIndex)
    {
        return Mathf.RoundToInt(LANE_PADDING_Y + laneIndex * LANE_ROW_HEIGHT);
    }

    private void BuildDraggingOwnerChildClipCache(AnimationClipItem owner)
    {
        draggingOwnerAnimationClipItem = owner;
        draggingOwnerChildClips = null;

        if (owner == null || trackContainer == null)
        {
            return;
        }

        if (!animationClipTrackMap.TryGetValue(owner, out var tracks) || tracks == null || tracks.Count == 0)
        {
            return;
        }

        // 建立 userData(IClipItem) -> element 的快速映射
        var clipElements = trackContainer.Query<VisualElement>(className: "timeline-clip").ToList();
        var elementByClip = new Dictionary<IClipItem, VisualElement>(clipElements.Count);
        for (int i = 0; i < clipElements.Count; ++i)
        {
            var el = clipElements[i];
            if (el?.userData is IClipItem clip)
            {
                elementByClip[clip] = el;
            }
        }

        var list = new List<(IClipItem item, VisualElement element)>(32);
        foreach (var t in tracks)
        {
            switch (t)
            {
                case EffectTrack et:
                    foreach (var c in et.ClipList)
                    {
                        if (c != null && elementByClip.TryGetValue(c, out var el))
                        {
                            list.Add((c, el));
                        }
                    }
                    break;
                case SoundTrack st:
                    foreach (var c in st.ClipList)
                    {
                        if (c != null && elementByClip.TryGetValue(c, out var el))
                        {
                            list.Add((c, el));
                        }
                    }
                    break;
                case HitBoxTrack ht:
                    foreach (var c in ht.ClipList)
                    {
                        if (c != null && elementByClip.TryGetValue(c, out var el))
                        {
                            list.Add((c, el));
                        }
                    }
                    break;
                case ActiveTrack at:
                    foreach (var c in at.ClipList)
                    {
                        if (c != null && elementByClip.TryGetValue(c, out var el))
                        {
                            list.Add((c, el));
                        }
                    }
                    break;
            }
        }

        draggingOwnerChildClips = list;
    }

    private void ClearDraggingOwnerChildClipCache()
    {
        draggingOwnerAnimationClipItem = null;
        draggingOwnerChildClips = null;
    }

    private void UpdateDraggingOwnerChildClipPositions(float ownerStartTimeSeconds)
    {
        var owner = draggingOwnerAnimationClipItem;
        var list = draggingOwnerChildClips;
        if (owner == null || list == null || list.Count == 0)
        {
            return;
        }

        float ownerDuration = Mathf.Max(0f, owner.Duration);
        var touchedTracks = new System.Collections.Generic.HashSet<VisualElement>();

        for (int i = 0; i < list.Count; ++i)
        {
            var (item, el) = list[i];
            if (item == null || el == null)
            {
                continue;
            }

            if (el.parent != null)
            {
                touchedTracks.Add(el.parent);
            }

            float start = item.StartTime;
            float duration = Mathf.Max(0f, item.Duration);

            switch (item)
            {
                case EffectClipItem effectClip when effectClip.EffectData != null:
                    start = ownerStartTimeSeconds + effectClip.EffectData.NormalizedStart * ownerDuration;
                    duration = Mathf.Max(0f, effectClip.Duration);
                    break;
                case SoundClipItem soundClip when soundClip.SoundData != null:
                    start = ownerStartTimeSeconds + soundClip.SoundData.NormalizedStart * ownerDuration;
                    duration = Mathf.Max(0f, soundClip.Duration);
                    break;
                case HitBoxClipItem hitBoxClip when hitBoxClip.HitBoxData != null:
                    start = ownerStartTimeSeconds + hitBoxClip.HitBoxData.NormalizedStart * ownerDuration;
                    duration = Mathf.Max(0f, (hitBoxClip.HitBoxData.NormalizedEnd - hitBoxClip.HitBoxData.NormalizedStart) * ownerDuration);
                    break;
                case ActiveClipItem activeClip when activeClip.ActiveData != null:
                    start = ownerStartTimeSeconds + activeClip.ActiveData.NormalizedStart * ownerDuration;
                    duration = Mathf.Max(0f, (activeClip.ActiveData.NormalizedEnd - activeClip.ActiveData.NormalizedStart) * ownerDuration);
                    break;
            }

            el.style.left = start * pixelsPerSecond;
            el.style.width = Mathf.Max(duration * pixelsPerSecond, MIN_CLIP_WIDTH_PX);
        }

        // 子 clip 的 UI 位置变了：刷新所在轨道的重叠高亮（使用 UI 的 left 计算当前区间）
        foreach (var trackElement in touchedTracks)
        {
            var clipElements = trackElement.Query<VisualElement>().Where(x => x is Label && x.userData is IClipItem).ToList();
            HighlightOverlappingClips(trackElement, clipElements);
        }
    }

    private void SyncOwnerChildClipsToOwner(AnimationClipItem owner)
    {
        if (owner == null)
        {
            return;
        }

        if (!animationClipTrackMap.TryGetValue(owner, out var tracks) || tracks == null)
        {
            return;
        }

        float ownerStart = owner.StartTime;
        float ownerDuration = Mathf.Max(0f, owner.Duration);

        foreach (var t in tracks)
        {
            switch (t)
            {
                case EffectTrack et:
                    foreach (var c in et.ClipList)
                    {
                        if (c?.EffectData == null) continue;
                        c.StartTime = ownerStart + c.EffectData.NormalizedStart * ownerDuration;
                        c.Frame = Mathf.RoundToInt(Mathf.Max(0f, c.Duration) * 60f);
                    }
                    break;
                case SoundTrack st:
                    foreach (var c in st.ClipList)
                    {
                        if (c?.SoundData == null) continue;
                        c.StartTime = ownerStart + c.SoundData.NormalizedStart * ownerDuration;
                        c.Frame = Mathf.RoundToInt(Mathf.Max(0f, c.Duration) * 60f);
                    }
                    break;
                case HitBoxTrack ht:
                    foreach (var c in ht.ClipList)
                    {
                        if (c?.HitBoxData == null) continue;
                        c.StartTime = ownerStart + c.HitBoxData.NormalizedStart * ownerDuration;
                        c.Duration = Mathf.Max(0f, (c.HitBoxData.NormalizedEnd - c.HitBoxData.NormalizedStart) * ownerDuration);
                        c.Frame = Mathf.RoundToInt(Mathf.Max(0f, c.Duration) * 60f);
                    }
                    break;
                case ActiveTrack at:
                    foreach (var c in at.ClipList)
                    {
                        if (c?.ActiveData == null) continue;
                        c.StartTime = ownerStart + c.ActiveData.NormalizedStart * ownerDuration;
                        c.Duration = Mathf.Max(0f, (c.ActiveData.NormalizedEnd - c.ActiveData.NormalizedStart) * ownerDuration);
                        c.Frame = Mathf.RoundToInt(Mathf.Max(0f, c.Duration) * 60f);
                    }
                    break;
            }
        }
    }

    private void ApplyLaneLayoutIfNeeded(VisualElement trackElement, ITrackItem trackData, List<VisualElement> clipElements)
    {
        if (!IsLaneTrack(trackData))
        {
            // 固定高度轨道：维持原来的垂直居中
            trackElement.style.height = TRACK_ITEM_HEIGHT;
            trackElement.style.minHeight = TRACK_ITEM_HEIGHT;
            for (int i = 0; i < clipElements.Count; ++i)
            {
                clipElements[i].style.top = GetClipTopOffset();
            }
            return;
        }

        if (clipElements == null || clipElements.Count == 0)
        {
            trackElement.style.height = TRACK_ITEM_HEIGHT;
            trackElement.style.minHeight = TRACK_ITEM_HEIGHT;
            return;
        }

        // 规则：新增永远追加 lane；刷新时压缩空洞 lane；拖拽不改 lane（重叠用高亮提示）。
        var infos = new List<(VisualElement element, IClipItem item)>(clipElements.Count);
        for (int i = 0; i < clipElements.Count; ++i)
        {
            var el = clipElements[i];
            if (el?.userData is IClipItem item)
            {
                infos.Add((el, item));
            }
        }

        // 1) 压缩空洞：oldLane -> newLane(0..n-1)
        var usedOldLanes = new System.Collections.Generic.SortedSet<int>();
        for (int i = 0; i < infos.Count; ++i)
        {
            var item = infos[i].item;
            if (laneIndexByClip.TryGetValue(item, out int oldLane))
            {
                usedOldLanes.Add(Mathf.Max(0, oldLane));
            }
        }

        if (usedOldLanes.Count > 0)
        {
            var remap = new Dictionary<int, int>(usedOldLanes.Count);
            int newLane = 0;
            foreach (var oldLane in usedOldLanes)
            {
                remap[oldLane] = newLane++;
            }

            // 只写回本轨道 clip，避免影响其它轨道
            for (int i = 0; i < infos.Count; ++i)
            {
                var item = infos[i].item;
                if (laneIndexByClip.TryGetValue(item, out int oldLane) && remap.TryGetValue(Mathf.Max(0, oldLane), out int mapped))
                {
                    laneIndexByClip[item] = mapped;
                }
            }
        }

        // 2) 未分配则追加 lane：max + 1
        int maxLaneIndex = -1;
        for (int i = 0; i < infos.Count; ++i)
        {
            var item = infos[i].item;
            if (laneIndexByClip.TryGetValue(item, out int laneIndex))
            {
                maxLaneIndex = Mathf.Max(maxLaneIndex, laneIndex);
            }
        }

        for (int i = 0; i < infos.Count; ++i)
        {
            var (el, item) = infos[i];
            if (!laneIndexByClip.TryGetValue(item, out int laneIndex))
            {
                laneIndex = maxLaneIndex + 1;
                laneIndexByClip[item] = laneIndex;
                maxLaneIndex = laneIndex;
            }

            el.style.top = GetLaneTopPx(laneIndex);
        }

        int laneCount = Mathf.Max(1, maxLaneIndex + 1);
        float neededHeight = LANE_PADDING_Y * 2f + laneCount * LANE_ROW_HEIGHT - LANE_GAP_Y; // 最后一行不额外加 gap
        float finalHeight = Mathf.Max(TRACK_ITEM_HEIGHT, neededHeight);
        trackElement.style.height = finalHeight;
        trackElement.style.minHeight = finalHeight;
    }

    // 仅清理轨道 UI（用于重新初始化数据前）
    private void ClearTrackUI()
    {
        trackContainer?.Clear();
        ClearAllAnimationEndLines();
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
        // 约束：播放/尺子最大范围应取“轨道内容最大结束时间”。
        // 说明：trackDataList 在视图切换（Global/ClipFocus）时可能会过滤为空；因此这里以 config 作为权威来源计算。
        if (config == null || config.Segments == null || config.Segments.Count == 0)
        {
            return 0f;
        }

        float maxEndTime = 0f;
        foreach (var seg in config.Segments)
        {
            if (seg == null)
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

            // AnimationClip 轨道：按“完整动画段”计算（时间轴可视化的最大范围）
            maxEndTime = Mathf.Max(maxEndTime, seg.StartTime + segDuration);

            // 段结束阈值：子轨道（Effect/Sound/HitBox/Active）的触发窗口限制在 AnimationEnd 内
            float endNorm = GetSegmentAnimationEndNorm(seg);

            // HitBox：区间结束点（不包含关键帧）
            if (seg.HitBoxes != null)
            {
                foreach (var hb in seg.HitBoxes)
                {
                    if (hb == null) continue;
                    float endN = Mathf.Clamp01(hb.NormalizedEnd);
                    endN = Mathf.Min(endN, endNorm);
                    float endTime = seg.StartTime + segDuration * endN;
                    maxEndTime = Mathf.Max(maxEndTime, endTime);
                }
            }

            // Active：区间结束点
            if (seg.AttachedActives != null)
            {
                foreach (var a in seg.AttachedActives)
                {
                    if (a == null) continue;
                    float endN = Mathf.Clamp01(a.NormalizedEnd);
                    endN = Mathf.Min(endN, endNorm);
                    float endTime = seg.StartTime + segDuration * endN;
                    maxEndTime = Mathf.Max(maxEndTime, endTime);
                }
            }

            // VFX：触发点 + Length（允许超出动画段/最后一个段）
            if (seg.VisualEffects != null)
            {
                foreach (var vfx in seg.VisualEffects)
                {
                    if (vfx == null) continue;
                    float tN = Mathf.Clamp01(vfx.NormalizedStart);
                    tN = Mathf.Min(tN, endNorm);
                    float trigger = seg.StartTime + segDuration * tN;
                    float length = Mathf.Max(0f, vfx.Length);
                    maxEndTime = Mathf.Max(maxEndTime, trigger + length);
                }
            }

            // SFX：触发点 + clip.length（允许超出动画段/最后一个段）
            if (seg.SoundEffects != null)
            {
                foreach (var sfx in seg.SoundEffects)
                {
                    if (sfx == null) continue;
                    float tN = Mathf.Clamp01(sfx.NormalizedStart);
                    tN = Mathf.Min(tN, endNorm);
                    float trigger = seg.StartTime + segDuration * tN;
                    float length = (sfx.Clip != null) ? Mathf.Max(0.01f, sfx.Clip.length) : 0f;
                    maxEndTime = Mathf.Max(maxEndTime, trigger + length);
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

        // UI布局完成后确保所有动画结束时间竖线都存在并更新位置
        EditorApplication.delayCall += () => RecreateAllAnimationEndLines();
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
        // 默认高度（lane 轨道会在创建完 clip 后二次调整）
        trackElement.style.height = TRACK_ITEM_HEIGHT;
        trackElement.style.minHeight = TRACK_ITEM_HEIGHT;
        // 关键：TrackContainer 在 UXML 里是 column + flex-grow，子元素默认允许 shrink，会把动态高度压扁，造成“lane 挤在一起/高度不对”。
        trackElement.style.flexShrink = 0;
        trackElement.style.flexGrow = 0;
        trackElement.style.overflow = Overflow.Visible;
        trackElement.userData = trackData;
        trackData.Index = index;

        float trackWidth = GetContentWidth();
        trackElement.style.width = trackWidth;
        trackElement.style.minWidth = trackWidth;

        SetupTrackInteractions(trackElement);

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

                // 为AnimationClip创建结束时间黄色竖线
                if (clipItem is AnimationClipItem animClipItem)
                {
                    EnsureAnimationEndLine(animClipItem);
                }
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
        else if (trackData is ActiveTrack activeTrack)
        {
            foreach (var clipItem in activeTrack.ClipList)
            {
                var clipElement = SetClipItem(trackElement, clipItem);
                clipElements.Add(clipElement);
            }
        }

        // Lane 轨道化：Effect/Sound/HitBox 固定 lane（不自动重排）
        ApplyLaneLayoutIfNeeded(trackElement, trackData, clipElements);

        // 重叠高亮：AnimationTrack + lane 轨道都需要（lane 允许重叠）
        if (trackData is AnimationTrack || IsLaneTrack(trackData))
        {
            HighlightOverlappingClips(trackElement, clipElements);
        }

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
        float clipWidth = Mathf.Max(clipItem.Duration * pixelsPerSecond, MIN_CLIP_WIDTH_PX); // 最小显示宽度
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
    private readonly struct ClipRange
    {
        public readonly float Start;
        public readonly float End;

        public ClipRange(float start, float end)
        {
            this.Start = start;
            this.End = end;
        }
    }

    /// <summary>
    /// 获取一个 Clip 的“当前区间”（秒）。
    /// 说明：拖拽过程中布局可能尚未重算，因此优先使用 style.left（如果有），否则回退到 layout.x。
    /// </summary>
    private ClipRange GetClipRangeSeconds(VisualElement clipElement, IClipItem clipItem, bool isDraggingClip, float draggingLeftPx, float pixelsPerSecond)
    {
        float startSeconds;

        if (isDraggingClip)
        {
            startSeconds = Mathf.Max(0f, draggingLeftPx / pixelsPerSecond);
        }
        else
        {
            float leftPx = GetElementLeftPx(clipElement);
            startSeconds = Mathf.Max(0f, leftPx / pixelsPerSecond);
        }

        float endSeconds = startSeconds + Mathf.Max(0f, clipItem.Duration);
        return new ClipRange(startSeconds, endSeconds);
    }

    private static bool TryGetOverlap(in ClipRange a, in ClipRange b, out float overlapStart, out float overlapEnd, out float overlapDuration)
    {
        overlapStart = Mathf.Max(a.Start, b.Start);
        overlapEnd = Mathf.Min(a.End, b.End);
        overlapDuration = overlapEnd - overlapStart;
        return overlapDuration > 0f;
    }

    private static float CalcOverlapRatio(float overlapDuration, in ClipRange a, in ClipRange b)
    {
        float total = Mathf.Max(a.End, b.End) - Mathf.Min(a.Start, b.Start);
        if (total <= 0f)
        {
            return 0f;
        }
        return Mathf.Clamp01(overlapDuration / total);
    }

    private VisualElement CreateOverlapHighlight(float overlapStartSeconds, float overlapDurationSeconds, bool isDraggingRelated, float overlapRatio, float highlightTopPx, float highlightHeightPx)
    {
        // 创建高亮覆盖层，只覆盖重叠部分
        var highlightElement = new VisualElement();
        highlightElement.AddToClassList("overlap-highlight");
        highlightElement.style.position = Position.Absolute;

        float highlightX = overlapStartSeconds * pixelsPerSecond;
        float highlightWidth = overlapDurationSeconds * pixelsPerSecond;
        float highlightY = highlightTopPx;
        float highlightHeight = highlightHeightPx;

        highlightElement.style.left = highlightX;
        highlightElement.style.top = highlightY;
        highlightElement.style.width = highlightWidth;
        highlightElement.style.height = highlightHeight;

        // 根据是否涉及被拖动 clip + 重叠程度调整颜色深度
        if (isDraggingRelated)
        {
            // 被拖动clip的重叠 - 使用更明显的颜色（红色系）
            highlightElement.style.backgroundColor = Color.Lerp(
                new Color(1f, 1f, 0f, 0.5f),  // 浅黄色（轻微重叠）
                new Color(1f, 0f, 0f, 0.8f),   // 红色（高度重叠）
                overlapRatio
            );
            highlightElement.style.borderTopWidth = 3;
            highlightElement.style.borderTopColor = new Color(1f, 0.2f, 0f, 1f);
            highlightElement.style.borderBottomWidth = 3;
            highlightElement.style.borderBottomColor = new Color(1f, 0.2f, 0f, 1f);
        }
        else
        {
            // 普通重叠 - 橙色系
            highlightElement.style.backgroundColor = Color.Lerp(
                new Color(1f, 1f, 0f, 0.3f),
                new Color(1f, 0.4f, 0f, 0.7f),
                overlapRatio
            );
            highlightElement.style.borderTopWidth = 2;
            highlightElement.style.borderTopColor = new Color(1f, 0.6f, 0f, 0.9f);
            highlightElement.style.borderBottomWidth = 2;
            highlightElement.style.borderBottomColor = new Color(1f, 0.6f, 0f, 0.9f);
        }

        // 不接收鼠标事件，让下面的clip能正常交互
        highlightElement.pickingMode = PickingMode.Ignore;
        return highlightElement;
    }

    private VisualElement CreateOverlapHighlightForLane(float overlapStartSeconds, float overlapDurationSeconds, int laneIndex, bool isDraggingRelated, float overlapRatio)
    {
        return CreateOverlapHighlight(
            overlapStartSeconds,
            overlapDurationSeconds,
            isDraggingRelated,
            overlapRatio,
            highlightTopPx: GetLaneTopPx(laneIndex),
            highlightHeightPx: CLIP_ITEM_HEIGHT
        );
    }

    private void HighlightOverlappingClips(VisualElement trackElement, List<VisualElement> clipElements)
    {
        // 清除之前的所有高亮覆盖层
        var existingHighlights = trackElement.Query<VisualElement>(className: "overlap-highlight").ToList();
        foreach (var highlight in existingHighlights)
        {
            trackElement.Remove(highlight);
        }

        bool isLaneTrack = trackElement.userData is ITrackItem t && IsLaneTrack(t);
        float defaultTopPx = GetClipTopOffset();
        float defaultHeightPx = CLIP_ITEM_HEIGHT;

        // 检测所有clip之间的重叠
        for (int i = 0; i < clipElements.Count; i++)
        {
            var clip1 = clipElements[i];
            var clipItem1 = clip1.userData as IClipItem;
            if (clipItem1 == null) continue;

            var r1 = GetClipRangeSeconds(clip1, clipItem1, isDraggingClip: false, draggingLeftPx: 0f, pixelsPerSecond);

            for (int j = i + 1; j < clipElements.Count; j++)
            {
                var clip2 = clipElements[j];
                var clipItem2 = clip2.userData as IClipItem;
                if (clipItem2 == null) continue;

                var r2 = GetClipRangeSeconds(clip2, clipItem2, isDraggingClip: false, draggingLeftPx: 0f, pixelsPerSecond);

                // 检查是否重叠
                if (TryGetOverlap(in r1, in r2, out float overlapStart, out float overlapEnd, out float overlapDuration))
                {
                    float overlapRatio = CalcOverlapRatio(overlapDuration, in r1, in r2);

                    if (!isLaneTrack)
                    {
                        trackElement.Add(CreateOverlapHighlight(overlapStart, overlapDuration, isDraggingRelated: false, overlapRatio, defaultTopPx, defaultHeightPx));
                        continue;
                    }

                    // Lane 轨道：只画在发生重叠的 clip 所在 lane 高度上（不涂满整条轨道）
                    if (!laneIndexByClip.TryGetValue(clipItem1, out int lane1))
                    {
                        lane1 = 0;
                    }
                    if (!laneIndexByClip.TryGetValue(clipItem2, out int lane2))
                    {
                        lane2 = 0;
                    }

                    trackElement.Add(CreateOverlapHighlightForLane(overlapStart, overlapDuration, lane1, isDraggingRelated: false, overlapRatio));
                    if (lane2 != lane1)
                    {
                        trackElement.Add(CreateOverlapHighlightForLane(overlapStart, overlapDuration, lane2, isDraggingRelated: false, overlapRatio));
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

        // 计算拖拽 clip 的当前像素位置（优先 style.left；如果未设置则回退到 layout.x）
        float draggingLeftPx = GetElementLeftPx(draggingClip);

        bool isLaneTrack = trackElement.userData is ITrackItem t && IsLaneTrack(t);
        float defaultTopPx = GetClipTopOffset();
        float defaultHeightPx = CLIP_ITEM_HEIGHT;

        // 检测所有clip之间的重叠（保持现有表现：包含“被拖动 clip 与其他 clip”的重叠，也包含“其他 clip 之间”的静态重叠）
        for (int i = 0; i < clipElements.Count; i++)
        {
            var clip1 = clipElements[i];
            var clipItem1 = clip1.userData as IClipItem;
            if (clipItem1 == null) continue;

            var r1 = GetClipRangeSeconds(clip1, clipItem1, isDraggingClip: clip1 == draggingClip, draggingLeftPx, pixelsPerSecond);

            for (int j = i + 1; j < clipElements.Count; j++)
            {
                var clip2 = clipElements[j];
                var clipItem2 = clip2.userData as IClipItem;
                if (clipItem2 == null) continue;

                var r2 = GetClipRangeSeconds(clip2, clipItem2, isDraggingClip: clip2 == draggingClip, draggingLeftPx, pixelsPerSecond);

                // 检查是否重叠
                if (TryGetOverlap(in r1, in r2, out float overlapStart, out float overlapEnd, out float overlapDuration))
                {
                    float overlapRatio = CalcOverlapRatio(overlapDuration, in r1, in r2);
                    bool isDraggingRelated = (clip1 == draggingClip || clip2 == draggingClip);

                    if (!isLaneTrack)
                    {
                        trackElement.Add(CreateOverlapHighlight(overlapStart, overlapDuration, isDraggingRelated, overlapRatio, defaultTopPx, defaultHeightPx));
                        continue;
                    }

                    // Lane 轨道：只画在发生重叠的 clip 所在 lane 高度上
                    int lane1 = clip1 == draggingClip
                        ? Mathf.Max(0, draggingLaneIndex)
                        : (laneIndexByClip.TryGetValue(clipItem1, out int l1) ? l1 : 0);
                    int lane2 = clip2 == draggingClip
                        ? Mathf.Max(0, draggingLaneIndex)
                        : (laneIndexByClip.TryGetValue(clipItem2, out int l2) ? l2 : 0);

                    trackElement.Add(CreateOverlapHighlightForLane(overlapStart, overlapDuration, lane1, isDraggingRelated, overlapRatio));
                    if (lane2 != lane1)
                    {
                        trackElement.Add(CreateOverlapHighlightForLane(overlapStart, overlapDuration, lane2, isDraggingRelated, overlapRatio));
                    }
                }
            }
        }
    }

    // 拖拽结束后的重叠处理：高亮刷新
    private void ResolveOverlapOnDragEnd(VisualElement trackElement, List<VisualElement> clipElements, VisualElement draggedClip)
    {
        var draggedClipItem = draggedClip.userData as IClipItem;
        if (draggedClipItem == null) return;
        
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

            // 离开时清理缩放光标样式
            clipElement.RemoveFromClassList(RESIZE_CURSOR_CLASS);
        });

        // 鼠标移动（非拖拽状态）：右侧显示缩放光标
        clipElement.RegisterCallback<MouseMoveEvent>(evt =>
        {
            if (clipElement == null)
            {
                return;
            }

            // 正在拖拽/缩放时不切换光标（避免闪烁）
            if (isDragging || isResizingClip)
            {
                return;
            }

            if (clipElement.userData is IClipItem item && IsResizableClip(item))
            {
                bool nearRight = IsMouseNearRightEdge(clipElement, evt.localMousePosition);
                clipElement.EnableInClassList(RESIZE_CURSOR_CLASS, nearRight);
            }
            else
            {
                clipElement.RemoveFromClassList(RESIZE_CURSOR_CLASS);
            }
        });

        // 鼠标按下 - 开始拖拽
        clipElement.RegisterCallback<MouseDownEvent>(evt =>
        {
            if (evt.button == 0) // 左键
            {
                // 优先处理“右侧拖拽缩放”（Active/Effect/HitBox）
                if (!isDragging && !isResizingClip && clipElement.userData is IClipItem resizeCandidate && IsResizableClip(resizeCandidate))
                {
                    if (IsMouseNearRightEdge(clipElement, evt.localMousePosition))
                    {
                        isResizingClip = true;
                        resizingClipElement = clipElement;
                        resizingClipItem = resizeCandidate;
                        resizeStartMouseX = evt.mousePosition.x;
                        resizeStartWidthPx = clipElement.resolvedStyle.width;
                        if (resizeStartWidthPx <= 0f)
                        {
                            resizeStartWidthPx = clipElement.layout.width;
                        }

                        // 记录左侧（时间轴像素），用于计算最大可用宽度
                        resizeClipLeftPx = GetElementLeftPx(clipElement);

                        // Active/HitBox 需要 AnimationEnd 约束（Effect 允许超过动画末尾）
                        resizingOwnerClipItem = (resizeCandidate is ActiveClipItem || resizeCandidate is HitBoxClipItem)
                            ? GetOwnerAnimationClipItem(resizeCandidate)
                            : null;
                        dragAnimationEndLineOwnerTrackElement = null;
                        dragAnimationEndLineChildTrackElement = null;
                        if (resizingOwnerClipItem != null)
                        {
                            var ownerEl = FindClipElement(resizingOwnerClipItem);
                            dragAnimationEndLineOwnerTrackElement = ownerEl?.parent;
                            dragAnimationEndLineChildTrackElement = clipElement.parent;
                        }

                        // 固定缩放光标（通过 USS）
                        clipElement.AddToClassList(RESIZE_CURSOR_CLASS);
                        clipElement.CaptureMouse();
                        evt.StopPropagation();
                        return;
                    }
                }

                // 选中Clip并更新显示（Selection 统一管理）
                if (clipElement.userData is IClipItem selectedItem)
                {
                    var trackElement = clipElement.parent;
                    ITrackItem trackItem = null;
                    if (trackElement != null && trackElement.userData is ITrackItem t)
                    {
                        trackItem = t;
                    }

                    SelectClip(selectedItem, trackItem);
                }

                isDragging = true;
                dragStartPosition = evt.mousePosition;

                // 计算鼠标点击位置相对于 clip 左侧的偏移量
                float currentLeft = clipElement.layout.x;
                dragOffset = evt.mousePosition.x - currentLeft;
                clipElement.AddToClassList("dragging"); // 添加拖拽样式类

                // 子 clip（Effect/Sound/HitBox/Active）：初始化 AnimationEnd 边界红线的纵向范围（横向位置在 MouseMove 里动态计算）
                if (clipElement.userData is IClipItem dragItem && dragItem is not AnimationClipItem)
                {
                    var owner = GetOwnerAnimationClipItem(dragItem);
                    var ownerEl = FindClipElement(owner);
                    dragAnimationEndLineOwnerTrackElement = ownerEl?.parent;
                    dragAnimationEndLineChildTrackElement = clipElement.parent;
                }
                else
                {
                    HideDragAnimationEndLine();
                }

                // AnimationClip：缓存其子 clip 元素，用于拖拽时实时同步位置
                if (clipElement.userData is AnimationClipItem animClipItem)
                {
                    BuildDraggingOwnerChildClipCache(animClipItem);
                }
                else
                {
                    ClearDraggingOwnerChildClipCache();
                }

                // Lane 轨道：记录 laneIndex，拖拽过程中保持垂直位置不跳动
                draggingLaneIndex = -1;
                var parentTrackElement = clipElement.parent;
                if (parentTrackElement != null && parentTrackElement.userData is ITrackItem trackData && IsLaneTrack(trackData))
                {
                    if (clipElement.userData is IClipItem c && laneIndexByClip.TryGetValue(c, out int laneIdx))
                    {
                        draggingLaneIndex = laneIdx;
                    }
                    else
                    {
                        // 兜底：根据当前 top 反推 lane（避免 map 未命中的情况下跳动）
                        float currentTopPx = clipElement.resolvedStyle.top;
                        draggingLaneIndex = Mathf.Max(0, Mathf.RoundToInt((currentTopPx - LANE_PADDING_Y) / LANE_ROW_HEIGHT));
                    }

                    clipElement.style.top = GetLaneTopPx(draggingLaneIndex);
                }

                clipElement.CaptureMouse(); // 捕获鼠标，确保能接收鼠标移动事件
                evt.StopPropagation(); // 阻止事件冒泡到轨道
            }
        });

        // 鼠标移动 - 拖拽/缩放过程
        clipElement.RegisterCallback<MouseMoveEvent>(evt =>
        {
            // 右侧拖拽缩放（Active/Effect/HitBox）
            if (isResizingClip && ReferenceEquals(resizingClipElement, clipElement) && clipElement.HasMouseCapture())
            {
                float deltaX = evt.mousePosition.x - resizeStartMouseX;
                float desiredWidthPx = Mathf.Max(0f, resizeStartWidthPx + deltaX);

                // 计算新 duration（秒），并保持 UI 最小宽度策略
                float newDurationSeconds = desiredWidthPx / pixelsPerSecond;
                if (desiredWidthPx <= MIN_CLIP_WIDTH_PX)
                {
                    desiredWidthPx = MIN_CLIP_WIDTH_PX;
                    newDurationSeconds = 0f;
                }

                // Active/HitBox：End 不能超过 AnimationEnd
                if (resizingOwnerClipItem != null && (resizingClipItem is ActiveClipItem || resizingClipItem is HitBoxClipItem))
                {
                    float ownerDurationSeconds = Mathf.Max(0f, resizingOwnerClipItem.Duration);
                    float endNorm = GetSegmentAnimationEndNorm(resizingOwnerClipItem.SegmentData);
                    float ownerEndPx = (resizingOwnerClipItem.StartTime + ownerDurationSeconds * endNorm) * pixelsPerSecond;

                    float maxWidthPx = Mathf.Max(0f, ownerEndPx - resizeClipLeftPx);
                    float clampedWidthPx = Mathf.Clamp(desiredWidthPx, MIN_CLIP_WIDTH_PX, Mathf.Max(MIN_CLIP_WIDTH_PX, maxWidthPx));
                    if (!Mathf.Approximately(clampedWidthPx, desiredWidthPx))
                    {
                        // 到达边界还在拖：显示红线（位置在 AnimationEnd 帧上）
                        ShowDragAnimationEndLine(ownerEndPx);
                    }
                    else
                    {
                        HideDragAnimationEndLine();
                    }

                    desiredWidthPx = clampedWidthPx;
                    newDurationSeconds = (desiredWidthPx <= MIN_CLIP_WIDTH_PX) ? 0f : (desiredWidthPx / pixelsPerSecond);
                }
                else
                {
                    HideDragAnimationEndLine();
                }

                clipElement.style.width = desiredWidthPx;

                // 实时更新右侧面板显示（仅当当前选中就是该 clip）
                if (selectedClip != null && ReferenceEquals(selectedClip, resizingClipItem))
                {
                    if (clipLengthField != null)
                    {
                        clipLengthField.SetValueWithoutNotify(newDurationSeconds);
                    }
                    if (frameField != null)
                    {
                        frameField.SetValueWithoutNotify(Mathf.RoundToInt(newDurationSeconds * 60f));
                    }
                }

                evt.StopPropagation();
                return;
            }

            if (isDragging && clipElement.HasMouseCapture())
            {
                float desiredLeft = evt.mousePosition.x - dragOffset;
                desiredLeft = Mathf.Max(0f, desiredLeft);
                float newLeft = desiredLeft;

                // 子 clip（Effect/Sound/HitBox）：StartTime 限制在所属 AnimationClip 的 0~1 区间内
                if (clipElement.userData is IClipItem draggingItem && draggingItem is not AnimationClipItem)
                {
                    var owner = GetOwnerAnimationClipItem(draggingItem);
                    if (owner != null)
                    {
                        float ownerStartPx = owner.StartTime * pixelsPerSecond;
                        float ownerDurationSeconds = Mathf.Max(0f, owner.Duration);
                        float ownerEndPx = (owner.StartTime + ownerDurationSeconds) * pixelsPerSecond;

                        // 子 clip：统一限制在 AnimationEnd 内（不能超过该阈值）
                        float endNorm = GetSegmentAnimationEndNorm(owner.SegmentData);
                        ownerEndPx = (owner.StartTime + ownerDurationSeconds * endNorm) * pixelsPerSecond;

                    // HitBox / Active：End 也必须落在 0~1 内，因此用 duration 反推最大可用 start
                    if (draggingItem is HitBoxClipItem || draggingItem is ActiveClipItem)
                        {
                            float durationPx = Mathf.Max(0f, draggingItem.Duration) * pixelsPerSecond;
                            float ownerLenPx = Mathf.Max(0f, ownerEndPx - ownerStartPx);
                            float maxStartPx = durationPx <= ownerLenPx ? (ownerEndPx - durationPx) : ownerStartPx;
                            newLeft = Mathf.Clamp(newLeft, ownerStartPx, maxStartPx);

                            // 超过 AnimationEnd 还在拖动：显示边界红线（位置在 AnimationEnd 帧上，而非 maxStart）
                            if (desiredLeft > maxStartPx + 0.01f)
                            {
                                ShowDragAnimationEndLine(ownerEndPx);
                            }
                            else
                            {
                                HideDragAnimationEndLine();
                            }
                        }
                        else
                        {
                            newLeft = Mathf.Clamp(newLeft, ownerStartPx, ownerEndPx);
                            // 超过 AnimationEnd 还在拖动：显示边界红线（位置在 AnimationEnd 帧上）
                            if (desiredLeft > ownerEndPx + 0.01f)
                            {
                                ShowDragAnimationEndLine(ownerEndPx);
                            }
                            else
                            {
                                HideDragAnimationEndLine();
                            }
                        }
                    }
                }
                clipElement.style.left = newLeft;

                // AnimationClip：拖拽时实时同步更新其子 clip 的 UI 位置（保持归一化语义）
                if (clipElement.userData is AnimationClipItem)
                {
                    float ownerStartTime = newLeft / pixelsPerSecond;
                    ownerStartTime = Mathf.Max(0f, ownerStartTime);
                    UpdateDraggingOwnerChildClipPositions(ownerStartTime);
                }

                var trackElement = clipElement.parent;
                if (trackElement != null)
                {
                    // Lane 轨道：拖拽过程中固定 lane，不重排（避免上下跳动）；允许重叠并高亮提示
                    if (trackElement.userData is ITrackItem trackData && IsLaneTrack(trackData))
                    {
                        if (draggingLaneIndex >= 0)
                        {
                            clipElement.style.top = GetLaneTopPx(draggingLaneIndex);
                        }

                        var clipElements = trackElement.Query<VisualElement>().Where(x => x is Label && x.userData is IClipItem).ToList();
                        HighlightDraggingClipOverlap(trackElement, clipElements, clipElement);
                    }
                    else
                    {
                        // AnimationTrack：保持原来的拖拽重叠高亮提示
                        var clipElements = trackElement.Query<VisualElement>().Where(x => x is Label && x.userData is IClipItem).ToList();
                        HighlightDraggingClipOverlap(trackElement, clipElements, clipElement);
                    }
                }

                evt.StopPropagation();
            }
        });

        // 鼠标释放 - 结束拖拽
        clipElement.RegisterCallback<MouseUpEvent>(evt =>
        {
            if (isResizingClip && ReferenceEquals(resizingClipElement, clipElement))
            {
                clipElement.ReleaseMouse();
                HideDragAnimationEndLine();

                // 计算最终 duration（秒）
                float widthPx = GetElementWidthPx(clipElement);
                float newDurationSeconds = (widthPx <= MIN_CLIP_WIDTH_PX) ? 0f : Mathf.Max(0f, widthPx / pixelsPerSecond);

                // 写回数据
                if (resizingClipItem is EffectClipItem effectClip && effectClip.EffectData != null)
                {
                    effectClip.Duration = newDurationSeconds;
                    effectClip.Frame = Mathf.RoundToInt(newDurationSeconds * 60f);
                    effectClip.EffectData.Length = newDurationSeconds;
                }
                else if (resizingClipItem is ActiveClipItem activeClip && activeClip.ActiveData != null && config != null)
                {
                    activeClip.Duration = newDurationSeconds;
                    activeClip.Frame = Mathf.RoundToInt(newDurationSeconds * 60f);
                    // 同步回配置的归一化 end
                    UpdateActiveClipTimes(activeClip, activeClip.StartTime, newDurationSeconds);
                }
                else if (resizingClipItem is HitBoxClipItem hitBoxClip && hitBoxClip.HitBoxData != null && config != null)
                {
                    hitBoxClip.Duration = newDurationSeconds;
                    hitBoxClip.Frame = Mathf.RoundToInt(newDurationSeconds * 60f);
                    // 同步回配置的归一化 end（同时会 clamp 到 AnimationEnd）
                    UpdateHitBoxClipTimes(hitBoxClip, hitBoxClip.StartTime, newDurationSeconds);
                }

                MarkAssetDirty();
                RefreshTrackContent();
                RefreshSelectionHighlight();

                // 清理缩放态
                isResizingClip = false;
                resizingClipElement = null;
                resizingClipItem = null;
                resizingOwnerClipItem = null;
                clipElement.RemoveFromClassList(RESIZE_CURSOR_CLASS);

                UpdateTimelineContentWidth();
                DrawTimelineRulerMarks();

                evt.StopPropagation();
                return;
            }

            if (isDragging)
            {
                clipElement.ReleaseMouse();
                clipElement.RemoveFromClassList("dragging");
                HideDragAnimationEndLine();

                // 拖拽结束后，同步更新数据
                var draggedClipItem = clipElement.userData as IClipItem;
                if (draggedClipItem != null)
                {
                    isDragging = false;
                    float currentLeftPx = GetElementLeftPx(clipElement);
                    float currentStartTime = currentLeftPx / pixelsPerSecond;
                    currentStartTime = Mathf.Max(0f, currentStartTime);

                    // 子 clip（Effect/Sound/HitBox）：写回数据时同样做区间限制（只限制 StartTime，不限制 EndTime）
                    if (draggedClipItem is not AnimationClipItem)
                    {
                        var owner = GetOwnerAnimationClipItem(draggedClipItem);
                        if (owner != null)
                        {
                            float ownerStart = owner.StartTime;
                            float ownerDurationSeconds = Mathf.Max(0f, owner.Duration);
                            // 子 clip：统一限制在 AnimationEnd 内（不能超过该阈值）
                            float endNorm = GetSegmentAnimationEndNorm(owner.SegmentData);
                            float ownerEnd = owner.StartTime + ownerDurationSeconds * endNorm;

                            if (draggedClipItem is HitBoxClipItem hitBox)
                            {
                                // HitBox：Start/End 都必须在 owner 的 0~1 内；必要时压缩 duration
                                float ownerLen = Mathf.Max(0f, ownerEnd - ownerStart);
                                float duration = Mathf.Max(0f, hitBox.Duration);
                                if (duration > ownerLen)
                                {
                                    duration = ownerLen;
                                    hitBox.Duration = duration;
                                    hitBox.Frame = Mathf.RoundToInt(duration * 60f);
                                    clipElement.style.width = Mathf.Max(duration * pixelsPerSecond, MIN_CLIP_WIDTH_PX);
                                }

                                float maxStart = ownerEnd - duration;
                                currentStartTime = Mathf.Clamp(currentStartTime, ownerStart, maxStart);
                                clipElement.style.left = currentStartTime * pixelsPerSecond;
                            }
                            else if (draggedClipItem is ActiveClipItem activeClip)
                            {
                                // Active：Start/End 必须在 owner 的 0~1 内；必要时压缩 duration
                                float ownerLen = Mathf.Max(0f, ownerEnd - ownerStart);
                                float duration = Mathf.Max(0f, activeClip.Duration);
                                if (duration > ownerLen)
                                {
                                    duration = ownerLen;
                                    activeClip.Duration = duration;
                                    activeClip.Frame = Mathf.RoundToInt(duration * 60f);
                                    clipElement.style.width = Mathf.Max(duration * pixelsPerSecond, MIN_CLIP_WIDTH_PX);
                                }

                                float maxStart = ownerEnd - duration;
                                currentStartTime = Mathf.Clamp(currentStartTime, ownerStart, maxStart);
                                clipElement.style.left = currentStartTime * pixelsPerSecond;
                            }
                            else
                            {
                                currentStartTime = Mathf.Clamp(currentStartTime, ownerStart, ownerEnd);
                                clipElement.style.left = currentStartTime * pixelsPerSecond;
                            }
                        }
                    }

                    draggedClipItem.StartTime = currentStartTime;

                    // 写回Config/数据结构
                    SyncDraggedClipToConfig(draggedClipItem);

                    UpdateClipProperties(draggedClipItem);

                    // AnimationClip：拖拽结束后写回子 clip 的绝对时间，并更新其 UI 位置
                    if (draggedClipItem is AnimationClipItem animClipItem)
                    {
                        SyncOwnerChildClipsToOwner(animClipItem);
                        UpdateDraggingOwnerChildClipPositions(animClipItem.StartTime);
                    }
                }

                var trackElement = clipElement.parent;
                if (trackElement != null)
                {
                    var clipElements = trackElement.Query<VisualElement>().Where(x => x is Label && x.userData is IClipItem).ToList();

                    // 拖拽结束后刷新布局：
                    // - AnimationTrack：保留重叠提示逻辑
                    // - Effect/Sound/HitBox：lane 轨道化重新分行（避免遮挡），不再做重叠高亮
                    if (trackElement.userData is ITrackItem trackData && trackData is AnimationTrack)
                    {
                        ResolveOverlapOnDragEnd(trackElement, clipElements, clipElement);
                    }
                    else if (trackElement.userData is ITrackItem laneTrack && IsLaneTrack(laneTrack))
                    {
                        ApplyLaneLayoutIfNeeded(trackElement, laneTrack, clipElements);
                        HighlightOverlappingClips(trackElement, clipElements);
                    }
                }

                // 清理拖拽态
                draggingLaneIndex = -1;
                ClearDraggingOwnerChildClipCache();

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

        // 支持从 Project 直接拖入 AnimationClip 到 AnimationTrack：自动创建对应的 Segment/Clip
        // 只在 AnimationTrack 上启用，避免误把资源丢到子轨道（Effect/Sound/HitBox）导致不可预期的数据结构。
        if (trackElement.userData is AnimationTrack)
        {
            trackElement.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (config == null)
                {
                    return;
                }

                bool hasAnimationClip = false;
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is AnimationClip)
                    {
                        hasAnimationClip = true;
                        break;
                    }
                }

                DragAndDrop.visualMode = hasAnimationClip ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                if (hasAnimationClip)
                {
                    evt.StopPropagation();
                }
            });

            trackElement.RegisterCallback<DragPerformEvent>(evt =>
            {
                if (config == null)
                {
                    return;
                }

                List<AnimationClip> clips = null;
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is AnimationClip clip)
                    {
                        clips ??= new List<AnimationClip>();
                        clips.Add(clip);
                    }
                }

                if (clips == null || clips.Count == 0)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    return;
                }

                DragAndDrop.AcceptDrag();

                // 计算落点时间（秒，绝对时间）
                Vector2 localPos = trackElement.WorldToLocal(evt.mousePosition);
                float x = Mathf.Max(0f, localPos.x);
                float startTime = x / pixelsPerSecond;

                AddAnimationClipsToAnimationTrack(clips, startTime);

                evt.StopPropagation();
            });
        }
        // 支持从 Project/Hierarchy 直接拖入 GameObject 到 EffectTrack：自动创建 VisualEffectData + EffectClipItem
        else if (trackElement.userData is EffectTrack effectTrack)
        {
            trackElement.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (config == null)
                {
                    return;
                }

                var owner = GetOwnerAnimationClipItem((ITrackItem)effectTrack);
                if (owner?.SegmentData == null)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    return;
                }

                bool hasPrefab = false;
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is GameObject go && UnityEditor.EditorUtility.IsPersistent(go))
                    {
                        hasPrefab = true;
                        break;
                    }
                }

                DragAndDrop.visualMode = hasPrefab ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                if (hasPrefab)
                {
                    evt.StopPropagation();
                }
            });

            trackElement.RegisterCallback<DragPerformEvent>(evt =>
            {
                if (config == null)
                {
                    return;
                }

                var owner = GetOwnerAnimationClipItem((ITrackItem)effectTrack);
                if (owner?.SegmentData == null)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    return;
                }

                List<GameObject> prefabs = null;
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is GameObject go && UnityEditor.EditorUtility.IsPersistent(go))
                    {
                        prefabs ??= new List<GameObject>();
                        prefabs.Add(go);
                    }
                }

                if (prefabs == null || prefabs.Count == 0)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    return;
                }

                DragAndDrop.AcceptDrag();

                Vector2 localPos = trackElement.WorldToLocal(evt.mousePosition);
                float x = Mathf.Max(0f, localPos.x);
                float dropTime = x / pixelsPerSecond;

                AddDraggedVisualEffects(effectTrack, owner, prefabs, dropTime);

                evt.StopPropagation();
            });
        }
        // 支持从 Project 直接拖入 AudioClip 到 SoundTrack：自动创建 SoundEffectData + SoundClipItem
        else if (trackElement.userData is SoundTrack soundTrack)
        {
            trackElement.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (config == null)
                {
                    return;
                }

                var owner = GetOwnerAnimationClipItem((ITrackItem)soundTrack);
                if (owner?.SegmentData == null)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    return;
                }

                bool hasAudio = false;
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is AudioClip)
                    {
                        hasAudio = true;
                        break;
                    }
                }

                DragAndDrop.visualMode = hasAudio ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                if (hasAudio)
                {
                    evt.StopPropagation();
                }
            });

            trackElement.RegisterCallback<DragPerformEvent>(evt =>
            {
                if (config == null)
                {
                    return;
                }

                var owner = GetOwnerAnimationClipItem((ITrackItem)soundTrack);
                if (owner?.SegmentData == null)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    return;
                }

                List<AudioClip> clips = null;
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is AudioClip clip)
                    {
                        clips ??= new List<AudioClip>();
                        clips.Add(clip);
                    }
                }

                if (clips == null || clips.Count == 0)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    return;
                }

                DragAndDrop.AcceptDrag();

                Vector2 localPos = trackElement.WorldToLocal(evt.mousePosition);
                float x = Mathf.Max(0f, localPos.x);
                float dropTime = x / pixelsPerSecond;

                AddDraggedSoundEffects(soundTrack, owner, clips, dropTime);

                evt.StopPropagation();
            });
        }
        // 支持从 Hierarchy 直接拖入“角色身上已有的子物体”到 ActiveTrack：生成 AttachedActiveData + ActiveClipItem
        else if (trackElement.userData is ActiveTrack activeTrack)
        {
            trackElement.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (config == null)
                {
                    return;
                }

                var owner = GetOwnerAnimationClipItem((ITrackItem)activeTrack);
                if (owner?.SegmentData == null)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    return;
                }

                var rootGo = selectObj != null ? selectObj.value as GameObject : null;
                var rootTf = rootGo != null ? rootGo.transform : null;
                if (rootTf == null)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    return;
                }

                bool hasChild = false;
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is GameObject go && go.transform != null && go.transform.IsChildOf(rootTf))
                    {
                        hasChild = true;
                        break;
                    }
                }

                DragAndDrop.visualMode = hasChild ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                if (hasChild)
                {
                    evt.StopPropagation();
                }
            });

            trackElement.RegisterCallback<DragPerformEvent>(evt =>
            {
                if (config == null)
                {
                    return;
                }

                var owner = GetOwnerAnimationClipItem((ITrackItem)activeTrack);
                if (owner?.SegmentData == null)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    return;
                }

                var rootGo = selectObj != null ? selectObj.value as GameObject : null;
                var rootTf = rootGo != null ? rootGo.transform : null;
                if (rootTf == null)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    return;
                }

                List<GameObject> children = null;
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is GameObject go && go.transform != null && go.transform.IsChildOf(rootTf))
                    {
                        children ??= new List<GameObject>();
                        children.Add(go);
                    }
                }

                if (children == null || children.Count == 0)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    return;
                }

                DragAndDrop.AcceptDrag();

                Vector2 localPos = trackElement.WorldToLocal(evt.mousePosition);
                float x = Mathf.Max(0f, localPos.x);
                float dropTime = x / pixelsPerSecond;

                AddDraggedAttachedActives(activeTrack, owner, rootTf, children, dropTime);

                evt.StopPropagation();
            });
        }

        trackElement.pickingMode = PickingMode.Position;
    }

    #region 动画结束时间黄色竖线

    /// <summary>
    /// 创建或更新动画结束时间的黄色竖线
    /// </summary>
    private void EnsureAnimationEndLine(AnimationClipItem clipItem)
    {
        if (timelineContent == null || clipItem?.SegmentData == null)
        {
            return;
        }

        // 如果不存在，创建新的黄色竖线元素
        if (!animationEndLineElements.TryGetValue(clipItem, out var lineElement))
        {
            lineElement = new VisualElement();
            lineElement.name = $"AnimationEndLine_{clipItem.Index}";
            lineElement.style.position = Position.Absolute;
            lineElement.style.top = 0f;
            lineElement.style.width = ANIMATION_END_LINE_WIDTH;
            lineElement.style.backgroundColor = new Color(1f, 1f, 0f, 1f); // 黄色高亮
            lineElement.pickingMode = PickingMode.Ignore; // 不接收鼠标事件

            timelineContent.Add(lineElement);
            animationEndLineElements[clipItem] = lineElement;
        }

        // 计算竖线位置和高度
        UpdateAnimationEndLinePosition(clipItem);
    }

    /// <summary>
    /// 更新动画结束时间竖线的位置
    /// </summary>
    private void UpdateAnimationEndLinePosition(AnimationClipItem clipItem)
    {
        if (clipItem?.SegmentData == null || !animationEndLineElements.TryGetValue(clipItem, out var lineElement))
        {
            return;
        }

        // 计算动画真正的结束时间：StartTime + AnimationEnd * Duration
        float animationEnd = clipItem.SegmentData.TimeWindow?.AnimationEnd ?? 1f;
        float duration = Mathf.Max(0f, clipItem.Duration);
        float endTime = clipItem.StartTime + animationEnd * duration;

        // 计算竖线位置（像素）
        float xPosition = endTime * pixelsPerSecond - ANIMATION_END_HALF_WIDTH;

        // 设置竖线位置
        lineElement.style.left = Mathf.Max(0f, xPosition);

        // 找到AnimationClip所在的轨道元素
        VisualElement trackElement = FindTrackElementForClipItem(clipItem);
        if (trackElement != null)
        {
            // 计算竖线在轨道内的位置和高度
            float trackTop = trackElement.worldBound.yMin - timelineContent.worldBound.yMin;
            float trackHeight = trackElement.layout.height;

            lineElement.style.top = Mathf.Max(0f, trackTop);
            lineElement.style.height = Mathf.Max(0f, trackHeight);
        }
        else
        {
            // 如果找不到轨道，使用默认位置
            lineElement.style.top = RULER_HEIGHT;
            lineElement.style.height = TRACK_ITEM_HEIGHT;
        }

        // 确保在最上层，但低于播放轴
        lineElement.BringToFront();
    }

    /// <summary>
    /// 找到包含指定ClipItem的轨道元素
    /// </summary>
    private VisualElement FindTrackElementForClipItem(IClipItem clipItem)
    {
        if (trackContainer == null || clipItem == null)
        {
            return null;
        }

        // 遍历所有轨道元素
        var trackElements = trackContainer.Query<VisualElement>(name: "track").ToList();
        foreach (var trackElement in trackElements)
        {
            // 检查轨道是否包含该clipItem
            var clipElements = trackElement.Query<VisualElement>(className: "timeline-clip").ToList();
            foreach (var clipElement in clipElements)
            {
                if (clipElement.userData is IClipItem item && ReferenceEquals(item, clipItem))
                {
                    return trackElement;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 移除指定动画片段的结束时间竖线
    /// </summary>
    private void RemoveAnimationEndLine(AnimationClipItem clipItem)
    {
        if (clipItem == null || !animationEndLineElements.TryGetValue(clipItem, out var lineElement))
        {
            return;
        }

        if (lineElement.parent != null)
        {
            lineElement.RemoveFromHierarchy();
        }

        animationEndLineElements.Remove(clipItem);
    }

    /// <summary>
    /// 更新所有动画结束时间竖线的位置
    /// </summary>
    private void UpdateAllAnimationEndLines()
    {
        foreach (var kvp in animationEndLineElements)
        {
            UpdateAnimationEndLinePosition(kvp.Key);
        }
    }

    /// <summary>
    /// 清理所有动画结束时间竖线
    /// </summary>
    private void ClearAllAnimationEndLines()
    {
        foreach (var lineElement in animationEndLineElements.Values)
        {
            if (lineElement.parent != null)
            {
                lineElement.RemoveFromHierarchy();
            }
        }
        animationEndLineElements.Clear();
    }

    /// <summary>
    /// 重新创建所有动画结束时间竖线
    /// </summary>
    private void RecreateAllAnimationEndLines()
    {
        // 遍历所有AnimationClip（不受视图模式限制）
        foreach (var animClipItem in allAnimationClipItems)
        {
            if (animClipItem != null)
            {
                EnsureAnimationEndLine(animClipItem);
            }
        }
    }

    #endregion

    private static string GetRelativePath(Transform root, Transform target)
    {
        if (root == null || target == null)
        {
            return string.Empty;
        }

        if (ReferenceEquals(root, target))
        {
            return string.Empty;
        }

        // target 必须在 root 下，否则无法生成稳定路径
        if (!target.IsChildOf(root))
        {
            return string.Empty;
        }

        var stack = new System.Collections.Generic.Stack<string>();
        Transform t = target;
        while (t != null && !ReferenceEquals(t, root))
        {
            stack.Push(t.name);
            t = t.parent;
        }

        return string.Join("/", stack);
    }

    private void AddDraggedAttachedActives(ActiveTrack activeTrack, AnimationClipItem owner, Transform rootTransform, List<GameObject> targets, float dropTimeSeconds)
    {
        if (config == null || activeTrack == null || owner?.SegmentData == null || rootTransform == null || targets == null || targets.Count == 0)
        {
            return;
        }

        var segment = owner.SegmentData;
        float ownerDuration = Mathf.Max(0.0001f, owner.Duration);
        float ownerStart = owner.StartTime;

        ActiveClipItem lastClipItem = null;

        for (int i = 0; i < targets.Count; ++i)
        {
            var go = targets[i];
            if (go == null)
            {
                continue;
            }

            string path = GetRelativePath(rootTransform, go.transform);
            if (path == null)
            {
                path = string.Empty;
            }

            float normalized = Mathf.Clamp01((dropTimeSeconds - ownerStart) / ownerDuration);

            // 默认区间长度：
            // - 若目标带 legacy Animation，则用 clip.length 推导
            // - 否则回退 0.2
            float normalizedEnd;
            var anim = go.GetComponentInChildren<Animation>(true);
            var clip = anim != null ? GetFirstLegacyAnimationClip(anim) : null;
            if (clip != null)
            {
                normalizedEnd = Mathf.Clamp01(normalized + (clip.length / ownerDuration));
            }
            else
            {
                normalizedEnd = Mathf.Min(1f, normalized + 0.2f);
            }
            float absStart = ownerStart + normalized * ownerDuration;

            var data = new AttachedActiveData
            {
                Name = go.name,
                RelativePath = path,
                NormalizedStart = normalized,
                NormalizedEnd = Mathf.Max(normalized, normalizedEnd),
            };

            segment.AttachedActives.Add(data);

            float dur = (data.NormalizedEnd - data.NormalizedStart) * ownerDuration;
            var clipItem = new ActiveClipItem
            {
                ActiveData = data,
                Name = string.IsNullOrEmpty(data.Name) ? "Active" : data.Name,
                StartTime = absStart,
                Duration = Mathf.Max(0f, dur),
                Frame = Mathf.RoundToInt(Mathf.Max(0f, dur) * 60f),
            };

            activeTrack.ClipList.Add(clipItem);
            lastClipItem = clipItem;
        }

        if (lastClipItem == null)
        {
            return;
        }

        SelectClip(lastClipItem, activeTrack);
        MarkAssetDirty();
        ApplyViewModeAndRefresh();
    }

    private AnimationClipItem GetOwnerAnimationClipItem(ITrackItem track)
    {
        if (track == null)
        {
            return null;
        }

        // AnimationTrack 是全局轨道，不属于某个 Segment
        if (track is AnimationTrack)
        {
            return null;
        }

        foreach (var kvp in animationClipTrackMap)
        {
            var owner = kvp.Key;
            var list = kvp.Value;
            if (owner == null || list == null)
            {
                continue;
            }

            for (int i = 0; i < list.Count; ++i)
            {
                if (ReferenceEquals(list[i], track))
                {
                    return owner;
                }
            }
        }

        return null;
    }

    private void AddDraggedVisualEffects(EffectTrack effectTrack, AnimationClipItem owner, List<GameObject> prefabs, float dropTimeSeconds)
    {
        if (config == null || effectTrack == null || owner?.SegmentData == null || prefabs == null || prefabs.Count == 0)
        {
            return;
        }

        var segment = owner.SegmentData;
        float ownerDuration = Mathf.Max(0.0001f, owner.Duration);
        float ownerStart = owner.StartTime;
        float endNorm = GetSegmentAnimationEndNorm(segment);

        EffectClipItem lastClipItem = null;

        for (int i = 0; i < prefabs.Count; ++i)
        {
            var prefab = prefabs[i];
            if (prefab == null)
            {
                continue;
            }

            float normalized = Mathf.Clamp((dropTimeSeconds - ownerStart) / ownerDuration, 0f, endNorm);
            float absStart = ownerStart + normalized * ownerDuration;

            var vfx = new VisualEffectData
            {
                Name = prefab.name,
                Prefab = prefab,
                NormalizedStart = normalized,
                Offset = Vector3.zero,
                FollowTarget = false,
                // Length：沿用数据默认值（由业务自行调整）
            };

            // Best practice：若 Prefab 自带 legacy Animation，则用 clip.length 作为 Length 真源（写回 config）。
            var anim = prefab.GetComponentInChildren<Animation>(true);
            var clip = anim != null ? GetFirstLegacyAnimationClip(anim) : null;
            if (clip != null)
            {
                vfx.Length = Mathf.Max(0.01f, clip.length);
            }

            segment.VisualEffects.Add(vfx);

            float duration = Mathf.Max(0f, vfx.Length);
            var clipItem = new EffectClipItem
            {
                EffectData = vfx,
                Name = string.IsNullOrEmpty(vfx.Name) ? "VFX" : vfx.Name,
                StartTime = absStart,
                Duration = duration,
                Frame = Mathf.RoundToInt(duration * 60f),
            };

            effectTrack.ClipList.Add(clipItem);
            lastClipItem = clipItem;
        }

        if (lastClipItem == null)
        {
            return;
        }

        SelectClip(lastClipItem, effectTrack);
        MarkAssetDirty();
        ApplyViewModeAndRefresh();
    }

    private void AddDraggedSoundEffects(SoundTrack soundTrack, AnimationClipItem owner, List<AudioClip> clips, float dropTimeSeconds)
    {
        if (config == null || soundTrack == null || owner?.SegmentData == null || clips == null || clips.Count == 0)
        {
            return;
        }

        var segment = owner.SegmentData;
        float ownerDuration = Mathf.Max(0.0001f, owner.Duration);
        float ownerStart = owner.StartTime;
        float endNorm = GetSegmentAnimationEndNorm(segment);

        SoundClipItem lastClipItem = null;

        for (int i = 0; i < clips.Count; ++i)
        {
            var clip = clips[i];
            if (clip == null)
            {
                continue;
            }

            float normalized = Mathf.Clamp((dropTimeSeconds - ownerStart) / ownerDuration, 0f, endNorm);
            float absStart = ownerStart + normalized * ownerDuration;

            var sfx = new SoundEffectData
            {
                Name = clip.name,
                Clip = clip,
                NormalizedStart = normalized,
                Volume = 1f,
            };

            segment.SoundEffects.Add(sfx);

            float duration = Mathf.Max(0.01f, clip.length);
            var clipItem = new SoundClipItem
            {
                SoundData = sfx,
                Name = string.IsNullOrEmpty(sfx.Name) ? "SFX" : sfx.Name,
                StartTime = absStart,
                Duration = duration,
                Frame = Mathf.RoundToInt(duration * 60f),
            };

            soundTrack.ClipList.Add(clipItem);
            lastClipItem = clipItem;
        }

        if (lastClipItem == null)
        {
            return;
        }

        SelectClip(lastClipItem, soundTrack);
        MarkAssetDirty();
        ApplyViewModeAndRefresh();
    }

    /// <summary>
    /// AnimationClip 拖入 AnimationTrack 后：自动创建 SegmentData + AnimationClipItem 并刷新时间轴。
    /// - 多选拖入时，按落点时间依次铺开，减少重叠
    /// </summary>
    private void AddAnimationClipsToAnimationTrack(List<AnimationClip> clips, float startTime)
    {
        if (clips == null || clips.Count == 0)
        {
            return;
        }

        if (config == null)
        {
            return;
        }

        // 确保全局 AnimationTrack 存在（InitData 里会创建；这里做兜底）
        AnimationTrack animationTrack = null;
        if (globalTrackDataList.Count > 0)
        {
            animationTrack = globalTrackDataList[0] as AnimationTrack;
        }
        if (animationTrack == null)
        {
            animationTrack = new AnimationTrack { Name = nameof(TrackType.Animation) };
            globalTrackDataList.Insert(0, animationTrack);
        }

        float t = Mathf.Max(0f, startTime);
        AnimationClipItem lastCreated = null;

        foreach (var clip in clips)
        {
            if (clip == null)
            {
                continue;
            }

            var segment = new AttackSegmentData
            {
                Id = config.Segments.Count,
                Name = clip.name,
                StartTime = t,
                ClipLength = clip.length,
            };

            segment.AnimationClipTrans = new Animancer.ClipTransition
            {
                Clip = clip,
                Speed = 1f,
                FadeDuration = 0.25f,
            };

            // 3) 时间轴真实时长：Duration = ClipLength / Speed
            float speed = Mathf.Max(0.01f, segment.AnimationClipTrans.Speed);
            segment.Duration = segment.ClipLength / speed;

            config.Segments.Add(segment);

            // 4) 生成运行期 ClipItem（用于时间轴显示/选择）
            var clipItem = new AnimationClipItem
            {
                SegmentData = segment,
                Name = string.IsNullOrEmpty(segment.Name) ? clip.name : segment.Name,
                StartTime = segment.StartTime,
                Duration = segment.Duration,
                Frame = Mathf.RoundToInt(segment.Duration * 60f),
            };

            // 5) 插入到 AnimationTrack（保持显示顺序接近时间顺序，但不影响 config.Segments）
            int insertIndex = animationTrack.ClipList.Count;
            for (int i = 0; i < animationTrack.ClipList.Count; ++i)
            {
                if (animationTrack.ClipList[i].StartTime > clipItem.StartTime)
                {
                    insertIndex = i;
                    break;
                }
            }
            animationTrack.ClipList.Insert(insertIndex, clipItem);

            // 6) 同步编辑器侧的索引/映射
            allAnimationClipItems.Insert(Mathf.Min(insertIndex, allAnimationClipItems.Count), clipItem);
            animationClipTrackMap[clipItem] = new List<ITrackItem>();

            lastCreated = clipItem;

            // 多选拖入时默认依次铺开
            t += clipItem.Duration;
        }

        if (lastCreated == null)
        {
            return;
        }

        // 选中：让局部视图能正确聚焦到新片段
        selectedTrack = animationTrack;
        selectedClip = lastCreated;

        MarkAssetDirty();
        ApplyViewModeAndRefresh();

        // UI Toolkit 在部分拖拽回调里可能延迟重绘
        trackContainer?.MarkDirtyRepaint();
        root?.MarkDirtyRepaint();
        Repaint();
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
                else if (trackData is ActiveTrack activeTrack)
                {
                    clipList = activeTrack.ClipList.ConvertAll(x => (IClipItem)x);
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
                    }

                    // Lane 轨道化：刷新 top 与轨道高度
                    ApplyLaneLayoutIfNeeded(trackElement, trackData, clipElements);

                    // 重叠高亮：AnimationTrack + lane 轨道都需要
                    if (trackData is AnimationTrack || IsLaneTrack(trackData))
                    {
                        HighlightOverlappingClips(trackElement, clipElements);
                    }
                }
            }
        }
    }
    
    private static float GetClipTopOffset()
    {
        return Mathf.RoundToInt((TRACK_ITEM_HEIGHT - CLIP_ITEM_HEIGHT) * 0.5f);
    }
}


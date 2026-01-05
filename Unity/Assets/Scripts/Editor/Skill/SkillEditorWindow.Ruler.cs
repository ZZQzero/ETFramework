using System;
using ET;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public partial class SkillEditorWindow : EditorWindow
{
    #region 时间轴标尺

    private void InitTimelineRuler()
    {
        timelineRuler = root.Q<VisualElement>("Ruler");
        if (timelineRuler == null) return;
        
        timelineRuler.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        timelineRuler.style.borderBottomWidth = 1;
        timelineRuler.style.borderBottomColor = Color.gray;
        timelineRuler.style.overflow = Overflow.Visible; // 允许刻度超出显示

        // 监听容器大小变化，重新绘制刻度
        timelineRuler.RegisterCallback<GeometryChangedEvent>(evt => {
            DrawTimelineRulerMarks();
        });

        // 添加鼠标滚轮缩放支持
        if (timelineScrollView != null)
        {
            timelineScrollView.RegisterCallback<WheelEvent>(OnTimelineWheel);
        }
    }
    
    // 根据缩放级别获取数字标签显示间隔
    private int GetDisplayInterval(float pixelsPerSecond)
    {
        // 当缩放很小的时候（每秒像素数小于等于50），每5秒显示一个值
        // 当缩放很大的时候（每秒像素数大于50），每1秒显示一个值
        return pixelsPerSecond <= 50f ? 5 : 1;
    }

    private void DrawTimelineRulerMarks()
    {
        if (timelineRuler == null) return;

        // 清除之前的刻度
        var existingMarks = timelineRuler.Query<VisualElement>(className: "timeline-mark").ToList();
        foreach (var mark in existingMarks)
        {
            timelineRuler.Remove(mark);
        }
        
        // 时间轴标尺宽度
        float rulerWidth = GetContentWidth();
        timelineRuler.style.width = rulerWidth;
        timelineRuler.style.minWidth = rulerWidth;

        // 根据标尺宽度计算需要显示的最大秒数
        float maxTime = rulerWidth / pixelsPerSecond;
        int maxSeconds = Mathf.CeilToInt(maxTime);

        for (int second = 0; second <= maxSeconds; second++)
        {
            float xPos = second * pixelsPerSecond;
            if (xPos > rulerWidth)
            {
                break;
            }
            // 主刻度线
            var mainMark = new VisualElement();
            mainMark.AddToClassList("timeline-mark");
            mainMark.style.position = Position.Absolute;
            mainMark.style.left = xPos;
            mainMark.style.top = 0;
            mainMark.style.width = 1;
            mainMark.style.height = RULER_HEIGHT * 0.8f; // 刻度线高度
            mainMark.style.backgroundColor = Color.white;
            timelineRuler.Add(mainMark);

            // 刻度数字标签 - 根据缩放级别动态调整显示频率
            int displayInterval = GetDisplayInterval(pixelsPerSecond);
            if (second % displayInterval == 0) // 根据缩放级别显示数字
            {
                string labelText = second.ToString();
                var label = new Label(labelText);
                label.AddToClassList("timeline-mark");
                label.style.position = Position.Absolute;

                // 先放到预期位置；随后在布局完成后拿到真实宽度，再把右边界 clamp 到 rulerWidth 内，
                // 避免 label 的可见溢出把 ScrollView 的可滚动范围撑大，导致最右端出现“多滚出一截”的空白区域。
                float preferredLeft = xPos + 2f;
                label.style.left = preferredLeft;

                label.style.top = RULER_HEIGHT * 0.1f;
                label.style.fontSize = 10;
                label.style.color = Color.white;
                label.style.unityTextAlign = TextAnchor.UpperLeft;
                timelineRuler.Add(label);

                // 用真实宽度做右对齐修正（只执行一次）
                EventCallback<GeometryChangedEvent> onGeometryChanged = null;
                onGeometryChanged = _ =>
                {
                    float w = label.resolvedStyle.width;
                    // width 可能在极早期为 0，这里做一次兜底；如果为 0，保持 preferredLeft 不动。
                    if (w > 0f)
                    {
                        float clampedLeft = Mathf.Min(preferredLeft, Mathf.Max(0f, rulerWidth - w));
                        label.style.left = clampedLeft;
                    }

                    label.UnregisterCallback(onGeometryChanged);
                };
                label.RegisterCallback(onGeometryChanged);
            }

            // 小刻度线（每0.2秒）
            for (int sub = 1; sub < 5; sub++) // 每秒4个小刻度
            {
                float subX = xPos + sub * pixelsPerSecond * 0.2f;
                if (subX > rulerWidth)
                {
                    break;
                }
                var subMark = new VisualElement();
                subMark.AddToClassList("timeline-mark");
                subMark.style.position = Position.Absolute;
                subMark.style.left = subX;
                subMark.style.top = RULER_HEIGHT * 0.6f;
                subMark.style.width = 1;
                subMark.style.height = RULER_HEIGHT * 0.4f;
                subMark.style.backgroundColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
                timelineRuler.Add(subMark);
            }
        }
    }

    // 处理时间轴滚轮缩放事件
    private void OnTimelineWheel(WheelEvent evt)
    {
        // 当config为空时不允许缩放
        if (config == null)
        {
            evt.StopPropagation();
            return;
        }
        
        // 根据滚轮方向调整缩放倍数（滚轮向上放大，向下缩小）
        float zoomFactor = evt.delta.y > 0 ? 0.9f : 1.1f; // 向上滚轮缩小，向下滚轮放大
        
        // 记录旧的每秒像素数用于后续计算
        float oldPixelsPerSecond = pixelsPerSecond;
        
        // 记录鼠标位置对应的时间，用于保持缩放中心
        Vector2 localMousePos = timelineContent.WorldToLocal(evt.mousePosition);
        float mouseTimePosition = localMousePos.x / oldPixelsPerSecond;
        
        // 记录当前滚动位置
        float scrollOffset = GetScrollOffset();
        
        // 更新每秒像素数（主控值）
        pixelsPerSecond *= zoomFactor;

        // 限制像素范围
        pixelsPerSecond = Mathf.Clamp(pixelsPerSecond, MIN_PIXELS_PER_SECOND, MAX_PIXELS_PER_SECOND);
        
        // 更新内容容器宽度
        UpdateTimelineContentWidth();

        // 更新所有clip的位置和宽度
        UpdateAllClipPositions();

        // 重新绘制时间轴标尺
        DrawTimelineRulerMarks();

        // 更新播放进度条位置（缩放后需要调整）
        UpdatePlayheadPosition();
        
        // 调整滚动位置，使鼠标位置对应的时间保持在同一位置
        if (timelineScrollView != null)
        {
            float newPixelsPerSecond = pixelsPerSecond;
            float newScrollOffset = mouseTimePosition * newPixelsPerSecond - (localMousePos.x - scrollOffset);
            
            // 限制滚动范围：最大滚动距离 = 可视区域宽度 × (缩放倍数 - 1)
            float viewWidth = GetTimelineViewWidth();
            float maxScrollOffset = viewWidth * (zoomScale - 1f);
            newScrollOffset = Mathf.Clamp(newScrollOffset, 0f, maxScrollOffset);
            
            timelineScrollView.horizontalScroller.value = newScrollOffset;
        }

        // 阻止事件冒泡
        evt.StopPropagation();
    }

    #endregion
}
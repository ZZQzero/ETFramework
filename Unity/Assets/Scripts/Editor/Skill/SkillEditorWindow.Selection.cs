using System;
using System.Linq;
using ET;
using UnityEditor;
using UnityEngine.UIElements;

public partial class SkillEditorWindow : EditorWindow
{
    // Selection：统一管理 Track/Clip 的选中（右侧面板同步 + 时间轴高亮）

    private void ClearSelection()
    {
        UpdateTrackInfo(null);
        UpdateClipProperties(null);
    }

    private void SelectTrack(ITrackItem track)
    {
        // 轨道被选中时：清空 clip 选择（保持原行为）
        UpdateTrackInfo(track);
        UpdateClipProperties(null);
    }

    private void SelectClip(IClipItem clip, ITrackItem track = null)
    {
        // 保持 Timeline 里原来的调用顺序：先更新 clip，再更新 track（否则局部视图可能依赖 selectedClip 推导 owner）
        UpdateClipProperties(clip);

        if (track != null)
        {
            UpdateTrackInfo(track);
        }
    }

    private void RefreshSelectionHighlight()
    {
        if (trackContainer == null)
        {
            return;
        }

        // Track：清空并设置选中
        var trackElements = trackContainer.Query<VisualElement>(name: "track").ToList();
        foreach (var trackElement in trackElements)
        {
            trackElement.RemoveFromClassList(SELECTED_CLASS);
        }

        if (selectedTrack != null)
        {
            foreach (var trackElement in trackElements)
            {
                if (ReferenceEquals(trackElement.userData, selectedTrack))
                {
                    trackElement.AddToClassList(SELECTED_CLASS);
                    break;
                }
            }
        }

        // Clip：清空并设置选中
        var clipElements = trackContainer.Query<VisualElement>(className: "timeline-clip").ToList();
        foreach (var clipElement in clipElements)
        {
            clipElement.RemoveFromClassList(SELECTED_CLASS);
        }

        if (selectedClip != null)
        {
            foreach (var clipElement in clipElements)
            {
                if (ReferenceEquals(clipElement.userData, selectedClip))
                {
                    clipElement.AddToClassList(SELECTED_CLASS);
                    break;
                }
            }
        }
    }
}


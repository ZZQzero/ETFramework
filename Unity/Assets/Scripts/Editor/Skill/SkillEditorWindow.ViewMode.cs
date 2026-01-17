using System;
using System.Collections.Generic;
using Animancer;
using ET;
using UnityEditor;
using UnityEngine;

public partial class SkillEditorWindow : EditorWindow
{
    // ViewMode：全局/片段视图的切换、轨道列表选择（数据视图层）以及切换后的整体刷新编排

    private void OnToggleViewModeClicked()
    {
        viewMode = viewMode == ViewMode.Global ? ViewMode.ClipFocus : ViewMode.Global;
        UpdateViewModeButtonText();
        ApplyViewModeAndRefresh();
    }

    private void UpdateViewModeButtonText()
    {
        if (toggleViewModeButton == null)
        {
            return;
        }

        toggleViewModeButton.text = viewMode == ViewMode.Global ? "视图：全局" : "视图：片段";
    }

    private void ApplyViewModeAndRefresh()
    {
        if (config == null)
        {
            return;
        }

        isApplyingViewMode = true;
        try
        {
            ApplyViewMode();
            RefreshTrackContent();
            UpdateTimelineContentWidth();
            DrawTimelineRulerMarks();
            UpdatePlayheadSize();
            UpdatePlayheadPosition();

            // UI布局完成后重新创建动画结束时间黄色竖线
            EditorApplication.delayCall += () => RecreateAllAnimationEndLines();
        }
        finally
        {
            isApplyingViewMode = false;
        }
    }

    private void ApplyViewMode()
    {
        if (viewMode == ViewMode.Global)
        {
            // 全局模式：显示所有轨道
            trackDataList = globalTrackDataList;
            focusedAnimationClipItem = null;
            return;
        }

        // 局部模式：根据选中的Clip确定聚焦的动画片段
        if (allAnimationClipItems.Count == 0)
        {
            trackDataList = new List<ITrackItem>();
            focusedAnimationClipItem = null;
            return;
        }

        // 优先使用当前选中 clip 推导聚焦的动画片段
        var owner = GetOwnerAnimationClipItem(selectedClip);
        if (owner != null)
        {
            focusedAnimationClipItem = owner;
        }

        // 没有选中时默认聚焦第一个动画片段
        focusedAnimationClipItem ??= allAnimationClipItems[0];

        // 构建局部模式的轨道列表
        trackDataList = BuildFocusTrackDataList(focusedAnimationClipItem);
    }

    private List<ITrackItem> BuildFocusTrackDataList(AnimationClipItem animClipItem)
    {
        if (animClipItem == null)
        {
            return new List<ITrackItem>();
        }

        // 获取该AnimationClip对应的子轨道列表
        animationClipTrackMap.TryGetValue(animClipItem, out var tracks);

        // 根据实际轨道数量初始化列表（1个AnimationTrack + 子轨道数量）
        int capacity = 1 + (tracks?.Count ?? 0);
        var list = new List<ITrackItem>(capacity);

        // 局部模式：Animation轨道仍然显示所有AnimationClip，只筛选子轨道
        // 从globalTrackDataList获取Animation轨道（第一个轨道）
        if (globalTrackDataList.Count > 0 && globalTrackDataList[0] is AnimationTrack globalAnimTrack)
        {
            list.Add(globalAnimTrack);
        }

        // 只添加当前选中AnimationClip对应的子轨道
        if (tracks == null)
        {
            return list;
        }

        foreach (var t in tracks)
        {
            switch (t)
            {
                case EffectTrack et when et.ClipList.Count > 0:
                    list.Add(et);
                    break;
                case SoundTrack st when st.ClipList.Count > 0:
                    list.Add(st);
                    break;
                case HitBoxTrack ht when ht.ClipList.Count > 0:
                    list.Add(ht);
                    break;
                case ActiveTrack at when at.ClipList.Count > 0:
                    list.Add(at);
                    break;
            }
        }

        return list;
    }

    private AnimationClipItem GetOwnerAnimationClipItem(IClipItem clip)
    {
        if (clip == null)
        {
            return null;
        }

        if (clip is AnimationClipItem animationClipItem)
        {
            return animationClipItem;
        }

        if (clip is EffectClipItem effectClipItem && effectClipItem.EffectData != null)
        {
            foreach (var animItem in allAnimationClipItems)
            {
                if (animItem?.SegmentData != null && animItem.SegmentData.VisualEffects.Contains(effectClipItem.EffectData))
                {
                    return animItem;
                }
            }
        }

        if (clip is SoundClipItem soundClipItem && soundClipItem.SoundData != null)
        {
            foreach (var animItem in allAnimationClipItems)
            {
                if (animItem?.SegmentData != null && animItem.SegmentData.SoundEffects.Contains(soundClipItem.SoundData))
                {
                    return animItem;
                }
            }
        }

        if (clip is HitBoxClipItem hitBoxClipItem && hitBoxClipItem.HitBoxData != null)
        {
            foreach (var animItem in allAnimationClipItems)
            {
                if (animItem?.SegmentData != null && animItem.SegmentData.HitBoxes.Contains(hitBoxClipItem.HitBoxData))
                {
                    return animItem;
                }
            }
        }

        if (clip is ActiveClipItem activeClipItem && activeClipItem.ActiveData != null)
        {
            foreach (var animItem in allAnimationClipItems)
            {
                if (animItem?.SegmentData != null && animItem.SegmentData.AttachedActives.Contains(activeClipItem.ActiveData))
                {
                    return animItem;
                }
            }
        }

        return null;
    }
}


using System;
using System.Collections.Generic;
using Animancer;
using ET;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public partial class SkillEditorWindow : EditorWindow
{
    #region AnimationEnd/重叠同步（相邻段计算）

    // === TimeWindowData <-> 时间轴重叠（Editor 侧同步） ===

    private List<AnimationClipItem> GetSortedAnimationClips()
    {
        var list = new List<AnimationClipItem>(allAnimationClipItems.Count);
        list.AddRange(allAnimationClipItems);
        list.Sort((a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            int c = a.StartTime.CompareTo(b.StartTime);
            if (c != 0) return c;
            return a.Index.CompareTo(b.Index);
        });
        return list;
    }

    private void GetNeighbors(AnimationClipItem current, out AnimationClipItem prev, out AnimationClipItem next)
    {
        prev = null;
        next = null;
        if (current == null) return;

        var list = GetSortedAnimationClips();
        int idx = -1;
        for (int i = 0; i < list.Count; ++i)
        {
            if (ReferenceEquals(list[i], current))
            {
                idx = i;
                break;
            }
        }

        if (idx < 0) return;
        if (idx - 1 >= 0) prev = list[idx - 1];
        if (idx + 1 < list.Count) next = list[idx + 1];
    }

    private void RecomputeAnimationEndFromOverlap(AnimationClipItem segment)
    {
        if (segment?.SegmentData == null) return;
        GetNeighbors(segment, out _, out var next);
        if (next == null) return;

        float duration = Mathf.Max(0.0001f, segment.Duration);
        float t = Mathf.Clamp01((next.StartTime - segment.StartTime) / duration);

        var tw = segment.SegmentData.TimeWindow ??= new TimeWindowData();
        tw.AnimationEnd = t;

        if (ReferenceEquals(selectedClip, segment) && animationEndField != null)
        {
            animationEndField.SetValueWithoutNotify(t);
        }
    }

    private void SyncNextAnimationClipStartTimeFromAnimationEnd(AnimationClipItem segment)
    {
        if (segment?.SegmentData == null) return;
        GetNeighbors(segment, out _, out var next);
        if (next?.SegmentData == null) return;

        float duration = Mathf.Max(0.0001f, segment.Duration);
        float end = Mathf.Clamp01((segment.SegmentData.TimeWindow ??= new TimeWindowData()).AnimationEnd);
        float newNextStart = segment.StartTime + duration * end;

        next.StartTime = newNextStart;
        next.SegmentData.StartTime = newNextStart;
        SyncOwnerChildClipsToOwner(next);

        RecomputeAnimationEndFromOverlap(next);
    }

    private void RecomputeAnimationEndsAround(AnimationClipItem changed)
    {
        if (changed == null) return;

        GetNeighbors(changed, out var prev, out _);
        if (prev != null) RecomputeAnimationEndFromOverlap(prev);
        RecomputeAnimationEndFromOverlap(changed);

        if (ReferenceEquals(selectedClip, changed) && changed.SegmentData != null)
        {
            var tw = changed.SegmentData.TimeWindow ??= new TimeWindowData();
            inputBufferStartField?.SetValueWithoutNotify(tw.InputBufferStart);
            cancelableTimeField?.SetValueWithoutNotify(tw.CancelableTime);
            animationEndField?.SetValueWithoutNotify(tw.AnimationEnd);
        }
    }

    #endregion
}


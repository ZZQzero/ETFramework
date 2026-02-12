using System;
using System.Collections.Generic;
using ET;
using UnityEditor;
using UnityEngine;

public partial class SkillEditorWindow : EditorWindow
{
    #region AnimationEnd/重叠同步（相邻段计算）

    // === TimeWindowData <-> 时间轴重叠（Editor 侧同步） ===

    /// <summary>
    /// 按“归一化时间保持不变”的规则，从 <paramref name="changed"/> 开始，链式重算后续 AnimationClip 的绝对 StartTime。
    /// 规则：
    /// - 不修改任何归一化字段（如 TimeWindow.AnimationEnd、子片段 NormalizedStart/End）
    /// - next.StartTime = cur.StartTime + cur.Duration * cur.AnimationEnd
    /// - 同步 next.SegmentData.StartTime，并刷新 next 的子片段绝对时间（保持归一化不变）
    /// </summary>
    private void PropagateAnimationClipStartTimesKeepNormalized(AnimationClipItem changed)
    {
        if (changed?.SegmentData == null)
        {
            return;
        }

        var list = GetSortedAnimationClips();
        int idx = -1;
        for (int i = 0; i < list.Count; ++i)
        {
            if (ReferenceEquals(list[i], changed))
            {
                idx = i;
                break;
            }
        }

        if (idx < 0)
        {
            return;
        }

        for (int i = idx; i < list.Count - 1; ++i)
        {
            var cur = list[i];
            var next = list[i + 1];
            if (cur?.SegmentData == null || next?.SegmentData == null)
            {
                continue;
            }

            float duration = Mathf.Max(0f, cur.Duration);
            float endNorm = cur.SegmentData.TimeWindow?.AnimationEnd ?? 1f;
            endNorm = Mathf.Clamp01(endNorm);

            float newNextStart = Mathf.Max(0f, cur.StartTime) + duration * endNorm;

            // 写回 next 的绝对开始时间（段本体 + 配置）
            next.StartTime = newNextStart;
            next.SegmentData.StartTime = newNextStart;

            // 由于 next 的 StartTime 变化：其子片段的归一化不变，但绝对时间需要重新换算
            SyncOwnerChildClipsToOwner(next);
        }
    }

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

        var tw = segment.SegmentData.TimeWindow ??= new TimeWindowData();

        if (next != null)
        {
            // 有下一段时，基于重叠关系计算AnimationEnd
            float duration = Mathf.Max(0.0001f, segment.Duration);
            float t = Mathf.Clamp01((next.StartTime - segment.StartTime) / duration);
            tw.AnimationEnd = t;
        }
        // 没有下一段时，保持现有的AnimationEnd值（用户设置的）

        if (ReferenceEquals(selectedClip, segment) && animationEndField != null)
        {
            animationEndField.SetValueWithoutNotify(tw.AnimationEnd);
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


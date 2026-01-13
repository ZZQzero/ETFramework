using System;
using Animancer;
using ET;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public partial class SkillEditorWindow : EditorWindow
{
    #region 归一化时间 ↔ 绝对时间 换算（Effect/Sound/HitBox）

    /// <summary>
    /// 根据归一化时间更新Clip的开始时间
    /// </summary>
    private void UpdateClipStartTimeFromNormalizedStart(IClipItem clipItem)
    {
        if (clipItem == null || config == null) return;

        // 找到对应的Segment
        foreach (var segment in config.Segments)
        {
            if (clipItem is EffectClipItem effectClip && segment.VisualEffects.Contains(effectClip.EffectData))
            {
                // 获取动画片段长度
                float animationLength = GetSegmentAnimationLength(segment);

                // 计算绝对开始时间：segment开始时间 + (归一化时间 × 动画长度)
                clipItem.StartTime = segment.StartTime + (effectClip.EffectData.NormalizedStart * animationLength);
                break;
            }
            else if (clipItem is SoundClipItem soundClip && segment.SoundEffects.Contains(soundClip.SoundData))
            {
                // 获取动画片段长度
                float animationLength = GetSegmentAnimationLength(segment);

                // 计算绝对开始时间：segment开始时间 + (归一化时间 × 动画长度)
                clipItem.StartTime = segment.StartTime + (soundClip.SoundData.NormalizedStart * animationLength);
                break;
            }
            else if (clipItem is ActiveClipItem activeClip && segment.AttachedActives.Contains(activeClip.ActiveData))
            {
                float animationLength = GetSegmentAnimationLength(segment);

                clipItem.StartTime = segment.StartTime + (Mathf.Clamp01(activeClip.ActiveData.NormalizedStart) * animationLength);
                break;
            }
        }
    }

    /// <summary>
    /// 根据归一化时间更新HitBox Clip的开始时间和持续时间
    /// </summary>
    private void UpdateHitBoxClipFromNormalizedTimes(HitBoxClipItem clipItem)
    {
        if (clipItem == null || clipItem.HitBoxData == null || config == null) return;

        // 找到对应的Segment
        foreach (var segment in config.Segments)
        {
            if (segment.HitBoxes.Contains(clipItem.HitBoxData))
            {
                // 获取动画片段长度
                float animationLength = GetSegmentAnimationLength(segment);

                // 计算绝对开始时间：segment开始时间 + (归一化开始时间 × 动画长度)
                clipItem.StartTime = segment.StartTime + (clipItem.HitBoxData.NormalizedStart * animationLength);

                // 计算持续时间：(归一化结束时间 - 归一化开始时间) × 动画长度
                clipItem.Duration = (clipItem.HitBoxData.NormalizedEnd - clipItem.HitBoxData.NormalizedStart) * animationLength;

                // 确保持续时间不为负数
                if (clipItem.Duration < 0f)
                {
                    clipItem.Duration = 0f;
                }
                // 更新帧数
                clipItem.Frame = Mathf.RoundToInt(clipItem.Duration * FRAMES_PER_SECOND);
                break;
            }
        }
    }

    private void UpdateActiveClipFromNormalizedTimes(ActiveClipItem clipItem)
    {
        if (clipItem == null || clipItem.ActiveData == null || config == null) return;

        foreach (var segment in config.Segments)
        {
            if (segment.AttachedActives.Contains(clipItem.ActiveData))
            {
                float animationLength = GetSegmentAnimationLength(segment);

                clipItem.StartTime = segment.StartTime + (Mathf.Clamp01(clipItem.ActiveData.NormalizedStart) * animationLength);
                clipItem.Duration = (Mathf.Clamp01(clipItem.ActiveData.NormalizedEnd) - Mathf.Clamp01(clipItem.ActiveData.NormalizedStart)) * animationLength;
                if (clipItem.Duration < 0f)
                {
                    clipItem.Duration = 0f;
                }
                clipItem.Frame = Mathf.RoundToInt(clipItem.Duration * FRAMES_PER_SECOND);
                break;
            }
        }
    }

    #endregion
}


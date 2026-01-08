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
    #region Animation Clip操作按钮
    
    /// <summary>
    /// 添加特效按钮点击事件
    /// </summary>
    private void OnAddEffectButtonClicked()
    {
        if (selectedClip is AnimationClipItem animClipItem && animClipItem.SegmentData != null && config != null)
        {
            var segment = animClipItem.SegmentData;
            
            // 创建新的特效数据
            var newEffect = new VisualEffectData
            {
                Name = "New Effect",
                NormalizedStart = 0f, // 相对于动画片段的归一化时间
                Length = 1.0f
            };
            
            // 添加到Config数据
            segment.VisualEffects.Add(newEffect);
            
            // 创建ClipItem
            var effectClipItem = new EffectClipItem
            {
                EffectData = newEffect,
                Name = newEffect.Name,
                StartTime = animClipItem.StartTime + newEffect.NormalizedStart * animClipItem.Duration,
                Duration = newEffect.Length,
                Frame = Mathf.RoundToInt(newEffect.Length * 60f)
            };
            
            // 获取或创建Effect轨道
            var effectTrack = GetOrCreateTrackForAnimationClip<EffectTrack>(animClipItem, TrackType.Effect);
            effectTrack.ClipList.Add(effectClipItem);
            
            MarkAssetDirty();
            ApplyViewModeAndRefresh();
            
            Debug.Log($"已为AnimationClip {animClipItem.Name} 添加特效");
        }
    }
    
    /// <summary>
    /// 添加音效按钮点击事件
    /// </summary>
    private void OnAddSoundButtonClicked()
    {
        if (selectedClip is AnimationClipItem animClipItem && animClipItem.SegmentData != null && config != null)
        {
            var segment = animClipItem.SegmentData;
            
            // 创建新的音效数据
            var newSound = new SoundEffectData
            {
                Name = "New Sound",
                NormalizedStart = 0f, // 相对于动画片段的归一化时间
                Volume = 1f
            };
            
            // 添加到Config数据
            segment.SoundEffects.Add(newSound);
            
            // 创建ClipItem
            var soundClipItem = new SoundClipItem
            {
                SoundData = newSound,
                Name = newSound.Name,
                StartTime = animClipItem.StartTime + newSound.NormalizedStart * animClipItem.Duration,
                Duration = newSound.Clip != null ? newSound.Clip.length : 1f,
                Frame = Mathf.RoundToInt((newSound.Clip != null ? newSound.Clip.length : 1f) * 60f)
            };
            
            // 获取或创建Sound轨道
            var soundTrack = GetOrCreateTrackForAnimationClip<SoundTrack>(animClipItem, TrackType.Sound);
            soundTrack.ClipList.Add(soundClipItem);
            
            MarkAssetDirty();
            ApplyViewModeAndRefresh();
            
            Debug.Log($"已为AnimationClip {animClipItem.Name} 添加音效");
        }
    }
    
    /// <summary>
    /// 添加Hitbox按钮点击事件
    /// </summary>
    private void OnAddHitboxButtonClicked()
    {
        if (selectedClip is AnimationClipItem animClipItem && animClipItem.SegmentData != null && config != null)
        {
            var segment = animClipItem.SegmentData;
            
            // 创建新的Hitbox数据
            var newHitbox = new HitBoxData
            {
                ShapeType = HitShapeType.Box,
                NormalizedStart = 0f,   // 相对于动画片段的归一化时间
                NormalizedEnd = 0.3f    // 默认持续30%的动画时长
            };
            
            // 添加到Config数据
            segment.HitBoxes.Add(newHitbox);
            
            // 创建ClipItem
            var hitboxClipItem = new HitBoxClipItem
            {
                HitBoxData = newHitbox,
                Name = newHitbox.ShapeType.ToString(),
                StartTime = animClipItem.StartTime + newHitbox.NormalizedStart * animClipItem.Duration,
                Duration = (newHitbox.NormalizedEnd - newHitbox.NormalizedStart) * animClipItem.Duration,
                Frame = Mathf.RoundToInt((newHitbox.NormalizedEnd - newHitbox.NormalizedStart) * animClipItem.Duration * 60f)
            };
            
            // 获取或创建HitBox轨道
            var hitboxTrack = GetOrCreateTrackForAnimationClip<HitBoxTrack>(animClipItem, TrackType.Hitbox);
            hitboxTrack.ClipList.Add(hitboxClipItem);

            // 选中新建 HitBox，方便立刻在 SceneView 里调整判定框
            selectedTrack = hitboxTrack;
            selectedClip = hitboxClipItem;
            
            MarkAssetDirty();
            ApplyViewModeAndRefresh();
            SceneView.RepaintAll();
            
            Debug.Log($"已为AnimationClip {animClipItem.Name} 添加Hitbox");
        }
    }

    /// <summary>
    /// 添加Active按钮点击事件（控制角色身上已有对象的 SetActive）
    /// </summary>
    private void OnAddActiveButtonClicked()
    {
        if (selectedClip is not AnimationClipItem animClipItem || animClipItem.SegmentData == null || config == null)
        {
            return;
        }

        var segment = animClipItem.SegmentData;

        // 创建新的Active数据（不引用场景对象，只记录相对路径；路径建议通过拖拽到 ActiveTrack 创建）
        var data = new AttachedActiveData
        {
            Name = "New Active",
            RelativePath = string.Empty,
            NormalizedStart = 0f,
            NormalizedEnd = 0.2f,
        };

        segment.AttachedActives.Add(data);

        float ownerDuration = Mathf.Max(0.0001f, animClipItem.Duration);
        float absStart = animClipItem.StartTime + Mathf.Clamp01(data.NormalizedStart) * ownerDuration;
        float dur = (Mathf.Clamp01(data.NormalizedEnd) - Mathf.Clamp01(data.NormalizedStart)) * ownerDuration;

        var clipItem = new ActiveClipItem
        {
            ActiveData = data,
            Name = data.Name,
            StartTime = absStart,
            Duration = Mathf.Max(0f, dur),
            Frame = Mathf.RoundToInt(Mathf.Max(0f, dur) * 60f),
        };

        var track = GetOrCreateTrackForAnimationClip<ActiveTrack>(animClipItem, TrackType.Active);
        track.ClipList.Add(clipItem);

        // 选中新建 clip，便于立刻拖拽/调整
        selectedTrack = track;
        selectedClip = clipItem;

        MarkAssetDirty();
        ApplyViewModeAndRefresh();

        Debug.Log($"已为AnimationClip {animClipItem.Name} 添加Active");
    }
    
    /// <summary>
    /// 添加Clip到轨道按钮点击事件
    /// </summary>
    private void OnAddClipToTrackButtonClicked()
    {
        if (selectedTrack == null || config == null) return;
        
        // 只有AnimationTrack才能添加Clip
        if (selectedTrack is AnimationTrack animTrack)
        {
            AnimationClipItem clipItem = new AnimationClipItem();
            AttackSegmentData data = new AttackSegmentData();
            config.Segments.Add(data);
            clipItem.Name = "AnimationClip";
            clipItem.Duration = 2f;
            clipItem.SegmentData = data;
            clipItem.Frame = Mathf.RoundToInt(clipItem.Duration * 60f);
            clipItem.StartTime = 0f;

            data.StartTime = clipItem.StartTime;
            data.Duration = clipItem.Duration;
            animTrack.ClipList.Add(clipItem);
            
            // 添加到allAnimationClipItems
            allAnimationClipItems.Add(clipItem);
            
            // 为新的AnimationClip创建空的轨道映射
            animationClipTrackMap[clipItem] = new List<ITrackItem>();
            
            MarkAssetDirty();
            ApplyViewModeAndRefresh();
            
            Debug.Log("已添加新的AnimationClip");
        }
    }
    
    /// <summary>
    /// 删除Clip按钮点击事件
    /// </summary>
    private void OnDeleteClipButtonClicked()
    {
        if (selectedClip == null) return;
        
        DeleteClip(selectedClip);
    }
    
    #endregion
}
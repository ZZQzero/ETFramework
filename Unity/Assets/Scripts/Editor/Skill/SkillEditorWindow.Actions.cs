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
    #region Clip操作 - 添加
    
    private void OnAddEffectButtonClicked()
    {
        if (selectedClip is AnimationClipItem animClipItem && animClipItem.SegmentData != null && config != null)
        {
            var segment = animClipItem.SegmentData;
            var newEffect = new VisualEffectData
            {
                Name = "New Effect",
                NormalizedStart = 0f,
                Length = 1.0f
            };
            
            segment.VisualEffects.Add(newEffect);
            
            var effectClipItem = new EffectClipItem
            {
                EffectData = newEffect,
                Name = newEffect.Name,
                StartTime = animClipItem.StartTime + newEffect.NormalizedStart * animClipItem.Duration,
                Duration = newEffect.Length,
                Frame = Mathf.RoundToInt(newEffect.Length * 60f)
            };
            
            var effectTrack = GetOrCreateTrackForAnimationClip<EffectTrack>(animClipItem, TrackType.Effect);
            effectTrack.ClipList.Add(effectClipItem);
            
            MarkAssetDirty();
            ApplyViewModeAndRefresh();
        }
    }
    
    private void OnAddSoundButtonClicked()
    {
        if (selectedClip is AnimationClipItem animClipItem && animClipItem.SegmentData != null && config != null)
        {
            var segment = animClipItem.SegmentData;
            var newSound = new SoundEffectData
            {
                Name = "New Sound",
                NormalizedStart = 0f,
                Volume = 1f
            };
            
            segment.SoundEffects.Add(newSound);
            
            var soundClipItem = new SoundClipItem
            {
                SoundData = newSound,
                Name = newSound.Name,
                StartTime = animClipItem.StartTime + newSound.NormalizedStart * animClipItem.Duration,
                Duration = newSound.Clip != null ? newSound.Clip.length : 1f,
                Frame = Mathf.RoundToInt((newSound.Clip != null ? newSound.Clip.length : 1f) * 60f)
            };
            
            var soundTrack = GetOrCreateTrackForAnimationClip<SoundTrack>(animClipItem, TrackType.Sound);
            soundTrack.ClipList.Add(soundClipItem);
            
            MarkAssetDirty();
            ApplyViewModeAndRefresh();
        }
    }
    
    private void OnAddHitboxButtonClicked()
    {
        if (selectedClip is AnimationClipItem animClipItem && animClipItem.SegmentData != null && config != null)
        {
            var segment = animClipItem.SegmentData;
            var newHitbox = new HitBoxData
            {
                ShapeType = HitShapeType.Box,
                NormalizedStart = 0f,
                NormalizedEnd = 0.3f
            };
            
            segment.HitBoxes.Add(newHitbox);
            
            var hitboxClipItem = new HitBoxClipItem
            {
                HitBoxData = newHitbox,
                Name = newHitbox.ShapeType.ToString(),
                StartTime = animClipItem.StartTime + newHitbox.NormalizedStart * animClipItem.Duration,
                Duration = (newHitbox.NormalizedEnd - newHitbox.NormalizedStart) * animClipItem.Duration,
                Frame = Mathf.RoundToInt((newHitbox.NormalizedEnd - newHitbox.NormalizedStart) * animClipItem.Duration * 60f)
            };
            
            var hitboxTrack = GetOrCreateTrackForAnimationClip<HitBoxTrack>(animClipItem, TrackType.Hitbox);
            hitboxTrack.ClipList.Add(hitboxClipItem);

            selectedTrack = hitboxTrack;
            selectedClip = hitboxClipItem;
            
            MarkAssetDirty();
            ApplyViewModeAndRefresh();
            SceneView.RepaintAll();
        }
    }

    private void OnAddActiveButtonClicked()
    {
        if (selectedClip is not AnimationClipItem animClipItem || animClipItem.SegmentData == null || config == null)
        {
            return;
        }

        var segment = animClipItem.SegmentData;
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

        selectedTrack = track;
        selectedClip = clipItem;

        MarkAssetDirty();
        ApplyViewModeAndRefresh();
    }
    
    private void OnAddClipToTrackButtonClicked()
    {
        if (selectedTrack == null || config == null) return;
        
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
            
            allAnimationClipItems.Add(clipItem);
            animationClipTrackMap[clipItem] = new List<ITrackItem>();
            
            MarkAssetDirty();
            ApplyViewModeAndRefresh();
        }
    }
    
    #endregion

    #region Clip操作 - 删除
    
    private void OnDeleteClipButtonClicked()
    {
        if (selectedClip == null) return;
        DeleteClip(selectedClip);
    }
    
    #endregion
}
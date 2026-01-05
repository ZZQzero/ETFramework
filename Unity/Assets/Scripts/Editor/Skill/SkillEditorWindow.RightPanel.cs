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
    #region Right面板更新方法

    // 隐藏所有Clip字段组
    private void HideAllClipFields()
    {
        if (animationFields != null) animationFields.style.display = DisplayStyle.None;
        if (effectFields != null) effectFields.style.display = DisplayStyle.None;
        if (soundFields != null) soundFields.style.display = DisplayStyle.None;
        if (hitBoxFields != null) hitBoxFields.style.display = DisplayStyle.None;
        if (animationActionButtons != null) animationActionButtons.style.display = DisplayStyle.None;
    }
    
    // 更新轨道信息显示
    private void UpdateTrackInfo(ITrackItem track)
    {
        selectedTrack = track;
        if (track == null)
        {
            if (trackTypeLabel != null) trackTypeLabel.text = "-";
            if (clipCountLabel != null) clipCountLabel.text = "-";
            if (totalDurationLabel != null) totalDurationLabel.text = "-";
            RefreshSelectionHighlight();
            return;
        }
        
        // 更新轨道类型
        if (trackTypeLabel != null)
        {
            trackTypeLabel.text = track.Type.ToString();
        }
        else
        {
            Debug.LogWarning("trackTypeLabel为null，无法更新轨道类型");
        }
        
        // 计算片段数和总时长
        int clipCount = 0;
        float totalDuration = 0f;
        
        if (track is AnimationTrack animTrack)
        {
            clipCount = animTrack.ClipList.Count;
            foreach (var clip in animTrack.ClipList)
            {
                totalDuration = Mathf.Max(totalDuration, clip.StartTime + clip.Duration);
            }
        }
        else if (track is EffectTrack effectTrack)
        {
            clipCount = effectTrack.ClipList.Count;
            foreach (var clip in effectTrack.ClipList)
            {
                totalDuration = Mathf.Max(totalDuration, clip.StartTime + clip.Duration);
            }
        }
        else if (track is SoundTrack soundTrack)
        {
            clipCount = soundTrack.ClipList.Count;
            foreach (var clip in soundTrack.ClipList)
            {
                totalDuration = Mathf.Max(totalDuration, clip.StartTime + clip.Duration);
            }
        }
        else if (track is HitBoxTrack hitBoxTrack)
        {
            clipCount = hitBoxTrack.ClipList.Count;
            foreach (var clip in hitBoxTrack.ClipList)
            {
                totalDuration = Mathf.Max(totalDuration, clip.StartTime + clip.Duration);
            }
        }
        
        if (clipCountLabel != null)
        {
            clipCountLabel.text = clipCount.ToString();
        }
        
        if (totalDurationLabel != null)
        {
            totalDurationLabel.text = $"{totalDuration:F2}s";
        }

        // 根据轨道类型显示/隐藏"添加Clip"按钮
        UpdateAddClipButtonVisibility(track);

        RefreshSelectionHighlight();
    }
    
    // 更新"添加Clip"按钮的可见性
    private void UpdateAddClipButtonVisibility(ITrackItem track)
    {
        if (addClipToTrackButton == null) return;
        
        // 只有AnimationTrack才显示"添加Clip"按钮
        if (track is AnimationTrack)
        {
            addClipToTrackButton.style.display = DisplayStyle.Flex;
            addClipToTrackButton.text = "添加动画片段";
        }
        else
        {
            addClipToTrackButton.style.display = DisplayStyle.None;
        }
    }
    
    // 更新Clip索引标签
    private void UpdateClipIndexLabel(IClipItem clip)
    {
        if (clipIndexLabel == null) return;
        
        int index = -1;
        // 根据Clip类型在对应的轨道中查找索引
        foreach (var track in trackDataList)
        {
            if (clip is AnimationClipItem animClip && track is AnimationTrack animTrack)
            {
                index = animTrack.ClipList.IndexOf(animClip);
                if (index >= 0) break;
            }
            else if (clip is EffectClipItem effectClip && track is EffectTrack effectTrack)
            {
                index = effectTrack.ClipList.IndexOf(effectClip);
                if (index >= 0) break;
            }
            else if (clip is SoundClipItem soundClip && track is SoundTrack soundTrack)
            {
                index = soundTrack.ClipList.IndexOf(soundClip);
                if (index >= 0) break;
            }
            else if (clip is HitBoxClipItem hitBoxClip && track is HitBoxTrack hitBoxTrack)
            {
                index = hitBoxTrack.ClipList.IndexOf(hitBoxClip);
                if (index >= 0) break;
            }
        }
        
        // 显示数组下标索引
        clipIndexLabel.text = index >= 0 ? $"动画顺序: {index}" : "";
    }
    
    // 更新Clip属性显示
    private void UpdateClipProperties(IClipItem clip)
    {
        selectedClip = clip;
        
        // 局部模式下：如果选中了不同的 AnimationClip，刷新轨道显示
        if (viewMode == ViewMode.ClipFocus && clip is AnimationClipItem animClip)
        {
            if (focusedAnimationClipItem != animClip)
            {
                focusedAnimationClipItem = animClip;
                // 刷新轨道会触发 UpdateClipProperties 再次调用，这里需要避免递归导致的“延迟刷新/闪动”。
                if (!isApplyingViewMode)
                {
                    ApplyViewModeAndRefresh();
                }
            }
        }
        
        // 先隐藏所有字段组
        HideAllClipFields();
        
        if (clip == null)
        {
            // 清空所有字段
            ClearAllClipFields();
            if (clipPropertiesTitle != null) clipPropertiesTitle.text = "片段属性";
            // 隐藏删除按钮和索引
            if (deleteClipButton != null) deleteClipButton.style.display = DisplayStyle.None;
            if (clipIndexLabel != null) clipIndexLabel.text = "";
            RefreshSelectionHighlight();
            return;
        }
        
        // 显示删除按钮
        if (deleteClipButton != null)
        {
            deleteClipButton.style.display = DisplayStyle.Flex;
        }
        
        // 更新索引显示
        UpdateClipIndexLabel(clip);
        
        // 更新标题
        if (clipPropertiesTitle != null)
        {
            string title = clip.Type switch
            {
                TrackType.Animation => "动画片段属性",
                TrackType.Effect => "特效片段属性",
                TrackType.Sound => "音效片段属性",
                TrackType.Hitbox => "碰撞盒属性",
                _ => "片段属性"
            };
            clipPropertiesTitle.text = title;
        }
        
        // 更新通用字段：名称、开始时间和帧数
        if (clipNameField != null)
        {
            clipNameField.SetValueWithoutNotify(clip.Name ?? "");
        }
        if (startTimeField != null)
        {
            startTimeField.SetValueWithoutNotify(clip.StartTime);
        }
        
        // 根据Clip类型更新对应的字段
        if (clip is AnimationClipItem animClipItem)
        {
            UpdateAnimationClipProperties(animClipItem);
        }
        else if (clip is EffectClipItem effectClipItem)
        {
            UpdateEffectClipProperties(effectClipItem);
        }
        else if (clip is SoundClipItem soundClipItem)
        {
            UpdateSoundClipProperties(soundClipItem);
        }
        else if (clip is HitBoxClipItem hitBoxClipItem)
        {
            UpdateHitBoxClipProperties(hitBoxClipItem);
        }

        // 统一同步 Length/Frame 等派生信息
        if (clipLengthField != null)
        {
            clipLengthField.SetValueWithoutNotify(Mathf.Max(0f, clip.Duration));
            bool readOnly = clip.Type == TrackType.Animation || clip.Type == TrackType.Sound;
            clipLengthField.SetEnabled(!readOnly);
        }
        // 总帧数始终只读：Frame = Length * 60
        int frame = Mathf.Max(0, Mathf.RoundToInt(clip.Duration * 60f));
        clip.Frame = frame;
        if (frameField != null)
        {
            frameField.SetValueWithoutNotify(frame);
            frameField.SetEnabled(false);
        }

        RefreshSelectionHighlight();
    }
    
    // 更新Animation Clip属性
    private void UpdateAnimationClipProperties(AnimationClipItem clipItem)
    {
        if (animationFields != null) animationFields.style.display = DisplayStyle.Flex;
        
        // 显示操作按钮组（只有AnimationClip才显示）
        if (animationActionButtons != null)
        {
            animationActionButtons.style.display = DisplayStyle.Flex;
        }
        
        var segmentData = clipItem.SegmentData;
        if (segmentData == null)
        {
            Debug.LogWarning("SegmentData为null，无法更新Animation Clip属性");
            return;
        }
        
        // 更新动画Clip引用
        AnimationClip animClip = null;
        if (animationClipField != null)
        {
            if (segmentData.AnimationClipTrans != null)
            {
                animClip = segmentData.AnimationClipTrans.Clip;
            }
            animationClipField.SetValueWithoutNotify(animClip);
        }
        
        // 更新播放速度
        float speed = 1f;
        if (speedField != null)
        {
            if (segmentData.AnimationClipTrans != null)
            {
                speed = segmentData.AnimationClipTrans.Speed;
            }
            speedField.SetValueWithoutNotify(speed);
        }
        
        // Duration：以 Segment.Duration 为真源；为空则用 Clip.length / Speed 推导（但不在这里改写数据）
        float calculatedDuration = segmentData.Duration > 0f
            ? segmentData.Duration
            : (animClip != null ? animClip.length / Mathf.Max(speed, 0.01f) : clipItem.Duration);
        
        // 更新Duration字段显示
        if (durationField != null)
        {
            durationField.SetValueWithoutNotify(calculatedDuration);
            durationField.SetEnabled(false); // 设置为只读，因为是根据动画Clip自动计算的
        }
        
        // 更新过渡时间
        if (fadeDurationField != null)
        {
            float fadeDuration = 0.25f;
            if (segmentData.AnimationClipTrans != null)
            {
                fadeDuration = segmentData.AnimationClipTrans.FadeDuration;
            }
            fadeDurationField.SetValueWithoutNotify(fadeDuration);
        }
        
        // 总帧数由通用字段统一计算（Length * 60）
    }
    
    // 更新Effect Clip属性
    private void UpdateEffectClipProperties(EffectClipItem clipItem)
    {
        if (effectFields != null) effectFields.style.display = DisplayStyle.Flex;
        
        var effectData = clipItem.EffectData;
        if (effectData == null)
        {
            Debug.LogWarning("EffectData为null，无法更新Effect Clip属性");
            return;
        }
        
        // 更新特效预制体
        if (effectPrefabField != null)
        {
            effectPrefabField.SetValueWithoutNotify(effectData.Prefab);
        }
        
        // 更新触发时间（需要从归一化时间转换为绝对时间）
        if (effectTriggerTimeField != null && config != null)
        {
            // 找到对应的Segment来计算绝对时间
            float absoluteTriggerTime = clipItem.StartTime; // 默认使用clip的开始时间
            foreach (var segment in config.Segments)
            {
                if (segment.VisualEffects.Contains(effectData))
                {
                    // 获取动画片段长度
                    float animationLength = segment.Duration > 0f ? segment.Duration : 2f; // 默认长度
                    if (animationLength <= 0f && segment.AnimationClipTrans != null && segment.AnimationClipTrans.Clip != null)
                    {
                        animationLength = segment.AnimationClipTrans.Clip.length;
                    }
            
                    // 计算绝对触发时间：segment开始时间 + (归一化时间 × 动画长度)
                    absoluteTriggerTime = segment.StartTime + (effectData.NormalizedStart * animationLength);
                    break;
                }
            }
            effectTriggerTimeField.SetValueWithoutNotify(absoluteTriggerTime);
            effectTriggerTimeField.SetEnabled(false);
        }
        
        // 更新是否跟随目标
        if (followTargetField != null)
        {
            followTargetField.SetValueWithoutNotify(effectData.FollowTarget);
        }

        // 更新归一化时间
        if (effectNormalizedStartField != null)
        {
            effectNormalizedStartField.SetValueWithoutNotify(effectData.NormalizedStart);
        }
    }
    
    // 更新Sound Clip属性
    private void UpdateSoundClipProperties(SoundClipItem clipItem)
    {
        if (soundFields != null) soundFields.style.display = DisplayStyle.Flex;
        
        var soundData = clipItem.SoundData;
        if (soundData == null)
        {
            Debug.LogWarning("SoundData为null，无法更新Sound Clip属性");
            return;
        }
        
        // 更新音频Clip
        if (audioClipField != null)
        {
            audioClipField.SetValueWithoutNotify(soundData.Clip);
        }
        
        // 更新触发时间（需要从归一化时间转换为绝对时间）
        if (soundTriggerTimeField != null && config != null)
        {
            // 找到对应的Segment来计算绝对时间
            float absoluteTriggerTime = clipItem.StartTime; // 默认使用clip的开始时间
            foreach (var segment in config.Segments)
            {
                if (segment.SoundEffects.Contains(soundData))
                {
                    // 获取动画片段长度
                    float animationLength = segment.Duration > 0f ? segment.Duration : 2f; // 默认长度
                    if (animationLength <= 0f && segment.AnimationClipTrans != null && segment.AnimationClipTrans.Clip != null)
                    {
                        animationLength = segment.AnimationClipTrans.Clip.length;
                    }
            
                    // 计算绝对触发时间：segment开始时间 + (归一化时间 × 动画长度)
                    absoluteTriggerTime = segment.StartTime + (soundData.NormalizedStart * animationLength);
                    break;
                }
            }
            soundTriggerTimeField.SetValueWithoutNotify(absoluteTriggerTime);
            soundTriggerTimeField.SetEnabled(false);
        }
        
        // 更新音量
        if (volumeField != null)
        {
            volumeField.SetValueWithoutNotify(soundData.Volume);
        }

        // 更新归一化时间
        if (soundNormalizedStartField != null)
        {
            soundNormalizedStartField.SetValueWithoutNotify(soundData.NormalizedStart);
        }
        //TODO 需要添加时长显示面板
        if (soundData.Clip != null)
        {
            clipItem.Duration = soundData.Clip.length;
        }
    }
    
    // 更新HitBox Clip属性
    private void UpdateHitBoxClipProperties(HitBoxClipItem clipItem)
    {
        if (hitBoxFields != null) hitBoxFields.style.display = DisplayStyle.Flex;
        
        var hitBoxData = clipItem.HitBoxData;
        if (hitBoxData == null)
        {
            Debug.LogWarning("HitBoxData为null，无法更新HitBox Clip属性");
            return;
        }
        
        // 更新形状类型
        if (shapeTypeField != null)
        {
            shapeTypeField.SetValueWithoutNotify(hitBoxData.ShapeType);
        }

        // 触发时间（秒，只读）：使用clip的绝对开始时间
        if (hitBoxTriggerTimeField != null)
        {
            hitBoxTriggerTimeField.SetValueWithoutNotify(clipItem.StartTime);
            hitBoxTriggerTimeField.SetEnabled(false);
        }

        // 更新归一化时间字段
        if (hitBoxNormalizedStartField != null)
        {
            hitBoxNormalizedStartField.SetValueWithoutNotify(hitBoxData.NormalizedStart);
        }
        if (hitBoxNormalizedEndField != null)
        {
            hitBoxNormalizedEndField.SetValueWithoutNotify(hitBoxData.NormalizedEnd);
        }
    }
    
    // 清空所有Clip字段
    private void ClearAllClipFields()
    {
        if (clipNameField != null) clipNameField.SetValueWithoutNotify("");
        if (startTimeField != null) startTimeField.SetValueWithoutNotify(0f);
        if (frameField != null) frameField.SetValueWithoutNotify(0);
        if (clipLengthField != null) clipLengthField.SetValueWithoutNotify(0f);
        
        // Animation字段
        if (animationClipField != null) animationClipField.SetValueWithoutNotify(null);
        if (speedField != null) speedField.SetValueWithoutNotify(1f);
        if (durationField != null) durationField.SetValueWithoutNotify(0f);
        if (fadeDurationField != null) fadeDurationField.SetValueWithoutNotify(0.25f);
        
        // Effect字段
        if (effectPrefabField != null) effectPrefabField.SetValueWithoutNotify(null);
        if (effectTriggerTimeField != null)
        {
            effectTriggerTimeField.SetValueWithoutNotify(0f);
            effectTriggerTimeField.SetEnabled(false);
        }
        if (effectNormalizedStartField != null) effectNormalizedStartField.SetValueWithoutNotify(0f);
        if (followTargetField != null) followTargetField.SetValueWithoutNotify(false);
        
        // Sound字段
        if (audioClipField != null) audioClipField.SetValueWithoutNotify(null);
        if (soundTriggerTimeField != null)
        {
            soundTriggerTimeField.SetValueWithoutNotify(0f);
            soundTriggerTimeField.SetEnabled(false);
        }
        if (soundNormalizedStartField != null) soundNormalizedStartField.SetValueWithoutNotify(0f);
        if (volumeField != null) volumeField.SetValueWithoutNotify(1f);
        
        // HitBox字段
        if (shapeTypeField != null) shapeTypeField.SetValueWithoutNotify(HitShapeType.Box);
        if (hitBoxTriggerTimeField != null)
        {
            hitBoxTriggerTimeField.SetValueWithoutNotify(0f);
            hitBoxTriggerTimeField.SetEnabled(false);
        }
        if (hitBoxNormalizedStartField != null) hitBoxNormalizedStartField.SetValueWithoutNotify(0f);
        if (hitBoxNormalizedEndField != null) hitBoxNormalizedEndField.SetValueWithoutNotify(1f);
    }
    
    // Clip属性字段值变化回调
    private void OnClipNameChanged(ChangeEvent<string> evt)
    {
        if (selectedClip == null) return;
        
        selectedClip.Name = evt.newValue;
        
        // 根据不同类型更新对应的数据
        if (selectedClip is AnimationClipItem animClipItem && animClipItem.SegmentData != null)
        {
            animClipItem.SegmentData.Name = evt.newValue;
        }
        else if (selectedClip is EffectClipItem effectClipItem && effectClipItem.EffectData != null)
        {
            effectClipItem.EffectData.Name = evt.newValue;
        }
        else if (selectedClip is SoundClipItem soundClipItem && soundClipItem.SoundData != null)
        {
            soundClipItem.SoundData.Name = evt.newValue;
        }
        
        MarkAssetDirty();
        RefreshTrackContent();
    }
    
    private void OnStartTimeChanged(ChangeEvent<float> evt)
    {
        if (selectedClip == null) return;
        
        selectedClip.StartTime = Mathf.Max(0f, evt.newValue);
        
        // 根据不同类型更新对应的数据
        if (selectedClip is AnimationClipItem animClipItem && animClipItem.SegmentData != null)
        {
            animClipItem.SegmentData.StartTime = selectedClip.StartTime;
            animClipItem.SegmentData.Duration = animClipItem.Duration;
        }
        
        MarkAssetDirty();
        RefreshTrackContent();
    }

    private void OnClipLengthChanged(ChangeEvent<float> evt)
    {
        if (selectedClip == null)
        {
            return;
        }

        float newLength = Mathf.Max(0f, evt.newValue);

        // Animation/Sound：只读，不应进入这里；做一层保护
        if (selectedClip.Type == TrackType.Animation || selectedClip.Type == TrackType.Sound)
        {
            if (clipLengthField != null)
            {
                clipLengthField.SetValueWithoutNotify(Mathf.Max(0f, selectedClip.Duration));
            }
            return;
        }

        selectedClip.Duration = newLength;

        // 同步到数据源
        if (selectedClip is EffectClipItem effectClipItem && effectClipItem.EffectData != null)
        {
            effectClipItem.EffectData.Length = newLength;
        }
        else if (selectedClip is HitBoxClipItem hitBoxClipItem && hitBoxClipItem.HitBoxData != null && config != null)
        {
            // HitBox 的 Length 为绝对秒数，需要反推到归一化 EndTime
            UpdateHitBoxClipTimes(hitBoxClipItem, hitBoxClipItem.StartTime, newLength);
        }

        MarkAssetDirty();
        RefreshTrackContent();
    }
    
    // Animation Clip回调
    private void OnAnimationClipChanged(ChangeEvent<Object> evt)
    {
        if (selectedClip is AnimationClipItem animClipItem && animClipItem.SegmentData != null)
        {
            if (animClipItem.SegmentData.AnimationClipTrans == null)
            {
                animClipItem.SegmentData.AnimationClipTrans = new ClipTransition();
            }
            
            animClipItem.SegmentData.AnimationClipTrans.Clip = evt.newValue as UnityEngine.AnimationClip;
            
            // 更新 Duration / Length（时间轴时长 = Clip.length / Speed）
            if (animClipItem.SegmentData.AnimationClipTrans.Clip != null)
            {
                float clipLen = animClipItem.SegmentData.AnimationClipTrans.Clip.length;
                float speed = Mathf.Max(0.01f, animClipItem.SegmentData.AnimationClipTrans.Speed);
                float duration = clipLen / speed;
                animClipItem.SegmentData.Duration = duration;
                animClipItem.SegmentData.ClipLength = clipLen;
                animClipItem.Duration = duration;
                animClipItem.Frame = Mathf.RoundToInt(duration * 60f);
            }
            
            MarkAssetDirty();
            RefreshTrackContent();
            
            // 更新帧数显示（因为动画Clip改变了）
            if (selectedClip == animClipItem)
            {
                UpdateAnimationClipProperties(animClipItem);
            }
        }
    }
    
    private void OnSpeedChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is AnimationClipItem animClipItem && animClipItem.SegmentData != null)
        {
            var speed = Mathf.Max(0.01f, evt.newValue);
            
            if (animClipItem.SegmentData.AnimationClipTrans != null)
            {
                animClipItem.SegmentData.AnimationClipTrans.Speed = speed;
                
                // 更新 Duration / Length（时间轴时长 = Clip.length / Speed）
                if (animClipItem.SegmentData.AnimationClipTrans.Clip != null)
                {
                    float clipLen = animClipItem.SegmentData.AnimationClipTrans.Clip.length;
                    float duration = clipLen / speed;
                    animClipItem.SegmentData.Duration = duration;
                    animClipItem.Duration = duration;
                    animClipItem.Frame = Mathf.RoundToInt(duration * 60f);
                }
            }
            
            MarkAssetDirty();
            RefreshTrackContent();
            
            // 更新帧数显示（因为速度改变了）
            if (selectedClip == animClipItem)
            {
                UpdateAnimationClipProperties(animClipItem);
            }
        }
    }
    
    private void OnFadeDurationChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is AnimationClipItem animClipItem && animClipItem.SegmentData != null)
        {
            if (animClipItem.SegmentData.AnimationClipTrans == null)
            {
                animClipItem.SegmentData.AnimationClipTrans = new ClipTransition();
            }
            
            animClipItem.SegmentData.AnimationClipTrans.FadeDuration = Mathf.Max(0f, evt.newValue);
            MarkAssetDirty();
        }
    }
    
    // Effect Clip回调
    private void OnEffectPrefabChanged(ChangeEvent<Object> evt)
    {
        if (selectedClip is EffectClipItem effectClipItem && effectClipItem.EffectData != null)
        {
            effectClipItem.EffectData.Prefab = evt.newValue as UnityEngine.GameObject;
            MarkAssetDirty();
        }
    }
    
    
    // 特效时长通过通用 Length 字段编辑（ClipLengthField）
    private void OnFollowTargetChanged(ChangeEvent<bool> evt)
    {
        if (selectedClip is EffectClipItem effectClipItem && effectClipItem.EffectData != null)
        {
            effectClipItem.EffectData.FollowTarget = evt.newValue;
            MarkAssetDirty();
        }
    }

    private void OnEffectNormalizedStartChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is EffectClipItem effectClipItem && effectClipItem.EffectData != null)
        {
            effectClipItem.EffectData.NormalizedStart = Mathf.Clamp01(evt.newValue);
            // 更新clip的开始时间
            UpdateClipStartTimeFromNormalizedStart(effectClipItem);
            // 标记config配置为脏，确保保存
            MarkAssetDirty();
            // 刷新轨道内容，因为clip位置发生了变化
            RefreshTrackContent();
            // 刷新选择高亮
            RefreshSelectionHighlight();
        }
    }
    
    // Sound Clip回调
    private void OnAudioClipChanged(ChangeEvent<Object> evt)
    {
        if (selectedClip is SoundClipItem soundClipItem && soundClipItem.SoundData != null)
        {
            var audioClip = evt.newValue as UnityEngine.AudioClip;
            soundClipItem.SoundData.Clip = audioClip;
            
            // 更新clip的长度（根据音频时长）
            if (audioClip != null)
            {
                soundClipItem.Duration = audioClip.length;
                soundClipItem.Frame = Mathf.RoundToInt(audioClip.length * 60f);
            }
            else
            {
                soundClipItem.Duration = 1f;
                soundClipItem.Frame = 60;
            }
            
            // 更新右侧面板的时长显示
            if (clipLengthField != null)
            {
                clipLengthField.SetValueWithoutNotify(soundClipItem.Duration);
            }
            if (frameField != null)
            {
                frameField.SetValueWithoutNotify(soundClipItem.Frame);
            }
            
            MarkAssetDirty();
            RefreshTrackContent();
        }
    }
    
    // 特效/音效触发时间
    private void OnVolumeChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is SoundClipItem soundClipItem && soundClipItem.SoundData != null)
        {
            soundClipItem.SoundData.Volume = Mathf.Clamp01(evt.newValue);
            MarkAssetDirty();
        }
    }

    private void OnSoundNormalizedStartChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is SoundClipItem soundClipItem && soundClipItem.SoundData != null)
        {
            soundClipItem.SoundData.NormalizedStart = Mathf.Clamp01(evt.newValue);
            // 更新clip的开始时间
            UpdateClipStartTimeFromNormalizedStart(soundClipItem);
            // 标记config配置为脏，确保保存
            MarkAssetDirty();
            // 刷新轨道内容，因为clip位置发生了变化
            RefreshTrackContent();
            // 刷新选择高亮
            RefreshSelectionHighlight();
        }
    }
    
    // HitBox Clip回调
    private void OnShapeTypeChanged(ChangeEvent<Enum> evt)
    {
        if (selectedClip is HitBoxClipItem hitBoxClipItem && hitBoxClipItem.HitBoxData != null)
        {
            if (evt.newValue != null)
            {
                hitBoxClipItem.HitBoxData.ShapeType = (HitShapeType)evt.newValue;
                hitBoxClipItem.Name = hitBoxClipItem.HitBoxData.ShapeType.ToString();
                MarkAssetDirty();
                RefreshTrackContent();
            }
        }
    }

    private void OnHitBoxNormalizedStartChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is HitBoxClipItem hitBoxClipItem && hitBoxClipItem.HitBoxData != null)
        {
            float newValue = Mathf.Clamp01(evt.newValue);
            // 确保结束时间永远大于开始时间
            if (newValue >= hitBoxClipItem.HitBoxData.NormalizedEnd)
            {
                hitBoxClipItem.HitBoxData.NormalizedEnd = Mathf.Min(1f, newValue + 0.01f);
                // 更新UI显示
                if (hitBoxNormalizedEndField != null)
                {
                    hitBoxNormalizedEndField.SetValueWithoutNotify(hitBoxClipItem.HitBoxData.NormalizedEnd);
                }
            }

            hitBoxClipItem.HitBoxData.NormalizedStart = newValue;
            // 更新clip的开始时间和持续时间
            UpdateHitBoxClipFromNormalizedTimes(hitBoxClipItem);
            // 标记config配置为脏，确保保存
            MarkAssetDirty();
            // 刷新轨道内容，因为clip位置和时长发生了变化
            RefreshTrackContent();
            // 刷新选择高亮
            RefreshSelectionHighlight();
        }
    }

    private void OnHitBoxNormalizedEndChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is HitBoxClipItem hitBoxClipItem && hitBoxClipItem.HitBoxData != null)
        {
            float newValue = Mathf.Clamp01(evt.newValue);
            // 确保结束时间永远大于开始时间
            if (newValue <= hitBoxClipItem.HitBoxData.NormalizedStart)
            {
                hitBoxClipItem.HitBoxData.NormalizedStart = Mathf.Max(0f, newValue - 0.01f);
                // 更新UI显示
                if (hitBoxNormalizedStartField != null)
                {
                    hitBoxNormalizedStartField.SetValueWithoutNotify(hitBoxClipItem.HitBoxData.NormalizedStart);
                }
            }

            hitBoxClipItem.HitBoxData.NormalizedEnd = newValue;
            // 更新clip的开始时间和持续时间
            UpdateHitBoxClipFromNormalizedTimes(hitBoxClipItem);
            // 标记config配置为脏，确保保存
            MarkAssetDirty();
            // 刷新轨道内容，因为clip位置和时长发生了变化
            RefreshTrackContent();
            // 刷新选择高亮
            RefreshSelectionHighlight();
        }
    }

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
                float animationLength = segment.Duration > 0f ? segment.Duration : 2f;
                if (animationLength <= 0f && segment.AnimationClipTrans != null && segment.AnimationClipTrans.Clip != null)
                {
                    animationLength = segment.AnimationClipTrans.Clip.length;
                }

                // 计算绝对开始时间：segment开始时间 + (归一化时间 × 动画长度)
                clipItem.StartTime = segment.StartTime + (effectClip.EffectData.NormalizedStart * animationLength);
                break;
            }
            else if (clipItem is SoundClipItem soundClip && segment.SoundEffects.Contains(soundClip.SoundData))
            {
                // 获取动画片段长度
                float animationLength = segment.Duration > 0f ? segment.Duration : 2f;
                if (animationLength <= 0f && segment.AnimationClipTrans != null && segment.AnimationClipTrans.Clip != null)
                {
                    animationLength = segment.AnimationClipTrans.Clip.length;
                }

                // 计算绝对开始时间：segment开始时间 + (归一化时间 × 动画长度)
                clipItem.StartTime = segment.StartTime + (soundClip.SoundData.NormalizedStart * animationLength);
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
                float animationLength = segment.Duration > 0f ? segment.Duration : 2f;
                if (animationLength <= 0f && segment.AnimationClipTrans != null && segment.AnimationClipTrans.Clip != null)
                {
                    animationLength = segment.AnimationClipTrans.Clip.length;
                }

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
                clipItem.Frame = Mathf.RoundToInt(clipItem.Duration * 60f);
                break;
            }
        }
    }

    #endregion
}
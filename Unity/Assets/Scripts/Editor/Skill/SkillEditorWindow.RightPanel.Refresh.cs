using System;
using Animancer;
using ET;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public partial class SkillEditorWindow : EditorWindow
{
    #region RightPanel - 刷新入口（轨道/Clip选择时驱动右侧 UI）

    // 隐藏所有Clip字段组
    private void HideAllClipFields()
    {
        if (animationFields != null) animationFields.style.display = DisplayStyle.None;
        if (effectFields != null) effectFields.style.display = DisplayStyle.None;
        if (soundFields != null) soundFields.style.display = DisplayStyle.None;
        if (hitBoxFields != null) hitBoxFields.style.display = DisplayStyle.None;
        if (activeFields != null) activeFields.style.display = DisplayStyle.None;
        if (animationActionButtons != null) animationActionButtons.style.display = DisplayStyle.None;

        // 清除按钮的类型样式类
        var buttons = new[] { addEffectButton, addSoundButton, addHitboxButton, addActiveButton };
        var classesToRemove = new[] { "action-button", "type-effect", "type-sound", "type-hitbox", "type-animation", "type-active" };
        
        foreach (var button in buttons)
        {
            if (button == null) continue;
            foreach (var className in classesToRemove)
            {
                button.RemoveFromClassList(className);
            }
        }
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
            UpdateGlobalConfigFields();
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
        else if (track is ActiveTrack activeTrack)
        {
            clipCount = activeTrack.ClipList.Count;
            foreach (var clip in activeTrack.ClipList)
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

        // 同步全局参数显示（与轨道选择无关）
        UpdateGlobalConfigFields();

        RefreshSelectionHighlight();
    }

    private void UpdateGlobalConfigFields()
    {
        bool hasConfig = config != null;

        if (inputBufferWindowMsField != null)
        {
            inputBufferWindowMsField.SetEnabled(hasConfig);
            inputBufferWindowMsField.SetValueWithoutNotify(hasConfig ? Mathf.Max(0, config.InputBufferWindowMs) : 0);
        }

        if (defaultHitStopMsField != null)
        {
            defaultHitStopMsField.SetEnabled(hasConfig);
            defaultHitStopMsField.SetValueWithoutNotify(hasConfig ? Mathf.Max(0, config.DefaultHitStopMs) : 0);
        }

        if (recoveryHoldMsField != null)
        {
            recoveryHoldMsField.SetEnabled(hasConfig);
            recoveryHoldMsField.SetValueWithoutNotify(hasConfig ? Mathf.Max(0, config.RecoveryHoldMs) : 0);
        }
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
            index = (clip, track) switch
            {
                (AnimationClipItem a, AnimationTrack at) => at.ClipList.IndexOf(a),
                (EffectClipItem e, EffectTrack et) => et.ClipList.IndexOf(e),
                (SoundClipItem s, SoundTrack st) => st.ClipList.IndexOf(s),
                (HitBoxClipItem h, HitBoxTrack ht) => ht.ClipList.IndexOf(h),
                (ActiveClipItem ac, ActiveTrack act) => act.ClipList.IndexOf(ac),
                _ => -1
            };
            if (index >= 0) break;
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
                TrackType.Active => "显隐片段属性",
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
        else if (clip is ActiveClipItem activeClipItem)
        {
            UpdateActiveClipProperties(activeClipItem);
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
        if (inputBufferStartField != null) inputBufferStartField.SetValueWithoutNotify(0.5f);
        if (cancelableTimeField != null) cancelableTimeField.SetValueWithoutNotify(0.4f);
        if (animationEndField != null) animationEndField.SetValueWithoutNotify(0.9f);
        if (comboTimeoutOffsetMsField != null) comboTimeoutOffsetMsField.SetValueWithoutNotify(200);
        if (segmentTimeoutMsPreviewField != null)
        {
            segmentTimeoutMsPreviewField.SetValueWithoutNotify(0);
            segmentTimeoutMsPreviewField.SetEnabled(false);
        }

        // Movement字段
        if (movementEnableField != null) movementEnableField.SetValueWithoutNotify(false);
        if (movementDistanceField != null) movementDistanceField.SetValueWithoutNotify(0f);
        if (movementStartField != null) movementStartField.SetValueWithoutNotify(0f);
        if (movementEndField != null) movementEndField.SetValueWithoutNotify(0.3f);
        if (movementCurveField != null) movementCurveField.SetValueWithoutNotify(AnimationCurve.EaseInOut(0, 0, 1, 1));
        if (movementTrackTargetField != null) movementTrackTargetField.SetValueWithoutNotify(false);
        if (movementTrackRangeField != null) movementTrackRangeField.SetValueWithoutNotify(5f);

        // Effect字段
        if (effectPrefabField != null) effectPrefabField.SetValueWithoutNotify(null);
        if (effectIsAnimationField != null) effectIsAnimationField.SetValueWithoutNotify(false);
        if (effectTriggerTimeField != null)
        {
            effectTriggerTimeField.SetValueWithoutNotify(0f);
            effectTriggerTimeField.SetEnabled(false);
        }
        if (effectNormalizedStartField != null) effectNormalizedStartField.SetValueWithoutNotify(0f);
        if (followTargetField != null) followTargetField.SetValueWithoutNotify(false);
        if (effectOffsetField != null)
        {
            effectOffsetField.SetValueWithoutNotify(Vector3.zero);
        }
        if (effectRotationField != null)
        {
            effectRotationField.SetValueWithoutNotify(Vector3.zero);
        }

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
        if (hitBoxTriggerFrameField != null)
        {
            hitBoxTriggerFrameField.SetValueWithoutNotify(0);
            hitBoxTriggerFrameField.SetEnabled(false);
        }
        if (hitBoxNormalizedStartField != null) hitBoxNormalizedStartField.SetValueWithoutNotify(0f);
        if (hitBoxNormalizedEndField != null) hitBoxNormalizedEndField.SetValueWithoutNotify(1f);
        if (hitBoxOffsetField != null) hitBoxOffsetField.SetValueWithoutNotify(Vector3.zero);
        if (hitBoxRotationField != null) hitBoxRotationField.SetValueWithoutNotify(Vector3.zero);
        if (hitBoxSizeField != null) hitBoxSizeField.SetValueWithoutNotify(Vector3.one);

        // Active字段
        if (activeTargetObjectField != null) activeTargetObjectField.SetValueWithoutNotify(null);
        if (activeRelativePathField != null) activeRelativePathField.SetValueWithoutNotify(string.Empty);

        // HitEffectData / HitFeedbackData
        if (hitEffectDamageMultiplierField != null) hitEffectDamageMultiplierField.SetValueWithoutNotify(1f);
        if (hitEffectReactionField != null) hitEffectReactionField.SetValueWithoutNotify(HitReactionType.Light);
        if (hitEffectKnockbackForceField != null) hitEffectKnockbackForceField.SetValueWithoutNotify(0f);
        if (hitEffectKnockupForceField != null) hitEffectKnockupForceField.SetValueWithoutNotify(0f);
        if (hitEffectHitStunMsField != null) hitEffectHitStunMsField.SetValueWithoutNotify(200);
        if (hitEffectTargetStateField != null) hitEffectTargetStateField.SetValueWithoutNotify(TargetStateMask.Any);

        if (hitFeedbackShakeIntensityField != null) hitFeedbackShakeIntensityField.SetValueWithoutNotify(0f);
        if (hitFeedbackShakeDurationField != null) hitFeedbackShakeDurationField.SetValueWithoutNotify(0f);
        if (hitFeedbackHitStopMsField != null) hitFeedbackHitStopMsField.SetValueWithoutNotify(0);
        if (hitFeedbackTimeScaleField != null) hitFeedbackTimeScaleField.SetValueWithoutNotify(1f);
        if (hitFeedbackTimeScaleDurationMsField != null) hitFeedbackTimeScaleDurationMsField.SetValueWithoutNotify(0);
    }

    #endregion
}


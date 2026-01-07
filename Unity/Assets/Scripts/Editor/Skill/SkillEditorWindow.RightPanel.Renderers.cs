using System;
using Animancer;
using ET;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public partial class SkillEditorWindow : EditorWindow
{
    #region RightPanel - Clip属性渲染（按类型：Animation/Effect/Sound/HitBox）

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

        // TimeWindowData（归一化 0-1）
        var tw = segmentData.TimeWindow ??= new TimeWindowData();
        if (inputBufferStartField != null)
        {
            inputBufferStartField.SetValueWithoutNotify(tw.InputBufferStart);
        }
        if (cancelableTimeField != null)
        {
            cancelableTimeField.SetValueWithoutNotify(tw.CancelableTime);
        }
        if (animationEndField != null)
        {
            animationEndField.SetValueWithoutNotify(tw.AnimationEnd);
        }

        // 段超时偏移（ms）& 预览（ms）
        if (comboTimeoutOffsetMsField != null)
        {
            comboTimeoutOffsetMsField.SetValueWithoutNotify(Mathf.Max(0, segmentData.ComboTimeoutOffsetMs));
        }
        if (segmentTimeoutMsPreviewField != null)
        {
            int durMs = Mathf.RoundToInt(Mathf.Max(0f, segmentData.Duration) * 1000f);
            int total = durMs + Mathf.Max(0, segmentData.ComboTimeoutOffsetMs);
            segmentTimeoutMsPreviewField.SetValueWithoutNotify(Mathf.Max(0, total));
            segmentTimeoutMsPreviewField.SetEnabled(false);
        }

        // 总帧数由通用字段统一计算（Length * 60）

        // 设置按钮颜色样式 - 清除之前的类型样式
        if (addEffectButton != null)
        {
            addEffectButton.RemoveFromClassList("type-effect");
            addEffectButton.RemoveFromClassList("type-sound");
            addEffectButton.RemoveFromClassList("type-hitbox");
            addEffectButton.RemoveFromClassList("type-animation");
            addEffectButton.AddToClassList("action-button");
            addEffectButton.AddToClassList("type-effect");
        }

        if (addSoundButton != null)
        {
            addSoundButton.RemoveFromClassList("type-effect");
            addSoundButton.RemoveFromClassList("type-sound");
            addSoundButton.RemoveFromClassList("type-hitbox");
            addSoundButton.RemoveFromClassList("type-animation");
            addSoundButton.AddToClassList("action-button");
            addSoundButton.AddToClassList("type-sound");
        }

        if (addHitboxButton != null)
        {
            addHitboxButton.RemoveFromClassList("type-effect");
            addHitboxButton.RemoveFromClassList("type-sound");
            addHitboxButton.RemoveFromClassList("type-hitbox");
            addHitboxButton.RemoveFromClassList("type-animation");
            addHitboxButton.AddToClassList("action-button");
            addHitboxButton.AddToClassList("type-hitbox");
        }
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

        // Offset / Rotation / Size
        if (hitBoxOffsetField != null)
        {
            hitBoxOffsetField.SetValueWithoutNotify(hitBoxData.Offset);
        }
        if (hitBoxRotationField != null)
        {
            hitBoxRotationField.SetValueWithoutNotify(hitBoxData.RotationEuler);
        }
        if (hitBoxSizeField != null)
        {
            hitBoxSizeField.SetValueWithoutNotify(hitBoxData.Size);
        }

        // HitEffectData / HitFeedbackData（命中效果/反馈）
        var effect = hitBoxData.Effect ??= new HitEffectData();
        if (hitEffectDamageMultiplierField != null)
        {
            hitEffectDamageMultiplierField.SetValueWithoutNotify(effect.DamageMultiplier);
        }
        if (hitEffectReactionField != null)
        {
            hitEffectReactionField.SetValueWithoutNotify(effect.HitReaction);
        }
        if (hitEffectKnockbackForceField != null)
        {
            hitEffectKnockbackForceField.SetValueWithoutNotify(effect.KnockbackForce);
        }
        if (hitEffectKnockupForceField != null)
        {
            hitEffectKnockupForceField.SetValueWithoutNotify(effect.KnockupForce);
        }
        if (hitEffectHitStunMsField != null)
        {
            hitEffectHitStunMsField.SetValueWithoutNotify(effect.HitStunMs);
        }
        if (hitEffectTargetStateField != null)
        {
            hitEffectTargetStateField.SetValueWithoutNotify(effect.TargetState);
        }

        var feedback = hitBoxData.Feedback ??= new HitFeedbackData();
        if (hitFeedbackShakeIntensityField != null)
        {
            hitFeedbackShakeIntensityField.SetValueWithoutNotify(feedback.ScreenShakeIntensity);
        }
        if (hitFeedbackShakeDurationField != null)
        {
            hitFeedbackShakeDurationField.SetValueWithoutNotify(feedback.ScreenShakeDuration);
        }
        if (hitFeedbackHitStopMsField != null)
        {
            hitFeedbackHitStopMsField.SetValueWithoutNotify(feedback.HitStopMs);
        }
        if (hitFeedbackTimeScaleField != null)
        {
            hitFeedbackTimeScaleField.SetValueWithoutNotify(feedback.TimeScale);
        }
        if (hitFeedbackTimeScaleDurationMsField != null)
        {
            hitFeedbackTimeScaleDurationMsField.SetValueWithoutNotify(feedback.TimeScaleDurationMs);
        }
    }

    #endregion
}


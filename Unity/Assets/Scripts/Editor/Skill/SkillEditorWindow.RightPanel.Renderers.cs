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
            int durMs = Mathf.RoundToInt(Mathf.Max(0f, segmentData.Duration * segmentData.TimeWindow.AnimationEnd) * 1000f);
            int total = durMs + Mathf.Max(0, segmentData.ComboTimeoutOffsetMs);
            segmentTimeoutMsPreviewField.SetValueWithoutNotify(Mathf.Max(0, total));
            segmentTimeoutMsPreviewField.SetEnabled(false);
        }

        // Movement 配置
        var movement = segmentData.Movement ??= new AttackMovementData();
        bool movementEnabled = movement.EnableMovement;
        
        if (movementEnableField != null)
        {
            movementEnableField.SetValueWithoutNotify(movementEnabled);
        }
        if (movementDistanceField != null)
        {
            movementDistanceField.SetValueWithoutNotify(movement.Distance);
            movementDistanceField.SetEnabled(movementEnabled);
        }
        if (movementStartField != null)
        {
            movementStartField.SetValueWithoutNotify(movement.NormalizedStart);
            movementStartField.SetEnabled(movementEnabled);
        }
        if (movementEndField != null)
        {
            movementEndField.SetValueWithoutNotify(movement.NormalizedEnd);
            movementEndField.SetEnabled(movementEnabled);
        }
        if (movementCurveField != null)
        {
            movementCurveField.SetValueWithoutNotify(movement.MoveCurve);
            movementCurveField.SetEnabled(movementEnabled);
        }
        if (movementTrackTargetField != null)
        {
            movementTrackTargetField.SetValueWithoutNotify(movement.TrackTarget);
            movementTrackTargetField.SetEnabled(movementEnabled);
        }
        if (movementTrackRangeField != null)
        {
            movementTrackRangeField.SetValueWithoutNotify(movement.TrackRange);
            movementTrackRangeField.SetEnabled(movementEnabled && movement.TrackTarget);
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

        if (addActiveButton != null)
        {
            addActiveButton.RemoveFromClassList("type-effect");
            addActiveButton.RemoveFromClassList("type-sound");
            addActiveButton.RemoveFromClassList("type-hitbox");
            addActiveButton.RemoveFromClassList("type-animation");
            addActiveButton.RemoveFromClassList("type-active");
            addActiveButton.AddToClassList("action-button");
            addActiveButton.AddToClassList("type-active");
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

        // Best practice：选中时若 Prefab 自带 legacy Animation，则把 clip.length 写回到 config 的 Length（真源），并同步 UI clip 时长。
        //（避免 Length 还停留在默认 2s 导致预览窗口/时间轴不匹配）
        bool hasAnimation = false;
        if (effectData.Prefab != null)
        {
            var anim = effectData.Prefab.GetComponentInChildren<Animation>(true);
            hasAnimation = anim != null;
            var legacyClip = anim != null ? GetFirstLegacyAnimationClip(anim) : null;
            if (legacyClip != null)
            {
                float len = Mathf.Max(0.01f, legacyClip.length);
                if (Mathf.Abs(effectData.Length - len) > 0.0001f)
                {
                    effectData.Length = len;
                    clipItem.Duration = len;
                    clipItem.Frame = Mathf.RoundToInt(len * 60f);
                    MarkAssetDirty();
                    RefreshTrackContent();
                }
            }
        }
        if (effectData.IsAnimation != hasAnimation)
        {
            effectData.IsAnimation = hasAnimation;
            MarkAssetDirty();
        }
        if (effectIsAnimationField != null)
        {
            effectIsAnimationField.SetValueWithoutNotify(effectData.IsAnimation);
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
                    float animationLength = GetSegmentAnimationLength(segment);

                    // 计算绝对触发时间：segment开始时间 + (归一化时间 × 动画长度)
                    absoluteTriggerTime = segment.StartTime + (effectData.NormalizedStart * animationLength);
                    break;
                }
            }
            effectTriggerTimeField.SetValueWithoutNotify(absoluteTriggerTime);
            effectTriggerTimeField.SetEnabled(false);
        }

        if (followTargetField != null)
        {
            followTargetField.SetValueWithoutNotify(effectData.FollowTarget);
        }

        // Offset（只读回显）
        if (effectOffsetField != null)
        {
            effectOffsetField.SetValueWithoutNotify(effectData.Offset);
        }
        if (effectRotationField != null)
        {
            effectRotationField.SetValueWithoutNotify(effectData.RotationEuler);
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
                    float animationLength = GetSegmentAnimationLength(segment);

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
        // 触发帧（只读）：关键帧 HitBox（Start==End）时用于表达“落在哪一帧”
        if (hitBoxTriggerFrameField != null)
        {
            int frame = Mathf.Max(0, Mathf.RoundToInt(clipItem.StartTime * FRAMES_PER_SECOND));
            hitBoxTriggerFrameField.SetValueWithoutNotify(frame);
            hitBoxTriggerFrameField.SetEnabled(false);
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
        var effect = hitBoxData.Effect;
        if (hitEffectDamageMultiplierField != null)
        {
            hitEffectDamageMultiplierField.SetValueWithoutNotify(Mathf.Max(0f, effect.DamageMultiplier));
        }
        if (hitEffectReactionField != null)
        {
            hitEffectReactionField.SetValueWithoutNotify(effect.HitReaction);
        }
        if (hitEffectPriorityField != null)
        {
            hitEffectPriorityField.SetValueWithoutNotify(effect.HitStrength);
        }
        if (hitMotionTypeField != null)
        {
            hitMotionTypeField.SetValueWithoutNotify(effect.HitMotion.MotionType);
        }
        if (hitMotionForceField != null)
        {
            hitMotionForceField.SetValueWithoutNotify(Mathf.Max(0f, effect.HitMotion.Force));
        }
        if (hitMotionDurationMsField != null)
        {
            hitMotionDurationMsField.SetValueWithoutNotify(Mathf.Max(0, effect.HitMotion.DurationMs));
        }
        if (hitMotionCurveField != null)
        {
            hitMotionCurveField.SetValueWithoutNotify(effect.HitMotion.MotionCurve ?? AnimationCurve.Linear(0, 1, 1, 0));
        }
        if (hitEffectHitStunMsField != null)
        {
            hitEffectHitStunMsField.SetValueWithoutNotify(Mathf.Max(0, effect.HitStunMs));
        }
        if (hitEffectTargetStateField != null)
        {
            var normalized = (TargetStateMask)((int)effect.TargetStates & (int)TargetStateMask.Any);
            if (normalized != effect.TargetStates)
            {
                effect.TargetStates = normalized;
                hitBoxData.Effect = effect;
                MarkAssetDirty();
            }
            hitEffectTargetStateField.SetValueWithoutNotify(normalized);
        }

        var feedback = hitBoxData.Feedback;
        if (hitFeedbackShakeIntensityField != null)
        {
            hitFeedbackShakeIntensityField.SetValueWithoutNotify(Mathf.Clamp01(feedback.ScreenShakeIntensity));
        }
        if (hitFeedbackShakeDurationMsField != null)
        {
            hitFeedbackShakeDurationMsField.SetValueWithoutNotify(Mathf.Max(0, feedback.ScreenShakeDurationMs));
        }
        if (hitFeedbackAttackerHitStopMsField != null)
        {
            hitFeedbackAttackerHitStopMsField.SetValueWithoutNotify(Mathf.Max(-1, feedback.AttackerHitStopMs));
        }
        if (hitFeedbackVictimHitStopMsField != null)
        {
            hitFeedbackVictimHitStopMsField.SetValueWithoutNotify(Mathf.Max(-1, feedback.VictimHitStopMs));
        }
        if (hitFeedbackTimeScaleField != null)
        {
            hitFeedbackTimeScaleField.SetValueWithoutNotify(Mathf.Max(0f, feedback.TimeScale));
        }
        if (hitFeedbackTimeScaleDurationMsField != null)
        {
            hitFeedbackTimeScaleDurationMsField.SetValueWithoutNotify(Mathf.Max(0, feedback.TimeScaleDurationMs));
        }
    }

    private void UpdateActiveClipProperties(ActiveClipItem clipItem)
    {
        if (activeFields != null) activeFields.style.display = DisplayStyle.Flex;

        var data = clipItem.ActiveData;
        if (data == null)
        {
            Debug.LogWarning("ActiveData为null，无法更新Active Clip属性");
            return;
        }

        // 选中 Active 时：尽量根据当前层级刷新相对路径（Hierarchy 里可能被改名/改层级）。
        // 约束：配置里只存 RelativePath，没有稳定 GUID/InstanceId，因此只能做 best-effort：
        // - 优先用当前 RelativePath 在 root 下 Find
        // - 找不到时尝试用 Name（通常等于目标 GameObject.name）做唯一匹配
        // - 若能定位到目标，则重算路径并写回配置（MarkDirty）
        string displayPath = data.RelativePath ?? string.Empty;
        var rootGo = selectObj != null ? selectObj.value as GameObject : null;
        Transform root = rootGo != null ? rootGo.transform : null;
        if (root != null)
        {
            Transform target = null;
            if (!string.IsNullOrEmpty(displayPath))
            {
                target = root.Find(displayPath);
            }

            if (target == null && !string.IsNullOrEmpty(data.Name))
            {
                target = FindUniqueChildByName(root, data.Name);
            }

            if (target != null)
            {
                string newPath = GetRelativePath(root, target);
                if (!string.IsNullOrEmpty(newPath))
                {
                    displayPath = newPath;
                    if (!string.Equals(data.RelativePath, newPath, StringComparison.Ordinal))
                    {
                        data.RelativePath = newPath;
                        MarkAssetDirty();
                    }
                }
            }

            if (activeTargetObjectField != null)
            {
                activeTargetObjectField.SetValueWithoutNotify(target != null ? target.gameObject : null);
            }
        }
        else
        {
            // 没有 SelectObj 根：只能显示路径；对象字段置空
            if (activeTargetObjectField != null)
            {
                activeTargetObjectField.SetValueWithoutNotify(null);
            }
        }

        if (activeRelativePathField != null)
        {
            activeRelativePathField.SetValueWithoutNotify(displayPath);
            activeRelativePathField.SetEnabled(false); // 只读
        }
    }

    /// <summary>
    /// 在 root 子树里按 name 查找唯一匹配；若 0 或 >1 个匹配，则返回 null（避免误绑）。
    /// </summary>
    private static Transform FindUniqueChildByName(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name))
        {
            return null;
        }

        Transform found = null;
        int count = 0;

        var queue = new Queue<Transform>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var t = queue.Dequeue();
            if (t == null)
            {
                continue;
            }

            // 不把 root 自己算进来（避免 name 恰好等于 root）
            if (!ReferenceEquals(t, root) && string.Equals(t.name, name, StringComparison.Ordinal))
            {
                found = t;
                count++;
                if (count > 1)
                {
                    return null;
                }
            }

            for (int i = 0; i < t.childCount; ++i)
            {
                queue.Enqueue(t.GetChild(i));
            }
        }

        return count == 1 ? found : null;
    }

    #endregion
}


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
    #region RightPanel - 字段回调

    #region RightPanel - 字段回调（通用：Name/StartTime/Length）

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

    #endregion

    #region RightPanel - 字段回调（AnimationClip）

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

    private void OnInputBufferStartChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is AnimationClipItem animClipItem && animClipItem.SegmentData != null)
        {
            var tw = animClipItem.SegmentData.TimeWindow ??= new TimeWindowData();
            tw.InputBufferStart = Mathf.Clamp01(evt.newValue);
            MarkAssetDirty();
        }
    }

    private void OnCancelableTimeChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is AnimationClipItem animClipItem && animClipItem.SegmentData != null)
        {
            var tw = animClipItem.SegmentData.TimeWindow ??= new TimeWindowData();
            tw.CancelableTime = Mathf.Clamp01(evt.newValue);
            MarkAssetDirty();
        }
    }

    private void OnAnimationEndChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is AnimationClipItem animClipItem && animClipItem.SegmentData != null)
        {
            float newEnd = Mathf.Clamp01(evt.newValue);
            var tw = animClipItem.SegmentData.TimeWindow ??= new TimeWindowData();
            tw.AnimationEnd = newEnd;

            // AnimationEnd <-> 重叠：把下一段的开始时间对齐到“上一段的结束阈值”
            SyncNextAnimationClipStartTimeFromAnimationEnd(animClipItem);

            MarkAssetDirty();
            ApplyViewModeAndRefresh();
        }
    }

    private void OnComboTimeoutOffsetMsChanged(ChangeEvent<int> evt)
    {
        if (selectedClip is AnimationClipItem animClipItem && animClipItem.SegmentData != null)
        {
            int v = Mathf.Max(0, evt.newValue);
            animClipItem.SegmentData.ComboTimeoutOffsetMs = v;

            // 更新预览：本段超时 = Duration(ms) + Offset(ms)
            if (segmentTimeoutMsPreviewField != null)
            {
                int durMs = Mathf.RoundToInt(Mathf.Max(0f, animClipItem.SegmentData.Duration) * 1000f);
                segmentTimeoutMsPreviewField.SetValueWithoutNotify(durMs + v);
            }

            MarkAssetDirty();
        }
    }

    #endregion

    #region RightPanel - 字段回调（EffectClip）

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

    #endregion

    #region RightPanel - 字段回调（SoundClip）

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

    #endregion

    #region RightPanel - 字段回调（HitBoxClip）

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

    private void OnHitBoxOffsetChanged(ChangeEvent<Vector3> evt)
    {
        if (selectedClip is HitBoxClipItem hitBoxClipItem && hitBoxClipItem.HitBoxData != null)
        {
            hitBoxClipItem.HitBoxData.Offset = evt.newValue;
            MarkAssetDirty();
            SceneView.RepaintAll();
        }
    }

    private void OnHitBoxRotationChanged(ChangeEvent<Vector3> evt)
    {
        if (selectedClip is HitBoxClipItem hitBoxClipItem && hitBoxClipItem.HitBoxData != null)
        {
            hitBoxClipItem.HitBoxData.RotationEuler = evt.newValue;
            MarkAssetDirty();
            SceneView.RepaintAll();
        }
    }

    private void OnHitBoxSizeChanged(ChangeEvent<Vector3> evt)
    {
        if (selectedClip is HitBoxClipItem hitBoxClipItem && hitBoxClipItem.HitBoxData != null)
        {
            Vector3 v = evt.newValue;
            v.x = Mathf.Max(0.001f, v.x);
            v.y = Mathf.Max(0.001f, v.y);
            v.z = Mathf.Max(0.001f, v.z);
            hitBoxClipItem.HitBoxData.Size = v;
            MarkAssetDirty();
            SceneView.RepaintAll();
        }
    }

    #endregion

    #region RightPanel - 字段回调（HitEffectData / HitFeedbackData）

    private void OnHitEffectDamageMultiplierChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is not HitBoxClipItem hitBoxClipItem || hitBoxClipItem.HitBoxData == null)
        {
            return;
        }

        hitBoxClipItem.HitBoxData.Effect ??= new HitEffectData();
        hitBoxClipItem.HitBoxData.Effect.DamageMultiplier = Mathf.Max(0f, evt.newValue);
        MarkAssetDirty();
    }

    private void OnHitEffectReactionChanged(ChangeEvent<Enum> evt)
    {
        if (selectedClip is not HitBoxClipItem hitBoxClipItem || hitBoxClipItem.HitBoxData == null)
        {
            return;
        }

        if (evt.newValue == null)
        {
            return;
        }

        hitBoxClipItem.HitBoxData.Effect ??= new HitEffectData();
        hitBoxClipItem.HitBoxData.Effect.HitReaction = (HitReactionType)evt.newValue;
        MarkAssetDirty();
    }

    private void OnHitEffectKnockbackForceChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is not HitBoxClipItem hitBoxClipItem || hitBoxClipItem.HitBoxData == null)
        {
            return;
        }

        hitBoxClipItem.HitBoxData.Effect ??= new HitEffectData();
        hitBoxClipItem.HitBoxData.Effect.KnockbackForce = Mathf.Max(0f, evt.newValue);
        MarkAssetDirty();
    }

    private void OnHitEffectKnockupForceChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is not HitBoxClipItem hitBoxClipItem || hitBoxClipItem.HitBoxData == null)
        {
            return;
        }

        hitBoxClipItem.HitBoxData.Effect ??= new HitEffectData();
        hitBoxClipItem.HitBoxData.Effect.KnockupForce = Mathf.Max(0f, evt.newValue);
        MarkAssetDirty();
    }

    private void OnHitEffectHitStunMsChanged(ChangeEvent<int> evt)
    {
        if (selectedClip is not HitBoxClipItem hitBoxClipItem || hitBoxClipItem.HitBoxData == null)
        {
            return;
        }

        hitBoxClipItem.HitBoxData.Effect ??= new HitEffectData();
        hitBoxClipItem.HitBoxData.Effect.HitStunMs = Mathf.Max(0, evt.newValue);
        MarkAssetDirty();
    }

    private void OnHitEffectTargetStateChanged(ChangeEvent<Enum> evt)
    {
        if (selectedClip is not HitBoxClipItem hitBoxClipItem || hitBoxClipItem.HitBoxData == null)
        {
            return;
        }

        if (evt.newValue == null)
        {
            return;
        }

        hitBoxClipItem.HitBoxData.Effect ??= new HitEffectData();
        hitBoxClipItem.HitBoxData.Effect.TargetState = (TargetStateType)evt.newValue;
        MarkAssetDirty();
    }

    private void OnHitFeedbackShakeIntensityChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is not HitBoxClipItem hitBoxClipItem || hitBoxClipItem.HitBoxData == null)
        {
            return;
        }

        hitBoxClipItem.HitBoxData.Feedback ??= new HitFeedbackData();
        hitBoxClipItem.HitBoxData.Feedback.ScreenShakeIntensity = Mathf.Clamp01(evt.newValue);
        MarkAssetDirty();
    }

    private void OnHitFeedbackShakeDurationChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is not HitBoxClipItem hitBoxClipItem || hitBoxClipItem.HitBoxData == null)
        {
            return;
        }

        hitBoxClipItem.HitBoxData.Feedback ??= new HitFeedbackData();
        hitBoxClipItem.HitBoxData.Feedback.ScreenShakeDuration = Mathf.Max(0f, evt.newValue);
        MarkAssetDirty();
    }

    private void OnHitFeedbackHitStopMsChanged(ChangeEvent<int> evt)
    {
        if (selectedClip is not HitBoxClipItem hitBoxClipItem || hitBoxClipItem.HitBoxData == null)
        {
            return;
        }

        hitBoxClipItem.HitBoxData.Feedback ??= new HitFeedbackData();
        hitBoxClipItem.HitBoxData.Feedback.HitStopMs = Mathf.Max(0, evt.newValue);
        MarkAssetDirty();
    }

    private void OnHitFeedbackTimeScaleChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is not HitBoxClipItem hitBoxClipItem || hitBoxClipItem.HitBoxData == null)
        {
            return;
        }

        hitBoxClipItem.HitBoxData.Feedback ??= new HitFeedbackData();
        hitBoxClipItem.HitBoxData.Feedback.TimeScale = Mathf.Max(0f, evt.newValue);
        MarkAssetDirty();
    }

    private void OnHitFeedbackTimeScaleDurationMsChanged(ChangeEvent<int> evt)
    {
        if (selectedClip is not HitBoxClipItem hitBoxClipItem || hitBoxClipItem.HitBoxData == null)
        {
            return;
        }

        hitBoxClipItem.HitBoxData.Feedback ??= new HitFeedbackData();
        hitBoxClipItem.HitBoxData.Feedback.TimeScaleDurationMs = Mathf.Max(0, evt.newValue);
        MarkAssetDirty();
    }

    #endregion

    #region RightPanel - 字段回调（AttackConfig 全局参数）

    private void OnInputBufferWindowMsChanged(ChangeEvent<int> evt)
    {
        if (config == null)
        {
            return;
        }

        int v = Mathf.Max(0, evt.newValue);
        config.InputBufferWindowMs = v;
        if (inputBufferWindowMsField != null && v != evt.newValue)
        {
            inputBufferWindowMsField.SetValueWithoutNotify(v);
        }
        MarkAssetDirty();
    }

    private void OnDefaultHitStopMsChanged(ChangeEvent<int> evt)
    {
        if (config == null)
        {
            return;
        }

        int v = Mathf.Max(0, evt.newValue);
        config.DefaultHitStopMs = v;
        if (defaultHitStopMsField != null && v != evt.newValue)
        {
            defaultHitStopMsField.SetValueWithoutNotify(v);
        }
        MarkAssetDirty();
    }

    private void OnRecoveryHoldMsChanged(ChangeEvent<int> evt)
    {
        if (config == null)
        {
            return;
        }

        int v = Mathf.Max(0, evt.newValue);
        config.RecoveryHoldMs = v;
        if (recoveryHoldMsField != null && v != evt.newValue)
        {
            recoveryHoldMsField.SetValueWithoutNotify(v);
        }
        MarkAssetDirty();
    }

    #endregion

    #endregion
}


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

    private void OnClipNameChanged(ChangeEvent<string> evt)
    {
        if (selectedClip == null) return;

        selectedClip.Name = evt.newValue;

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

        float v = Mathf.Max(0f, evt.newValue);
        selectedClip.StartTime = v;
        if (startTimeField != null && !Mathf.Approximately(v, evt.newValue))
        {
            startTimeField.SetValueWithoutNotify(v);
        }

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
        if (clipLengthField != null && !Mathf.Approximately(newLength, evt.newValue))
        {
            clipLengthField.SetValueWithoutNotify(newLength);
        }

        if (selectedClip.Type == TrackType.Animation || selectedClip.Type == TrackType.Sound)
        {
            if (clipLengthField != null)
            {
                clipLengthField.SetValueWithoutNotify(Mathf.Max(0f, selectedClip.Duration));
            }
            return;
        }

        selectedClip.Duration = newLength;

        if (selectedClip is EffectClipItem effectClipItem && effectClipItem.EffectData != null)
        {
            effectClipItem.EffectData.Length = newLength;
        }
        else if (selectedClip is HitBoxClipItem hitBoxClipItem && hitBoxClipItem.HitBoxData != null && config != null)
        {
            UpdateHitBoxClipTimes(hitBoxClipItem, hitBoxClipItem.StartTime, newLength);
        }
        else if (selectedClip is ActiveClipItem activeClipItem && activeClipItem.ActiveData != null && config != null)
        {
            UpdateActiveClipTimes(activeClipItem, activeClipItem.StartTime, newLength);
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

            // 更新动画结束时间黄色竖线位置
            UpdateAnimationEndLinePosition(animClipItem);

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

            // 更新动画结束时间黄色竖线位置
            UpdateAnimationEndLinePosition(animClipItem);

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

            // 更新动画结束时间黄色竖线位置
            UpdateAnimationEndLinePosition(animClipItem);

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
                int durMs = Mathf.RoundToInt(Mathf.Max(0f, animClipItem.SegmentData.Duration * animClipItem.SegmentData.TimeWindow.AnimationEnd) * 1000f);
                segmentTimeoutMsPreviewField.SetValueWithoutNotify(durMs + v);
            }

            MarkAssetDirty();
        }
    }

    #endregion

    #region RightPanel - 字段回调（Movement）

    // Movement 回调
    private void OnMovementEnableChanged(ChangeEvent<bool> evt)
    {
        if (selectedClip is AnimationClipItem animClipItem && animClipItem.SegmentData != null)
        {
            var movement = animClipItem.SegmentData.Movement ??= new AttackMovementData();
            movement.EnableMovement = evt.newValue;
            
            // 更新其他字段的启用状态
            bool enabled = evt.newValue;
            if (movementDistanceField != null) movementDistanceField.SetEnabled(enabled);
            if (movementStartField != null) movementStartField.SetEnabled(enabled);
            if (movementEndField != null) movementEndField.SetEnabled(enabled);
            if (movementCurveField != null) movementCurveField.SetEnabled(enabled);
            if (movementTrackTargetField != null) movementTrackTargetField.SetEnabled(enabled);
            if (movementTrackRangeField != null) movementTrackRangeField.SetEnabled(enabled && movement.TrackTarget);
            
            MarkAssetDirty();
            // 刷新 SceneView 以更新位移轨迹显示
            SceneView.RepaintAll();
        }
    }

    private void OnMovementDistanceChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is AnimationClipItem animClipItem && animClipItem.SegmentData != null)
        {
            var movement = animClipItem.SegmentData.Movement ??= new AttackMovementData();
            movement.Distance = Mathf.Max(0f, evt.newValue);
            MarkAssetDirty();
            SceneView.RepaintAll();
        }
    }

    private void OnMovementStartChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is AnimationClipItem animClipItem && animClipItem.SegmentData != null)
        {
            var movement = animClipItem.SegmentData.Movement ??= new AttackMovementData();
            movement.NormalizedStart = Mathf.Clamp01(evt.newValue);
            MarkAssetDirty();
            SceneView.RepaintAll();
        }
    }

    private void OnMovementEndChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is AnimationClipItem animClipItem && animClipItem.SegmentData != null)
        {
            var movement = animClipItem.SegmentData.Movement ??= new AttackMovementData();
            movement.NormalizedEnd = Mathf.Clamp01(evt.newValue);
            MarkAssetDirty();
            SceneView.RepaintAll();
        }
    }

    private void OnMovementCurveChanged(ChangeEvent<AnimationCurve> evt)
    {
        if (selectedClip is AnimationClipItem animClipItem && animClipItem.SegmentData != null)
        {
            var movement = animClipItem.SegmentData.Movement ??= new AttackMovementData();
            movement.MoveCurve = evt.newValue ?? AnimationCurve.EaseInOut(0, 0, 1, 1);
            MarkAssetDirty();
            SceneView.RepaintAll();
        }
    }

    private void OnMovementTrackTargetChanged(ChangeEvent<bool> evt)
    {
        if (selectedClip is AnimationClipItem animClipItem && animClipItem.SegmentData != null)
        {
            var movement = animClipItem.SegmentData.Movement ??= new AttackMovementData();
            movement.TrackTarget = evt.newValue;
            
            // 更新 TrackRange 字段的启用状态
            if (movementTrackRangeField != null)
            {
                movementTrackRangeField.SetEnabled(movement.EnableMovement && evt.newValue);
            }
            
            MarkAssetDirty();
        }
    }

    private void OnMovementTrackRangeChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is AnimationClipItem animClipItem && animClipItem.SegmentData != null)
        {
            var movement = animClipItem.SegmentData.Movement ??= new AttackMovementData();
            movement.TrackRange = Mathf.Max(0f, evt.newValue);
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
            var prefab = evt.newValue as UnityEngine.GameObject;
            effectClipItem.EffectData.Prefab = prefab;

            // Best practice：若 Prefab 自带 legacy Animation，则以 clip.length 作为特效时长真源，写回 config。
            // ParticleSystem 则不自动覆盖 Length（按配置手填）。
            bool hasAnimation = false;
            if (prefab != null)
            {
                var anim = prefab.GetComponentInChildren<Animation>(true);
                hasAnimation = anim != null;
                var clip = anim != null ? GetFirstLegacyAnimationClip(anim) : null;
                if (clip != null)
                {
                    float len = Mathf.Max(0.01f, clip.length);
                    effectClipItem.EffectData.Length = len;
                    effectClipItem.Duration = len;
                    effectClipItem.Frame = Mathf.RoundToInt(len * 60f);

                    if (clipLengthField != null)
                    {
                        clipLengthField.SetValueWithoutNotify(effectClipItem.Duration);
                    }
                    if (frameField != null)
                    {
                        frameField.SetValueWithoutNotify(effectClipItem.Frame);
                    }
                }
            }
            effectClipItem.EffectData.IsAnimation = hasAnimation;
            if (effectIsAnimationField != null)
            {
                effectIsAnimationField.SetValueWithoutNotify(effectClipItem.EffectData.IsAnimation);
                effectIsAnimationField.SetEnabled(false);
            }

            MarkAssetDirty();
            RefreshTrackContent();
            RefreshSelectionHighlight();
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
            var owner = GetOwnerAnimationClipItem(effectClipItem);
            float endNorm = GetSegmentAnimationEndNorm(owner?.SegmentData);
            float v = Mathf.Clamp(evt.newValue, 0f, endNorm);
            if (effectNormalizedStartField != null && !Mathf.Approximately(v, evt.newValue))
            {
                effectNormalizedStartField.SetValueWithoutNotify(v);
            }
            effectClipItem.EffectData.NormalizedStart = v;
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

    private void OnEffectOffsetChanged(ChangeEvent<Vector3> evt)
    {
        if (selectedClip is not EffectClipItem effectClipItem || effectClipItem.EffectData == null)
        {
            return;
        }

        effectClipItem.EffectData.Offset = evt.newValue;
        MarkAssetDirty();

        // 非播放时：立即按当前时间点重建预览，便于实时调偏移。
        if (!isPlaying)
        {
            EvaluatePreviewVfxAtTime(currentPlaybackTime);
            SceneView.RepaintAll();
        }
    }

    private void OnEffectRotationChanged(ChangeEvent<Vector3> evt)
    {
        if (selectedClip is not EffectClipItem effectClipItem || effectClipItem.EffectData == null)
        {
            return;
        }

        effectClipItem.EffectData.RotationEuler = evt.newValue;
        MarkAssetDirty();

        // 非播放时：立即按当前时间点重建预览，便于实时调旋转。
        if (!isPlaying)
        {
            EvaluatePreviewVfxAtTime(currentPlaybackTime);
            SceneView.RepaintAll();
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

    private void OnActiveTargetObjectChanged(ChangeEvent<Object> evt)
    {
        if (selectedClip is not ActiveClipItem activeClipItem || activeClipItem.ActiveData == null)
        {
            return;
        }

        var rootGo = selectObj != null ? selectObj.value as GameObject : null;
        var root = rootGo != null ? rootGo.transform : null;
        if (root == null)
        {
            // 没有根对象时无法计算相对路径：直接回退显示
            if (activeTargetObjectField != null)
            {
                activeTargetObjectField.SetValueWithoutNotify(null);
            }
            return;
        }

        var go = evt.newValue as GameObject;
        if (go == null)
        {
            // 清空选择：不改数据（避免误删路径）；只回显为空
            if (activeTargetObjectField != null)
            {
                activeTargetObjectField.SetValueWithoutNotify(null);
            }
            return;
        }

        if (go.transform == null || !go.transform.IsChildOf(root))
        {
            // 只能选角色根节点下的子物体：回退到旧值
            if (activeTargetObjectField != null)
            {
                var oldPath = activeClipItem.ActiveData.RelativePath ?? string.Empty;
                var oldTf = string.IsNullOrEmpty(oldPath) ? root : root.Find(oldPath);
                activeTargetObjectField.SetValueWithoutNotify(oldTf != null ? oldTf.gameObject : null);
            }
            return;
        }

        // 计算相对路径并写回
        string path = GetRelativePath(root, go.transform);
        activeClipItem.ActiveData.Name = go.name;
        activeClipItem.ActiveData.RelativePath = path;
        activeClipItem.Name = go.name; // 时间轴显示名称同步

        // 若目标带 legacy Animation，则用 clip.length 自动推导本段内的区间长度（更新 NormalizedEnd）
        // 只在“已有 end<=start 或 end-start 约等于默认值0.2”时自动覆盖，避免破坏用户手工调的区间。
        var owner = GetOwnerAnimationClipItem(activeClipItem);
        float ownerDuration = owner != null ? Mathf.Max(0.0001f, owner.Duration) : 0f;
        if (ownerDuration > 0f)
        {
            var anim = go.GetComponentInChildren<Animation>(true);
            var clip = anim != null ? GetFirstLegacyAnimationClip(anim) : null;
            if (clip != null)
            {
                float startN = Mathf.Clamp01(activeClipItem.ActiveData.NormalizedStart);
                float endN = Mathf.Clamp01(activeClipItem.ActiveData.NormalizedEnd);
                float delta = endN - startN;
                bool shouldAuto = delta <= 0f || Mathf.Abs(delta - 0.2f) < 0.0001f;
                if (shouldAuto)
                {
                    float clipN = Mathf.Clamp01(clip.length / ownerDuration);
                    activeClipItem.ActiveData.NormalizedEnd = Mathf.Clamp01(startN + clipN);
                }

                // 同步 UI clip 的绝对时长
                float dur = (Mathf.Clamp01(activeClipItem.ActiveData.NormalizedEnd) - startN) * ownerDuration;
                activeClipItem.Duration = Mathf.Max(0f, dur);
                activeClipItem.Frame = Mathf.RoundToInt(activeClipItem.Duration * 60f);
                if (clipLengthField != null)
                {
                    clipLengthField.SetValueWithoutNotify(activeClipItem.Duration);
                }
                if (frameField != null)
                {
                    frameField.SetValueWithoutNotify(activeClipItem.Frame);
                }
            }
        }

        if (activeRelativePathField != null)
        {
            activeRelativePathField.SetValueWithoutNotify(path);
        }

        MarkAssetDirty();
        ApplyViewModeAndRefresh();
    }

    private void OnSoundNormalizedStartChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is SoundClipItem soundClipItem && soundClipItem.SoundData != null)
        {
            var owner = GetOwnerAnimationClipItem(soundClipItem);
            float endNorm = GetSegmentAnimationEndNorm(owner?.SegmentData);
            float v = Mathf.Clamp(evt.newValue, 0f, endNorm);
            if (soundNormalizedStartField != null && !Mathf.Approximately(v, evt.newValue))
            {
                soundNormalizedStartField.SetValueWithoutNotify(v);
            }
            soundClipItem.SoundData.NormalizedStart = v;
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
            var owner = GetOwnerAnimationClipItem(hitBoxClipItem);
            float endNorm = GetSegmentAnimationEndNorm(owner?.SegmentData);
            float newValue = Mathf.Clamp(evt.newValue, 0f, endNorm);
            // 确保结束时间永远大于开始时间
            if (newValue >= hitBoxClipItem.HitBoxData.NormalizedEnd)
            {
                hitBoxClipItem.HitBoxData.NormalizedEnd = Mathf.Min(endNorm, newValue + 0.01f);
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
            var owner = GetOwnerAnimationClipItem(hitBoxClipItem);
            float endNorm = GetSegmentAnimationEndNorm(owner?.SegmentData);
            float newValue = Mathf.Clamp(evt.newValue, 0f, endNorm);
            // 确保结束时间永远大于开始时间
            if (newValue <= hitBoxClipItem.HitBoxData.NormalizedStart)
            {
                hitBoxClipItem.HitBoxData.NormalizedStart = Mathf.Clamp(newValue - 0.01f, 0f, endNorm);
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

        float v = Mathf.Max(0f, evt.newValue);
        if (hitEffectDamageMultiplierField != null && !Mathf.Approximately(v, evt.newValue))
        {
            hitEffectDamageMultiplierField.SetValueWithoutNotify(v);
        }
        hitBoxClipItem.HitBoxData.Effect ??= new HitEffectData();
        hitBoxClipItem.HitBoxData.Effect.DamageMultiplier = v;
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

        float v = Mathf.Max(0f, evt.newValue);
        if (hitEffectKnockbackForceField != null && !Mathf.Approximately(v, evt.newValue))
        {
            hitEffectKnockbackForceField.SetValueWithoutNotify(v);
        }
        hitBoxClipItem.HitBoxData.Effect ??= new HitEffectData();
        hitBoxClipItem.HitBoxData.Effect.KnockbackForce = v;
        MarkAssetDirty();
    }

    private void OnHitEffectKnockupForceChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is not HitBoxClipItem hitBoxClipItem || hitBoxClipItem.HitBoxData == null)
        {
            return;
        }

        float v = Mathf.Max(0f, evt.newValue);
        if (hitEffectKnockupForceField != null && !Mathf.Approximately(v, evt.newValue))
        {
            hitEffectKnockupForceField.SetValueWithoutNotify(v);
        }
        hitBoxClipItem.HitBoxData.Effect ??= new HitEffectData();
        hitBoxClipItem.HitBoxData.Effect.KnockupForce = v;
        MarkAssetDirty();
    }

    private void OnHitEffectHitStunMsChanged(ChangeEvent<int> evt)
    {
        if (selectedClip is not HitBoxClipItem hitBoxClipItem || hitBoxClipItem.HitBoxData == null)
        {
            return;
        }

        int v = Mathf.Max(0, evt.newValue);
        if (hitEffectHitStunMsField != null && v != evt.newValue)
        {
            hitEffectHitStunMsField.SetValueWithoutNotify(v);
        }
        hitBoxClipItem.HitBoxData.Effect ??= new HitEffectData();
        hitBoxClipItem.HitBoxData.Effect.HitStunMs = v;
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

        float v = Mathf.Clamp01(evt.newValue);
        if (hitFeedbackShakeIntensityField != null && !Mathf.Approximately(v, evt.newValue))
        {
            hitFeedbackShakeIntensityField.SetValueWithoutNotify(v);
        }
        hitBoxClipItem.HitBoxData.Feedback ??= new HitFeedbackData();
        hitBoxClipItem.HitBoxData.Feedback.ScreenShakeIntensity = v;
        MarkAssetDirty();
    }

    private void OnHitFeedbackShakeDurationChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is not HitBoxClipItem hitBoxClipItem || hitBoxClipItem.HitBoxData == null)
        {
            return;
        }

        float v = Mathf.Max(0f, evt.newValue);
        if (hitFeedbackShakeDurationField != null && !Mathf.Approximately(v, evt.newValue))
        {
            hitFeedbackShakeDurationField.SetValueWithoutNotify(v);
        }
        hitBoxClipItem.HitBoxData.Feedback ??= new HitFeedbackData();
        hitBoxClipItem.HitBoxData.Feedback.ScreenShakeDuration = v;
        MarkAssetDirty();
    }

    private void OnHitFeedbackHitStopMsChanged(ChangeEvent<int> evt)
    {
        if (selectedClip is not HitBoxClipItem hitBoxClipItem || hitBoxClipItem.HitBoxData == null)
        {
            return;
        }

        int v = Mathf.Max(0, evt.newValue);
        if (hitFeedbackHitStopMsField != null && v != evt.newValue)
        {
            hitFeedbackHitStopMsField.SetValueWithoutNotify(v);
        }
        hitBoxClipItem.HitBoxData.Feedback ??= new HitFeedbackData();
        hitBoxClipItem.HitBoxData.Feedback.HitStopMs = v;
        MarkAssetDirty();
    }

    private void OnHitFeedbackTimeScaleChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is not HitBoxClipItem hitBoxClipItem || hitBoxClipItem.HitBoxData == null)
        {
            return;
        }

        float v = Mathf.Max(0f, evt.newValue);
        if (hitFeedbackTimeScaleField != null && !Mathf.Approximately(v, evt.newValue))
        {
            hitFeedbackTimeScaleField.SetValueWithoutNotify(v);
        }
        hitBoxClipItem.HitBoxData.Feedback ??= new HitFeedbackData();
        hitBoxClipItem.HitBoxData.Feedback.TimeScale = v;
        MarkAssetDirty();
    }

    private void OnHitFeedbackTimeScaleDurationMsChanged(ChangeEvent<int> evt)
    {
        if (selectedClip is not HitBoxClipItem hitBoxClipItem || hitBoxClipItem.HitBoxData == null)
        {
            return;
        }

        int v = Mathf.Max(0, evt.newValue);
        if (hitFeedbackTimeScaleDurationMsField != null && v != evt.newValue)
        {
            hitFeedbackTimeScaleDurationMsField.SetValueWithoutNotify(v);
        }
        hitBoxClipItem.HitBoxData.Feedback ??= new HitFeedbackData();
        hitBoxClipItem.HitBoxData.Feedback.TimeScaleDurationMs = v;
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


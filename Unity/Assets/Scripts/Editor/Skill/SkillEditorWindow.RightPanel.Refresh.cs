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
        if (animationActionButtons != null) animationActionButtons.style.display = DisplayStyle.None;

        // 清除按钮的类型样式类
        if (addEffectButton != null)
        {
            addEffectButton.RemoveFromClassList("action-button");
            addEffectButton.RemoveFromClassList("type-effect");
            addEffectButton.RemoveFromClassList("type-sound");
            addEffectButton.RemoveFromClassList("type-hitbox");
            addEffectButton.RemoveFromClassList("type-animation");
        }

        if (addSoundButton != null)
        {
            addSoundButton.RemoveFromClassList("action-button");
            addSoundButton.RemoveFromClassList("type-effect");
            addSoundButton.RemoveFromClassList("type-sound");
            addSoundButton.RemoveFromClassList("type-hitbox");
            addSoundButton.RemoveFromClassList("type-animation");
        }

        if (addHitboxButton != null)
        {
            addHitboxButton.RemoveFromClassList("action-button");
            addHitboxButton.RemoveFromClassList("type-effect");
            addHitboxButton.RemoveFromClassList("type-sound");
            addHitboxButton.RemoveFromClassList("type-hitbox");
            addHitboxButton.RemoveFromClassList("type-animation");
        }

        // 注意：addClipToTrackButton的track-add-button样式是永久的，不需要清除
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
        if (hitBoxOffsetField != null) hitBoxOffsetField.SetValueWithoutNotify(Vector3.zero);
        if (hitBoxRotationField != null) hitBoxRotationField.SetValueWithoutNotify(Vector3.zero);
        if (hitBoxSizeField != null) hitBoxSizeField.SetValueWithoutNotify(Vector3.one);
    }

    #endregion
}


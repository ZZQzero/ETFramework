using System;
using System.Collections.Generic;
using ET;
using UnityEditor;
using UnityEngine;

public partial class SkillEditorWindow : EditorWindow
{
    // 数据操作/同步：删除、归一化时间同步、标记资源 Dirty、通用移除等

    /// <summary>
    /// 清理空的非动画轨道（Effect/Sound/HitBox）。
    /// 设计意图：删除最后一个 clip 后，轨道行也应立即消失；同时避免 selectedTrack 指向一个已不在当前视图中的轨道。
    /// </summary>
    private void CleanupEmptyNonAnimationTracks()
    {
        if (globalTrackDataList == null || globalTrackDataList.Count == 0)
        {
            return;
        }

        // 先从 globalTrackDataList 移除（倒序安全删除）
        for (int i = globalTrackDataList.Count - 1; i >= 0; --i)
        {
            var t = globalTrackDataList[i];
            bool isEmptyNonAnim = t switch
            {
                EffectTrack et => et.ClipList.Count == 0,
                SoundTrack st => st.ClipList.Count == 0,
                HitBoxTrack ht => ht.ClipList.Count == 0,
                ActiveTrack at => at.ClipList.Count == 0,
                _ => false
            };

            if (!isEmptyNonAnim)
            {
                continue;
            }

            // 选中轨道正好被移除时，立刻清空右侧面板轨道信息
            if (selectedTrack != null && ReferenceEquals(selectedTrack, t))
            {
                selectedTrack = null;
                UpdateTrackInfo(null);
            }

            globalTrackDataList.RemoveAt(i);

            // 同步移除所有 local track list 中对该轨道的引用（同一个对象在全局/局部列表里会共用引用）
            foreach (var kvp in animationClipTrackMap)
            {
                kvp.Value?.RemoveAll(x => ReferenceEquals(x, t));
            }
        }
    }
    
    /// <summary>
    /// 拖拽结束后，把 Clip 的绝对时间写回配置数据（只处理数据，不处理 UI）。
    /// 约定：调用方已把 <paramref name="draggedClipItem"/>.StartTime 更新为拖拽后的绝对时间。
    /// </summary>
    private void SyncDraggedClipToConfig(IClipItem draggedClipItem)
    {
        if (draggedClipItem == null)
        {
            return;
        }

        bool dataChanged = false;

        // AnimationClip：直接写回 SegmentData.StartTime（不依赖 config 不为空）
        if (draggedClipItem is AnimationClipItem animationClipItem && animationClipItem.SegmentData != null)
        {
            animationClipItem.SegmentData.StartTime = draggedClipItem.StartTime;
            // AnimationClip 移动时：子 clip 的归一化时间不变，因此需要重算它们的绝对时间
            SyncOwnerChildClipsToOwner(animationClipItem);
            // AnimationEnd：与下一段的重叠保持一致（拖拽/手填 StartTime 后同步重算）
            RecomputeAnimationEndsAround(animationClipItem);
            dataChanged = true;
        }
        // Effect/Sound/HitBox：需要根据所在 Segment 计算归一化时间（依赖 config）
        else if (draggedClipItem is EffectClipItem effectClipItem && effectClipItem.EffectData != null && config != null)
        {
            UpdateEffectClipTriggerTime(effectClipItem, draggedClipItem.StartTime);
            dataChanged = true;
        }
        else if (draggedClipItem is SoundClipItem soundClipItem && soundClipItem.SoundData != null && config != null)
        {
            UpdateSoundClipTriggerTime(soundClipItem, draggedClipItem.StartTime);
            dataChanged = true;
        }
        else if (draggedClipItem is HitBoxClipItem hitBoxClipItem && hitBoxClipItem.HitBoxData != null && config != null)
        {
            UpdateHitBoxClipTimes(hitBoxClipItem, draggedClipItem.StartTime, draggedClipItem.Duration);
            dataChanged = true;
        }
        else if (draggedClipItem is ActiveClipItem activeClipItem && activeClipItem.ActiveData != null && config != null)
        {
            UpdateActiveClipTimes(activeClipItem, draggedClipItem.StartTime, draggedClipItem.Duration);
            dataChanged = true;
        }

        if (dataChanged)
        {
            MarkAssetDirty();
        }
    }

    // 删除Clip
    private void DeleteClip(IClipItem clipItem)
    {
        if (clipItem == null || config == null)
        {
            return;
        }

        // 同步清理 lane 映射（避免删除后出现空白行/高度不收缩）
        laneIndexByClip.Remove(clipItem);

        bool dataChanged = false;

        if (clipItem is AnimationClipItem animClipItem)
        {
            // 删除AnimationClip及其所有子轨道：从Config.Segments中删除对应的SegmentData
            if (animClipItem.SegmentData != null && config.Segments.Contains(animClipItem.SegmentData))
            {
                config.Segments.Remove(animClipItem.SegmentData);
                dataChanged = true;
            }

            // 获取与该AnimationClip关联的所有子轨道，并删除这些轨道中的所有Clip
            if (animationClipTrackMap.TryGetValue(animClipItem, out var childTracks))
            {
                foreach (var childTrack in childTracks)
                {
                    // 从config数据中删除子轨道的所有Clip
                    if (childTrack is EffectTrack effectTrack)
                    {
                        foreach (var effectClip in effectTrack.ClipList)
                        {
                            laneIndexByClip.Remove(effectClip);
                            if (effectClip.EffectData != null)
                            {
                                foreach (var segment in config.Segments)
                                {
                                    if (segment.VisualEffects.Contains(effectClip.EffectData))
                                    {
                                        segment.VisualEffects.Remove(effectClip.EffectData);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else if (childTrack is SoundTrack soundTrack)
                    {
                        foreach (var soundClip in soundTrack.ClipList)
                        {
                            laneIndexByClip.Remove(soundClip);
                            if (soundClip.SoundData != null)
                            {
                                foreach (var segment in config.Segments)
                                {
                                    if (segment.SoundEffects.Contains(soundClip.SoundData))
                                    {
                                        segment.SoundEffects.Remove(soundClip.SoundData);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else if (childTrack is HitBoxTrack hitBoxTrack)
                    {
                        foreach (var hitBoxClip in hitBoxTrack.ClipList)
                        {
                            laneIndexByClip.Remove(hitBoxClip);
                            if (hitBoxClip.HitBoxData != null)
                            {
                                foreach (var segment in config.Segments)
                                {
                                    if (segment.HitBoxes.Contains(hitBoxClip.HitBoxData))
                                    {
                                        segment.HitBoxes.Remove(hitBoxClip.HitBoxData);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else if (childTrack is ActiveTrack activeTrack)
                    {
                        foreach (var activeClip in activeTrack.ClipList)
                        {
                            laneIndexByClip.Remove(activeClip);
                            if (activeClip.ActiveData != null)
                            {
                                foreach (var segment in config.Segments)
                                {
                                    if (segment.AttachedActives.Contains(activeClip.ActiveData))
                                    {
                                        segment.AttachedActives.Remove(activeClip.ActiveData);
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    // 从globalTrackDataList中移除子轨道
                    globalTrackDataList.Remove(childTrack);
                }

                // 清空子轨道列表
                childTracks.Clear();
            }

            // 从allAnimationClipItems中移除
            allAnimationClipItems.Remove(animClipItem);

            // 从animationClipTrackMap中移除
            animationClipTrackMap.Remove(animClipItem);

            // 从全局AnimationTrack中移除
            if (globalTrackDataList.Count > 0 && globalTrackDataList[0] is AnimationTrack animTrack)
            {
                animTrack.ClipList.Remove(animClipItem);
            }

            // 如果删除的是当前聚焦的AnimationClip，重置聚焦
            if (focusedAnimationClipItem == animClipItem)
            {
                focusedAnimationClipItem = allAnimationClipItems.Count > 0 ? allAnimationClipItems[0] : null;
            }
        }
        else if (clipItem is EffectClipItem effectClipItem)
        {
            // 删除EffectClip：从对应Segment的VisualEffects中删除
            if (effectClipItem.EffectData != null)
            {
                foreach (var segment in config.Segments)
                {
                    if (segment.VisualEffects.Contains(effectClipItem.EffectData))
                    {
                        segment.VisualEffects.Remove(effectClipItem.EffectData);
                        dataChanged = true;
                        break;
                    }
                }

                RemoveClipFromTracks<EffectTrack, EffectClipItem>(effectClipItem);
            }
        }
        else if (clipItem is SoundClipItem soundClipItem)
        {
            // 删除SoundClip：从对应Segment的SoundEffects中删除
            if (soundClipItem.SoundData != null)
            {
                foreach (var segment in config.Segments)
                {
                    if (segment.SoundEffects.Contains(soundClipItem.SoundData))
                    {
                        segment.SoundEffects.Remove(soundClipItem.SoundData);
                        dataChanged = true;
                        break;
                    }
                }

                RemoveClipFromTracks<SoundTrack, SoundClipItem>(soundClipItem);
            }
        }
        else if (clipItem is HitBoxClipItem hitBoxClipItem)
        {
            // 删除HitBoxClip：从对应Segment的HitBoxes中删除
            if (hitBoxClipItem.HitBoxData != null)
            {
                foreach (var segment in config.Segments)
                {
                    if (segment.HitBoxes.Contains(hitBoxClipItem.HitBoxData))
                    {
                        segment.HitBoxes.Remove(hitBoxClipItem.HitBoxData);
                        dataChanged = true;
                        break;
                    }
                }

                RemoveClipFromTracks<HitBoxTrack, HitBoxClipItem>(hitBoxClipItem);
            }
        }
        else if (clipItem is ActiveClipItem activeClipItem)
        {
            if (activeClipItem.ActiveData != null)
            {
                foreach (var segment in config.Segments)
                {
                    if (segment.AttachedActives.Contains(activeClipItem.ActiveData))
                    {
                        segment.AttachedActives.Remove(activeClipItem.ActiveData);
                        dataChanged = true;
                        break;
                    }
                }

                RemoveClipFromTracks<ActiveTrack, ActiveClipItem>(activeClipItem);
            }
        }

        if (dataChanged)
        {
            MarkAssetDirty();
        }

        // 清空选中状态
        if (selectedClip == clipItem)
        {
            selectedClip = null;
            UpdateClipProperties(null);
        }

        // 删除 clip 可能导致某条子轨道变空：需要把空轨道也清理掉，否则全局视图会留下“空行”，看起来像没刷新。
        CleanupEmptyNonAnimationTracks();

        // 刷新轨道显示
        ApplyViewModeAndRefresh();

        // UI Toolkit 在某些编辑器事件回调中可能延迟重绘；这里主动请求一次重绘确保“删除后立即看到变化”。
        trackContainer?.MarkDirtyRepaint();
        root?.MarkDirtyRepaint();
        Repaint();
    }

    // 从轨道中移除指定的Clip
    private void RemoveClipFromTracks<TTrack, TClip>(TClip clipItem)
        where TTrack : class, ITrackItem
        where TClip : class, IClipItem
    {
        // 从globalTrackDataList中移除
        foreach (var track in globalTrackDataList)
        {
            if (track is TTrack typedTrack)
            {
                var clipList = GetClipList<TTrack, TClip>(typedTrack);
                clipList?.Remove(clipItem);
            }
        }

        // 从animationClipTrackMap中的轨道移除
        foreach (var kvp in animationClipTrackMap)
        {
            foreach (var track in kvp.Value)
            {
                if (track is TTrack typedTrack)
                {
                    var clipList = GetClipList<TTrack, TClip>(typedTrack);
                    clipList?.Remove(clipItem);
                }
            }
        }
    }

    // 获取轨道的ClipList
    private List<TClip> GetClipList<TTrack, TClip>(TTrack track)
        where TTrack : class, ITrackItem
        where TClip : class, IClipItem
    {
        return track switch
        {
            EffectTrack et => et.ClipList as List<TClip>,
            SoundTrack st => st.ClipList as List<TClip>,
            HitBoxTrack ht => ht.ClipList as List<TClip>,
            ActiveTrack at => at.ClipList as List<TClip>,
            AnimationTrack at => at.ClipList as List<TClip>,
            _ => null
        };
    }

    // 更新EffectClip的TriggerTime（归一化时间）
    private void UpdateEffectClipTriggerTime(EffectClipItem effectClipItem, float absoluteStartTime)
    {
        if (config == null || effectClipItem.EffectData == null) return;

        foreach (var segment in config.Segments)
        {
            if (segment.VisualEffects.Contains(effectClipItem.EffectData))
            {
                float animationLength = segment.Duration > 0f ? segment.Duration : 2f;
                if (animationLength <= 0f && segment.AnimationClipTrans != null && segment.AnimationClipTrans.Clip != null)
                {
                    animationLength = segment.AnimationClipTrans.Clip.length;
                }

                if (animationLength > 0)
                {
                    float normalizedTime = (absoluteStartTime - segment.StartTime) / animationLength;
                    effectClipItem.EffectData.NormalizedStart = Mathf.Clamp01(normalizedTime);
                }
                break;
            }
        }
    }

    // 更新SoundClip的TriggerTime（归一化时间）
    private void UpdateSoundClipTriggerTime(SoundClipItem soundClipItem, float absoluteStartTime)
    {
        if (config == null || soundClipItem.SoundData == null) return;

        foreach (var segment in config.Segments)
        {
            if (segment.SoundEffects.Contains(soundClipItem.SoundData))
            {
                float animationLength = segment.Duration > 0f ? segment.Duration : 2f;
                if (animationLength <= 0f && segment.AnimationClipTrans != null && segment.AnimationClipTrans.Clip != null)
                {
                    animationLength = segment.AnimationClipTrans.Clip.length;
                }

                if (animationLength > 0)
                {
                    float normalizedTime = (absoluteStartTime - segment.StartTime) / animationLength;
                    soundClipItem.SoundData.NormalizedStart = Mathf.Clamp01(normalizedTime);
                }
                break;
            }
        }
    }

    // 更新HitBoxClip的StartTime和EndTime（归一化时间）
    private void UpdateHitBoxClipTimes(HitBoxClipItem hitBoxClipItem, float absoluteStartTime, float absoluteDuration)
    {
        if (config == null || hitBoxClipItem.HitBoxData == null) return;

        foreach (var segment in config.Segments)
        {
            if (segment.HitBoxes.Contains(hitBoxClipItem.HitBoxData))
            {
                float animationLength = segment.Duration > 0f ? segment.Duration : 2f;
                if (animationLength <= 0f && segment.AnimationClipTrans != null && segment.AnimationClipTrans.Clip != null)
                {
                    animationLength = segment.AnimationClipTrans.Clip.length;
                }

                if (animationLength > 0)
                {
                    float normalizedStartTime = (absoluteStartTime - segment.StartTime) / animationLength;
                    float normalizedEndTime = normalizedStartTime + (absoluteDuration / animationLength);

                    hitBoxClipItem.HitBoxData.NormalizedStart = Mathf.Clamp01(normalizedStartTime);
                    hitBoxClipItem.HitBoxData.NormalizedEnd = Mathf.Clamp01(normalizedEndTime);

                    if (hitBoxClipItem.HitBoxData.NormalizedEnd < hitBoxClipItem.HitBoxData.NormalizedStart)
                    {
                        hitBoxClipItem.HitBoxData.NormalizedEnd = hitBoxClipItem.HitBoxData.NormalizedStart;
                    }
                }
                break;
            }
        }
    }

    private void UpdateActiveClipTimes(ActiveClipItem activeClipItem, float absoluteStartTime, float absoluteDuration)
    {
        if (config == null || activeClipItem.ActiveData == null) return;

        foreach (var segment in config.Segments)
        {
            if (segment.AttachedActives.Contains(activeClipItem.ActiveData))
            {
                float animationLength = segment.Duration > 0f ? segment.Duration : 2f;
                if (animationLength <= 0f && segment.AnimationClipTrans != null && segment.AnimationClipTrans.Clip != null)
                {
                    animationLength = segment.AnimationClipTrans.Clip.length;
                }

                if (animationLength > 0f)
                {
                    float normalizedStartTime = (absoluteStartTime - segment.StartTime) / animationLength;
                    float normalizedEndTime = normalizedStartTime + (absoluteDuration / animationLength);

                    activeClipItem.ActiveData.NormalizedStart = Mathf.Clamp01(normalizedStartTime);
                    activeClipItem.ActiveData.NormalizedEnd = Mathf.Clamp01(normalizedEndTime);

                    if (activeClipItem.ActiveData.NormalizedEnd < activeClipItem.ActiveData.NormalizedStart)
                    {
                        activeClipItem.ActiveData.NormalizedEnd = activeClipItem.ActiveData.NormalizedStart;
                    }
                }
                break;
            }
        }
    }

    // 标记AttackConfigAsset为dirty，以便Unity保存更改
    private void MarkAssetDirty()
    {
        if (selectConfigAsset != null && selectConfigAsset.value != null)
        {
            var asset = selectConfigAsset.value as AttackConfigAsset;
            if (asset != null)
            {
                EditorUtility.SetDirty(asset);
            }
        }
    }

    /// <summary>
    /// 获取或创建指定 AnimationClip 对应的子轨道（写入 animationClipTrackMap + globalTrackDataList）
    /// </summary>
    private T GetOrCreateTrackForAnimationClip<T>(AnimationClipItem animClipItem, TrackType trackType)
        where T : class, ITrackItem, new()
    {
        // 确保animationClipTrackMap中有该AnimationClip的条目
        if (!animationClipTrackMap.TryGetValue(animClipItem, out var trackList))
        {
            trackList = new List<ITrackItem>();
            animationClipTrackMap[animClipItem] = trackList;
        }

        // 查找是否已有对应类型的轨道
        foreach (var track in trackList)
        {
            if (track is T existingTrack)
            {
                return existingTrack;
            }
        }

        // 创建新轨道
        var newTrack = new T();
        newTrack.Name = trackType.ToString();

        // 添加到animationClipTrackMap
        trackList.Add(newTrack);

        // 添加到globalTrackDataList
        globalTrackDataList.Add(newTrack);

        return newTrack;
    }

}


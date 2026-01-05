using Animancer;
using ET;
using UnityEditor;
using UnityEngine;

public partial class SkillEditorWindow : EditorWindow
{
    // InitData：从选择的 Config / GameObject 初始化运行期数据结构（trackDataList、globalTrackDataList、animationClipTrackMap 等）

    private void InitTrackData()
    {
        if (config == null)
        {
            var asset = selectConfigAsset.value as AttackConfigAsset;
            if (asset != null)
            {
                config = asset.Config;
            }
        }

        if (animancer == null)
        {
            var obj = selectObj.value as GameObject;
            if (obj != null)
            {
                animancer = obj.GetComponent<AnimancerComponent>();
            }
        }
        InitTrackAndClipData();
    }

    private void InitTrackAndClipData()
    {
        if (trackContainer == null || config == null)
        {
            return;
        }

        // 清空所有数据
        trackDataList.Clear();
        ClearTrackUI();
        animationClipTrackMap.Clear();
        globalTrackDataList.Clear();
        allAnimationClipItems.Clear();

        // 创建全局动画轨道
        AnimationTrack animationTrack = new AnimationTrack();
        animationTrack.Name = nameof(TrackType.Animation);
        globalTrackDataList.Add(animationTrack);

        if (config.Segments.Count > 0)
        {
            foreach (var segment in config.Segments)
            {
                AnimationClipItem clipItem = new AnimationClipItem();

                // 初始化每个AnimationClipItem对应的子轨道映射
                var localTrackList = new System.Collections.Generic.List<ITrackItem>();
                animationClipTrackMap[clipItem] = localTrackList;

                // 添加到全局动画片段列表
                allAnimationClipItems.Add(clipItem);

                clipItem.SegmentData = segment;
                clipItem.Duration = 2f;
                clipItem.Name = "NULL";
                if (clipItem.SegmentData != null && clipItem.SegmentData.AnimationClipTrans != null)
                {
                    if (clipItem.SegmentData.AnimationClipTrans.Clip != null)
                    {
                        // 以时间轴真实时长为准：Duration = Clip.length / Speed
                        float speed = Mathf.Max(0.01f, clipItem.SegmentData.AnimationClipTrans.Speed);
                        segment.ClipLength = clipItem.SegmentData.AnimationClipTrans.Clip.length;
                        segment.Duration = segment.ClipLength / speed;
                        clipItem.Duration = segment.Duration;
                    }
                    if (!string.IsNullOrEmpty(clipItem.SegmentData.Name))
                    {
                        clipItem.Name = clipItem.SegmentData.Name;
                    }
                    else
                    {
                        clipItem.Name = clipItem.SegmentData.AnimationClipTrans.Name;
                    }
                    clipItem.Frame = Mathf.RoundToInt(clipItem.Duration * 60f);
                }

                clipItem.StartTime = segment.StartTime;
                animationTrack.ClipList.Add(clipItem);

                // 创建该动画片段的子轨道（Effect、Sound、HitBox）
                if (segment.VisualEffects.Count > 0)
                {
                    EffectTrack effectTrack = new EffectTrack();
                    effectTrack.Name = nameof(TrackType.Effect);
                    globalTrackDataList.Add(effectTrack);
                    localTrackList.Add(effectTrack);
                    foreach (var effect in segment.VisualEffects)
                    {
                        EffectClipItem effectClipItem = new EffectClipItem();
                        effectClipItem.EffectData = effect;
                        effectClipItem.Name = effect.Name;
                        effectClipItem.StartTime = segment.StartTime + effect.NormalizedStart * clipItem.Duration;
                        effectClipItem.Duration = effect.Length;
                        effectClipItem.Frame = Mathf.RoundToInt(effectClipItem.Duration * 60f);
                        effectTrack.ClipList.Add(effectClipItem);
                    }
                }

                if (segment.SoundEffects.Count > 0)
                {
                    SoundTrack soundTrack = new SoundTrack();
                    soundTrack.Name = nameof(TrackType.Sound);
                    globalTrackDataList.Add(soundTrack);
                    localTrackList.Add(soundTrack);
                    foreach (var sound in segment.SoundEffects)
                    {
                        SoundClipItem soundClipItem = new SoundClipItem();
                        soundClipItem.SoundData = sound;
                        soundClipItem.Name = sound.Name;
                        soundClipItem.StartTime = segment.StartTime + (sound.NormalizedStart * clipItem.Duration);
                        soundClipItem.Duration = sound.Clip != null ? sound.Clip.length : 2;
                        soundClipItem.Frame = Mathf.RoundToInt(soundClipItem.Duration * 60f);
                        soundTrack.ClipList.Add(soundClipItem);
                    }
                }

                if (segment.HitBoxes.Count > 0)
                {
                    HitBoxTrack hitBoxTrack = new HitBoxTrack();
                    hitBoxTrack.Name = nameof(TrackType.Hitbox);
                    globalTrackDataList.Add(hitBoxTrack);
                    localTrackList.Add(hitBoxTrack);
                    foreach (var hitbox in segment.HitBoxes)
                    {
                        HitBoxClipItem hitboxClipItem = new HitBoxClipItem();
                        hitboxClipItem.HitBoxData = hitbox;
                        hitboxClipItem.Name = hitbox.ShapeType.ToString();
                        hitboxClipItem.StartTime = segment.StartTime + (hitbox.NormalizedStart * clipItem.Duration);
                        hitboxClipItem.Duration = (hitbox.NormalizedEnd - hitbox.NormalizedStart) * clipItem.Duration;
                        hitboxClipItem.Frame = Mathf.RoundToInt(hitboxClipItem.Duration * 60f);
                        hitBoxTrack.ClipList.Add(hitboxClipItem);
                    }
                }
            }
        }

        // 根据当前视图模式设置 trackDataList
        ApplyViewMode();
    }
}


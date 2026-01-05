using System.Collections.Generic;
using UnityEngine;

namespace ET
{
    #region 轨道数据

    public enum TrackType
    {
        Animation, //动画
        Effect, //特效
        Sound, //音效
        Hitbox, //检测框
    }

    public interface ITrackItem
    {
        public string Name { get; set; }
        public TrackType Type { get; }
        public int Index { get; set; }
        public float TotalDuration { get; set; }
        public Color Color { get; set; }
    }

    public interface IClipItem
    {
        public string Name { get; set; }
        public TrackType Type { get; }
        public int Index { get; set; }
        public float Duration { get; set; }
        public float StartTime { get; set; }
        public int Frame  { get; set; } 
        public Color Color { get; set; }
    }

    public class AnimationClipItem : IClipItem
    {
        public int Index { get; set; }
        public float Duration { get; set; }
        public float StartTime { get; set; }
        public int Frame { get; set; }
        public Color Color { get; set; }
        public string Name { get; set; }
        public TrackType Type { get; } = TrackType.Animation;
        public AttackSegmentData SegmentData { get; set; }
    }

    public class AnimationTrack : ITrackItem
    {
        public int Index { get; set; }
        public float TotalDuration { get; set; }
        public Color Color { get; set; }
        public string Name { get; set; }
        public TrackType Type { get; } = TrackType.Animation;
        public List<AnimationClipItem> ClipList = new();
    }

    public class EffectClipItem : IClipItem
    {
        public int Index { get; set; }
        public float Duration { get; set; }
        public float StartTime { get; set; }
        public int Frame { get; set; }
        public Color Color { get; set; }
        public string Name { get; set; }
        public TrackType Type { get; } = TrackType.Effect;
        public VisualEffectData EffectData { get; set; }
    }

    public class EffectTrack : ITrackItem
    {
        public int Index { get; set; }
        public float TotalDuration { get; set; }
        public Color Color { get; set; }
        public string Name { get; set; }
        public TrackType Type { get; } = TrackType.Effect;
        public List<EffectClipItem> ClipList = new();
    }

    public class SoundClipItem : IClipItem
    {
        public string Name { get; set; }
        public TrackType Type { get; } = TrackType.Sound;
        public int Index { get; set; }
        public float Duration { get; set; }
        public float StartTime { get; set; }
        public int Frame { get; set; }
        public Color Color { get; set; }
        public SoundEffectData SoundData { get; set; }
    }

    public class SoundTrack : ITrackItem
    {
        public int Index { get; set; }
        public float TotalDuration { get; set; }
        public Color Color { get; set; }
        public string Name { get; set; }
        public TrackType Type { get; } = TrackType.Sound;
        public List<SoundClipItem> ClipList = new();
    }

    public class HitBoxClipItem : IClipItem
    {
        public string Name { get; set; }
        public TrackType Type { get; } = TrackType.Hitbox;
        public int Index { get; set; }
        public float Duration { get; set; }
        public float StartTime { get; set; }
        public int Frame { get; set; }
        public Color Color { get; set; }
        public HitBoxData HitBoxData { get; set; }
    }

    public class HitBoxTrack : ITrackItem
    {
        public int Index { get; set; }
        public float TotalDuration { get; set; }
        public Color Color { get; set; }
        public string Name { get; set; }
        public TrackType Type { get; } = TrackType.Hitbox;
        public List<HitBoxClipItem> ClipList = new();
    }

    #endregion

}
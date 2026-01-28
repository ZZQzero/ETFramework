using System.Collections.Generic;
using Animancer;
using ET;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public partial class SkillEditorWindow : EditorWindow
{
    #region 初始化 - UI元素
    
    private void InitData()
    {
        selectConfigAsset = root.Q<ObjectField>("SelectConfig");
        selectConfigAsset.objectType = typeof(AttackConfigAsset);
        selectObj = root.Q<ObjectField>("SelectObj");
        selectObj.objectType = typeof(GameObject);

        selectConfigAsset.RegisterValueChangedCallback(OnObjectFieldChanged);
        selectObj.RegisterValueChangedCallback(OnObjectFieldChanged);

        timelineScrollView = root.Q<ScrollView>("TimelineScrollView");
        timelineContent = root.Q<VisualElement>("TimelineContent");
        trackContainer = root.Q<VisualElement>("TrackContainer");
        
        if (trackContainer != null)
        {
            trackContainer.RegisterCallback<GeometryChangedEvent>(evt => UpdatePlayheadSize());
        }
        
        var refresh = root.Q<Button>("Refresh");
        // Refresh：重建数据/视图（不仅仅是重绘 trackContainer）
        refresh.clicked += RebuildFromCurrentSelection;

        toggleViewModeButton = root.Q<Button>("ToggleViewMode");
        if (toggleViewModeButton != null)
        {
            toggleViewModeButton.clicked += OnToggleViewModeClicked;
            UpdateViewModeButtonText();
        }
        
        var playBtn = root.Q<Button>("Play");
        if (playBtn != null)
        {
            playBtn.clicked += OnPlayButtonClicked;
        }
        
        var stopBtn = root.Q<Button>("Stop");
        if (stopBtn != null)
        {
            stopBtn.clicked += OnStopButtonClicked;
        }
        
        var loopBtn = root.Q<Button>("Loop");
        if (loopBtn != null)
        {
            loopBtn.clicked += OnLoopButtonClicked;
        }
        UpdateLoopButtonVisual();
        
        leftContainer = root.Q<VisualElement>("Left");
        
        if (timelineScrollView != null)
        {
            timelineScrollView.horizontalScroller.valueChanged += OnTimelineScrollChanged;
            timelineScrollView.RegisterCallback<GeometryChangedEvent>(evt => {
                UpdateTimelineContentWidth();
                DrawTimelineRulerMarks();
            });
        }
        
        timeLengthLabel = root.Q<Label>("TimeLength");
        UpdateTimeLengthDisplay();
        InitPlaybackSpeedControl();
        InitConfigHint();
        InitRightPanel();
        
        root.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Delete && selectedClip != null)
            {
                DeleteClip(selectedClip);
                evt.StopPropagation();
            }
        });
        
        root.focusable = true;
    }
    
    private void OnObjectFieldChanged(ChangeEvent<Object> evt)
    {
        RebuildFromCurrentSelection();
    }

    /// <summary>
    /// 根据当前 SelectConfig/SelectObj 重建数据与 UI。
    /// 说明：旧逻辑只在 config==null 时绑定一次，导致切换 Config/对象后仍显示旧数据，Refresh 按钮也无效。
    /// </summary>
    private void RebuildFromCurrentSelection()
    {
        // 先停止播放预览，避免旧状态影响新对象/新配置
        if (isPlaying || isDraggingPlayhead)
        {
            StopPreviewPlayback(resetTime: false, sampleAfterStop: false);
        }

        // 每次都从当前选择重新绑定 config（允许切换 Config）
        var asset = selectConfigAsset != null ? selectConfigAsset.value as AttackConfigAsset : null;
        config = asset != null ? asset.Config : null;

        // 每次都尝试绑定预览对象（允许切换 GameObject）
        EnsurePreviewObject();

        // 清空选中与 UI（避免 selectedTrack/selectedClip 悬挂到上一次的数据对象）
        ClearSelection();
        ClearTrackUI();
        laneIndexByClip.Clear();
        animationClipTrackMap.Clear();
        globalTrackDataList.Clear();
        allAnimationClipItems.Clear();

        UpdateConfigHint();

        // 任意一项为空：维持空态
        if (config == null || animancer == null)
        {
            UpdateTimelineContentWidth();
            DrawTimelineRulerMarks();
            UpdatePlayheadSize();
            UpdatePlayheadPosition();
            return;
        }

        // 重建轨道数据与 UI
        InitTimelineRuler();
        InitPlayHead();
        InitTrackAndClipData();
        CreateTrack();
        DrawTimelineRulerMarks();
        UpdateViewModeButtonText();
        UpdatePlayheadSize();
        UpdatePlayheadPosition();
    }
    
    #endregion

    #region 初始化 - 数据
    
    private void InitTrackData()
    {
        // 旧逻辑只在 config==null 时赋值，会导致切换 Config 后仍然用旧数据。
        var asset = selectConfigAsset != null ? selectConfigAsset.value as AttackConfigAsset : null;
        config = asset != null ? asset.Config : null;

        EnsurePreviewObject();
        InitTrackAndClipData();
    }

    private void InitTrackAndClipData()
    {
        if (trackContainer == null || config == null)
        {
            return;
        }

        trackDataList.Clear();
        ClearTrackUI();
        laneIndexByClip.Clear();
        animationClipTrackMap.Clear();
        globalTrackDataList.Clear();
        allAnimationClipItems.Clear();

        AnimationTrack animationTrack = new AnimationTrack();
        animationTrack.Name = nameof(TrackType.Animation);
        globalTrackDataList.Add(animationTrack);

        if (config.Segments.Count > 0)
        {
            foreach (var segment in config.Segments)
            {
                AnimationClipItem clipItem = new AnimationClipItem();

                var localTrackList = new List<ITrackItem>();
                animationClipTrackMap[clipItem] = localTrackList;
                allAnimationClipItems.Add(clipItem);

                clipItem.SegmentData = segment;
                clipItem.Duration = 2f;
                clipItem.Name = "NULL";
                if (clipItem.SegmentData != null && clipItem.SegmentData.AnimationClipTrans != null)
                {
                    if (clipItem.SegmentData.AnimationClipTrans.Clip != null)
                    {
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

                if (segment.AttachedActives.Count > 0)
                {
                    ActiveTrack activeTrack = new ActiveTrack();
                    activeTrack.Name = nameof(TrackType.Active);
                    globalTrackDataList.Add(activeTrack);
                    localTrackList.Add(activeTrack);
                    foreach (var a in segment.AttachedActives)
                    {
                        if (a == null)
                        {
                            continue;
                        }
                        ActiveClipItem activeClipItem = new ActiveClipItem();
                        activeClipItem.ActiveData = a;
                        activeClipItem.Name = string.IsNullOrEmpty(a.Name) ? "Active" : a.Name;
                        activeClipItem.StartTime = segment.StartTime + (Mathf.Clamp01(a.NormalizedStart) * clipItem.Duration);
                        float dur = (Mathf.Clamp01(a.NormalizedEnd) - Mathf.Clamp01(a.NormalizedStart)) * clipItem.Duration;
                        activeClipItem.Duration = Mathf.Max(0f, dur);
                        activeClipItem.Frame = Mathf.RoundToInt(activeClipItem.Duration * 60f);
                        activeTrack.ClipList.Add(activeClipItem);
                    }
                }
            }
        }

        ApplyViewMode();
    }
    
    #endregion
}


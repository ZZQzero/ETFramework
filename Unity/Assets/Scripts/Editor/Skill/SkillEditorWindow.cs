using System;
using System.Collections.Generic;
using Animancer;
using ET;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public class SkillEditorWindow : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;
    private ObjectField selectConfigAsset;
    private ObjectField selectObj;
    private VisualElement trackContainer;
    private ListView listView;
    private VisualElement timelineRuler;
    private VisualElement root;
    private VisualElement playheadElement; // 播放进度条
    private VisualElement leftContainer; // Left容器引用
    private ScrollView timelineScrollView; // 时间轴滚动视图
    private VisualElement timelineContent; // 时间轴内容容器
    private Label timeLengthLabel; // 时间长度显示标签
    private Slider speedSlider; // 播放速度滑块
    private TextField speedNumField; // 播放速度数值输入框（可编辑）
    private Label configHintLabel; // Config提示标签
    
    // Right面板UI元素
    private Label trackTypeLabel; // 轨道类型标签
    private Label clipCountLabel; // 片段数标签
    private Label totalDurationLabel; // 总时长标签
    private Label clipPropertiesTitle; // Clip属性标题
    
    // 通用字段
    private TextField clipNameField; // Clip名称输入框
    private FloatField startTimeField; // 开始时间字段
    private FloatField frameField; // 帧数字段
    
    // Animation Clip字段
    private VisualElement animationFields; // Animation字段组
    private VisualElement animationActionButtons; // Animation操作按钮组
    private Button addEffectButton; // 添加特效按钮
    private Button addSoundButton; // 添加音效按钮
    private Button addHitboxButton; // 添加Hitbox按钮
    private ObjectField animationClipField; // 动画Clip引用字段
    private FloatField speedField; // 播放速度字段
    private FloatField durationField; // 持续时间字段
    private FloatField fadeDurationField; // 过渡时间字段
    
    // Effect Clip字段
    private VisualElement effectFields; // Effect字段组
    private ObjectField effectPrefabField; // 特效预制体字段
    private FloatField effectTriggerTimeField; // 特效触发时间字段
    private FloatField effectDurationField; // 特效持续时间字段
    private Toggle followTargetField; // 是否跟随目标字段
    
    // Sound Clip字段
    private VisualElement soundFields; // Sound字段组
    private ObjectField audioClipField; // 音频Clip字段
    private FloatField soundTriggerTimeField; // 音效触发时间字段
    private FloatField volumeField; // 音量字段
    
    // HitBox Clip字段
    private VisualElement hitBoxFields; // HitBox字段组
    private EnumField shapeTypeField; // 形状类型字段
    private FloatField hitBoxStartTimeField; // HitBox开始时间字段
    private FloatField hitBoxEndTimeField; // HitBox结束时间字段
    
    private AttackConfig config;
    private AnimancerComponent animancer;
    
    private List<ITrackItem> trackDataList = new List<ITrackItem>();
    private static int nextTrackId = 1; // 轨道ID生成器
    
    // 当前选中的轨道和Clip
    private ITrackItem selectedTrack; // 当前选中的轨道
    private IClipItem selectedClip; // 当前选中的Clip

    private const float TRACK_ITEM_HEIGHT = 40f; // 轨道条目的高度
    private const float CLIP_ITEM_HEIGHT = 35f; // clip条目的高度
    private const float RULER_HEIGHT = 30f; // 时间轴标尺高度
    private const float BASE_PIXELS_PER_SECOND = 50f; // 基础每秒像素数（缩放倍数为1时）
    private const float MIN_PIXELS_PER_SECOND = 50f; // 最小每秒像素数（对应缩放倍数1）
    private const float MAX_PIXELS_PER_SECOND = 500f; // 最大每秒像素数（对应缩放倍数10）
    private float pixelsPerSecond = BASE_PIXELS_PER_SECOND; // 时间轴每秒像素数
    private float zoomScale => pixelsPerSecond / BASE_PIXELS_PER_SECOND; // 缩放倍数（由像素推导）
    private bool isDragging = false;
    private Vector2 dragStartPosition;
    private float dragOffset;
    private bool isDraggingPlayhead = false; // 是否正在拖动播放进度条
    private float currentPlaybackTime = 0f; // 当前播放时间（秒）
    private float playbackSpeed = 1f; // 播放速度（0-6）
    private bool isPlaying = false; // 是否正在播放
    private bool isLooping = false; // 是否循环播放
    
    private const float PLAYHEAD_LINE_WIDTH = 2f;
    private const float PLAYHEAD_WIDTH = PLAYHEAD_LINE_WIDTH;
    private const float PLAYHEAD_HALF_WIDTH = PLAYHEAD_WIDTH * 0.5f;
    
    // 根据轨道类型获取颜色
    private Color GetTrackColor(TrackType type)
    {
        return type switch
        {
            TrackType.Animation => new Color(0.2f, 0.6f, 0.9f, 0.3f), // 蓝色
            TrackType.Effect => new Color(0.9f, 0.4f, 0.2f, 0.3f), // 橙色
            TrackType.Sound => new Color(0.4f, 0.8f, 0.4f, 0.3f), // 绿色
            TrackType.Hitbox => new Color(0.9f, 0.2f, 0.2f, 0.3f), // 红色
            _ => new Color(0.5f, 0.5f, 0.5f, 0.3f) // 灰色默认
        };
    }
    
    // 根据clip类型获取颜色
    private Color GetClipColor(TrackType type)
    {
        return type switch
        {
            TrackType.Animation => new Color(0.2f, 0.6f, 0.9f, 0.5f), // 蓝色
            TrackType.Effect => new Color(0.9f, 0.4f, 0.2f, 0.5f), // 橙色
            TrackType.Sound => new Color(0.4f, 0.8f, 0.4f, 0.5f), // 绿色
            TrackType.Hitbox => new Color(0.9f, 0.2f, 0.2f, 0.5f), // 红色
            _ => new Color(0.5f, 0.5f, 0.5f, 0.8f) // 灰色默认
        };
    }
    
    [MenuItem("ET/SkillEditorWindow")]
    public static void ShowExample()
    {
        SkillEditorWindow wnd = GetWindow<SkillEditorWindow>();
        wnd.titleContent = new GUIContent("SkillEditorWindow");
    }

    public void CreateGUI()
    {
        root = rootVisualElement;

        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/Editor/Skill/SkillEditorWindow.uxml");
        visualTree.CloneTree(root);
        
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/Skill/SkillEditorWindow.uss");
        root.styleSheets.Add(styleSheet);
        InitData();
    }
    
    private void InitData()
    {
        selectConfigAsset = root.Q<ObjectField>("SelectConfig");
        selectConfigAsset.objectType = typeof(AttackConfigAsset);
        selectObj = root.Q<ObjectField>("SelectObj");
        selectObj.objectType = typeof(GameObject);

        // 监听selectConfigAsset和selectObj的值变化
        selectConfigAsset.RegisterValueChangedCallback(OnObjectFieldChanged);
        selectObj.RegisterValueChangedCallback(OnObjectFieldChanged);

        // 获取ScrollView和内容容器
        timelineScrollView = root.Q<ScrollView>("TimelineScrollView");
        timelineContent = root.Q<VisualElement>("TimelineContent");
        trackContainer = root.Q<VisualElement>("TrackContainer");
        
        listView = root.Q<ListView>("InfoListView");
        var refresh = root.Q<Button>("Refresh");
        refresh.clicked += RefreshTrackContent;
        
        // 注册播放控制按钮的点击事件
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
        
        // 获取Left容器引用
        leftContainer = root.Q<VisualElement>("Left");
        
        // 设置ScrollView的滚动事件监听
        if (timelineScrollView != null)
        {
            timelineScrollView.horizontalScroller.valueChanged += OnTimelineScrollChanged;
            
            // 监听ScrollView大小变化，更新内容宽度和刻度
            timelineScrollView.RegisterCallback<GeometryChangedEvent>(evt => {
                UpdateTimelineContentWidth();
                DrawTimelineRulerMarks();
            });
            
        }
        
        // 获取时间长度显示标签
        timeLengthLabel = root.Q<Label>("TimeLength");
        
        // 初始化时间显示
        UpdateTimeLengthDisplay();
        
        // 初始化播放速度控制
        InitPlaybackSpeedControl();
        
        // 初始化Config提示标签
        InitConfigHint();
        
        // 初始化Right面板UI元素
        InitRightPanel();
    }
    
    // 初始化Right面板UI元素
    private void InitRightPanel()
    {
        // 先获取Right容器
        var rightContainer = root.Q<VisualElement>("Right");
        if (rightContainer == null)
        {
            Debug.LogWarning("Right容器未找到！");
            return;
        }
        
        // 获取轨道信息面板的UI元素
        trackTypeLabel = rightContainer.Q<Label>("TrackTypeLabel");
        clipCountLabel = rightContainer.Q<Label>("ClipCountLabel");
        totalDurationLabel = rightContainer.Q<Label>("TotalDurationLabel");
        
        // 获取Clip属性面板的标题和字段组
        clipPropertiesTitle = rightContainer.Q<Label>("ClipPropertiesTitle");
        animationFields = rightContainer.Q<VisualElement>("AnimationFields");
        effectFields = rightContainer.Q<VisualElement>("EffectFields");
        soundFields = rightContainer.Q<VisualElement>("SoundFields");
        hitBoxFields = rightContainer.Q<VisualElement>("HitBoxFields");
        
        // 获取通用字段
        clipNameField = rightContainer.Q<TextField>("ClipNameField");
        startTimeField = rightContainer.Q<FloatField>("StartTimeField");
        frameField = rightContainer.Q<FloatField>("FrameField");
        
        // 获取Animation Clip字段
        animationActionButtons = rightContainer.Q<VisualElement>("AnimationActionButtons");
        addEffectButton = rightContainer.Q<Button>("AddEffectButton");
        addSoundButton = rightContainer.Q<Button>("AddSoundButton");
        addHitboxButton = rightContainer.Q<Button>("AddHitboxButton");
        animationClipField = rightContainer.Q<ObjectField>("AnimationClipField");
        if (animationClipField != null)
        {
            animationClipField.objectType = typeof(UnityEngine.AnimationClip);
        }
        speedField = rightContainer.Q<FloatField>("SpeedField");
        durationField = rightContainer.Q<FloatField>("DurationField");
        fadeDurationField = rightContainer.Q<FloatField>("FadeDurationField");
        
        // 获取Effect Clip字段
        effectPrefabField = rightContainer.Q<ObjectField>("EffectPrefabField");
        if (effectPrefabField != null)
        {
            effectPrefabField.objectType = typeof(UnityEngine.GameObject);
        }
        effectTriggerTimeField = rightContainer.Q<FloatField>("EffectTriggerTimeField");
        effectDurationField = rightContainer.Q<FloatField>("EffectDurationField");
        followTargetField = rightContainer.Q<Toggle>("FollowTargetField");
        
        // 获取Sound Clip字段
        audioClipField = rightContainer.Q<ObjectField>("AudioClipField");
        if (audioClipField != null)
        {
            audioClipField.objectType = typeof(UnityEngine.AudioClip);
        }
        soundTriggerTimeField = rightContainer.Q<FloatField>("SoundTriggerTimeField");
        volumeField = rightContainer.Q<FloatField>("VolumeField");
        
        // 获取HitBox Clip字段
        shapeTypeField = rightContainer.Q<EnumField>("ShapeTypeField");
        if (shapeTypeField != null)
        {
            shapeTypeField.Init(HitShapeType.Box);
        }
        hitBoxStartTimeField = rightContainer.Q<FloatField>("HitBoxStartTimeField");
        hitBoxEndTimeField = rightContainer.Q<FloatField>("HitBoxEndTimeField");
        
        // 注册所有字段的值变化事件
        if (clipNameField != null)
        {
            clipNameField.RegisterValueChangedCallback(OnClipNameChanged);
        }
        if (startTimeField != null)
        {
            startTimeField.RegisterValueChangedCallback(OnStartTimeChanged);
        }
        // Frame字段不注册回调，因为AnimationClip的帧数是只读的，由动画时长自动计算
        if (addEffectButton != null)
        {
            addEffectButton.clicked += OnAddEffectButtonClicked;
        }
        if (addSoundButton != null)
        {
            addSoundButton.clicked += OnAddSoundButtonClicked;
        }
        if (addHitboxButton != null)
        {
            addHitboxButton.clicked += OnAddHitboxButtonClicked;
        }
        if (animationClipField != null)
        {
            animationClipField.RegisterValueChangedCallback(OnAnimationClipChanged);
        }
        if (speedField != null)
        {
            speedField.RegisterValueChangedCallback(OnSpeedChanged);
        }
        if (fadeDurationField != null)
        {
            fadeDurationField.RegisterValueChangedCallback(OnFadeDurationChanged);
        }
        if (effectPrefabField != null)
        {
            effectPrefabField.RegisterValueChangedCallback(OnEffectPrefabChanged);
        }
        if (effectTriggerTimeField != null)
        {
            effectTriggerTimeField.RegisterValueChangedCallback(OnEffectTriggerTimeChanged);
        }
        if (effectDurationField != null)
        {
            effectDurationField.RegisterValueChangedCallback(OnEffectDurationChanged);
        }
        if (followTargetField != null)
        {
            followTargetField.RegisterValueChangedCallback(OnFollowTargetChanged);
        }
        if (audioClipField != null)
        {
            audioClipField.RegisterValueChangedCallback(OnAudioClipChanged);
        }
        if (soundTriggerTimeField != null)
        {
            soundTriggerTimeField.RegisterValueChangedCallback(OnSoundTriggerTimeChanged);
        }
        if (volumeField != null)
        {
            volumeField.RegisterValueChangedCallback(OnVolumeChanged);
        }
        if (shapeTypeField != null)
        {
            shapeTypeField.RegisterValueChangedCallback(OnShapeTypeChanged);
        }
        if (hitBoxStartTimeField != null)
        {
            hitBoxStartTimeField.RegisterValueChangedCallback(OnHitBoxStartTimeChanged);
        }
        if (hitBoxEndTimeField != null)
        {
            hitBoxEndTimeField.RegisterValueChangedCallback(OnHitBoxEndTimeChanged);
        }
        
        // 初始状态：隐藏所有字段组
        HideAllClipFields();
        
        // 初始状态：没有选中任何轨道或Clip
        UpdateTrackInfo(null);
        UpdateClipProperties(null);
    }
    
    // 隐藏所有Clip字段组
    private void HideAllClipFields()
    {
        if (animationFields != null) animationFields.style.display = DisplayStyle.None;
        if (effectFields != null) effectFields.style.display = DisplayStyle.None;
        if (soundFields != null) soundFields.style.display = DisplayStyle.None;
        if (hitBoxFields != null) hitBoxFields.style.display = DisplayStyle.None;
        if (animationActionButtons != null) animationActionButtons.style.display = DisplayStyle.None;
    }
    
    // 初始化Config提示标签
    private void InitConfigHint()
    {
        if (timelineContent == null) return;
        
        // 创建提示标签
        configHintLabel = new Label("请选择Config和角色");
        configHintLabel.name = "ConfigHint";
        configHintLabel.style.position = Position.Absolute;
        configHintLabel.style.left = 0;
        configHintLabel.style.top = 0;
        configHintLabel.style.width = Length.Percent(100);
        configHintLabel.style.height = Length.Percent(100);
        configHintLabel.style.fontSize = 20;
        configHintLabel.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        configHintLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        configHintLabel.style.display = DisplayStyle.Flex;
        timelineContent.Add(configHintLabel);
    }
    
    // 更新Config提示显示状态
    private void UpdateConfigHint()
    {
        if (configHintLabel == null) return;
        
        // 如果config为空，显示提示；否则隐藏提示
        if (config == null)
        {
            configHintLabel.style.display = DisplayStyle.Flex;
        }
        else
        {
            configHintLabel.style.display = DisplayStyle.None;
        }
    }
    
    
    // 当ScrollView滚动时更新playhead位置
    private void OnTimelineScrollChanged(float value)
    {
        UpdatePlayheadPosition();
    }
    
    
    // 获取时间轴可视区域宽度
    private float GetTimelineViewWidth()
    {
        if (timelineScrollView == null)
        {
            return 800f;
        }
        float viewWidth = timelineScrollView.layout.width;
        return viewWidth > 0 ? viewWidth : 800f;
    }
    
    // 获取内容宽度
    private float GetContentWidth()
    {
        float viewWidth = GetTimelineViewWidth();
        
        if (config == null)
        {
            return viewWidth;
        }
        // 基础内容宽度 = 可视区域宽度 × 缩放倍数
        float baseContentWidth = viewWidth * zoomScale;
        
        // 获取clip的最大结束位置，
        float maxClipEndTime = GetMaxClipEndTime();
        float clipBasedWidth = maxClipEndTime > 0 ? maxClipEndTime * pixelsPerSecond : 0f;
        // 内容宽度 = max(缩放后的宽度, clip最大位置)
        return Mathf.Max(baseContentWidth, clipBasedWidth);
    }
    
    // 更新时间轴内容容器的宽度
    private void UpdateTimelineContentWidth()
    {
        if (timelineContent == null) return;
        
        float contentWidth = GetContentWidth();
        
        timelineContent.style.width = contentWidth;
        timelineContent.style.minWidth = contentWidth;
        // 更新标尺的宽度
        if (timelineRuler != null)
        {
            timelineRuler.style.width = contentWidth;
            timelineRuler.style.minWidth = contentWidth;
        }
        // 同时更新所有轨道的宽度
        UpdateAllTrackWidths();
    }
    
    // 更新所有轨道的宽度（含视觉缓冲，拖动时不穿帮）
    private void UpdateAllTrackWidths()
    {
        if (trackContainer == null) return;
        
        // 轨道宽度
        float trackWidth = GetContentWidth();
        
        var trackElements = trackContainer.Query<VisualElement>(name: "track").ToList();
        foreach (var trackElement in trackElements)
        {
            trackElement.style.width = trackWidth;
            trackElement.style.minWidth = trackWidth;
        }
    }
    
    // 获取所有clip中最大的结束时间
    private float GetMaxClipEndTime()
    {
        float maxEndTime = 0f;
        foreach (var trackItem in trackDataList)
        {
            if (trackItem is AnimationTrack animTrack)
            {
                foreach (var clip in animTrack.ClipList)
                {
                    float endTime = clip.StartTime + clip.Length;
                    if (endTime > maxEndTime) maxEndTime = endTime;
                }
            }
            else if (trackItem is EffectTrack effectTrack)
            {
                foreach (var clip in effectTrack.ClipList)
                {
                    float endTime = clip.StartTime + clip.Length;
                    if (endTime > maxEndTime) maxEndTime = endTime;
                }
            }
            else if (trackItem is SoundTrack soundTrack)
            {
                foreach (var clip in soundTrack.ClipList)
                {
                    float endTime = clip.StartTime + clip.Length;
                    if (endTime > maxEndTime) maxEndTime = endTime;
                }
            }
            else if (trackItem is HitBoxTrack hitBoxTrack)
            {
                foreach (var clip in hitBoxTrack.ClipList)
                {
                    float endTime = clip.StartTime + clip.Length;
                    if (endTime > maxEndTime) maxEndTime = endTime;
                }
            }
        }
        return maxEndTime;
    }
    
    // 获取当前水平滚动偏移量
    private float GetScrollOffset()
    {
        if (timelineScrollView == null) return 0f;
        return timelineScrollView.horizontalScroller.value;
    }
    
    // 初始化播放速度控制
    private void InitPlaybackSpeedControl()
    {
        // 找到Speed标签后面的Slider（在Top容器中查找）
        var topContainer = root.Q<VisualElement>("Top");
        if (topContainer != null)
        {
            // 查找Slider（在Speed标签后面）
            var speedLabel = root.Q<Label>("Speed");
            if (speedLabel != null)
            {
                // 查找Speed标签后面的Slider
                var elements = topContainer.Children();
                bool foundSpeed = false;
                foreach (var element in elements)
                {
                    if (element == speedLabel)
                    {
                        foundSpeed = true;
                        continue;
                    }
                    if (foundSpeed && element is Slider slider)
                    {
                        speedSlider = slider;
                        break;
                    }
                }
            }
            
            // 获取SpeedNum（TextField，可直接编辑）
            speedNumField = root.Q<TextField>("SpeedNum");
        }
        
        // 设置Slider范围（0-6）
        if (speedSlider != null)
        {
            speedSlider.lowValue = 0f;
            speedSlider.highValue = 6f;
            speedSlider.value = playbackSpeed; // 初始值1.0
            
            // 监听Slider值变化
            speedSlider.RegisterValueChangedCallback(evt => {
                playbackSpeed = evt.newValue;
                UpdateSpeedDisplay(); // 更新TextField显示（不会触发TextField的ValueChanged，因为设置的是新值）
            });
        }
        
        // 监听SpeedNum值变化（用户直接编辑时）
        if (speedNumField != null)
        {
            speedNumField.RegisterValueChangedCallback(evt => {
                // 解析输入的值（可能是"2x"、"0.5x"或"2"、"0.5"等格式）
                string input = evt.newValue.Trim();
                
                // 移除"x"后缀（如果有）
                if (input.EndsWith("x", StringComparison.OrdinalIgnoreCase))
                {
                    input = input.Substring(0, input.Length - 1);
                }
                
                // 尝试解析为浮点数
                if (float.TryParse(input, out float speedValue))
                {
                    // 限制在0-6范围内
                    speedValue = Mathf.Clamp(speedValue, 0f, 6f);
                    playbackSpeed = speedValue;
                    
                    // 更新Slider位置（使用SetValueWithoutNotify避免触发回调）
                    if (speedSlider != null)
                    {
                        speedSlider.SetValueWithoutNotify(playbackSpeed);
                    }
                    
                    // 更新显示（会自动格式化）
                    UpdateSpeedDisplay();
                }
                else
                {
                    // 解析失败，恢复之前的显示
                    UpdateSpeedDisplay();
                }
            });
        }
        
        // 初始化显示
        UpdateSpeedDisplay();
    }
    
    // 更新速度显示
    private void UpdateSpeedDisplay()
    {
        if (speedNumField == null) return;
        
        // 格式化显示：如果速度是整数，显示为"2x"，否则显示为"0.5x"
        if (Mathf.Approximately(playbackSpeed, Mathf.Round(playbackSpeed)))
        {
            speedNumField.value = $"{(int)playbackSpeed}x";
        }
        else
        {
            speedNumField.value = $"{playbackSpeed:F1}x";
        }
    }


    private void OnObjectFieldChanged(ChangeEvent<Object> evt)
    {
        if (selectObj != null && selectObj.value != null &&
            selectConfigAsset != null && selectConfigAsset.value != null)
        {
            InitTrackData();
            InitTimelineRuler();
            InitPlayHead();
            CreateTrack();
            UpdateConfigHint();
            DrawTimelineRulerMarks();
        }
    }

    #region 轨道相关

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

        trackDataList.Clear();
        trackContainer.Clear();

        AnimationTrack animationTrack = new AnimationTrack();
        animationTrack.Name = nameof(TrackType.Animation);
        animationTrack.Color = GetTrackColor(TrackType.Animation);
        trackDataList.Add(animationTrack);
        
        if (config.Segments.Count > 0)
        {
            foreach (var segment in config.Segments)
            {
                AnimationClipItem clipItem = new AnimationClipItem();

                clipItem.SegmentData = segment;
                clipItem.Length = 2f;
                clipItem.Name = "NULL";
                if (clipItem.SegmentData != null && clipItem.SegmentData.AnimationClipTrans != null)
                {
                    if (clipItem.SegmentData.AnimationClipTrans.Clip != null)
                    {
                        clipItem.Length = clipItem.SegmentData.AnimationClipTrans.Clip.length;
                    }
                    if (!string.IsNullOrEmpty(clipItem.SegmentData.Name))
                    {
                        clipItem.Name = clipItem.SegmentData.Name;
                    }
                    else
                    {
                        clipItem.Name = clipItem.SegmentData.AnimationClipTrans.Name;
                    }
                    clipItem.Frame = Mathf.RoundToInt(clipItem.Length * 60f);
                }

                clipItem.StartTime = segment.StartTime;
                clipItem.Color = GetClipColor(TrackType.Animation);
                animationTrack.ClipList.Add(clipItem);
                
                if (segment.VisualEffects.Count > 0)
                {
                    EffectTrack effectTrack = new EffectTrack();
                    effectTrack.Name = nameof(TrackType.Effect);
                    effectTrack.Color = GetTrackColor(TrackType.Effect);
                    trackDataList.Add(effectTrack);
                    foreach (var effect in segment.VisualEffects)
                    {
                        EffectClipItem effectClipItem = new EffectClipItem();
                        effectClipItem.EffectData = effect;
                        effectClipItem.Name = effect.Name;
                        effectClipItem.StartTime = segment.StartTime + effect.StartTime * clipItem.Length;;
                        effectClipItem.Length = effect.Length;
                        effectClipItem.Color = GetClipColor(TrackType.Effect);
                        effectClipItem.Frame = Mathf.RoundToInt(effectClipItem.Length * 60f);
                        effectTrack.ClipList.Add(effectClipItem);
                    }
                }

                if (segment.SoundEffects.Count > 0)
                {
                    SoundTrack soundTrack = new SoundTrack();
                    soundTrack.Name = nameof(TrackType.Sound);
                    soundTrack.Color = GetTrackColor(TrackType.Sound);
                    trackDataList.Add(soundTrack);
                    foreach (var sound in segment.SoundEffects)
                    {
                        SoundClipItem soundClipItem = new SoundClipItem();
                        soundClipItem.SoundData = sound;
                        soundClipItem.Name = sound.Name;
                        soundClipItem.StartTime = segment.StartTime + (sound.StartTime * clipItem.Length);
                        soundClipItem.Length = sound.Clip != null ?  sound.Clip.length : 2;
                        soundClipItem.Color = GetClipColor(TrackType.Sound);
                        soundClipItem.Frame = Mathf.RoundToInt(soundClipItem.Length * 60f);
                        soundTrack.ClipList.Add(soundClipItem);
                    }
                }

                if (segment.HitBoxes.Count > 0)
                {
                    HitBoxTrack hitBoxTrack = new HitBoxTrack();
                    hitBoxTrack.Name = nameof(TrackType.Hitbox);
                    hitBoxTrack.Color = GetTrackColor(TrackType.Hitbox);
                    trackDataList.Add(hitBoxTrack);
                    foreach (var hitbox in segment.HitBoxes)
                    {
                        HitBoxClipItem hitboxClipItem = new HitBoxClipItem();
                        hitboxClipItem.HitBoxData = hitbox;
                        hitboxClipItem.Name = hitbox.ShapeType.ToString();
                        hitboxClipItem.StartTime = segment.StartTime + (hitbox.StartTime * clipItem.Length);
                        hitboxClipItem.Length = (hitbox.EndTime - hitbox.StartTime) * clipItem.Length;
                        hitboxClipItem.Color = GetClipColor(TrackType.Hitbox);
                        hitboxClipItem.Frame = Mathf.RoundToInt(hitboxClipItem.Length * 60f);
                        hitBoxTrack.ClipList.Add(hitboxClipItem);
                    }
                }
            }
        }
    }

    private void CreateTrack()
    {
        // 创建轨道元素
        for (int i = 0; i < trackDataList.Count; i++)
        {
            var trackElement = CreateTrackElement(trackDataList[i], i);
            trackContainer.Add(trackElement);
        }
        
        // 更新播放进度条高度（轨道数量可能改变）
        if (playheadElement != null)
        {
            UpdatePlayheadSize();
        }
        
        // 如果有选中的轨道，更新轨道信息显示
        if (selectedTrack != null)
        {
            // 检查选中的轨道是否仍然存在
            bool trackStillExists = trackDataList.Contains(selectedTrack);
            if (trackStillExists)
            {
                UpdateTrackInfo(selectedTrack);
            }
            else
            {
                // 轨道已不存在，清空选择
                UpdateTrackInfo(null);
                UpdateClipProperties(null);
            }
        }
        else if (trackDataList.Count > 0)
        {
            // 如果没有选中轨道，默认选中第一个轨道
            UpdateTrackInfo(trackDataList[0]);
        }
    }
    
    // 创建轨道元素
    private VisualElement CreateTrackElement(ITrackItem trackData, int index)
    {
        var trackElement = new VisualElement();
        trackElement.name = "track";
        trackElement.AddToClassList("timeline-track");
        trackElement.style.position = Position.Relative;
        trackElement.style.height = TRACK_ITEM_HEIGHT;
        trackElement.style.overflow = Overflow.Visible; // 允许clip超出显示（ScrollView会处理滚动）
        trackElement.userData = trackData;
        trackData.Index = index;
        
        // 设置轨道宽度
        float trackWidth = GetContentWidth();
        trackElement.style.width = trackWidth;
        trackElement.style.minWidth = trackWidth;
        
        // 设置轨道背景颜色
        trackElement.style.backgroundColor = trackData.Color;
        
        // 给轨道容器添加点击事件和右键菜单
        SetupTrackInteractions(trackElement);
        ContextualMenuManipulator menuManipulator = new ContextualMenuManipulator(menuEvent => 
            BuildContextMenu(menuEvent, trackElement));
        trackElement.AddManipulator(menuManipulator);
        
        // 创建轨道中的clip
        List<VisualElement> clipElements = new List<VisualElement>();
        
        if (trackData is AnimationTrack animationTrack)
        {
            foreach (var clipItem in animationTrack.ClipList)
            {
                var clipElement = SetClipItem(trackElement, clipItem);
                if (clipItem.SegmentData != null && clipItem.SegmentData.AnimationClipTrans != null)
                {
                    if (clipElement is Label label)
                    {
                        label.text = clipItem.Name;
                    }
                }
                clipElements.Add(clipElement);
            }
        }
        else if (trackData is EffectTrack effectTrack)
        {
            foreach (var clipItem in effectTrack.ClipList)
            {
                var clipElement = SetClipItem(trackElement, clipItem);
                clipElements.Add(clipElement);
            }
        }
        else if (trackData is SoundTrack soundTrack)
        {
            foreach (var clipItem in soundTrack.ClipList)
            {
                var clipElement = SetClipItem(trackElement, clipItem);
                clipElements.Add(clipElement);
            }
        }
        else if (trackData is HitBoxTrack hitBoxTrack)
        {
            foreach (var clipItem in hitBoxTrack.ClipList)
            {
                var clipElement = SetClipItem(trackElement, clipItem);
                clipElements.Add(clipElement);
            }
        }
        
        // 检测并高亮显示重叠区域
        HighlightOverlappingClips(trackElement, clipElements);
        
        return trackElement;
    }

    private void BuildContextMenu(ContextualMenuPopulateEvent menuEvent, VisualElement element)
    {
        menuEvent.menu.AppendAction("添加Clip", action =>
        {
            if (element.userData is AnimationTrack trackData)
            {
                AnimationClipItem clipItem = new AnimationClipItem();
                AttackSegmentData data = new AttackSegmentData();
                config.Segments.Add(data);
                clipItem.Name = "AnimationClip";
                clipItem.Length = 2;
                clipItem.SegmentData = data;
                clipItem.Color = GetClipColor(TrackType.Animation);
                // AnimationClip的帧数根据动画时长和速度自动计算，这里先设置为0
                clipItem.Frame = 0;
                trackData.ClipList.Add(clipItem);
                
                // 重新创建轨道内容
                RefreshTrackContent();
            }
        });
    }
    
    // 刷新轨道内容
    private void RefreshTrackContent()
    {
        if (trackContainer == null) return;
        
        trackContainer.Clear();
        
        for (int i = 0; i < trackDataList.Count; i++)
        {
            var trackElement = CreateTrackElement(trackDataList[i], i);
            trackContainer.Add(trackElement);
        }
        
        // 更新播放进度条高度
        UpdatePlayheadSize();
        
        // 如果有选中的轨道或Clip，更新显示
        if (selectedTrack != null)
        {
            UpdateTrackInfo(selectedTrack);
        }
        if (selectedClip != null)
        {
            UpdateClipProperties(selectedClip);
        }
    }

    private VisualElement SetClipItem(VisualElement element, IClipItem clipItem)
    {
        var clipElement = new Label(clipItem.Name);
        clipElement.text = clipItem.Name;
        clipElement.AddToClassList("timeline-clip");
        clipElement.style.position = Position.Absolute; // 绝对定位以支持拖拽

        // 根据开始时间和时长计算位置和宽度
        float xPosition = clipItem.StartTime * pixelsPerSecond;
        float clipWidth = Mathf.Max(clipItem.Length * pixelsPerSecond, 20f); // 最小宽度20像素
        clipElement.style.left = xPosition;
        clipElement.style.top = (TRACK_ITEM_HEIGHT - CLIP_ITEM_HEIGHT) / 2; // 垂直居中
        clipElement.style.height = CLIP_ITEM_HEIGHT;
        clipElement.style.width = clipWidth;
        clipElement.text = clipItem.Name;
        
        // 设置clip背景颜色
        clipElement.style.backgroundColor = clipItem.Color;
        clipElement.style.borderTopWidth = 1;
        clipElement.style.borderBottomWidth = 1;
        clipElement.style.borderLeftWidth = 1;
        clipElement.style.borderRightWidth = 1;
        clipElement.style.borderTopColor = new Color(clipItem.Color.r * 0.7f, clipItem.Color.g * 0.7f, clipItem.Color.b * 0.7f, 1f);
        clipElement.style.borderBottomColor = new Color(clipItem.Color.r * 0.7f, clipItem.Color.g * 0.7f, clipItem.Color.b * 0.7f, 1f);
        clipElement.style.borderLeftColor = new Color(clipItem.Color.r * 0.7f, clipItem.Color.g * 0.7f, clipItem.Color.b * 0.7f, 1f);
        clipElement.style.borderRightColor = new Color(clipItem.Color.r * 0.7f, clipItem.Color.g * 0.7f, clipItem.Color.b * 0.7f, 1f);

        SetupClipInteractions(clipElement);
        element.Add(clipElement);

        // 存储clip数据到元素中，方便后续重叠检测
        clipElement.userData = clipItem;

        return clipElement;
    }

    // 检测并高亮显示重叠的clip - 只高亮重叠部分
    private void HighlightOverlappingClips(VisualElement trackElement, List<VisualElement> clipElements)
    {
        // 清除之前的所有高亮覆盖层
        var existingHighlights = trackElement.Query<VisualElement>(className: "overlap-highlight").ToList();
        foreach (var highlight in existingHighlights)
        {
            trackElement.Remove(highlight);
        }

        // 检测所有clip之间的重叠
        for (int i = 0; i < clipElements.Count; i++)
        {
            var clip1 = clipElements[i];
            var clipItem1 = clip1.userData as IClipItem;
            if (clipItem1 == null) continue;

            float clip1Start = clipItem1.StartTime;
            float clip1End = clipItem1.StartTime + clipItem1.Length;

            for (int j = i + 1; j < clipElements.Count; j++)
            {
                var clip2 = clipElements[j];
                var clipItem2 = clip2.userData as IClipItem;
                if (clipItem2 == null) continue;

                float clip2Start = clipItem2.StartTime;
                float clip2End = clipItem2.StartTime + clipItem2.Length;

                // 检查是否重叠
                if (clip1Start < clip2End && clip2Start < clip1End)
                {
                    // 计算重叠区域
                    float overlapStart = Mathf.Max(clip1Start, clip2Start);
                    float overlapEnd = Mathf.Min(clip1End, clip2End);
                    float overlapDuration = overlapEnd - overlapStart;

                    if (overlapDuration > 0)
                    {
                        // 计算重叠程度 (0-1)
                        float totalDuration = Mathf.Max(clip1End, clip2End) - Mathf.Min(clip1Start, clip2Start);
                        float overlapRatio = overlapDuration / totalDuration;

                        // 创建高亮覆盖层，只覆盖重叠部分
                        var highlightElement = new VisualElement();
                        highlightElement.AddToClassList("overlap-highlight");
                        highlightElement.style.position = Position.Absolute;

                        float highlightX = overlapStart * pixelsPerSecond;
                        float highlightWidth = overlapDuration * pixelsPerSecond;
                        float highlightY = (TRACK_ITEM_HEIGHT - CLIP_ITEM_HEIGHT) / 2;
                        float highlightHeight = CLIP_ITEM_HEIGHT;

                        highlightElement.style.left = highlightX;
                        highlightElement.style.top = highlightY;
                        highlightElement.style.width = highlightWidth;
                        highlightElement.style.height = highlightHeight;

                        // 根据重叠程度调整颜色深度
                        Color highlightColor = Color.Lerp(
                            new Color(1f, 1f, 0f, 0.3f),  // 浅黄色（轻微重叠）
                            new Color(1f, 0.4f, 0f, 0.7f), // 深橙色（高度重叠）
                            overlapRatio
                        );

                        highlightElement.style.backgroundColor = highlightColor;
                        highlightElement.style.borderTopWidth = 2;
                        highlightElement.style.borderTopColor = new Color(1f, 0.6f, 0f, 0.9f);
                        highlightElement.style.borderBottomWidth = 2;
                        highlightElement.style.borderBottomColor = new Color(1f, 0.6f, 0f, 0.9f);

                        // 确保高亮层在clip之上但低于鼠标事件层级
                        highlightElement.pickingMode = PickingMode.Ignore; // 不接收鼠标事件，让下面的clip能正常交互

                        trackElement.Add(highlightElement);
                    }
                }
            }
        }
    }

    // 拖拽过程中实时检测当前clip与其他clip的重叠
    private void HighlightDraggingClipOverlap(VisualElement trackElement, List<VisualElement> clipElements, VisualElement draggingClip)
    {
        // 清除之前的所有高亮覆盖层
        var existingHighlights = trackElement.Query<VisualElement>(className: "overlap-highlight").ToList();
        foreach (var highlight in existingHighlights)
        {
            trackElement.Remove(highlight);
        }

        var draggingClipItem = draggingClip.userData as IClipItem;
        if (draggingClipItem == null) return;

        // 计算拖拽clip的当前时间位置
        float draggingClipStart = draggingClip.style.left.value.value / pixelsPerSecond;
        float draggingClipEnd = draggingClipStart + draggingClipItem.Length;

        // 检测所有clip之间的重叠（包括被拖动clip与其他clip的重叠，以及其他clip之间的重叠）
        for (int i = 0; i < clipElements.Count; i++)
        {
            var clip1 = clipElements[i];
            var clipItem1 = clip1.userData as IClipItem;
            if (clipItem1 == null) continue;

            // 对于被拖动的clip，使用当前位置；对于其他clip，使用存储的StartTime
            float clip1Start, clip1End;
            if (clip1 == draggingClip)
            {
                clip1Start = draggingClipStart;
                clip1End = draggingClipEnd;
            }
            else
            {
                clip1Start = clipItem1.StartTime;
                clip1End = clipItem1.StartTime + clipItem1.Length;
            }

            for (int j = i + 1; j < clipElements.Count; j++)
            {
                var clip2 = clipElements[j];
                var clipItem2 = clip2.userData as IClipItem;
                if (clipItem2 == null) continue;

                // 对于被拖动的clip，使用当前位置；对于其他clip，使用存储的StartTime
                float clip2Start, clip2End;
                if (clip2 == draggingClip)
                {
                    clip2Start = draggingClipStart;
                    clip2End = draggingClipEnd;
                }
                else
                {
                    clip2Start = clipItem2.StartTime;
                    clip2End = clipItem2.StartTime + clipItem2.Length;
                }

                // 检查是否重叠
                if (clip1Start < clip2End && clip2Start < clip1End)
                {
                    // 计算重叠区域
                    float overlapStart = Mathf.Max(clip1Start, clip2Start);
                    float overlapEnd = Mathf.Min(clip1End, clip2End);
                    float overlapDuration = overlapEnd - overlapStart;

                    if (overlapDuration > 0)
                    {
                        // 计算重叠程度 (0-1)
                        float totalDuration = Mathf.Max(clip1End, clip2End) - Mathf.Min(clip1Start, clip2Start);
                        float overlapRatio = overlapDuration / totalDuration;

                        // 判断是否涉及被拖动的clip，使用不同的颜色
                        bool isDraggingRelated = (clip1 == draggingClip || clip2 == draggingClip);

                        // 创建高亮覆盖层，只覆盖重叠部分
                        var highlightElement = new VisualElement();
                        highlightElement.AddToClassList("overlap-highlight");
                        highlightElement.style.position = Position.Absolute;

                        float highlightX = overlapStart * pixelsPerSecond;
                        float highlightWidth = overlapDuration * pixelsPerSecond;
                        float highlightY = (TRACK_ITEM_HEIGHT - CLIP_ITEM_HEIGHT) / 2;
                        float highlightHeight = CLIP_ITEM_HEIGHT;

                        highlightElement.style.left = highlightX;
                        highlightElement.style.top = highlightY;
                        highlightElement.style.width = highlightWidth;
                        highlightElement.style.height = highlightHeight;

                        // 根据是否涉及被拖动clip和重叠程度调整颜色深度
                        Color highlightColor;
                        if (isDraggingRelated)
                        {
                            // 被拖动clip的重叠 - 使用更明显的颜色（红色系）
                            highlightColor = Color.Lerp(
                                new Color(1f, 1f, 0f, 0.5f),  // 浅黄色（轻微重叠）
                                new Color(1f, 0f, 0f, 0.8f),   // 红色（高度重叠）- 拖拽时更明显
                                overlapRatio
                            );
                            highlightElement.style.borderTopWidth = 3;
                            highlightElement.style.borderTopColor = new Color(1f, 0.2f, 0f, 1f);
                            highlightElement.style.borderBottomWidth = 3;
                            highlightElement.style.borderBottomColor = new Color(1f, 0.2f, 0f, 1f);
                        }
                        else
                        {
                            // 其他clip之间的重叠 - 使用普通颜色（橙色系）
                            highlightColor = Color.Lerp(
                                new Color(1f, 1f, 0f, 0.3f),  // 浅黄色（轻微重叠）
                                new Color(1f, 0.4f, 0f, 0.7f), // 深橙色（高度重叠）
                                overlapRatio
                            );
                            highlightElement.style.borderTopWidth = 2;
                            highlightElement.style.borderTopColor = new Color(1f, 0.6f, 0f, 0.9f);
                            highlightElement.style.borderBottomWidth = 2;
                            highlightElement.style.borderBottomColor = new Color(1f, 0.6f, 0f, 0.9f);
                        }

                        highlightElement.style.backgroundColor = highlightColor;

                        // 确保高亮层在clip之上但低于鼠标事件层级
                        highlightElement.pickingMode = PickingMode.Ignore;

                        trackElement.Add(highlightElement);
                    }
                }
            }
        }
    }

    // 拖拽结束后自动调整位置消除重叠
    private void ResolveOverlapOnDragEnd(VisualElement trackElement, List<VisualElement> clipElements, VisualElement draggedClip)
    {
        var draggedClipItem = draggedClip.userData as IClipItem;
        if (draggedClipItem == null) return;

        // 使用已经同步的StartTime
        float draggedClipStart = draggedClipItem.StartTime;
        float draggedClipEnd = draggedClipStart + draggedClipItem.Length;

        // 检测与其他clip的重叠
        List<(VisualElement clip, float overlapStart, float overlapEnd)> overlappingClips = new List<(VisualElement, float, float)>();

        foreach (var otherClip in clipElements)
        {
            if (otherClip == draggedClip) continue;

            var otherClipItem = otherClip.userData as IClipItem;
            if (otherClipItem == null) continue;

            float otherClipStart = otherClipItem.StartTime;
            float otherClipEnd = otherClipItem.StartTime + otherClipItem.Length;

            if (draggedClipStart < otherClipEnd && otherClipStart < draggedClipEnd)
            {
                float overlapStart = Mathf.Max(draggedClipStart, otherClipStart);
                float overlapEnd = Mathf.Min(draggedClipEnd, otherClipEnd);
                overlappingClips.Add((otherClip, overlapStart, overlapEnd));
            }
        }

        // 拖拽结束，不自动调整位置，保持用户拖拽的确切位置
        // 只显示重叠警告信息
        if (overlappingClips.Count > 0)
        {
            float totalOverlapDuration = 0f;
            foreach (var (_, overlapStart, overlapEnd) in overlappingClips)
            {
                totalOverlapDuration += (overlapEnd - overlapStart);
            }

            float overlapRatio = totalOverlapDuration / draggedClipItem.Length;
            Debug.Log($"clip重叠检测 - 重叠占比: {overlapRatio:P1}, 位置保持不变");
        }

        // 重新检测所有clip的重叠并更新高亮
        HighlightOverlappingClips(trackElement, clipElements);
    }

    private void SetupClipInteractions(VisualElement clipElement)
    {
        // 允许Clip元素接收鼠标事件
        clipElement.pickingMode = PickingMode.Position;

        // 鼠标悬停效果
        clipElement.RegisterCallback<MouseEnterEvent>(evt => {
            if (!isDragging)
            {
                clipElement.AddToClassList("hovered");
            }
        });

        clipElement.RegisterCallback<MouseLeaveEvent>(evt => {
            clipElement.RemoveFromClassList("hovered");
            if (!isDragging)
            {
                clipElement.RemoveFromClassList("dragging");
            }
        });

        // 鼠标按下 - 开始拖拽
        clipElement.RegisterCallback<MouseDownEvent>(evt => {
            if (evt.button == 0) // 左键
            {
                // 选中Clip并更新显示
                if (clipElement.userData is IClipItem clipItem)
                {
                    Debug.Log($"点击Clip: {clipItem.Name}");
                    UpdateClipProperties(clipItem);
                    
                    // 如果Clip属于某个轨道，也更新轨道信息
                    var trackElement = clipElement.parent;
                    if (trackElement != null && trackElement.userData is ITrackItem trackItem)
                    {
                        UpdateTrackInfo(trackItem);
                    }
                }
                
                isDragging = true;
                dragStartPosition = evt.mousePosition;

                // 计算鼠标点击位置相对于 clip 左侧的偏移量
                float currentLeft = clipElement.layout.x;
                dragOffset = evt.mousePosition.x - currentLeft;
                clipElement.AddToClassList("dragging"); // 添加拖拽样式类
                clipElement.CaptureMouse(); // 捕获鼠标，确保能接收鼠标移动事件
                evt.StopPropagation(); // 阻止事件冒泡到轨道
            }
        });

        // 鼠标移动 - 拖拽过程
        clipElement.RegisterCallback<MouseMoveEvent>(evt => {
            if (isDragging && clipElement.HasMouseCapture())
            {
                // X方向：不允许超过0刻度（左边界）
                // Y方向：保持固定（clip垂直居中在轨道内，不进行垂直拖拽）
                float newLeft = evt.mousePosition.x - dragOffset;
                
                // 限制不能超过0刻度
                newLeft = Mathf.Max(0f, newLeft);
                // 更新位置（只更新X方向，Y方向保持固定）
                clipElement.style.left = newLeft;

                // 实时更新高亮区域 - 只针对当前拖拽的clip
                var trackElement = clipElement.parent;
                if (trackElement != null)
                {
                    var clipElements = trackElement.Query<VisualElement>().Where(x => x is Label && x.userData is IClipItem).ToList();
                    HighlightDraggingClipOverlap(trackElement, clipElements, clipElement);
                }

                evt.StopPropagation(); // 阻止事件冒泡到轨道
            }
        });

        // 鼠标释放 - 结束拖拽
        clipElement.RegisterCallback<MouseUpEvent>(evt => {
            if (isDragging)
            {
                clipElement.ReleaseMouse(); // 释放鼠标捕获
                clipElement.RemoveFromClassList("dragging"); // 移除拖拽样式类
                bool dataChanged = false;
                // 拖拽结束后，首先同步更新clip的数据，确保视觉位置与数据一致
                var draggedClipItem = clipElement.userData as IClipItem;
                if (draggedClipItem != null)
                {
                    isDragging = false;
                    // 根据当前的视觉位置更新StartTime
                    float currentStartTime = clipElement.style.left.value.value / pixelsPerSecond;
                    draggedClipItem.StartTime = Mathf.Max(0f, currentStartTime); // 确保不为负
                    
                    // 根据不同的Clip类型，同步更新对应的数据源
                    if (draggedClipItem is AnimationClipItem animationClipItem && animationClipItem.SegmentData != null)
                    {
                        // AnimationClipItem: 直接更新SegmentData的StartTime
                        animationClipItem.SegmentData.StartTime = draggedClipItem.StartTime;
                        dataChanged = true;
                    }
                    else if (draggedClipItem is EffectClipItem effectClipItem && effectClipItem.EffectData != null && config != null)
                    {
                        // EffectClipItem: 需要找到对应的Segment，计算归一化时间并更新TriggerTime
                        UpdateEffectClipTriggerTime(effectClipItem, draggedClipItem.StartTime);
                        dataChanged = true;
                    }
                    else if (draggedClipItem is SoundClipItem soundClipItem && soundClipItem.SoundData != null && config != null)
                    {
                        // SoundClipItem: 需要找到对应的Segment，计算归一化时间并更新TriggerTime
                        UpdateSoundClipTriggerTime(soundClipItem, draggedClipItem.StartTime);
                        dataChanged = true;
                    }
                    else if (draggedClipItem is HitBoxClipItem hitBoxClipItem && hitBoxClipItem.HitBoxData != null && config != null)
                    {
                        // HitBoxClipItem: 需要找到对应的Segment，计算归一化时间并更新StartTime和EndTime
                        UpdateHitBoxClipTimes(hitBoxClipItem, draggedClipItem.StartTime, draggedClipItem.Length);
                        dataChanged = true;
                    }
                    
                    if (dataChanged)
                    {
                        MarkAssetDirty();
                    }
                    // 确保clip保持垂直居中
                    clipElement.style.top = (TRACK_ITEM_HEIGHT - CLIP_ITEM_HEIGHT) / 2;
                    
                    // 更新Clip属性显示（StartTime可能已改变）
                    UpdateClipProperties(draggedClipItem);
                }

                // 然后自动解决重叠冲突
                var trackElement = clipElement.parent;
                if (trackElement != null)
                {
                    var clipElements = trackElement.Query<VisualElement>().Where(x => x is Label && x.userData is IClipItem).ToList();
                    ResolveOverlapOnDragEnd(trackElement, clipElements, clipElement);
                }
                
                // clip位置改变后，更新内容宽度和时间轴刻度（clip可能被拖到更远的位置）
                UpdateTimelineContentWidth();
                DrawTimelineRulerMarks();

                evt.StopPropagation(); // 阻止事件冒泡到轨道
            }
        });
    }
    
    private void SetupTrackInteractions(VisualElement trackElement)
    {
        // 轨道点击事件（点击轨道空白区域）
        trackElement.RegisterCallback<MouseDownEvent>(evt => {
            // 如果正在拖拽 label，忽略轨道点击事件
            if (isDragging) return;

            // 获取轨道信息
            if (trackElement.userData is ITrackItem trackInfo)
            {
                if (evt.button == 0) // 左键点击轨道 - 选中轨道并更新显示
                {
                    Debug.Log($"点击轨道: {trackInfo.Type}");
                    UpdateTrackInfo(trackInfo);
                    UpdateClipProperties(null); // 清空Clip选择
                    evt.StopPropagation();
                }
                else if (evt.button == 1) // 右键点击轨道 - 显示轨道菜单
                {
                    //Debug.Log($"右键点击轨道 - ID:{trackInfo.Id}, 位置:{evt.mousePosition}");
                    evt.StopPropagation();
                }
            }
        });

        // 轨道悬停效果
        trackElement.RegisterCallback<MouseEnterEvent>(evt => {
            if (!isDragging)
            {
                trackElement.AddToClassList("hovered");
            }
        });

        trackElement.RegisterCallback<MouseLeaveEvent>(evt => {
            if (!isDragging)
            {
                trackElement.RemoveFromClassList("hovered");
            }
        });

        // 允许轨道接收鼠标事件
        trackElement.pickingMode = PickingMode.Position;
    }

    
    // 更新EffectClip的TriggerTime（归一化时间）
    private void UpdateEffectClipTriggerTime(EffectClipItem effectClipItem, float absoluteStartTime)
    {
        if (config == null || effectClipItem.EffectData == null) return;

        // 遍历所有Segment，找到包含该EffectData的Segment
        foreach (var segment in config.Segments)
        {
            if (segment.VisualEffects.Contains(effectClipItem.EffectData))
            {
                // 获取动画片段长度作为基准
                float animationLength = 2f; // 默认长度
                if (segment.AnimationClipTrans != null && segment.AnimationClipTrans.Clip != null)
                {
                    animationLength = segment.AnimationClipTrans.Clip.length;
                }

                // 计算归一化时间：StartTime = (绝对时间 - Segment开始时间) / 动画片段长度
                if (animationLength > 0)
                {
                    float normalizedTime = (absoluteStartTime - segment.StartTime) / animationLength;
                    effectClipItem.EffectData.StartTime = Mathf.Clamp01(normalizedTime);
                }
                break;
            }
        }
    }
    
    // 更新SoundClip的TriggerTime（归一化时间）
    private void UpdateSoundClipTriggerTime(SoundClipItem soundClipItem, float absoluteStartTime)
    {
        if (config == null || soundClipItem.SoundData == null) return;

        // 遍历所有Segment，找到包含该SoundData的Segment
        foreach (var segment in config.Segments)
        {
            if (segment.SoundEffects.Contains(soundClipItem.SoundData))
            {
                // 获取动画片段长度作为基准
                float animationLength = 2f; // 默认长度
                if (segment.AnimationClipTrans != null && segment.AnimationClipTrans.Clip != null)
                {
                    animationLength = segment.AnimationClipTrans.Clip.length;
                }

                // 计算归一化时间：StartTime = (绝对时间 - Segment开始时间) / 动画片段长度
                if (animationLength > 0)
                {
                    float normalizedTime = (absoluteStartTime - segment.StartTime) / animationLength;
                    soundClipItem.SoundData.StartTime = Mathf.Clamp01(normalizedTime);
                }
                break;
            }
        }
    }
    
    // 更新HitBoxClip的StartTime和EndTime（归一化时间）
    private void UpdateHitBoxClipTimes(HitBoxClipItem hitBoxClipItem, float absoluteStartTime, float absoluteDuration)
    {
        if (config == null || hitBoxClipItem.HitBoxData == null) return;

        // 遍历所有Segment，找到包含该HitBoxData的Segment
        foreach (var segment in config.Segments)
        {
            if (segment.HitBoxes.Contains(hitBoxClipItem.HitBoxData))
            {
                // 获取动画片段长度作为基准
                float animationLength = 2f; // 默认长度
                if (segment.AnimationClipTrans != null && segment.AnimationClipTrans.Clip != null)
                {
                    animationLength = segment.AnimationClipTrans.Clip.length;
                }

                // 计算归一化时间
                if (animationLength > 0)
                {
                    float normalizedStartTime = (absoluteStartTime - segment.StartTime) / animationLength;
                    float normalizedEndTime = normalizedStartTime + (absoluteDuration / animationLength);

                    hitBoxClipItem.HitBoxData.StartTime = Mathf.Clamp01(normalizedStartTime);
                    hitBoxClipItem.HitBoxData.EndTime = Mathf.Clamp01(normalizedEndTime);

                    // 确保EndTime >= StartTime
                    if (hitBoxClipItem.HitBoxData.EndTime < hitBoxClipItem.HitBoxData.StartTime)
                    {
                        hitBoxClipItem.HitBoxData.EndTime = hitBoxClipItem.HitBoxData.StartTime;
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
    #endregion
    
    #region Right面板更新方法
    
    // 更新轨道信息显示
    private void UpdateTrackInfo(ITrackItem track)
    {
        selectedTrack = track;
        if (track == null)
        {
            if (trackTypeLabel != null) trackTypeLabel.text = "-";
            if (clipCountLabel != null) clipCountLabel.text = "-";
            if (totalDurationLabel != null) totalDurationLabel.text = "-";
            return;
        }
        
        // 更新轨道类型
        if (trackTypeLabel != null)
        {
            trackTypeLabel.text = track.Type.ToString();
            Debug.Log($"轨道类型已更新: {track.Type}");
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
                totalDuration = Mathf.Max(totalDuration, clip.StartTime + clip.Length);
            }
        }
        else if (track is EffectTrack effectTrack)
        {
            clipCount = effectTrack.ClipList.Count;
            foreach (var clip in effectTrack.ClipList)
            {
                totalDuration = Mathf.Max(totalDuration, clip.StartTime + clip.Length);
            }
        }
        else if (track is SoundTrack soundTrack)
        {
            clipCount = soundTrack.ClipList.Count;
            foreach (var clip in soundTrack.ClipList)
            {
                totalDuration = Mathf.Max(totalDuration, clip.StartTime + clip.Length);
            }
        }
        else if (track is HitBoxTrack hitBoxTrack)
        {
            clipCount = hitBoxTrack.ClipList.Count;
            foreach (var clip in hitBoxTrack.ClipList)
            {
                totalDuration = Mathf.Max(totalDuration, clip.StartTime + clip.Length);
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
    }
    
    // 更新Clip属性显示
    private void UpdateClipProperties(IClipItem clip)
    {
        selectedClip = clip;
        // 先隐藏所有字段组
        HideAllClipFields();
        
        if (clip == null)
        {
            // 清空所有字段
            ClearAllClipFields();
            if (clipPropertiesTitle != null) clipPropertiesTitle.text = "Clip Properties";
            return;
        }
        
        // 更新标题
        if (clipPropertiesTitle != null)
        {
            string title = clip.Type switch
            {
                TrackType.Animation => "Animation Properties",
                TrackType.Effect => "Effect Properties",
                TrackType.Sound => "Sound Properties",
                TrackType.Hitbox => "HitBox Properties",
                _ => "Clip Properties"
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
            // AnimationClip的帧数在UpdateAnimationClipProperties中根据动画时长计算
        }
        else if (clip is EffectClipItem effectClipItem)
        {
            UpdateEffectClipProperties(effectClipItem);
            // 其他类型的Clip直接显示帧数
            if (frameField != null)
            {
                frameField.SetValueWithoutNotify(clip.Frame);
                frameField.SetEnabled(true); // 其他类型可以编辑
            }
        }
        else if (clip is SoundClipItem soundClipItem)
        {
            UpdateSoundClipProperties(soundClipItem);
            // 其他类型的Clip直接显示帧数
            if (frameField != null)
            {
                frameField.SetValueWithoutNotify(clip.Frame);
                frameField.SetEnabled(true); // 其他类型可以编辑
            }
        }
        else if (clip is HitBoxClipItem hitBoxClipItem)
        {
            UpdateHitBoxClipProperties(hitBoxClipItem);
            // 其他类型的Clip直接显示帧数
            if (frameField != null)
            {
                frameField.SetValueWithoutNotify(clip.Frame);
                frameField.SetEnabled(true); // 其他类型可以编辑
            }
        }
    }
    
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
        
        // 计算并更新Duration（根据动画Clip的时长和播放速度）
        float calculatedDuration = clipItem.Length;
        if (animClip != null)
        {
            // Duration = 动画时长 / 播放速度
            calculatedDuration = animClip.length / Mathf.Max(speed, 0.01f);
            clipItem.Length = animClip.length;
        }
        
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
        
        // 计算并更新帧数（根据动画Clip的时长和播放速度）
        if (frameField != null)
        {
            float calculatedFrame = 0;
            if (animClip != null)
            {
                // 帧数 = (动画时长 / 播放速度) * 60fps
                calculatedFrame = Mathf.RoundToInt(animClip.length * 60f);
            }
            else
            {
                // 如果没有动画Clip，使用Duration计算
                calculatedFrame = Mathf.RoundToInt(clipItem.Length * 60f);
            }
            
            frameField.SetValueWithoutNotify(calculatedFrame);
            frameField.SetEnabled(false); // 设置为只读
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
                    float animationLength = 2f; // 默认长度
                    if (segment.AnimationClipTrans != null && segment.AnimationClipTrans.Clip != null)
                    {
                        animationLength = segment.AnimationClipTrans.Clip.length;
                    }
            
                    // 计算绝对触发时间：segment开始时间 + (归一化时间 × 动画长度)
                    absoluteTriggerTime = segment.StartTime + (effectData.StartTime * animationLength);
                    break;
                }
            }
            effectTriggerTimeField.SetValueWithoutNotify(absoluteTriggerTime);
        }
        
        // 更新持续时间
        if (effectDurationField != null)
        {
            effectDurationField.SetValueWithoutNotify(effectData.Length);
        }
        
        // 更新是否跟随目标
        if (followTargetField != null)
        {
            followTargetField.SetValueWithoutNotify(effectData.FollowTarget);
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
                    float animationLength = 2f; // 默认长度
                    if (segment.AnimationClipTrans != null && segment.AnimationClipTrans.Clip != null)
                    {
                        animationLength = segment.AnimationClipTrans.Clip.length;
                    }
            
                    // 计算绝对触发时间：segment开始时间 + (归一化时间 × 动画长度)
                    absoluteTriggerTime = segment.StartTime + (soundData.StartTime * animationLength);
                    break;
                }
            }
            soundTriggerTimeField.SetValueWithoutNotify(absoluteTriggerTime);
        }
        
        // 更新音量
        if (volumeField != null)
        {
            volumeField.SetValueWithoutNotify(soundData.Volume);
        }
        //TODO 需要添加时长显示面板
        if (soundData.Clip != null)
        {
            clipItem.Length = soundData.Clip.length;
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
        
        // 更新开始时间和结束时间（归一化时间）
        if (hitBoxStartTimeField != null)
        {
            hitBoxStartTimeField.SetValueWithoutNotify(hitBoxData.StartTime);
        }
        if (hitBoxEndTimeField != null)
        {
            hitBoxEndTimeField.SetValueWithoutNotify(hitBoxData.EndTime);
        }
    }
    
    // 清空所有Clip字段
    private void ClearAllClipFields()
    {
        if (clipNameField != null) clipNameField.SetValueWithoutNotify("");
        if (startTimeField != null) startTimeField.SetValueWithoutNotify(0f);
        if (frameField != null) frameField.SetValueWithoutNotify(0);
        
        // Animation字段
        if (animationClipField != null) animationClipField.SetValueWithoutNotify(null);
        if (speedField != null) speedField.SetValueWithoutNotify(1f);
        if (durationField != null) durationField.SetValueWithoutNotify(0f);
        if (fadeDurationField != null) fadeDurationField.SetValueWithoutNotify(0.25f);
        
        // Effect字段
        if (effectPrefabField != null) effectPrefabField.SetValueWithoutNotify(null);
        if (effectTriggerTimeField != null) effectTriggerTimeField.SetValueWithoutNotify(0f);
        if (effectDurationField != null) effectDurationField.SetValueWithoutNotify(2f);
        if (followTargetField != null) followTargetField.SetValueWithoutNotify(false);
        
        // Sound字段
        if (audioClipField != null) audioClipField.SetValueWithoutNotify(null);
        if (soundTriggerTimeField != null) soundTriggerTimeField.SetValueWithoutNotify(0f);
        if (volumeField != null) volumeField.SetValueWithoutNotify(1f);
        
        // HitBox字段
        if (shapeTypeField != null) shapeTypeField.SetValueWithoutNotify(HitShapeType.Box);
        if (hitBoxStartTimeField != null) hitBoxStartTimeField.SetValueWithoutNotify(0.2f);
        if (hitBoxEndTimeField != null) hitBoxEndTimeField.SetValueWithoutNotify(0.5f);
    }
    
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
        }
        
        MarkAssetDirty();
        RefreshTrackContent();
    }
    
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
            
            // 更新Duration（根据新的动画Clip长度和速度）
            if (animClipItem.SegmentData.AnimationClipTrans.Clip != null)
            {
                animClipItem.Length = animClipItem.SegmentData.AnimationClipTrans.Clip.length;
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
                
                // 更新Duration（根据新的速度）
                if (animClipItem.SegmentData.AnimationClipTrans.Clip != null)
                {
                    animClipItem.Length = animClipItem.SegmentData.AnimationClipTrans.Clip.length;
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
    
    // Effect Clip回调
    private void OnEffectPrefabChanged(ChangeEvent<Object> evt)
    {
        if (selectedClip is EffectClipItem effectClipItem && effectClipItem.EffectData != null)
        {
            effectClipItem.EffectData.Prefab = evt.newValue as UnityEngine.GameObject;
            MarkAssetDirty();
        }
    }
    
    private void OnEffectTriggerTimeChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is EffectClipItem effectClipItem && effectClipItem.EffectData != null && config != null)
        {
            // 将绝对时间转换为归一化时间
            float absoluteTime = evt.newValue;
            foreach (var segment in config.Segments)
            {
                if (segment.VisualEffects.Contains(effectClipItem.EffectData))
                {
                    // 获取动画片段长度作为基准
                    float animationLength = 2f; // 默认长度
                    if (segment.AnimationClipTrans != null && segment.AnimationClipTrans.Clip != null)
                    {
                        animationLength = segment.AnimationClipTrans.Clip.length;
                    }

                    if (animationLength > 0)
                    {
                        float normalizedTime = (absoluteTime - segment.StartTime) / animationLength;
                        effectClipItem.EffectData.StartTime = Mathf.Clamp01(normalizedTime);
                    }
                    break;
                }
            }
            MarkAssetDirty();
            RefreshTrackContent();
        }
    }
    
    private void OnEffectDurationChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is EffectClipItem effectClipItem && effectClipItem.EffectData != null)
        {
            //TODO 这里有问题
            effectClipItem.EffectData.StartTime = Mathf.Max(0f, evt.newValue);
            effectClipItem.Length = effectClipItem.EffectData.EndTime;
            MarkAssetDirty();
            RefreshTrackContent();
        }
    }
    
    private void OnFollowTargetChanged(ChangeEvent<bool> evt)
    {
        if (selectedClip is EffectClipItem effectClipItem && effectClipItem.EffectData != null)
        {
            effectClipItem.EffectData.FollowTarget = evt.newValue;
            MarkAssetDirty();
        }
    }
    
    // Sound Clip回调
    private void OnAudioClipChanged(ChangeEvent<Object> evt)
    {
        if (selectedClip is SoundClipItem soundClipItem && soundClipItem.SoundData != null)
        {
            soundClipItem.SoundData.Clip = evt.newValue as UnityEngine.AudioClip;
            MarkAssetDirty();
        }
    }
    
    private void OnSoundTriggerTimeChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is SoundClipItem soundClipItem && soundClipItem.SoundData != null && config != null)
        {
            // 将绝对时间转换为归一化时间
            float absoluteTime = evt.newValue;
            foreach (var segment in config.Segments)
            {
                if (segment.SoundEffects.Contains(soundClipItem.SoundData))
                {
                    // 获取动画片段长度作为基准
                    float animationLength = 2f; // 默认长度
                    if (segment.AnimationClipTrans != null && segment.AnimationClipTrans.Clip != null)
                    {
                        animationLength = segment.AnimationClipTrans.Clip.length;
                    }

                    if (animationLength > 0)
                    {
                        float normalizedTime = (absoluteTime - segment.StartTime) / animationLength;
                        soundClipItem.SoundData.StartTime = Mathf.Clamp01(normalizedTime);
                    }
                    break;
                }
            }
            MarkAssetDirty();
            RefreshTrackContent();
        }
    }
    
    private void OnVolumeChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is SoundClipItem soundClipItem && soundClipItem.SoundData != null)
        {
            soundClipItem.SoundData.Volume = Mathf.Clamp01(evt.newValue);
            MarkAssetDirty();
        }
    }
    
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
    
    private void OnHitBoxStartTimeChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is HitBoxClipItem hitBoxClipItem && hitBoxClipItem.HitBoxData != null && config != null)
        {
            // 将绝对时间转换为归一化时间
            float absoluteTime = evt.newValue;
            foreach (var segment in config.Segments)
            {
                if (segment.HitBoxes.Contains(hitBoxClipItem.HitBoxData))
                {
                    // 获取动画片段长度作为基准
                    float animationLength = 2f; // 默认长度
                    if (segment.AnimationClipTrans != null && segment.AnimationClipTrans.Clip != null)
                    {
                        animationLength = segment.AnimationClipTrans.Clip.length;
                    }

                    if (animationLength > 0)
                    {
                        float normalizedStartTime = (absoluteTime - segment.StartTime) / animationLength;
                        hitBoxClipItem.HitBoxData.StartTime = Mathf.Clamp01(normalizedStartTime);

                        // 确保EndTime >= StartTime
                        if (hitBoxClipItem.HitBoxData.EndTime < hitBoxClipItem.HitBoxData.StartTime)
                        {
                            hitBoxClipItem.HitBoxData.EndTime = hitBoxClipItem.HitBoxData.StartTime;
                            if (hitBoxEndTimeField != null)
                            {
                                hitBoxEndTimeField.SetValueWithoutNotify(hitBoxClipItem.HitBoxData.EndTime);
                            }
                        }

                        // 更新Clip的Duration（基于归一化时间）
                        float normalizedDuration = hitBoxClipItem.HitBoxData.EndTime - hitBoxClipItem.HitBoxData.StartTime;
                        hitBoxClipItem.Length = normalizedDuration * animationLength;
                    }
                    break;
                }
            }

            MarkAssetDirty();
            RefreshTrackContent();
        }
    }
    
    private void OnHitBoxEndTimeChanged(ChangeEvent<float> evt)
    {
        if (selectedClip is HitBoxClipItem hitBoxClipItem && hitBoxClipItem.HitBoxData != null && config != null)
        {
            // 将绝对时间转换为归一化时间
            float absoluteTime = evt.newValue;
            foreach (var segment in config.Segments)
            {
                if (segment.HitBoxes.Contains(hitBoxClipItem.HitBoxData))
                {
                    // 获取动画片段长度作为基准
                    float animationLength = 2f; // 默认长度
                    if (segment.AnimationClipTrans != null && segment.AnimationClipTrans.Clip != null)
                    {
                        animationLength = segment.AnimationClipTrans.Clip.length;
                    }

                    if (animationLength > 0)
                    {
                        float normalizedEndTime = (absoluteTime - segment.StartTime) / animationLength;
                        hitBoxClipItem.HitBoxData.EndTime = Mathf.Clamp01(normalizedEndTime);

                        // 确保EndTime >= StartTime
                        if (hitBoxClipItem.HitBoxData.EndTime < hitBoxClipItem.HitBoxData.StartTime)
                        {
                            hitBoxClipItem.HitBoxData.EndTime = hitBoxClipItem.HitBoxData.StartTime;
                            if (hitBoxEndTimeField != null)
                            {
                                hitBoxEndTimeField.SetValueWithoutNotify(hitBoxClipItem.HitBoxData.EndTime);
                            }
                        }

                        // 更新Clip的Duration（基于归一化时间）
                        float normalizedDuration = hitBoxClipItem.HitBoxData.EndTime - hitBoxClipItem.HitBoxData.StartTime;
                        hitBoxClipItem.Length = normalizedDuration * animationLength;
                    }
                    break;
                }
            }

            MarkAssetDirty();
            RefreshTrackContent();
        }
    }
    
    #endregion
    
    #region 播放进度条

     // 初始化播放进度条
    private void InitPlayHead()
    {
        if (timelineContent == null) return;

        // 创建播放进度条容器（只保留红色进度线，去掉箭头）
        playheadElement = new VisualElement();
        playheadElement.name = "Playhead";
        playheadElement.AddToClassList("timeline-playhead");
        playheadElement.style.position = Position.Absolute;
        playheadElement.style.top = 0;
        // 固定为线宽，确保不会出现负坐标/越界几何导致 ScrollView 误判需要横向滚动条
        playheadElement.style.width = PLAYHEAD_WIDTH;
        playheadElement.style.alignItems = Align.Center;
        playheadElement.pickingMode = PickingMode.Position; // 允许接收鼠标事件

        // 创建进度线（垂直红线）- 从顶部开始，连接到轨道底部
        var playheadLine = new VisualElement();
        playheadLine.name = "PlayheadLine";
        playheadLine.AddToClassList("timeline-playhead-line");
        playheadLine.style.position = Position.Absolute;
        playheadLine.style.top = 0f;
        playheadLine.style.left = 0f;
        playheadLine.style.width = PLAYHEAD_LINE_WIDTH;
        playheadLine.style.backgroundColor = new Color(1f, 0.3f, 0.3f, 1f);

        // 将线添加到播放进度条容器
        playheadElement.Add(playheadLine);

        // 添加到内容容器，确保在最上层
        timelineContent.Add(playheadElement);

        // 设置初始位置
        UpdatePlayheadPosition();

        // 监听内容容器大小变化，更新播放进度条位置
        timelineContent.RegisterCallback<GeometryChangedEvent>(evt => {
            UpdatePlayheadPosition();
        });

        // 初始更新一次大小和位置
        UpdatePlayheadSize();

        // 设置拖动交互（点击/拖动进度线即可）
        SetupPlayheadInteractions();
    }

    // 更新播放进度条的高度（从Ruler顶部到最后一个轨道底部）
    private void UpdatePlayheadSize()
    {
        if (playheadElement == null || timelineContent == null) return;

        // 计算高度：Ruler高度 + (轨道数量 * 轨道高度)
        float height = RULER_HEIGHT;
        
        if (trackContainer != null && trackDataList != null && trackDataList.Count > 0)
        {
            height += trackDataList.Count * TRACK_ITEM_HEIGHT;
        }

        playheadElement.style.height = height;
        
        // 更新进度线的高度（从顶部到轨道底部）
        var playheadLine = playheadElement.Q<VisualElement>("PlayheadLine");
        if (playheadLine != null)
        {
            playheadLine.style.height = height;
            playheadLine.style.top = 0f;
        }
    }

    // 更新播放进度条的位置
    private void UpdatePlayheadPosition()
    {
        if (playheadElement == null) return;

        // playhead在内容容器内的绝对位置（不需要减去滚动偏移）
        float xPosition = currentPlaybackTime * pixelsPerSecond;
        // 红线居中在时间位置，所以需要减去线宽的一半；同时 clamp 避免越界触发 ScrollView 横向滚动条。
        float contentWidth = GetContentWidth();
        float desiredLeft = xPosition - PLAYHEAD_HALF_WIDTH;
        playheadElement.style.left = Mathf.Clamp(desiredLeft, 0f, Mathf.Max(0f, contentWidth - PLAYHEAD_WIDTH));
        
        // 更新时间显示
        UpdateTimeLengthDisplay();
    }
    
    // 更新时间长度显示
    private void UpdateTimeLengthDisplay()
    {
        if (timeLengthLabel == null) return;
        
        // 获取clip的最大结束时间
        float maxClipTime = GetMaxClipEndTime();
        float displayMaxTime = maxClipTime > 0 ? maxClipTime : 0f;
        
        // 格式化显示：当前播放时间s / clip最大时间s
        timeLengthLabel.text = $"{currentPlaybackTime:F2}s / {displayMaxTime:F2}s";
    }

    // 设置播放进度条的交互
    private void SetupPlayheadInteractions()
    {
        if (playheadElement == null) return;

        playheadElement.RegisterCallback<MouseDownEvent>(evt => {
            // 点击播放条即可开始拖动
            if (evt.button == 0)
            {
                isDraggingPlayhead = true;
                playheadElement.CaptureMouse();
                evt.StopPropagation();
            }
        });

        // 鼠标移动 - 拖动过程
        playheadElement.RegisterCallback<MouseMoveEvent>(evt => {
            if (isDraggingPlayhead && playheadElement.HasMouseCapture())
            {
                // 获取相对于内容容器的鼠标位置
                Vector2 localMousePos = timelineContent.WorldToLocal(evt.mousePosition);
                float newX = localMousePos.x;

                // 计算实际播放时间
                currentPlaybackTime = newX / pixelsPerSecond;
                // 限制在clip的最大时间范围内
                float maxClipTime = GetMaxClipEndTime();
                currentPlaybackTime = Mathf.Clamp(currentPlaybackTime, 0f, maxClipTime);

                // 更新位置
                UpdatePlayheadPosition();

                // 更新动画预览
                UpdateAnimationPreview();

                evt.StopPropagation();
            }
        });

        // 鼠标释放 - 结束拖动
        playheadElement.RegisterCallback<MouseUpEvent>(evt => {
            if (isDraggingPlayhead)
            {
                isDraggingPlayhead = false;
                playheadElement.ReleaseMouse();
                evt.StopPropagation();
            }
        });

        // 在内容容器上也添加点击来移动播放进度条
        if (timelineContent != null)
        {
            timelineContent.RegisterCallback<MouseDownEvent>(evt => {
                // 如果点击的是轨道空白区域（不是clip），移动播放进度条
                if (evt.button == 0 && !isDragging && !isDraggingPlayhead)
                {
                    Vector2 localMousePos = timelineContent.WorldToLocal(evt.mousePosition);
                    float newX = localMousePos.x;
                    
                    // 检查是否点击在Ruler或轨道区域
                    VisualElement target = evt.target as VisualElement;
                    bool isOnRuler = target == timelineRuler;
                    bool isOnTrack = false;
                    
                    // 检查是否在轨道区域内（通过向上查找父元素）
                    if (target != null && trackContainer != null)
                    {
                        VisualElement parent = target.parent;
                        while (parent != null && parent != timelineContent)
                        {
                            if (parent == trackContainer || parent.name == "track")
                            {
                                isOnTrack = true;
                                break;
                            }
                            parent = parent.parent;
                        }
                    }
                    
                    if (isOnRuler || isOnTrack)
                    {
                        // 计算实际播放时间
                        currentPlaybackTime = newX / pixelsPerSecond;
                        // 限制在clip的最大时间范围内
                        float maxClipTime = GetMaxClipEndTime();
                        currentPlaybackTime = Mathf.Clamp(currentPlaybackTime, 0f, maxClipTime);

                        UpdatePlayheadPosition();
                        UpdateAnimationPreview();
                    }
                }
            });
        }
    }
    
    // 更新动画预览（根据当前播放时间）
    private void UpdateAnimationPreview()
    {
        if (animancer == null || config == null) return;

        // 遍历所有segment，找到当前时间对应的segment和归一化时间
        foreach (var segment in config.Segments)
        {
            float segmentStart = segment.StartTime;

            // 获取动画片段长度作为segment持续时间
            float animationLength = 2f; // 默认长度
            if (segment.AnimationClipTrans != null && segment.AnimationClipTrans.Clip != null)
            {
                animationLength = segment.AnimationClipTrans.Clip.length;
            }

            float segmentEnd = segment.StartTime + animationLength;

            if (currentPlaybackTime >= segmentStart && currentPlaybackTime <= segmentEnd)
            {
                // 计算在这个segment内的归一化时间 (0-1)
                float normalizedTime = (currentPlaybackTime - segmentStart) / animationLength;
                
                // 这里可以更新Animancer的播放进度
                // 示例：如果segment有对应的动画剪辑，可以设置时间
                // animancer.Playable.SetTime(normalizedTime * clipLength);
                
                // 暂时只记录日志，你可以根据实际需求来实现动画预览
                // Debug.Log($"Preview at time: {currentPlaybackTime:F3}s, segment: {segment.Name}, normalized: {normalizedTime:F3}");
                break;
            }
        }
    }

    #endregion

    #region 时间轴标尺

    private void InitTimelineRuler()
    {
        timelineRuler = root.Q<VisualElement>("Ruler");
        if (timelineRuler == null) return;
        
        timelineRuler.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        timelineRuler.style.borderBottomWidth = 1;
        timelineRuler.style.borderBottomColor = Color.gray;
        timelineRuler.style.overflow = Overflow.Visible; // 允许刻度超出显示

        // 监听容器大小变化，重新绘制刻度
        timelineRuler.RegisterCallback<GeometryChangedEvent>(evt => {
            DrawTimelineRulerMarks();
        });

        // 添加鼠标滚轮缩放支持
        if (timelineScrollView != null)
        {
            timelineScrollView.RegisterCallback<WheelEvent>(OnTimelineWheel);
        }
    }
    
    // 根据缩放级别获取数字标签显示间隔
    private int GetDisplayInterval(float pixelsPerSecond)
    {
        // 当缩放很小的时候（每秒像素数小于等于50），每5秒显示一个值
        // 当缩放很大的时候（每秒像素数大于50），每1秒显示一个值
        return pixelsPerSecond <= 50f ? 5 : 1;
    }

    private void DrawTimelineRulerMarks()
    {
        if (timelineRuler == null) return;

        // 清除之前的刻度
        var existingMarks = timelineRuler.Query<VisualElement>(className: "timeline-mark").ToList();
        foreach (var mark in existingMarks)
        {
            timelineRuler.Remove(mark);
        }
        
        // 时间轴标尺宽度
        float rulerWidth = GetContentWidth();
        timelineRuler.style.width = rulerWidth;
        timelineRuler.style.minWidth = rulerWidth;

        // 根据标尺宽度计算需要显示的最大秒数
        float maxTime = rulerWidth / pixelsPerSecond;
        int maxSeconds = Mathf.CeilToInt(maxTime);

        for (int second = 0; second <= maxSeconds; second++)
        {
            float xPos = second * pixelsPerSecond;
            if (xPos > rulerWidth)
            {
                break;
            }
            // 主刻度线
            var mainMark = new VisualElement();
            mainMark.AddToClassList("timeline-mark");
            mainMark.style.position = Position.Absolute;
            mainMark.style.left = xPos;
            mainMark.style.top = 0;
            mainMark.style.width = 1;
            mainMark.style.height = RULER_HEIGHT * 0.8f; // 刻度线高度
            mainMark.style.backgroundColor = Color.white;
            timelineRuler.Add(mainMark);

            // 刻度数字标签 - 根据缩放级别动态调整显示频率
            int displayInterval = GetDisplayInterval(pixelsPerSecond);
            if (second % displayInterval == 0) // 根据缩放级别显示数字
            {
                string labelText = second.ToString();
                var label = new Label(labelText);
                label.AddToClassList("timeline-mark");
                label.style.position = Position.Absolute;

                // 先放到预期位置；随后在布局完成后拿到真实宽度，再把右边界 clamp 到 rulerWidth 内，
                // 避免 label 的可见溢出把 ScrollView 的可滚动范围撑大，导致最右端出现“多滚出一截”的空白区域。
                float preferredLeft = xPos + 2f;
                label.style.left = preferredLeft;

                label.style.top = RULER_HEIGHT * 0.1f;
                label.style.fontSize = 10;
                label.style.color = Color.white;
                label.style.unityTextAlign = TextAnchor.UpperLeft;
                timelineRuler.Add(label);

                // 用真实宽度做右对齐修正（只执行一次）
                EventCallback<GeometryChangedEvent> onGeometryChanged = null;
                onGeometryChanged = _ =>
                {
                    float w = label.resolvedStyle.width;
                    // width 可能在极早期为 0，这里做一次兜底；如果为 0，保持 preferredLeft 不动。
                    if (w > 0f)
                    {
                        float clampedLeft = Mathf.Min(preferredLeft, Mathf.Max(0f, rulerWidth - w));
                        label.style.left = clampedLeft;
                    }

                    label.UnregisterCallback(onGeometryChanged);
                };
                label.RegisterCallback(onGeometryChanged);
            }

            // 小刻度线（每0.2秒）
            for (int sub = 1; sub < 5; sub++) // 每秒4个小刻度
            {
                float subX = xPos + sub * pixelsPerSecond * 0.2f;
                if (subX > rulerWidth)
                {
                    break;
                }
                var subMark = new VisualElement();
                subMark.AddToClassList("timeline-mark");
                subMark.style.position = Position.Absolute;
                subMark.style.left = subX;
                subMark.style.top = RULER_HEIGHT * 0.6f;
                subMark.style.width = 1;
                subMark.style.height = RULER_HEIGHT * 0.4f;
                subMark.style.backgroundColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
                timelineRuler.Add(subMark);
            }
        }
    }

    // 更新所有clip的位置和宽度
    private void UpdateAllClipPositions()
    {
        if (trackContainer == null) return;

        // 遍历所有轨道元素
        var trackElements = trackContainer.Query<VisualElement>(name: "track").ToList();
        foreach (var trackElement in trackElements)
        {
            if (trackElement.userData is ITrackItem trackData)
            {
                // 获取轨道中的所有clip元素
                var clipElements = trackElement.Query<VisualElement>().Where(x => x is Label && x.userData is IClipItem).ToList();

                // 根据轨道类型获取对应的clip列表
                List<IClipItem> clipList = null;
                if (trackData is AnimationTrack animationTrack)
                {
                    clipList = animationTrack.ClipList.ConvertAll(x => (IClipItem)x);
                }
                else if (trackData is EffectTrack effectTrack)
                {
                    clipList = effectTrack.ClipList.ConvertAll(x => (IClipItem)x);
                }
                else if (trackData is SoundTrack soundTrack)
                {
                    clipList = soundTrack.ClipList.ConvertAll(x => (IClipItem)x);
                }
                else if (trackData is HitBoxTrack hitBoxTrack)
                {
                    clipList = hitBoxTrack.ClipList.ConvertAll(x => (IClipItem)x);
                }

                if (clipList != null)
                {
                    // 更新每个clip的位置和宽度
                    for (int i = 0; i < Mathf.Min(clipElements.Count, clipList.Count); i++)
                    {
                        var clipElement = clipElements[i];
                        var clipItem = clipList[i];

                        float xPosition = clipItem.StartTime * pixelsPerSecond;
                        float clipWidth = clipItem.Length * pixelsPerSecond;

                        clipElement.style.left = xPosition;
                        clipElement.style.width = clipWidth;
                        clipElement.style.top = (TRACK_ITEM_HEIGHT - CLIP_ITEM_HEIGHT) / 2; // 确保垂直居中
                    }

                    // 重新检测并高亮显示重叠区域
                    HighlightOverlappingClips(trackElement, clipElements);
                }
            }
        }
    }


    // 处理时间轴滚轮缩放事件
    private void OnTimelineWheel(WheelEvent evt)
    {
        // 当config为空时不允许缩放
        if (config == null)
        {
            evt.StopPropagation();
            return;
        }
        
        // 根据滚轮方向调整缩放倍数（滚轮向上放大，向下缩小）
        float zoomFactor = evt.delta.y > 0 ? 0.9f : 1.1f; // 向上滚轮缩小，向下滚轮放大
        
        // 记录旧的每秒像素数用于后续计算
        float oldPixelsPerSecond = pixelsPerSecond;
        
        // 记录鼠标位置对应的时间，用于保持缩放中心
        Vector2 localMousePos = timelineContent.WorldToLocal(evt.mousePosition);
        float mouseTimePosition = localMousePos.x / oldPixelsPerSecond;
        
        // 记录当前滚动位置
        float scrollOffset = GetScrollOffset();
        
        // 更新每秒像素数（主控值）
        pixelsPerSecond *= zoomFactor;

        // 限制像素范围
        pixelsPerSecond = Mathf.Clamp(pixelsPerSecond, MIN_PIXELS_PER_SECOND, MAX_PIXELS_PER_SECOND);
        
        // 更新内容容器宽度
        UpdateTimelineContentWidth();

        // 更新所有clip的位置和宽度
        UpdateAllClipPositions();

        // 重新绘制时间轴标尺
        DrawTimelineRulerMarks();

        // 更新播放进度条位置（缩放后需要调整）
        UpdatePlayheadPosition();
        
        // 调整滚动位置，使鼠标位置对应的时间保持在同一位置
        if (timelineScrollView != null)
        {
            float newPixelsPerSecond = pixelsPerSecond;
            float newScrollOffset = mouseTimePosition * newPixelsPerSecond - (localMousePos.x - scrollOffset);
            
            // 限制滚动范围：最大滚动距离 = 可视区域宽度 × (缩放倍数 - 1)
            float viewWidth = GetTimelineViewWidth();
            float maxScrollOffset = viewWidth * (zoomScale - 1f);
            newScrollOffset = Mathf.Clamp(newScrollOffset, 0f, maxScrollOffset);
            
            timelineScrollView.horizontalScroller.value = newScrollOffset;
        }

        // 阻止事件冒泡
        evt.StopPropagation();
    }

    #endregion
    
    #region 播放控制按钮事件

    /// <summary>
    /// Play按钮点击事件
    /// </summary>
    private void OnPlayButtonClicked()
    {
        // TODO: 实现播放功能
        // 1. 设置 isPlaying = true
        // 2. 如果 animancer 不为空，开始播放动画
        // 3. 根据 currentPlaybackTime 找到对应的 segment
        // 4. 播放对应的动画片段，设置正确的起始时间（normalizedTime）
        // 5. 设置动画播放速度为 playbackSpeed
        // 6. 如果是循环模式（isLooping），设置动画循环
        // 7. 可能需要启动一个协程或Update方法来更新 currentPlaybackTime
        // 8. 更新按钮状态（如果需要显示暂停/播放图标）
        
        isPlaying = true;
        Debug.Log($"Play clicked. Current time: {currentPlaybackTime:F2}s, Speed: {playbackSpeed:F1}x, Loop: {isLooping}");
    }

    /// <summary>
    /// Stop按钮点击事件
    /// </summary>
    private void OnStopButtonClicked()
    {
        // TODO: 实现停止功能
        // 1. 设置 isPlaying = false
        // 2. 如果 animancer 不为空，停止所有动画播放
        // 3. 重置 currentPlaybackTime = 0
        // 4. 更新播放进度条位置（调用 UpdatePlayheadPosition）
        // 5. 更新动画预览（调用 UpdateAnimationPreview）
        // 6. 更新按钮状态
        
        isPlaying = false;
        currentPlaybackTime = 0f;
        UpdatePlayheadPosition();
        UpdateAnimationPreview();
        Debug.Log("Stop clicked. Reset playback time to 0.");
    }

    /// <summary>
    /// Loop按钮点击事件
    /// </summary>
    private void OnLoopButtonClicked()
    {
        // TODO: 实现循环切换功能
        // 1. 切换 isLooping 状态（true <-> false）
        // 2. 更新按钮视觉状态（例如改变按钮文字或样式，显示"Loop: ON/OFF"）
        // 3. 如果正在播放，更新当前动画的循环设置
        // 4. 如果使用 Animancer，设置 AnimancerState.IsLooping
        
        isLooping = !isLooping;
        
        // 更新按钮文字显示循环状态
        var loopBtn = root.Q<Button>("Loop");
        if (loopBtn != null)
        {
            loopBtn.text = isLooping ? "Loop: ON" : "Loop: OFF";
        }
        
        // 如果正在播放，更新动画循环设置
        if (isPlaying && animancer != null)
        {
            // TODO: 更新当前播放动画的循环设置
            // if (animancer.CurrentState != null)
            // {
            //     animancer.CurrentState.IsLooping = isLooping;
            // }
        }
        
        Debug.Log($"Loop clicked. Loop mode: {isLooping}");
    }

    #endregion
    
    #region Animation Clip操作按钮
    
    /// <summary>
    /// 添加特效按钮点击事件
    /// </summary>
    private void OnAddEffectButtonClicked()
    {
        if (selectedClip is AnimationClipItem animClipItem && animClipItem.SegmentData != null && config != null)
        {
            var segment = animClipItem.SegmentData;
            
            //TODO
            // 创建新的特效数据
            var newEffect = new VisualEffectData
            {
                Name = "New Effect",
                StartTime = 0.5f, // 默认在中间触发
                Length = 2.0f
            };
            
            segment.VisualEffects.Add(newEffect);
            
            MarkAssetDirty();
            RefreshTrackContent();
            
            Debug.Log($"已为AnimationClip {animClipItem.Name} 添加特效");
        }
    }
    
    /// <summary>
    /// 添加音效按钮点击事件
    /// </summary>
    private void OnAddSoundButtonClicked()
    {
        if (selectedClip is AnimationClipItem animClipItem && animClipItem.SegmentData != null && config != null)
        {
            var segment = animClipItem.SegmentData;
            
            // 创建新的音效数据
            var newSound = new SoundEffectData
            {
                Name = "New Sound",
                StartTime = 0.5f, // 默认在中间触发
                Volume = 1f
            };
            
            segment.SoundEffects.Add(newSound);
            
            MarkAssetDirty();
            RefreshTrackContent();
            
            Debug.Log($"已为AnimationClip {animClipItem.Name} 添加音效");
        }
    }
    
    /// <summary>
    /// 添加Hitbox按钮点击事件
    /// </summary>
    private void OnAddHitboxButtonClicked()
    {
        if (selectedClip is AnimationClipItem animClipItem && animClipItem.SegmentData != null && config != null)
        {
            var segment = animClipItem.SegmentData;
            
            // 创建新的Hitbox数据
            var newHitbox = new HitBoxData
            {
                ShapeType = HitShapeType.Box,
                StartTime = 0.2f, // 默认开始时间
                EndTime = 0.5f    // 默认结束时间
            };
            
            segment.HitBoxes.Add(newHitbox);
            
            MarkAssetDirty();
            RefreshTrackContent();
            
            Debug.Log($"已为AnimationClip {animClipItem.Name} 添加Hitbox");
        }
    }
    
    #endregion

}

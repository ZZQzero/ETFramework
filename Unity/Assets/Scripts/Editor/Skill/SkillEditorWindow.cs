using System;
using System.Collections.Generic;
using Animancer;
using ET;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public partial class SkillEditorWindow : EditorWindow
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
    private Button toggleViewModeButton; // 视图切换按钮
    
    // Right面板UI元素
    private Label trackTypeLabel; // 轨道类型标签
    private Label clipCountLabel; // 片段数标签
    private Label totalDurationLabel; // 总时长标签
    private Label clipPropertiesTitle; // Clip属性标题
    
    // 通用字段
    private TextField clipNameField; // Clip名称输入框
    private FloatField startTimeField; // 开始时间字段
    private IntegerField frameField; // 总帧数字段（只读）
    private FloatField clipLengthField; // 时长字段（读取/编辑 IClipItem.Length）
    private Label clipIndexLabel; // Clip索引标签
    
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
    private FloatField effectNormalizedStartField; // 特效归一化时间字段
    private Toggle followTargetField; // 是否跟随目标字段
    
    // Sound Clip字段
    private VisualElement soundFields; // Sound字段组
    private ObjectField audioClipField; // 音频Clip字段
    private FloatField soundTriggerTimeField; // 音效触发时间字段
    private FloatField soundNormalizedStartField; // 音效归一化时间字段
    private FloatField volumeField; // 音量字段
    
    // HitBox Clip字段
    private VisualElement hitBoxFields; // HitBox字段组
    private EnumField shapeTypeField; // 形状类型字段
    private FloatField hitBoxTriggerTimeField; // HitBox触发时间字段（只读，秒）
    private FloatField hitBoxNormalizedStartField; // HitBox归一化开始时间字段
    private FloatField hitBoxNormalizedEndField; // HitBox归一化结束时间字段
    
    // 操作按钮
    private Button addClipToTrackButton; // 添加Clip到轨道按钮
    private Button deleteClipButton; // 删除Clip按钮
    
    private AttackConfig config;
    private AnimancerComponent animancer;
    
    private List<ITrackItem> trackDataList = new List<ITrackItem>();
    private Dictionary<AnimationClipItem, List<ITrackItem>> animationClipTrackMap = new();
    private readonly List<ITrackItem> globalTrackDataList = new();
    private readonly List<AnimationClipItem> allAnimationClipItems = new();
    private static int nextTrackId = 1; // 轨道ID生成器
    
    // 当前选中的轨道和Clip
    private ITrackItem selectedTrack; // 当前选中的轨道
    private IClipItem selectedClip; // 当前选中的Clip

    private enum ViewMode
    {
        Global,
        ClipFocus,
    }

    private ViewMode viewMode = ViewMode.Global;
    private AnimationClipItem focusedAnimationClipItem;
    private bool isApplyingViewMode;

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

    // 选中态高亮（USS class）
    private const string SELECTED_CLASS = "is-selected";
    private const string TYPE_ANIMATION_CLASS = "type-animation";
    private const string TYPE_EFFECT_CLASS = "type-effect";
    private const string TYPE_SOUND_CLASS = "type-sound";
    private const string TYPE_HITBOX_CLASS = "type-hitbox";
    
    private static string GetTypeClass(TrackType type)
    {
        return type switch
        {
            TrackType.Animation => TYPE_ANIMATION_CLASS,
            TrackType.Effect => TYPE_EFFECT_CLASS,
            TrackType.Sound => TYPE_SOUND_CLASS,
            TrackType.Hitbox => TYPE_HITBOX_CLASS,
            _ => string.Empty,
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
        
        // 监听trackContainer布局变化，更新playhead高度
        if (trackContainer != null)
        {
            trackContainer.RegisterCallback<GeometryChangedEvent>(evt => UpdatePlayheadSize());
        }
        
        listView = root.Q<ListView>("InfoListView");
        var refresh = root.Q<Button>("Refresh");
        refresh.clicked += RefreshTrackContent;

        toggleViewModeButton = root.Q<Button>("ToggleViewMode");
        if (toggleViewModeButton != null)
        {
            toggleViewModeButton.clicked += OnToggleViewModeClicked;
            UpdateViewModeButtonText();
        }
        
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
        
        // 全局键盘事件 - Delete键删除选中的Clip
        root.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Delete && selectedClip != null)
            {
                DeleteClip(selectedClip);
                evt.StopPropagation();
            }
        });
        
        // 让root可以获取焦点以接收键盘事件
        root.focusable = true;
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
        frameField = rightContainer.Q<IntegerField>("FrameField");
        clipLengthField = rightContainer.Q<FloatField>("ClipLengthField");
        clipIndexLabel = rightContainer.Q<Label>("ClipIndexLabel");
        
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
        effectNormalizedStartField = rightContainer.Q<FloatField>("EffectNormalizedStartField");
        followTargetField = rightContainer.Q<Toggle>("FollowTargetField");
        
        // 获取Sound Clip字段
        audioClipField = rightContainer.Q<ObjectField>("AudioClipField");
        if (audioClipField != null)
        {
            audioClipField.objectType = typeof(UnityEngine.AudioClip);
        }
        soundTriggerTimeField = rightContainer.Q<FloatField>("SoundTriggerTimeField");
        soundNormalizedStartField = rightContainer.Q<FloatField>("SoundNormalizedStartField");
        volumeField = rightContainer.Q<FloatField>("VolumeField");
        
        // 获取HitBox Clip字段
        shapeTypeField = rightContainer.Q<EnumField>("ShapeTypeField");
        if (shapeTypeField != null)
        {
            shapeTypeField.Init(HitShapeType.Box);
        }
        hitBoxTriggerTimeField = rightContainer.Q<FloatField>("HitBoxTriggerTimeField");
        hitBoxNormalizedStartField = rightContainer.Q<FloatField>("HitBoxNormalizedStartField");
        hitBoxNormalizedEndField = rightContainer.Q<FloatField>("HitBoxNormalizedEndField");
        
        // 获取操作按钮（使用root查找确保能找到嵌套的元素）
        addClipToTrackButton = root.Q<Button>("AddClipToTrackButton");
        deleteClipButton = root.Q<Button>("DeleteClipButton");
        
        // 注册操作按钮点击事件
        if (addClipToTrackButton != null)
        {
            addClipToTrackButton.clicked += OnAddClipToTrackButtonClicked;
        }
        if (deleteClipButton != null)
        {
            deleteClipButton.clicked += OnDeleteClipButtonClicked;
        }
        
        // 注册所有字段的值变化事件
        if (clipNameField != null)
        {
            clipNameField.RegisterValueChangedCallback(OnClipNameChanged);
        }
        if (startTimeField != null)
        {
            startTimeField.RegisterValueChangedCallback(OnStartTimeChanged);
        }
        if (clipLengthField != null)
        {
            clipLengthField.RegisterValueChangedCallback(OnClipLengthChanged);
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
            // 触发时间只读显示
            effectTriggerTimeField.SetEnabled(false);
        }
        if (effectNormalizedStartField != null)
        {
            effectNormalizedStartField.RegisterValueChangedCallback(OnEffectNormalizedStartChanged);
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
            // 触发时间只读显示
            soundTriggerTimeField.SetEnabled(false);
        }
        if (soundNormalizedStartField != null)
        {
            soundNormalizedStartField.RegisterValueChangedCallback(OnSoundNormalizedStartChanged);
        }
        if (volumeField != null)
        {
            volumeField.RegisterValueChangedCallback(OnVolumeChanged);
        }
        if (shapeTypeField != null)
        {
            shapeTypeField.RegisterValueChangedCallback(OnShapeTypeChanged);
        }
        if (hitBoxNormalizedStartField != null)
        {
            hitBoxNormalizedStartField.RegisterValueChangedCallback(OnHitBoxNormalizedStartChanged);
        }
        if (hitBoxNormalizedEndField != null)
        {
            hitBoxNormalizedEndField.RegisterValueChangedCallback(OnHitBoxNormalizedEndChanged);
        }
        
        // 初始状态：隐藏所有字段组
        HideAllClipFields();
        
        // 初始状态：没有选中任何轨道或Clip
        ClearSelection();
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
            // 初始化视图按钮文字
            UpdateViewModeButtonText();
        }
    }
}

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
    // 右侧面板宽度（像素）。ScrollView 出现滚动条后会占用部分宽度，为避免按钮/字段被挤压可适当调大。
    private const float RIGHT_PANEL_WIDTH_PX = 380f;
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;
    private ObjectField selectConfigAsset;
    private ObjectField selectObj;
    private VisualElement trackContainer;
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

    // AttackConfig 全局参数（显示在 TrackInfoPanel 下方）
    private IntegerField inputBufferWindowMsField; // 输入缓冲有效期（ms）
    private IntegerField defaultHitStopMsField; // 默认顿帧（ms）
    private IntegerField recoveryHoldMsField; // 后摇保持（ms）
    
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
    private FloatField inputBufferStartField; // 输入缓冲开始（归一化 0-1）
    private FloatField cancelableTimeField; // 可取消时间点（归一化 0-1）
    private FloatField animationEndField; // 动画结束阈值（归一化 0-1）
    private IntegerField comboTimeoutOffsetMsField; // 段超时偏移（ms）
    private IntegerField segmentTimeoutMsPreviewField; // 本段超时（ms，预览，只读）
    
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
    private Vector3Field hitBoxOffsetField; // HitBox偏移（局部）
    private Vector3Field hitBoxRotationField; // HitBox旋转（局部欧拉角）
    private Vector3Field hitBoxSizeField; // HitBox尺寸

    // HitEffectData（命中效果）
    private FloatField hitEffectDamageMultiplierField;
    private EnumField hitEffectReactionField;
    private FloatField hitEffectKnockbackForceField;
    private FloatField hitEffectKnockupForceField;
    private IntegerField hitEffectHitStunMsField;
    private EnumField hitEffectTargetStateField;

    // HitFeedbackData（命中反馈）
    private FloatField hitFeedbackShakeIntensityField;
    private FloatField hitFeedbackShakeDurationField;
    private IntegerField hitFeedbackHitStopMsField;
    private FloatField hitFeedbackTimeScaleField;
    private IntegerField hitFeedbackTimeScaleDurationMsField;

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
    private const float MIN_CLIP_WIDTH_PX = 10f; // Clip 最小显示宽度（像素），防止超短片段不可见/难选中
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
        
        var right = root.Q<VisualElement>("Right");
        if (right != null)
        {
            right.style.width = RIGHT_PANEL_WIDTH_PX;
        }

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
        // 初始化 Loop 按钮颜色状态（不通过文字表达）
        UpdateLoopButtonVisual();
        
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

            // 同步右侧面板全局参数（不依赖选中）
            UpdateTrackInfo(selectedTrack);
        }
    }
}

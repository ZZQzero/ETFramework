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
    #region 常量定义
    
    // UI尺寸
    private const float RIGHT_PANEL_WIDTH_PX = 380f; // 右侧面板宽度（像素）
    private const float TRACK_ITEM_HEIGHT = 40f; // 轨道条目高度（像素）
    private const float CLIP_ITEM_HEIGHT = 35f; // Clip条目高度（像素）
    private const float RULER_HEIGHT = 30f; // 时间轴标尺高度（像素）
    private const float MIN_CLIP_WIDTH_PX = 10f; // Clip最小显示宽度（像素），防止超短片段不可见
    
    // 时间轴缩放
    private const float BASE_PIXELS_PER_SECOND = 50f; // 基础每秒像素数（缩放倍数为1时）
    private const float MIN_PIXELS_PER_SECOND = 50f; // 最小每秒像素数（对应缩放倍数1）
    private const float MAX_PIXELS_PER_SECOND = 500f; // 最大每秒像素数（对应缩放倍数10）
    
    // 播放进度条
    private const float PLAYHEAD_LINE_WIDTH = 2f; // 播放进度条线宽（像素）
    private const float PLAYHEAD_WIDTH = PLAYHEAD_LINE_WIDTH; // 播放进度条宽度
    private const float PLAYHEAD_HALF_WIDTH = PLAYHEAD_WIDTH * 0.5f; // 播放进度条半宽（用于定位）
    
    // USS样式类名
    private const string SELECTED_CLASS = "is-selected"; // 选中态样式类
    private const string TYPE_ANIMATION_CLASS = "type-animation"; // 动画轨道样式类
    private const string TYPE_EFFECT_CLASS = "type-effect"; // 特效轨道样式类
    private const string TYPE_SOUND_CLASS = "type-sound"; // 音效轨道样式类
    private const string TYPE_HITBOX_CLASS = "type-hitbox"; // 判定框轨道样式类
    private const string TYPE_ACTIVE_CLASS = "type-active"; // Active轨道样式类
    
    // 动画相关常量
    private const float DEFAULT_ANIMATION_LENGTH = 2f; // 默认动画长度（秒）
    private const float FRAMES_PER_SECOND = 60f; // 帧率（帧/秒）
    
    #endregion

    #region UI元素 - 主窗口

    private VisualElement root;
    private VisualElement leftContainer;
    private ScrollView timelineScrollView;
    private VisualElement timelineContent;
    private VisualElement trackContainer;
    private VisualElement timelineRuler;
    private VisualElement playheadElement;
    
    private ObjectField selectConfigAsset;
    private ObjectField selectObj;
    private Label timeLengthLabel;
    private Slider speedSlider;
    private TextField speedNumField;
    private Label configHintLabel;
    private Button toggleViewModeButton;
    
    #endregion

    #region UI元素 - 右侧面板（轨道信息）
    
    private Label trackTypeLabel;
    private Label clipCountLabel;
    private Label totalDurationLabel;
    private Label clipPropertiesTitle;
    
    private IntegerField inputBufferWindowMsField;
    private IntegerField defaultHitStopMsField;
    private IntegerField recoveryHoldMsField;
    private LayerMaskField previewHitTargetLayerMaskField;
    
    #endregion

    #region UI元素 - 右侧面板（通用Clip字段）
    
    private TextField clipNameField;
    private FloatField startTimeField;
    private IntegerField frameField;
    private FloatField clipLengthField;
    private Label clipIndexLabel;
    
    #endregion

    #region UI元素 - 右侧面板（Animation Clip）
    
    private VisualElement animationFields;
    private VisualElement animationActionButtons;
    private Button addEffectButton;
    private Button addSoundButton;
    private Button addHitboxButton;
    private Button addActiveButton;
    private ObjectField animationClipField;
    private FloatField speedField;
    private FloatField durationField;
    private FloatField fadeDurationField;
    private FloatField inputBufferStartField;
    private FloatField cancelableTimeField;
    private FloatField animationEndField;
    private IntegerField comboTimeoutOffsetMsField;
    private IntegerField segmentTimeoutMsPreviewField;
    
    #endregion

    #region UI元素 - 右侧面板（Movement）
    
    private Toggle movementEnableField;
    private FloatField movementDistanceField;
    private FloatField movementStartField;
    private FloatField movementEndField;
    private CurveField movementCurveField;
    private Toggle movementTrackTargetField;
    private FloatField movementTrackRangeField;
    
    #endregion

    #region UI元素 - 右侧面板（Effect Clip）
    
    private VisualElement effectFields;
    private ObjectField effectPrefabField;
    private Toggle effectIsAnimationField;
    private FloatField effectTriggerTimeField;
    private FloatField effectNormalizedStartField;
    private Toggle followTargetField;
    private Vector3Field effectOffsetField;
    private Vector3Field effectRotationField;
    
    #endregion

    #region UI元素 - 右侧面板（Sound Clip）
    
    private VisualElement soundFields;
    private ObjectField audioClipField;
    private FloatField soundTriggerTimeField;
    private FloatField soundNormalizedStartField;
    private FloatField volumeField;
    
    #endregion

    #region UI元素 - 右侧面板（HitBox Clip）
    
    private VisualElement hitBoxFields;
    private EnumField shapeTypeField;
    private FloatField hitBoxTriggerTimeField;
    private IntegerField hitBoxTriggerFrameField;
    private FloatField hitBoxNormalizedStartField;
    private FloatField hitBoxNormalizedEndField;
    private Vector3Field hitBoxOffsetField;
    private Vector3Field hitBoxRotationField;
    private Vector3Field hitBoxSizeField;
    
    private FloatField hitEffectDamageMultiplierField;
    private EnumField hitEffectReactionField;
    private FloatField hitEffectKnockbackForceField;
    private FloatField hitEffectKnockupForceField;
    private IntegerField hitEffectHitStunMsField;
    private UnityEditor.UIElements.EnumFlagsField hitEffectTargetStateField;
    
    private FloatField hitFeedbackShakeIntensityField;
    private FloatField hitFeedbackShakeDurationField;
    private IntegerField hitFeedbackHitStopMsField;
    private FloatField hitFeedbackTimeScaleField;
    private IntegerField hitFeedbackTimeScaleDurationMsField;
    
    #endregion

    #region UI元素 - 右侧面板（Active Clip）
    
    private VisualElement activeFields;
    private ObjectField activeTargetObjectField;
    private TextField activeRelativePathField;
    
    #endregion

    #region UI元素 - 操作按钮
    
    private Button addClipToTrackButton;
    private Button deleteClipButton;
    
    #endregion

    #region 数据状态
    
    private AttackConfig config;
    private AnimancerComponent animancer;
    
    private List<ITrackItem> trackDataList = new List<ITrackItem>();
    private Dictionary<AnimationClipItem, List<ITrackItem>> animationClipTrackMap = new();
    private readonly List<ITrackItem> globalTrackDataList = new();
    private readonly List<AnimationClipItem> allAnimationClipItems = new();
    
    // 预览命中检测的 LayerMask：由 UI 配置，避免硬编码层名（商业工程要求）
    private LayerMask previewHitTargetLayerMask = ~0; // 默认 Everything，用户可按项目需求配置
    
    private ITrackItem selectedTrack;
    private IClipItem selectedClip;
    
    #endregion

    #region 视图模式
    
    private enum ViewMode
    {
        Global,
        ClipFocus,
    }

    private ViewMode viewMode = ViewMode.Global;
    private AnimationClipItem focusedAnimationClipItem;
    private bool isApplyingViewMode;
    
    #endregion

    #region 时间轴状态
    
    private float pixelsPerSecond = BASE_PIXELS_PER_SECOND;
    private float zoomScale => pixelsPerSecond / BASE_PIXELS_PER_SECOND;
    private bool isDragging = false;
    private Vector2 dragStartPosition;
    private float dragOffset;
    
    #endregion

    #region 播放状态
    
    private bool isDraggingPlayhead = false;
    private float currentPlaybackTime = 0f;
    private float playbackSpeed = 1f;
    private bool isPlaying = false;
    private bool isLooping = true;
    
    #endregion

    #region 工具方法
    
    private static string GetTypeClass(TrackType type)
    {
        return type switch
        {
            TrackType.Animation => TYPE_ANIMATION_CLASS,
            TrackType.Effect => TYPE_EFFECT_CLASS,
            TrackType.Sound => TYPE_SOUND_CLASS,
            TrackType.Hitbox => TYPE_HITBOX_CLASS,
            TrackType.Active => TYPE_ACTIVE_CLASS,
            _ => string.Empty,
        };
    }
    
    /// <summary>
    /// 获取 Segment 的动画长度（优先使用 Duration，其次使用 Clip.length，最后使用默认值）
    /// </summary>
    private static float GetSegmentAnimationLength(AttackSegmentData segment)
    {
        if (segment == null)
        {
            return DEFAULT_ANIMATION_LENGTH;
        }
        
        if (segment.Duration > 0f)
        {
            return segment.Duration;
        }
        
        if (segment.AnimationClipTrans != null && segment.AnimationClipTrans.Clip != null)
        {
            return segment.AnimationClipTrans.Clip.length;
        }
        
        return DEFAULT_ANIMATION_LENGTH;
    }

    /// <summary>
    /// 获取 Segment 的有效播放结束阈值（归一化 0~1）。
    /// - 运行时：超过该阈值认为段结束（不再触发后续逻辑）
    /// - 编辑器：用于限制 HitBox 等子片段的可编辑范围
    /// </summary>
    private static float GetSegmentAnimationEndNorm(AttackSegmentData segment)
    {
        float endNorm = segment?.TimeWindow != null ? segment.TimeWindow.AnimationEnd : 1f;
        if (endNorm <= 0f)
        {
            endNorm = 1f;
        }

        return Mathf.Clamp01(endNorm);
    }
    
    #endregion
    
    #region 窗口生命周期
    
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
    
    #endregion
}

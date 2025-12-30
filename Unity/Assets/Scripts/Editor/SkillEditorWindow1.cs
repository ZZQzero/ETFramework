#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Animancer;
using ET;

public class SkillEditorWindow1 : EditorWindow
{
    // ========== 基本数据 ==========
    private AttackConfigAsset _configAsset;
    private AttackConfig _config;
    private int _selectedSegmentIndex = 0;
    private int _selectedAnimationIndex = -1;

    // ========== 预览相关 ==========
    private GameObject _previewCharacter;
    private AnimancerComponent _animancer;
    private float _previewTime = 0f; // 时间轴单位：秒（全局）
    private bool _isPlaying = false;
    private double _lastTime;
    private float _playbackSpeed = 1.0f;
    private bool _loopPreview = true;
    private float _lastSampledTime = -1f; // 最后采样的时间，避免重复采样
    private bool _forceSample = false; // 强制采样标志

    // ========== Timeline 视图/交互 ==========
    private Vector2 _timelineScroll = Vector2.zero;
    private Vector2 _inspectorScroll = Vector2.zero;
    private float _zoom = 1.0f; // 缩放，1.0 = 基准
    private const float _basePixelsPerSecond = 150f; // 基准像素每秒
    private float PixelsPerSecond => _basePixelsPerSecond * Mathf.Max(0.1f, _zoom);

    // 拖拽/调整相关
    private int _draggingAnimationIndex = -1;
    private float _dragStartMouseX;
    private float _dragStartMouseTime; // new: store mouse time (seconds) on drag start
    private float _animationStartTimeCache;
    private float _animationDurationCache;
    private DragMode _dragMode = DragMode.None;

    private enum DragMode { None, Move, ResizeLeft, ResizeRight }
    private enum TrackType { Animation, HitBox, VFX, SFX }
    private enum VFXDragMode { None, Move, ResizeLeft, ResizeRight }

    // 选中
    private int _selectedHitBoxIndex = -1;
    private int _selectedVFXIndex = -1;
    private VFXDragMode _vfxDragMode = VFXDragMode.None;
    private float _vfxTriggerTimeCache;
    private float _vfxDurationCache;
    private float _sfxTriggerTimeCache;
    private int _selectedSFXIndex = -1;
    
    // 其他轨道的拖动
    private int _draggingHitBoxIndex = -1;
    private int _draggingVFXIndex = -1;
    private int _draggingSFXIndex = -1;
    private float _originalTimeValue; // 用于存储拖动开始时的时间值
    private string _draggingTrackType = ""; // "HitBox", "VFX", "SFX"
    
    // 播放头拖动
    private bool _draggingPlayhead = false;

    // Editor-only curves cache (用于更真实的 blend 预览)
    private Dictionary<AttackSegmentData, AnimationCurve> _fadeInCurveCache = new Dictionary<AttackSegmentData, AnimationCurve>();
    private Dictionary<AttackSegmentData, AnimationCurve> _fadeOutCurveCache = new Dictionary<AttackSegmentData, AnimationCurve>();

    // GUI 样式缓存
    private GUIStyle _trackStyle;
    private GUIStyle _clipStyle;
    private GUIStyle _smallLabelStyle;
    private GUIStyle _eventPointStyle;
    private GUIStyle _trackHeaderStyle;
    private GUIStyle _rulerLabelStyle;
    
    // Unity Timeline风格颜色
    private static readonly Color TimelineBackgroundColor = new Color(0.16f, 0.16f, 0.16f);
    private static readonly Color TrackBackgroundColor = new Color(0.22f, 0.22f, 0.22f);
    private static readonly Color TrackBorderColor = new Color(0.3f, 0.3f, 0.3f);
    private static readonly Color RulerBackgroundColor = new Color(0.2f, 0.2f, 0.2f);
    private static readonly Color RulerLineColor = new Color(0.4f, 0.4f, 0.4f);
    private static readonly Color RulerTextColor = new Color(0.7f, 0.7f, 0.7f);
    private static readonly Color ClipSelectedColor = new Color(0.24f, 0.48f, 0.9f);
    private static readonly Color ClipNormalColor = new Color(0.27f, 0.5f, 0.78f);
    private static readonly Color PlayheadColor = new Color(1f, 0.24f, 0.24f);

    // Undo 管理：记录是否在拖拽中已记录 Undo
    private bool _undoRecorded = false;

    [MenuItem("ET/Combat/Skill Editor")]
    public static void ShowWindow()
    {
        GetWindow<SkillEditorWindow1>("Skill Editor");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        Undo.undoRedoPerformed += OnUndoRedo;
        
        // 初始化GUI样式
        _trackStyle = new GUIStyle(EditorStyles.helpBox);
        _trackStyle.normal.background = MakeColorTexture(TrackBackgroundColor);
        
        _trackHeaderStyle = new GUIStyle(EditorStyles.miniLabel);
        _trackHeaderStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
        _trackHeaderStyle.padding = new RectOffset(8, 4, 4, 4);
        _trackHeaderStyle.alignment = TextAnchor.MiddleLeft;
        
        _clipStyle = new GUIStyle("flow node 0");
        _smallLabelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
        _smallLabelStyle.normal.textColor = Color.white;
        
        _rulerLabelStyle = new GUIStyle(EditorStyles.miniLabel);
        _rulerLabelStyle.normal.textColor = RulerTextColor;
        
        _eventPointStyle = new GUIStyle(EditorStyles.miniButton) { alignment = TextAnchor.MiddleCenter };
    }
    
    private Texture2D MakeColorTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        Undo.undoRedoPerformed -= OnUndoRedo;
    }

    private void OnUndoRedo()
    {
        if (_configAsset != null) _config = _configAsset.Config;
        // 清除选中状态，避免引用失效
        _selectedAnimationIndex = -1;
        _selectedHitBoxIndex = -1;
        _selectedVFXIndex = -1;
        _selectedSFXIndex = -1;
        Repaint();
    }

    private void OnGUI()
    {
        // 处理键盘快捷键
        HandleKeyboardShortcuts();
        
        DrawToolbar();

        if (_config == null)
        {
            EditorGUILayout.HelpBox("请选择 AttackConfigAsset（ScriptableObject）以开始编辑。", MessageType.Info);
            return;
        }

        GUILayout.BeginHorizontal();
        {
            // 左：Timeline
            GUILayout.BeginVertical();
            DrawTimelineArea();
            GUILayout.EndVertical();

            // 右：Inspector
            GUILayout.BeginVertical(GUILayout.Width(320));
            _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll, GUILayout.Height(position.height - 60)); // 减去toolbar高度
            DrawInspector();
            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
        GUILayout.EndHorizontal();

        // 播放驱动 - 只有在非拖动状态下才自动播放
        if (_isPlaying && !_draggingPlayhead)
        {
            double timeNow = EditorApplication.timeSinceStartup;
            float deltaTime = (float)(timeNow - _lastTime);
            _lastTime = timeNow;
            UpdatePreviewPlay(deltaTime * _playbackSpeed);
            Repaint();
        }
        else
        {
            _lastTime = EditorApplication.timeSinceStartup;
        }
    }

    #region Toolbar

    private void DrawToolbar()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);

        // 资源选择
        var newAsset = (AttackConfigAsset)EditorGUILayout.ObjectField(
            _configAsset,
            typeof(AttackConfigAsset),
            false,
            GUILayout.Width(220)
        );

        if (newAsset != _configAsset)
        {
            _configAsset = newAsset;
            _config = _configAsset != null ? _configAsset.Config : null;
            _selectedSegmentIndex = 0;
            _previewTime = 0f;
        }

        // Segment 下拉：注意，不要把 Segment 当 Unity Object 来选（AttackSegmentData 不是 UnityEngine.Object）
        if (_config != null)
        {
            string[] segNames;
            if (_config.Segments.Count > 0)
            {
                segNames = new string[_config.Segments.Count];
                for (int i = 0; i < _config.Segments.Count; i++)
                {
                    var n = string.IsNullOrEmpty(_config.Segments[i].Name) ? "Unnamed" : _config.Segments[i].Name;
                    segNames[i] = $"{i}: {n}";
                }
            }
            else
            {
                segNames = new string[] { "No Segments" };
            }

            int newIndex = EditorGUILayout.Popup(_selectedSegmentIndex, segNames, EditorStyles.toolbarPopup, GUILayout.Width(220));
            if (newIndex != _selectedSegmentIndex && newIndex < _config.Segments.Count)
            {
                _selectedSegmentIndex = newIndex;
                _selectedAnimationIndex = newIndex; // 同步选中对应的动画segment
                _selectedHitBoxIndex = -1;
                _selectedVFXIndex = -1;
                _selectedSFXIndex = -1;
                _previewTime = 0f;
                SampleAnimation(_previewTime);
            }

            // add / remove
            if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(24)))
            {
                Undo.RecordObject(_configAsset, "Add Segment");
                var newSeg = new AttackSegmentData { Id = _config.Segments.Count, Name = $"Segment {_config.Segments.Count}" };
                _config.Segments.Add(newSeg);
                EditorUtility.SetDirty(_configAsset);
                AssetDatabase.SaveAssets();
                _selectedSegmentIndex = _config.Segments.Count - 1;
            }

            if (GUILayout.Button("-", EditorStyles.toolbarButton, GUILayout.Width(24)))
            {
                if (_config.Segments.Count > 0)
                {
                    Undo.RecordObject(_configAsset, "Remove Segment");
                    _config.Segments.RemoveAt(_selectedSegmentIndex);
                    EditorUtility.SetDirty(_configAsset);
                    AssetDatabase.SaveAssets();
                    _selectedSegmentIndex = Mathf.Clamp(_selectedSegmentIndex - 1, 0, Mathf.Max(0, _config.Segments.Count - 1));
                }
            }
        }

        GUILayout.Space(10);

        // 预览角色
        _previewCharacter = (GameObject)EditorGUILayout.ObjectField(_previewCharacter, typeof(GameObject), true, GUILayout.Width(220));

        GUILayout.FlexibleSpace();

        // 播放控制（toolbar 上只有这一组）
        GUILayout.Label("Speed:", EditorStyles.toolbarButton);
        _playbackSpeed = GUILayout.HorizontalSlider(_playbackSpeed, 0.1f, 2.0f, GUILayout.Width(90));
        GUILayout.Label($"{_playbackSpeed:F1}x", EditorStyles.miniLabel, GUILayout.Width(36));

        if (GUILayout.Button(_isPlaying ? "Pause" : "Play", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            _isPlaying = !_isPlaying;
            _lastTime = EditorApplication.timeSinceStartup;
        }

        if (GUILayout.Button("Stop", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            _isPlaying = false;
            _previewTime = 0f;
            if (_animancer != null) _animancer.Stop();
            SampleAnimation(_previewTime);
        }

        // Loop 开关
        _loopPreview = GUILayout.Toggle(_loopPreview, "Loop", EditorStyles.toolbarButton, GUILayout.Width(60));

        // 时长显示
        float totalDuration = 0f;
        if (_config != null && _config.Segments.Count > _selectedSegmentIndex)
        {
            totalDuration = _config.Segments[_selectedSegmentIndex].TotalDuration;
        }
        GUILayout.Label($"{_previewTime:F2}s / {totalDuration:F2}s", EditorStyles.toolbarButton, GUILayout.Width(150));

        GUILayout.EndHorizontal();
    }

    #endregion

    #region Timeline Area (Scroll + Zoom + Draw Tracks)

    private void DrawTimelineArea()
    {
        if (_config == null || _config.Segments.Count == 0) return;
        var segment = _config.Segments[_selectedSegmentIndex];

        GUILayout.BeginVertical();
            float pps = PixelsPerSecond;

            // 计算所有segments的最远结束时间
            float maxEndTime = 1.0f;
            foreach (var seg in _config.Segments)
            {
                maxEndTime = Mathf.Max(maxEndTime, seg.EndTime);
            }
            float totalDuration = Mathf.Max(maxEndTime, 1.0f);

            float contentWidth = Mathf.Max(position.width - 340f, totalDuration * pps + 200f); // 340 = 320(inspector) + 20(margin)

            // 计算所有segment的总VFX和SFX数量
            int totalVFXCount = 0;
            int totalSFXCount = 0;
            foreach (var seg in _config.Segments)
            {
                totalVFXCount += seg.VisualEffects.Count;
                totalSFXCount += seg.SoundEffects.Count;
            }

            // 计算实际需要的content高度
            float rulerHeight = 25f;
            float animTrackHeight = 48f;
            float hitTrackHeight = Mathf.Max(32, segment.HitBoxes.Count * 22 + 8);
            float vfxTrackHeight = 48f;
            float sfxTrackHeight = 48f;
            float spacing = 2f * 4; // 4个轨道间的间距
            float contentHeight = rulerHeight + animTrackHeight + hitTrackHeight + vfxTrackHeight + sfxTrackHeight + spacing;

            _timelineScroll = EditorGUILayout.BeginScrollView(_timelineScroll, GUILayout.Height(260), GUILayout.MinWidth(200), GUILayout.MaxWidth(position.width - 340f));
            Rect timelineRect = GUILayoutUtility.GetRect(contentWidth, contentHeight);

            HandleZoomWheel(timelineRect, pps, totalDuration);

            // Timeline背景（Unity Timeline风格）
            EditorGUI.DrawRect(timelineRect, TimelineBackgroundColor);
            DrawTimeRuler(timelineRect, totalDuration, pps);

            float currentY = timelineRect.y + 25; // 从ruler下方开始
            Rect animTrackRect = new Rect(timelineRect.x, currentY, timelineRect.width, 48);
            DrawAnimationTrack(animTrackRect, segment, pps);
            currentY += 48 + 2; // 轨道间距减小

            Rect hitRect = new Rect(timelineRect.x, currentY, timelineRect.width, Mathf.Max(32, segment.HitBoxes.Count * 22 + 8));
            DrawHitBoxesTrack(hitRect, segment, pps);
            currentY += hitRect.height + 2;

            // VFX轨道 - 单行显示多个clips
            Rect vfxRect = new Rect(timelineRect.x, currentY, timelineRect.width, 48);
            DrawVFXTrack(vfxRect, pps);
            currentY += vfxRect.height + 2;

            // SFX轨道 - 单行显示多个clips
            Rect sfxRect = new Rect(timelineRect.x, currentY, timelineRect.width, 48);
            DrawSFXTrack(sfxRect, pps);
            currentY += sfxRect.height + 2;

            DrawPlayhead(timelineRect, totalDuration, pps);

            // 统一处理所有轨道交互 - 按从上到下的顺序
            HandleAllTrackInteractions(animTrackRect, hitRect, vfxRect, sfxRect, segment, pps, totalDuration);

            EditorGUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void HandleZoomWheel(Rect timelineRect, float pixelsPerSecond, float totalDuration)
    {
        Event e = Event.current;
        if (e.type == EventType.ScrollWheel && timelineRect.Contains(e.mousePosition))
        {
            float delta = -e.delta.y * 0.08f;
            _zoom = Mathf.Clamp(_zoom + delta, 0.25f, 3.0f);

            float mouseGlobalX = e.mousePosition.x + _timelineScroll.x - timelineRect.x;
            float mouseTime = mouseGlobalX / PixelsPerSecond;
            _timelineScroll.x = mouseTime * PixelsPerSecond - (e.mousePosition.x - timelineRect.x);

            e.Use();
        }
    }

    private void DrawTimeRuler(Rect rect, float totalDuration, float pps)
    {
        Rect ruler = new Rect(rect.x, rect.y, rect.width, 25);
        EditorGUI.DrawRect(ruler, RulerBackgroundColor);
        
        // 绘制边框
        Handles.color = TrackBorderColor;
        Handles.DrawLine(new Vector3(rect.x, rect.y + ruler.height), new Vector3(rect.x + rect.width, rect.y + ruler.height));
        
        // 处理ruler区域的点击（用于拖动播放头）
        Event e = Event.current;
        if (ruler.Contains(e.mousePosition))
        {
            EditorGUIUtility.AddCursorRect(ruler, MouseCursor.MoveArrow);

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                _draggingPlayhead = true;
                float mouseTime = (e.mousePosition.x + _timelineScroll.x - rect.x) / pps;
                _previewTime = Mathf.Clamp(mouseTime, 0f, totalDuration);
                _isPlaying = false; // 拖动时停止播放
                _forceSample = true; // 拖动开始时强制重新采样
                SampleAnimation(_previewTime);
                Repaint();
                e.Use();
            }
        }

        // 主刻度（每秒）
        int majorCount = Mathf.CeilToInt(totalDuration);
        for (int i = 0; i <= majorCount; i++)
        {
            float t = i;
            float x = rect.x + t * pps;
            Handles.color = RulerLineColor;
            Handles.DrawLine(new Vector3(x, rect.y + 0), new Vector3(x, rect.y + ruler.height));
            
            // 时间标签
            GUI.Label(new Rect(x + 4, rect.y + 2, 50, 20), $"{t:F1}", _rulerLabelStyle);
        }
        
        // 次刻度（每0.5秒）
        for (float t = 0.5f; t <= totalDuration; t += 0.5f)
        {
            float x = rect.x + t * pps;
            Handles.color = RulerLineColor * 0.6f;
            Handles.DrawLine(new Vector3(x, rect.y + ruler.height * 0.6f), new Vector3(x, rect.y + ruler.height));
        }
    }

    private void DrawPlayhead(Rect timelineRect, float totalDuration, float pps)
    {
        float x = timelineRect.x + _previewTime * pps;
        // Unity Timeline风格的播放头（红色，稍微粗一点）
        Handles.color = PlayheadColor;
        Handles.DrawLine(new Vector3(x, timelineRect.y), new Vector3(x, timelineRect.y + timelineRect.height));
        // 绘制播放头顶部的小三角
        Vector3[] triangle = new Vector3[]
        {
            new Vector3(x, timelineRect.y),
            new Vector3(x - 4, timelineRect.y + 8),
            new Vector3(x + 4, timelineRect.y + 8)
        };
        Handles.DrawAAConvexPolygon(triangle);
        
        // 添加可拖动区域（播放头周围的小区域）
        Rect playheadDragRect = new Rect(x - 5, timelineRect.y, 10, timelineRect.height);
        EditorGUIUtility.AddCursorRect(playheadDragRect, MouseCursor.MoveArrow);
        
        // 处理播放头的拖动
        Event e = Event.current;
        if (playheadDragRect.Contains(e.mousePosition) && !_draggingPlayhead)
        {
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                _draggingPlayhead = true;
                _isPlaying = false; // 拖动时停止播放
                e.Use();
            }
        }

        if (_draggingPlayhead)
        {
            if (e.type == EventType.MouseDrag)
            {
                float mouseTime = (e.mousePosition.x + _timelineScroll.x - timelineRect.x) / pps;
                _previewTime = Mathf.Clamp(mouseTime, 0f, totalDuration);
                _forceSample = true; // 拖动时强制重新采样
                SampleAnimation(_previewTime);
                Repaint();
                e.Use();
            }
            else if (e.type == EventType.MouseUp)
            {
                _draggingPlayhead = false;
                e.Use();
            }
        }
    }

    #endregion

    #region Animation Track Draw & Interaction (Resize / Move / Blend)

    private void DrawAnimationTrack(Rect trackRect, AttackSegmentData segment, float pps)
    {
        // 验证Animation选中状态是否仍然有效
        if (_selectedAnimationIndex >= _config.Segments.Count) _selectedAnimationIndex = -1;

        // 轨道背景
        EditorGUI.DrawRect(trackRect, TrackBackgroundColor);

        // 轨道边框
        Handles.color = TrackBorderColor;
        Handles.DrawLine(new Vector3(trackRect.x, trackRect.y), new Vector3(trackRect.x + trackRect.width, trackRect.y));
        Handles.DrawLine(new Vector3(trackRect.x, trackRect.yMax), new Vector3(trackRect.x + trackRect.width, trackRect.yMax));

        // 处理拖拽到轨道上的AnimationClip
        HandleDragDrop(trackRect, segment, pps, TrackType.Animation);

        // 轨道标签
        Rect headerRect = new Rect(trackRect.x + 4, trackRect.y + 2, 120, 20);
        GUI.Label(headerRect, "Animation Segments", _trackHeaderStyle);

        // 处理鼠标事件 - 先收集所有clip信息，然后统一处理选择
        List<(Rect clipRect, int index)> clipInfos = new List<(Rect, int)>();
        bool hasMouseEvent = Event.current.type == EventType.MouseDown && Event.current.button == 0;

        // 显示所有segments作为动画clips
        for (int i = 0; i < _config.Segments.Count; i++)
        {
            var seg = _config.Segments[i];
            // 不跳过没有动画的segment，显示所有segment

            float px = trackRect.x + seg.StartTime * pps;
            float pw = Mathf.Max(8f, seg.Duration * pps);
            Rect clipRect = new Rect(px, trackRect.y + 4, pw, trackRect.height - 8);

            // Unity Timeline风格的Clip颜色
            bool isSelected = (_selectedAnimationIndex == i);
            bool hasClip = seg.AnimationClipTrans.Clip != null;
            Color baseColor = hasClip
                ? (isSelected ? ClipSelectedColor : ClipNormalColor)
                : (isSelected ? new Color(0.8f, 0.8f, 0.8f, 0.5f) : new Color(0.5f, 0.5f, 0.5f, 0.3f)); // 没有动画clip的segment使用灰色

            // 收集clip信息用于鼠标事件处理
            clipInfos.Add((clipRect, i));

            // 绘制Clip背景（带圆角效果，使用简单的矩形近似）
            EditorGUI.DrawRect(clipRect, baseColor);

            // Clip边框（选中时更明显）
            Color borderColor = isSelected ? new Color(1f, 1f, 1f, 0.8f) : new Color(0f, 0f, 0f, 0.3f);
            DrawRectOutline(clipRect, borderColor, isSelected ? 2 : 1);

            float fadeIn = GetClipFadeIn(seg);
            float fadeOut = GetClipFadeOut(seg);

            if (fadeIn > 0)
            {
                Rect r = new Rect(clipRect.xMin, clipRect.yMin, Mathf.Min(clipRect.width, fadeIn * pps), clipRect.height);
                EditorGUI.DrawRect(r, new Color(1f, 1f, 1f, 0.12f));
            }
            if (fadeOut > 0)
            {
                Rect r = new Rect(clipRect.xMax - Mathf.Min(clipRect.width, fadeOut * pps), clipRect.yMin, Mathf.Min(clipRect.width, fadeOut * pps), clipRect.height);
                EditorGUI.DrawRect(r, new Color(1f, 1f, 1f, 0.12f));
            }

            // Clip标签（只有在足够宽的时候才显示）
            if (clipRect.width > 60)
            {
                string label = string.IsNullOrEmpty(seg.Name)
                    ? (hasClip ? seg.AnimationClipTrans.Clip.name : "No Animation")
                    : seg.Name;
                GUI.Label(new Rect(clipRect.x + 4, clipRect.y + 2, clipRect.width - 8, 16),
                    label, hasClip ? EditorStyles.whiteLabel : EditorStyles.miniLabel);
                if (clipRect.height > 30)
                {
                    GUI.Label(new Rect(clipRect.x + 4, clipRect.y + 18, clipRect.width - 8, 12),
                        $"{seg.StartTime:F2}s", _smallLabelStyle);
                }
            }

            // 拖拽手柄
            Rect leftHandle = new Rect(clipRect.xMin - 1, clipRect.yMin, 6, clipRect.height);
            Rect rightHandle = new Rect(clipRect.xMax - 5, clipRect.yMin, 6, clipRect.height);

            EditorGUIUtility.AddCursorRect(leftHandle, MouseCursor.ResizeHorizontal);
            EditorGUIUtility.AddCursorRect(rightHandle, MouseCursor.ResizeHorizontal);
        }

        // 统一处理鼠标选择事件 - 确保只处理一次
        if (hasMouseEvent)
        {
            Vector2 mousePos = Event.current.mousePosition;
            bool clickedOnClip = false;

            // 倒序检查（后绘制的clip优先级更高）
            for (int i = clipInfos.Count - 1; i >= 0; i--)
            {
                var (clipRect, index) = clipInfos[i];
                if (clipRect.Contains(mousePos))
                {
                    _selectedAnimationIndex = index;
                    _selectedSegmentIndex = index;
                    clickedOnClip = true;
                    Event.current.Use(); // 标记事件已处理，防止其他UI元素响应
                    Repaint();
                    break; // 找到第一个匹配的clip就停止
                }
            }

            // 如果点击在空白区域，取消选择
            if (!clickedOnClip && trackRect.Contains(mousePos))
            {
                _selectedAnimationIndex = -1;
                _selectedSegmentIndex = -1;
                Event.current.Use(); // 即使是取消选择也要标记事件已处理
                Repaint();
            }
        }
    }

    private float GetClipFadeIn(AttackSegmentData segment)
    {
        var t = segment.AnimationClipTrans;
        // 使用FadeDuration作为fade in时间
        return t.IsValid ? t.FadeDuration : 0f;
    }

    private float GetClipFadeOut(AttackSegmentData segment)
    {
        // Animancer中没有直接的fade out时间，使用默认值
        return 0.1f;
    }

    private bool HandleTimelineInteraction(Rect timelineRect, AttackSegmentData segment, float pps)
    {
        Event e = Event.current;
        if (!timelineRect.Contains(e.mousePosition)) return false;

        // MouseDown: check left/right handles, body for all segments
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            for (int i = 0; i < _config.Segments.Count; i++)
            {
                var seg = _config.Segments[i];

                float px = seg.StartTime * pps;
                float pw = Mathf.Max(8f, seg.Duration * pps);
                // 相对于传入的轨道矩形计算
                Rect r = new Rect(timelineRect.x + px, timelineRect.y, pw, timelineRect.height);
                Rect leftHandle = new Rect(r.xMin, r.yMin, 6, r.height);
                Rect rightHandle = new Rect(r.xMax - 6, r.yMin, 6, r.height);

                if (leftHandle.Contains(e.mousePosition))
                {
                    BeginSegmentInteraction(DragMode.ResizeLeft, e, timelineRect, pps, seg);
                    SetSelectedAnimation(i);
                    e.Use();
                    return true;
                }
                else if (rightHandle.Contains(e.mousePosition))
                {
                    BeginSegmentInteraction(DragMode.ResizeRight, e, timelineRect, pps, seg);
                    SetSelectedAnimation(i);
                    e.Use();
                    return true;
                }
                else if (r.Contains(e.mousePosition))
                {
                    BeginSegmentInteraction(DragMode.Move, e, timelineRect, pps, seg);
                    SetSelectedAnimation(i);
                    e.Use();
                    return true;
                }
            }
        }

        // MouseDrag: use stored dragStartMouseTime and scroll offsets for smoothness
        if (e.type == EventType.MouseDrag && _draggingAnimationIndex >= 0 && _draggingAnimationIndex < _config.Segments.Count)
        {
            var draggingSegment = _config.Segments[_draggingAnimationIndex];

            // compute current mouse time (考虑滚动与 timelineRect 的 offset)
            float mouseTimeNow = (e.mousePosition.x + _timelineScroll.x - timelineRect.x) / pps;
            float deltaSeconds = mouseTimeNow - _dragStartMouseTime;

            if (_dragMode == DragMode.Move)
            {
                // 自由移动开始时间
                float newStart = Mathf.Max(0f, _animationStartTimeCache + deltaSeconds);
                draggingSegment.StartTime = newStart;
            }
            else if (_dragMode == DragMode.ResizeLeft)
            {
                float newStart = Mathf.Max(0f, _animationStartTimeCache + deltaSeconds);
                float newDuration = _animationDurationCache + (_animationStartTimeCache - newStart);
                newDuration = Mathf.Max(0.05f, newDuration);
                draggingSegment.StartTime = newStart;
                // 调整速度以匹配新的持续时间
                if (draggingSegment.AnimationClipTrans.Clip != null && draggingSegment.AnimationClipTrans.Clip.length > 0f)
                {
                    draggingSegment.AnimationClipTrans.Speed = draggingSegment.AnimationClipTrans.Clip.length / newDuration;
                }
            }
            else if (_dragMode == DragMode.ResizeRight)
            {
                // 调整持续时间（通过调整速度）
                float newDuration = Mathf.Max(0.05f, _animationDurationCache + deltaSeconds);
                if (draggingSegment.AnimationClipTrans.Clip != null && draggingSegment.AnimationClipTrans.Clip.length > 0f)
                {
                    draggingSegment.AnimationClipTrans.Speed = draggingSegment.AnimationClipTrans.Clip.length / newDuration;
                }
            }

            EditorUtility.SetDirty(_configAsset);
            Repaint();
            e.Use();
            return true;
        }

        // MouseUp finalize
        if (e.type == EventType.MouseUp)
        {
            if (_draggingAnimationIndex != -1)
            {
                _draggingAnimationIndex = -1;
                _dragMode = DragMode.None;
                _undoRecorded = false;
                EditorUtility.SetDirty(_configAsset);
                AssetDatabase.SaveAssets();
                e.Use();
                return true;
            }
        }

        return false;
    }
    
    private bool HandleOtherTracksInteraction(Rect timelineRect, AttackSegmentData segment, float pps, Event e)
    {
        float totalDuration = segment.TotalDuration;
        float currentY = timelineRect.y + 25 + 48 + 2; // Animation轨道下方
        
        // HitBoxes轨道
        Rect hitRect = new Rect(timelineRect.x, currentY, timelineRect.width, Mathf.Max(32, segment.HitBoxes.Count * 22 + 8));
        if (HandleHitBoxTrackInteraction(hitRect, pps, e))
        {
            return true;
        }
        currentY += hitRect.height + 2;
        
        // VFX轨道 (VFX交互现在在DrawVFXTrack中处理)
        currentY += 48 + 2;
        
        // SFX轨道 (SFX交互现在在DrawSFXTrack中处理)
        
        return false;
    }
    
    private bool HandleHitBoxTrackInteraction(Rect trackRect, float pps, Event e)
    {
        if (!trackRect.Contains(e.mousePosition)) return false;

        // MouseDown: check for selection and drag start across all segments
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            bool handled = false;
            float y = trackRect.y + 4;
            for (int segIndex = 0; segIndex < _config.Segments.Count && !handled; segIndex++)
            {
                var seg = _config.Segments[segIndex];
                for (int i = 0; i < seg.HitBoxes.Count; i++)
                {
                    var hb = seg.HitBoxes[i];
                    float startSec = hb.StartTime * seg.TotalDuration;
                    float endSec = hb.EndTime * seg.TotalDuration;
                    Rect r = new Rect(trackRect.x + startSec * pps, y, Mathf.Max(4f, (endSec - startSec) * pps), 18);

                    if (r.Contains(e.mousePosition))
                    {
                        // 如果这是当前选中的segment，直接使用本地索引
                        if (segIndex == _selectedSegmentIndex)
                        {
                            Undo.RecordObject(_configAsset, "Drag HitBox");
                            _draggingHitBoxIndex = i;
                            _draggingTrackType = "HitBox";
                            _originalTimeValue = hb.StartTime;
                            _dragStartMouseTime = (e.mousePosition.x + _timelineScroll.x - trackRect.x) / pps / seg.TotalDuration;
                            SetSelectedHitBox(i);
                            Repaint();
                        }
                        else
                        {
                            // 如果是其他segment的HitBox，需要切换segment并选中
                            _selectedSegmentIndex = segIndex;
                            SetSelectedHitBox(i);
                            Repaint();
                        }
                        e.Use();
                        return true;
                    }

                    y += 20;
                }
                // Reset y for next segment (stacked layout)
                y = trackRect.y + 4;
            }

            // 如果没有点击到任何HitBox，清空选中状态
            if (!handled)
            {
                ClearAllSelection();
                Repaint();
            }
        }

        // MouseDrag: handle dragging
        if (e.type == EventType.MouseDrag && _draggingHitBoxIndex >= 0 && _draggingTrackType == "HitBox")
        {
            if (_selectedSegmentIndex >= 0 && _selectedSegmentIndex < _config.Segments.Count)
            {
                var segment = _config.Segments[_selectedSegmentIndex];
                if (_draggingHitBoxIndex < segment.HitBoxes.Count)
                {
                    float mouseTimeNow = (e.mousePosition.x + _timelineScroll.x - trackRect.x) / pps / segment.TotalDuration;
                    float deltaTime = mouseTimeNow - _dragStartMouseTime;
                    var hb = segment.HitBoxes[_draggingHitBoxIndex];
                    float newStartTime = Mathf.Clamp01(_originalTimeValue + deltaTime);
                    float duration = hb.EndTime - hb.StartTime;
                    hb.StartTime = newStartTime;
                    hb.EndTime = Mathf.Clamp01(newStartTime + duration);
                    EditorUtility.SetDirty(_configAsset);
                    Repaint();
                    e.Use();
                    return true;
                }
            }
        }

        // MouseUp: finalize drag
        if (e.type == EventType.MouseUp && _draggingHitBoxIndex >= 0 && _draggingTrackType == "HitBox")
        {
            _draggingHitBoxIndex = -1;
            _draggingTrackType = "";
            EditorUtility.SetDirty(_configAsset);
            AssetDatabase.SaveAssets();
            e.Use();
            return true;
        }

        return false;
    }
    
    private bool HandleVFXTrackInteraction(Rect trackRect, AttackSegmentData segment, float totalDuration, float pps, Event e)
    {
        if (!trackRect.Contains(e.mousePosition)) return false;
        
        float y = trackRect.y + 4;
        for (int i = 0; i < segment.VisualEffects.Count; i++)
        {
            var vfx = segment.VisualEffects[i];
            float sec = vfx.TriggerTime * totalDuration;
            Rect r = new Rect(trackRect.x + sec * pps - 6, y, 12, 16);
            
            if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition))
            {
                Undo.RecordObject(_configAsset, "Drag VFX");
                _draggingVFXIndex = i;
                _draggingTrackType = "VFX";
                _originalTimeValue = vfx.TriggerTime;
                _dragStartMouseTime = (e.mousePosition.x + _timelineScroll.x - trackRect.x) / pps / totalDuration;
                e.Use();
                return true;
            }
            
            y += 20;
        }
        
        if (e.type == EventType.MouseDrag && _draggingVFXIndex >= 0 && _draggingTrackType == "VFX")
        {
            float mouseTimeNow = (e.mousePosition.x + _timelineScroll.x - trackRect.x) / pps / totalDuration;
            float deltaTime = mouseTimeNow - _dragStartMouseTime;
            var vfx = segment.VisualEffects[_draggingVFXIndex];
            vfx.TriggerTime = Mathf.Clamp01(_originalTimeValue + deltaTime);
            EditorUtility.SetDirty(_configAsset);
            Repaint();
            e.Use();
            return true;
        }
        
        if (e.type == EventType.MouseUp && _draggingVFXIndex >= 0 && _draggingTrackType == "VFX")
        {
            _draggingVFXIndex = -1;
            _draggingTrackType = "";
            EditorUtility.SetDirty(_configAsset);
            AssetDatabase.SaveAssets();
            e.Use();
            return true;
        }
        
        return false;
    }
    

    private void BeginSegmentInteraction(DragMode mode, Event e, Rect timelineRect, float pps, AttackSegmentData segment)
    {
        if (!_undoRecorded)
        {
            Undo.RecordObject(_configAsset, "Modify Animation Segment");
            _undoRecorded = true;
        }

        // 找到segment在列表中的索引
        int segmentIndex = _config.Segments.IndexOf(segment);
        _draggingAnimationIndex = segmentIndex;
        _dragMode = mode;
        _dragStartMouseX = e.mousePosition.x;

        // record mouse time in seconds at start (consider scroll & timelineRect)
        _dragStartMouseTime = (e.mousePosition.x + _timelineScroll.x - timelineRect.x) / pps;

        _animationStartTimeCache = segment.StartTime;
        _animationDurationCache = segment.Duration;
    }

    private void DrawRectOutline(Rect rect, Color color, int width = 1)
    {
        Handles.color = color;
        // 上边
        Handles.DrawLine(new Vector3(rect.xMin, rect.yMin), new Vector3(rect.xMax, rect.yMin));
        // 下边
        Handles.DrawLine(new Vector3(rect.xMin, rect.yMax), new Vector3(rect.xMax, rect.yMax));
        // 左边
        Handles.DrawLine(new Vector3(rect.xMin, rect.yMin), new Vector3(rect.xMin, rect.yMax));
        // 右边
        Handles.DrawLine(new Vector3(rect.xMax, rect.yMin), new Vector3(rect.xMax, rect.yMax));
    }

    #endregion

    #region HitBoxes / VFX / SFX Tracks

    private void DrawHitBoxesTrack(Rect rect, AttackSegmentData segment, float pps)
    {
        // 轨道背景
        EditorGUI.DrawRect(rect, TrackBackgroundColor);
        Handles.color = TrackBorderColor;
        Handles.DrawLine(new Vector3(rect.x, rect.y), new Vector3(rect.x + rect.width, rect.y));
        Handles.DrawLine(new Vector3(rect.x, rect.yMax), new Vector3(rect.x + rect.width, rect.yMax));
        Rect headerRect = new Rect(rect.x + 4, rect.y + 2, 120, 20);
        GUI.Label(headerRect, "HitBoxes", _trackHeaderStyle);

        // 轨道标题右键菜单 - 清空所有HitBoxes
        if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && headerRect.Contains(Event.current.mousePosition))
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Clear All HitBoxes"), false, () =>
            {
                Undo.RecordObject(_configAsset, "Clear All HitBoxes");
                segment.HitBoxes.Clear();
                EditorUtility.SetDirty(_configAsset);
                AssetDatabase.SaveAssets();
                Repaint();
            });
            menu.ShowAsContext();
            Event.current.Use();
        }
        
        float y = rect.y + 4;
        for (int i = 0; i < segment.HitBoxes.Count; i++)
        {
            var hb = segment.HitBoxes[i];
            float startSec = hb.StartTime * segment.TotalDuration;
            float endSec = hb.EndTime * segment.TotalDuration;
                Rect r = new Rect(rect.x + startSec * pps, y, Mathf.Max(4f, (endSec - startSec) * pps), 18);
                bool isSelected = (_selectedHitBoxIndex == i);
                Color baseColor = (_previewTime >= startSec && _previewTime <= endSec) ? new Color(1, 0.2f, 0.2f, 0.6f) : new Color(0.8f, 0.4f, 0.4f, 0.3f);
                if (isSelected) baseColor = Color.Lerp(baseColor, Color.blue, 0.3f);
                EditorGUI.DrawRect(r, baseColor);
                DrawRectOutline(r, isSelected ? new Color(0f, 0.8f, 1f, 0.8f) : new Color(1f, 1f, 1f, 0.4f), isSelected ? 2 : 1);

                // 点击选中现在在HandleHitBoxTrackInteraction中处理

            GUI.Label(new Rect(r.x + 2, r.y, 120, r.height), $"HB{i} {startSec:F2}-{endSec:F2}", _smallLabelStyle);

            // 右键菜单删除HitBox
            if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && r.Contains(Event.current.mousePosition))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Delete HitBox"), false, () =>
                {
                    Undo.RecordObject(_configAsset, "Delete HitBox");
                    if (i < segment.HitBoxes.Count)
                    {
                        segment.HitBoxes.RemoveAt(i);
                    }
                    EditorUtility.SetDirty(_configAsset);
                    AssetDatabase.SaveAssets();
                    Repaint();
                });
                menu.ShowAsContext();
                Event.current.Use();
            }

            y += 20;
        }
    }

    private void DrawVFXTrack(Rect rect, float pps)
    {
        // 轨道背景
        EditorGUI.DrawRect(rect, TrackBackgroundColor);
        Handles.color = TrackBorderColor;
        Handles.DrawLine(new Vector3(rect.x, rect.y), new Vector3(rect.x + rect.width, rect.y));
        Handles.DrawLine(new Vector3(rect.x, rect.yMax), new Vector3(rect.x + rect.width, rect.yMax));

        // 处理拖拽到轨道上的GameObject（特效预制体）
        HandleDragDrop(rect, _config.Segments[_selectedSegmentIndex], pps, TrackType.VFX);
        Rect headerRect = new Rect(rect.x + 4, rect.y + 2, 120, 20);
        GUI.Label(headerRect, "Visual Effects", _trackHeaderStyle);

        // 轨道标题右键菜单 - 清空所有VFX
        if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && headerRect.Contains(Event.current.mousePosition))
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Clear All Visual Effects"), false, () =>
            {
                Undo.RecordObject(_configAsset, "Clear All Visual Effects");
                foreach (var seg in _config.Segments)
                {
                    seg.VisualEffects.Clear();
                }
                _selectedVFXIndex = -1;
                EditorUtility.SetDirty(_configAsset);
                AssetDatabase.SaveAssets();
                Repaint();
            });
            menu.ShowAsContext();
            Event.current.Use();
        }
        
        // 绘制VFX clips - 只显示当前选中segment的VFX
        const float CLIP_HEIGHT = 32f;
        var segment = _config.Segments[_selectedSegmentIndex];
        for (int i = 0; i < segment.VisualEffects.Count; i++)
        {
            var vfx = segment.VisualEffects[i];
            float startSec = vfx.TriggerTime * segment.TotalDuration;
            float duration = vfx.Duration;
            float px = rect.x + startSec * pps;
            float pw = Mathf.Max(20f, duration * pps); // 最小宽度20像素
            Rect clipRect = new Rect(px, rect.y + 4, pw, CLIP_HEIGHT);

            // Unity Timeline风格的VFX Clip颜色
            bool isSelected = (_selectedVFXIndex == i);
            Color baseColor = new Color(0.8f, 0.3f, 0.9f, 0.8f);
            if (isSelected) baseColor = Color.Lerp(baseColor, new Color(0.4f, 0.6f, 1f), 0.4f);

            // 绘制Clip背景
            EditorGUI.DrawRect(clipRect, baseColor);

            // Clip边框（选中时更明显）
            Color borderColor = isSelected ? new Color(1f, 1f, 1f, 0.8f) : new Color(0f, 0f, 0f, 0.3f);
            DrawRectOutline(clipRect, borderColor, isSelected ? 2 : 1);

            // Clip标签
            if (clipRect.width > 40)
            {
                string label = string.IsNullOrEmpty(vfx.Name) ? "VFX" : vfx.Name;
                GUI.Label(new Rect(clipRect.x + 4, clipRect.y + 2, clipRect.width - 8, 14),
                    label, EditorStyles.whiteMiniLabel);
                GUI.Label(new Rect(clipRect.x + 4, clipRect.y + 16, clipRect.width - 8, 12),
                    $"{startSec:F2}s", _smallLabelStyle);
            }

            // 拖拽手柄
            Rect leftHandle = new Rect(clipRect.xMin - 1, clipRect.yMin, 6, clipRect.height);
            Rect rightHandle = new Rect(clipRect.xMax - 5, clipRect.yMin, 6, clipRect.height);

            EditorGUIUtility.AddCursorRect(leftHandle, MouseCursor.ResizeHorizontal);
            EditorGUIUtility.AddCursorRect(rightHandle, MouseCursor.ResizeHorizontal);

            // 点击选中
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                clipRect.Contains(Event.current.mousePosition))
            {
                SetSelectedVFX(_selectedSegmentIndex, i);
                Repaint();
            }

            // 右键菜单删除VFX
            if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && clipRect.Contains(Event.current.mousePosition))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Delete VFX"), false, () =>
                {
                    Undo.RecordObject(_configAsset, "Delete VFX");
                    if (i < segment.VisualEffects.Count)
                    {
                        segment.VisualEffects.RemoveAt(i);
                        if (_selectedVFXIndex == i)
                        {
                            _selectedVFXIndex = -1;
                        }
                        else if (_selectedVFXIndex > i)
                        {
                            _selectedVFXIndex--; // 调整选中索引
                        }
                    }
                    EditorUtility.SetDirty(_configAsset);
                    AssetDatabase.SaveAssets();
                    Repaint();
                });
                menu.ShowAsContext();
                Event.current.Use();
            }
        }

        // 处理VFX拖拽交互
        HandleVFXInteraction(rect, pps);
    }

    private void DrawSFXTrack(Rect rect, float pps)
    {
        // 轨道背景
        EditorGUI.DrawRect(rect, TrackBackgroundColor);
        Handles.color = TrackBorderColor;
        Handles.DrawLine(new Vector3(rect.x, rect.y), new Vector3(rect.x + rect.width, rect.y));
        Handles.DrawLine(new Vector3(rect.x, rect.yMax), new Vector3(rect.x + rect.width, rect.yMax));

        // 处理拖拽到轨道上的AudioClip
        HandleDragDrop(rect, _config.Segments[_selectedSegmentIndex], pps, TrackType.SFX);
        Rect headerRect = new Rect(rect.x + 4, rect.y + 2, 120, 20);
        GUI.Label(headerRect, "Sound Effects", _trackHeaderStyle);

        // 轨道标题右键菜单 - 清空所有SFX
        if (Event.current.type == EventType.MouseDown && Event.current.button == 1 &&
            headerRect.Contains(Event.current.mousePosition))
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Clear All Sound Effects"), false, () =>
            {
                Undo.RecordObject(_configAsset, "Clear All Sound Effects");
                foreach (var seg in _config.Segments)
                {
                    seg.SoundEffects.Clear();
                }

                _selectedSFXIndex = -1;
                EditorUtility.SetDirty(_configAsset);
                AssetDatabase.SaveAssets();
                Repaint();
            });
            menu.ShowAsContext();
            Event.current.Use();
        }

        // 绘制SFX clips - 只显示当前选中segment的SFX
        const float CLIP_HEIGHT = 32f;
        var segment = _config.Segments[_selectedSegmentIndex];
        for (int i = 0; i < segment.SoundEffects.Count; i++)
        {
            var sfx = segment.SoundEffects[i];
            float startSec = sfx.TriggerTime * segment.TotalDuration;
            // 使用音频剪辑长度作为默认持续时间，如果没有则使用1秒
            float duration = sfx.Clip != null ? sfx.Clip.length : 1f;
            float px = rect.x + startSec * pps;
            float pw = Mathf.Max(20f, duration * pps); // 最小宽度20像素
            Rect clipRect = new Rect(px, rect.y + 4, pw, CLIP_HEIGHT);

            // Unity Timeline风格的SFX Clip颜色
            bool isSelected = (_selectedSFXIndex == i);
            Color baseColor = new Color(0.3f, 0.9f, 0.3f, 0.8f);
            if (isSelected) baseColor = Color.Lerp(baseColor, new Color(0.4f, 0.6f, 1f), 0.4f);

            // 绘制Clip背景
            EditorGUI.DrawRect(clipRect, baseColor);

            // Clip边框（选中时更明显）
            Color borderColor = isSelected ? new Color(1f, 1f, 1f, 0.8f) : new Color(0f, 0f, 0f, 0.3f);
            DrawRectOutline(clipRect, borderColor, isSelected ? 2 : 1);

            // Clip标签
            if (clipRect.width > 40)
            {
                string label = string.IsNullOrEmpty(sfx.Name) ? (sfx.Clip ? sfx.Clip.name : "SFX") : sfx.Name;
                GUI.Label(new Rect(clipRect.x + 4, clipRect.y + 2, clipRect.width - 8, 14),
                    label, EditorStyles.whiteMiniLabel);
                GUI.Label(new Rect(clipRect.x + 4, clipRect.y + 16, clipRect.width - 8, 12),
                    $"{startSec:F2}s", _smallLabelStyle);
            }

            // 拖拽手柄 (SFX只支持移动，不支持调整长度，因为长度由音频决定)
            Rect moveHandle = new Rect(clipRect.xMin, clipRect.yMin, clipRect.width, clipRect.height);

            // 点击选中
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                clipRect.Contains(Event.current.mousePosition))
            {
                SetSelectedSFX(_selectedSegmentIndex, i);
                Repaint();
            }

            // 右键菜单删除SFX
            if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && clipRect.Contains(Event.current.mousePosition))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Delete SFX"), false, () =>
                {
                    Undo.RecordObject(_configAsset, "Delete SFX");
                    if (i < segment.SoundEffects.Count)
                    {
                        segment.SoundEffects.RemoveAt(i);
                        if (_selectedSFXIndex == i)
                        {
                            _selectedSFXIndex = -1;
                        }
                        else if (_selectedSFXIndex > i)
                        {
                            _selectedSFXIndex--; // 调整选中索引
                        }
                    }
                    EditorUtility.SetDirty(_configAsset);
                    AssetDatabase.SaveAssets();
                    Repaint();
                });
                menu.ShowAsContext();
                Event.current.Use();
            }
        }

        // SFX交互现在在HandleAllTrackInteractions中统一处理
    }

    #endregion

    #region Preview / Animancer Sampling (Blend with Curve) + scale-safe

    private void UpdatePreviewPlay(float deltaTime)
    {
        if (_config == null || _config.Segments.Count == 0) return;

        // 计算所有segments的最大结束时间
        float maxEndTime = 0f;
        foreach (var seg in _config.Segments)
        {
            maxEndTime = Mathf.Max(maxEndTime, seg.EndTime);
        }

        _previewTime += deltaTime;
        if (_previewTime > maxEndTime)
        {
            if (_loopPreview) _previewTime = 0f;
            else { _previewTime = maxEndTime; _isPlaying = false; }
        }
        SampleAnimation(_previewTime);
    }

    /// <summary>
    /// 核心：基于当前 globalTime 采样 Animancer 状态，支持 overlap blending。
    /// 在 Evaluate 前后保存并恢复预览角色的 localScale 避免动画对缩放的永久影响。
    /// </summary>
    private void SampleAnimation(float globalTime)
    {
        if (_config == null || _previewCharacter == null) return;
        var segment = _config.Segments[_selectedSegmentIndex];
        if (segment.AnimationClipTrans.Clip == null)
        {
            if (_animancer != null && _animancer.IsGraphInitialized) _animancer.Stop();
            return;
        }

        // 避免重复采样相同的帧（优化性能）
        if (Mathf.Approximately(_lastSampledTime, globalTime) && !_forceSample)
        {
            return;
        }
        _lastSampledTime = globalTime;
        _forceSample = false;

        // ensure animancer on previewCharacter
        if (_animancer == null)
        {
            _animancer = _previewCharacter.GetComponent<AnimancerComponent>();
            if (_animancer == null) _animancer = _previewCharacter.AddComponent<AnimancerComponent>();

            // 禁用RootMotion以避免位置和旋转被动画影响
            if (_animancer.Animator != null)
            {
                _animancer.Animator.applyRootMotion = false;
            }
        }

        // 保存所有Transform的原始scale（包括根对象和所有子对象）
        Dictionary<Transform, Vector3> originalScales = new Dictionary<Transform, Vector3>();
        SaveTransformsScale(_previewCharacter.transform, originalScales);

        // 找到当前时间应该播放的segment
        AttackSegmentData currentSegment = null;
        foreach (var seg in _config.Segments)
        {
            if (seg.AnimationClipTrans.Clip != null &&
                globalTime >= seg.StartTime && globalTime <= seg.EndTime)
            {
                currentSegment = seg;
                break;
            }
        }

        if (currentSegment != null)
        {
            // 播放找到的segment动画
            float localTime = Mathf.Clamp((globalTime - currentSegment.StartTime) * currentSegment.AnimationClipTrans.Speed,
                                         0f, currentSegment.AnimationClipTrans.Clip.length);

            var state = _animancer.Play(currentSegment.AnimationClipTrans);
            state.Speed = 0f;
            state.Time = localTime;
            state.Weight = 1f;
            _animancer.Evaluate();
        }
        else
        {
            // 没有找到合适的segment，停止动画
            if (_animancer.IsGraphInitialized) _animancer.Stop();
        }

        // 立即恢复所有Transform的原始scale，防止动画修改scale
        RestoreTransformsScale(originalScales);
    }
    
    private void SaveTransformsScale(Transform root, Dictionary<Transform, Vector3> scales)
    {
        scales[root] = root.localScale;
        for (int i = 0; i < root.childCount; i++)
        {
            SaveTransformsScale(root.GetChild(i), scales);
        }
    }
    
    private void RestoreTransformsScale(Dictionary<Transform, Vector3> scales)
    {
        foreach (var kvp in scales)
        {
            if (kvp.Key != null)
            {
                kvp.Key.localScale = kvp.Value;
            }
        }
    }

    private float EvaluateFadeInCurve(AttackSegmentData segment, float t)
    {
        // 简化实现：由于现在只有一个动画片段，不需要复杂的blend曲线
        return Mathf.Clamp01(t);
    }

    #endregion

    #region Inspector (右侧) - 保留并增强 Undo/SetDirty/Save

    private void DrawInspector()
    {
        if (_config == null || _config.Segments.Count == 0) return;
        if (_selectedSegmentIndex < 0 || _selectedSegmentIndex >= _config.Segments.Count) return;

        var segment = _config.Segments[_selectedSegmentIndex];

        // 验证选中状态是否仍然有效
        if (_selectedAnimationIndex >= _config.Segments.Count) _selectedAnimationIndex = -1;
        if (_selectedHitBoxIndex >= segment.HitBoxes.Count) _selectedHitBoxIndex = -1;
        if (_selectedVFXIndex >= segment.VisualEffects.Count) _selectedVFXIndex = -1;
        if (_selectedSFXIndex >= segment.SoundEffects.Count) _selectedSFXIndex = -1;

        EditorGUILayout.LabelField("Segment Config", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUI.BeginChangeCheck();
            segment.Name = EditorGUILayout.TextField("Segment Name", segment.Name);
            EditorGUILayout.LabelField("Total Duration", $"{segment.TotalDuration:F2}s");
            segment.AnimationSpeed = EditorGUILayout.FloatField("Animation Speed", segment.AnimationSpeed);
            if (EditorGUI.EndChangeCheck())
            {
                RegisterUndoAndSave("Edit Segment");
            }
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Animation Properties", EditorStyles.boldLabel);
        if (_selectedAnimationIndex == _selectedSegmentIndex)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();
                segment.Name = EditorGUILayout.TextField("Segment Name", segment.Name);
                segment.AnimationClipTrans.Clip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", segment.AnimationClipTrans.Clip, typeof(AnimationClip), false);
                segment.StartTime = EditorGUILayout.FloatField("Start Time (s)", segment.StartTime);
                float newSpeed = EditorGUILayout.FloatField("Speed", segment.AnimationClipTrans.Speed);
                if (Mathf.Abs(newSpeed) > 0.0001f) segment.AnimationClipTrans.Speed = newSpeed;

                segment.AnimationClipTrans.FadeDuration = EditorGUILayout.FloatField(new GUIContent("Fade Duration", "Crossfade time from previous animation"), segment.AnimationClipTrans.FadeDuration);

                EditorGUILayout.HelpBox($"End Time: {segment.EndTime:F2}s (Duration: {segment.Duration:F2}s)", MessageType.Info);

                if (EditorGUI.EndChangeCheck())
                {
                    RegisterUndoAndSave("Edit Animation Segment");
                    SampleAnimation(_previewTime);
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Select the animation clip in the Timeline to edit its properties.", MessageType.None);
        }

        EditorGUILayout.Space();
        DrawVisualEffectsInspector(segment);
        EditorGUILayout.Space();
        DrawSoundEffectsInspector(segment);
        EditorGUILayout.Space();
        DrawHitBoxesInspector(segment);
    }

    private void DrawVisualEffectsInspector(AttackSegmentData segment)
    {
        EditorGUILayout.LabelField("Visual Effects", EditorStyles.boldLabel);
        for (int i = 0; i < segment.VisualEffects.Count; i++)
        {
            var vfx = segment.VisualEffects[i];
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"VFX {i}", EditorStyles.miniBoldLabel);
                if (GUILayout.Button("X", GUILayout.Width(20))) { Undo.RecordObject(_configAsset, "Delete VFX"); segment.VisualEffects.RemoveAt(i); EditorUtility.SetDirty(_configAsset); AssetDatabase.SaveAssets(); return; }
                GUILayout.EndHorizontal();

                vfx.Prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", vfx.Prefab, typeof(GameObject), false);
                float displayTime = vfx.TriggerTime * segment.TotalDuration;
                displayTime = EditorGUILayout.FloatField("Trigger Time (s)", displayTime);
                vfx.TriggerTime = Mathf.Clamp01(displayTime / Mathf.Max(0.0001f, segment.TotalDuration));
                vfx.Offset = EditorGUILayout.Vector3Field("Offset", vfx.Offset);
                vfx.Duration = EditorGUILayout.FloatField("Duration", vfx.Duration);
            }
        }
        if (GUILayout.Button("Add VFX"))
        {
            Undo.RecordObject(_configAsset, "Add VFX");
            segment.VisualEffects.Add(new VisualEffectData { TriggerTime = Mathf.Clamp01(_previewTime / Mathf.Max(0.0001f, segment.TotalDuration)) });
            EditorUtility.SetDirty(_configAsset);
            AssetDatabase.SaveAssets();
        }
    }

    private void DrawSoundEffectsInspector(AttackSegmentData segment)
    {
        EditorGUILayout.LabelField("Sound Effects", EditorStyles.boldLabel);
        for (int i = 0; i < segment.SoundEffects.Count; i++)
        {
            var sfx = segment.SoundEffects[i];
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"SFX {i}", EditorStyles.miniBoldLabel);
                if (GUILayout.Button("X", GUILayout.Width(20))) { Undo.RecordObject(_configAsset, "Delete SFX"); segment.SoundEffects.RemoveAt(i); EditorUtility.SetDirty(_configAsset); AssetDatabase.SaveAssets(); return; }
                GUILayout.EndHorizontal();

                sfx.Clip = (AudioClip)EditorGUILayout.ObjectField("Clip", sfx.Clip, typeof(AudioClip), false);
                float displayTime = sfx.TriggerTime * segment.TotalDuration;
                displayTime = EditorGUILayout.FloatField("Trigger Time (s)", displayTime);
                sfx.TriggerTime = Mathf.Clamp01(displayTime / Mathf.Max(0.0001f, segment.TotalDuration));
                sfx.Volume = EditorGUILayout.Slider("Volume", sfx.Volume, 0f, 1f);
            }
        }
        if (GUILayout.Button("Add SFX"))
        {
            Undo.RecordObject(_configAsset, "Add SFX");
            segment.SoundEffects.Add(new SoundEffectData { TriggerTime = Mathf.Clamp01(_previewTime / Mathf.Max(0.0001f, segment.TotalDuration)) });
            EditorUtility.SetDirty(_configAsset);
            AssetDatabase.SaveAssets();
        }
    }

    private void DrawHitBoxesInspector(AttackSegmentData segment)
    {
        EditorGUILayout.LabelField("HitBoxes", EditorStyles.boldLabel);
        for (int i = 0; i < segment.HitBoxes.Count; i++)
        {
            var hb = segment.HitBoxes[i];
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"HB {i}", EditorStyles.miniBoldLabel);
                if (GUILayout.Button("X", GUILayout.Width(20))) { Undo.RecordObject(_configAsset, "Delete HitBox"); segment.HitBoxes.RemoveAt(i); EditorUtility.SetDirty(_configAsset); AssetDatabase.SaveAssets(); return; }
                GUILayout.EndHorizontal();

                hb.ShapeType = (HitShapeType)EditorGUILayout.EnumPopup("Shape", hb.ShapeType);
                hb.Offset = EditorGUILayout.Vector3Field("Offset", hb.Offset);

                float startSec = hb.StartTime * segment.TotalDuration;
                float endSec = hb.EndTime * segment.TotalDuration;
                startSec = EditorGUILayout.FloatField("Start (s)", startSec);
                endSec = EditorGUILayout.FloatField("End (s)", Mathf.Max(endSec, startSec + 0.01f));
                hb.StartTime = Mathf.Clamp01(startSec / Mathf.Max(0.0001f, segment.TotalDuration));
                hb.EndTime = Mathf.Clamp01(endSec / Mathf.Max(0.0001f, segment.TotalDuration));

                hb.Size = EditorGUILayout.Vector3Field("Size", hb.Size);
            }
        }

        if (GUILayout.Button("Add HitBox"))
        {
            Undo.RecordObject(_configAsset, "Add HitBox");
            var h = new HitBoxData { StartTime = 0f, EndTime = 0.2f, Offset = new Vector3(0, 1f, 1f), Size = new Vector3(1f, 1f, 2f) };
            _config.Segments[_selectedSegmentIndex].HitBoxes.Add(h);
            EditorUtility.SetDirty(_configAsset);
            AssetDatabase.SaveAssets();
        }
    }

    #endregion

    #region SceneView Gizmos (HitBoxes visualization)

    private void OnSceneGUI(SceneView sceneView)
    {
        if (_previewCharacter == null || _config == null) return;
        if (_selectedSegmentIndex >= _config.Segments.Count) return;

        var segment = _config.Segments[_selectedSegmentIndex];
        for (int i = 0; i < segment.HitBoxes.Count; i++)
        {
            var hitBox = segment.HitBoxes[i];
            float startSec = hitBox.StartTime * segment.TotalDuration;
            float endSec = hitBox.EndTime * segment.TotalDuration;
            bool isActive = _previewTime >= startSec && _previewTime <= endSec;

            Color color = isActive ? new Color(1, 0, 0, 0.45f) : new Color(1, 1, 1, 0.12f);
            Handles.color = color;

            Vector3 center = _previewCharacter.transform.TransformPoint(hitBox.Offset);
            Quaternion rot = _previewCharacter.transform.rotation;

            switch (hitBox.ShapeType)
            {
                case HitShapeType.Box:
                    Matrix4x4 mat = Matrix4x4.TRS(center, rot, hitBox.Size);
                    using (new Handles.DrawingScope(mat))
                    {
                        Handles.DrawWireCube(Vector3.zero, Vector3.one);
                        if (isActive) Handles.CubeHandleCap(0, Vector3.zero, Quaternion.identity, 1f, EventType.Repaint);
                    }
                    break;
                case HitShapeType.Sphere:
                    Handles.DrawWireDisc(center, Vector3.up, hitBox.Size.x);
                    if (isActive) Handles.SphereHandleCap(0, center, rot, hitBox.Size.x * 2f, EventType.Repaint);
                    break;
                case HitShapeType.Fan:
                    Vector3 forward = _previewCharacter.transform.forward;
                    Handles.DrawWireArc(center, Vector3.up, Quaternion.Euler(0, -hitBox.Size.y / 2f, 0) * forward, hitBox.Size.y, hitBox.Size.x);
                    if (isActive) Handles.DrawSolidArc(center, Vector3.up, Quaternion.Euler(0, -hitBox.Size.y / 2f, 0) * forward, hitBox.Size.y, hitBox.Size.x);
                    break;
            }
        }

        sceneView.Repaint();
    }

    #endregion

    #region Utilities
    
    private void HandleKeyboardShortcuts()
    {
        Event e = Event.current;

        // 只在窗口有焦点且有选中元素时处理Delete键
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete)
        {
            // 检查是否有选中的元素
            bool hasSelection = _selectedAnimationIndex >= 0 ||
                               _selectedHitBoxIndex >= 0 ||
                               _selectedVFXIndex >= 0 ||
                               _selectedSFXIndex >= 0;
            
            if (hasSelection && _config != null && _config.Segments.Count > _selectedSegmentIndex)
            {
                var segment = _config.Segments[_selectedSegmentIndex];
                bool deleted = false;

                // 删除选中的Animation Clip (清除当前segment的动画)
                if (_selectedAnimationIndex == _selectedSegmentIndex)
                {
                    Undo.RecordObject(_configAsset, "Clear Animation Clip");
                    segment.AnimationClipTrans.Clip = null;
                    segment.StartTime = 0f;
                    _selectedAnimationIndex = -1;
                    deleted = true;
                }
                // 删除选中的HitBox
                else if (_selectedHitBoxIndex >= 0 && _selectedHitBoxIndex < segment.HitBoxes.Count)
                {
                    Undo.RecordObject(_configAsset, "Delete HitBox");
                    segment.HitBoxes.RemoveAt(_selectedHitBoxIndex);
                    _selectedHitBoxIndex = -1;
                    deleted = true;
                }
                // 删除选中的VFX
                else if (_selectedVFXIndex >= 0 && _selectedVFXIndex < segment.VisualEffects.Count)
                {
                    Undo.RecordObject(_configAsset, "Delete VFX");
                    segment.VisualEffects.RemoveAt(_selectedVFXIndex);
                    _selectedVFXIndex = -1;
                    deleted = true;
                }
                // 删除选中的SFX
                else if (_selectedSFXIndex >= 0 && _selectedSFXIndex < segment.SoundEffects.Count)
                {
                    Undo.RecordObject(_configAsset, "Delete SFX");
                    segment.SoundEffects.RemoveAt(_selectedSFXIndex);
                    _selectedSFXIndex = -1;
                    deleted = true;
                }

                if (deleted)
                {
                    EditorUtility.SetDirty(_configAsset);
                    AssetDatabase.SaveAssets();
                    SampleAnimation(_previewTime); // 更新预览
                    e.Use(); // 阻止事件进一步传播
                    Repaint();
                }
            }
        }
    }

    private void RegisterUndoAndSave(string actionName)
    {
        if (_configAsset == null) return;
        Undo.RecordObject(_configAsset, actionName);
        EditorUtility.SetDirty(_configAsset);
        AssetDatabase.SaveAssets();
    }

    #endregion

    #region Drag and Drop Support

    private void HandleDragDrop(Rect trackRect, AttackSegmentData segment, float pps, TrackType trackType)
    {
        Event e = Event.current;

        // 处理拖拽更新
        if (e.type == EventType.DragUpdated && trackRect.Contains(e.mousePosition))
        {
            // 检查拖拽的对象
            bool canDrop = false;
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (trackType == TrackType.Animation && obj is AnimationClip)
                {
                    canDrop = true;
                    break;
                }
                else if (trackType == TrackType.SFX && obj is AudioClip)
                {
                    canDrop = true;
                    break;
                }
                else if (trackType == TrackType.VFX && obj is GameObject)
                {
                    canDrop = true;
                    break;
                }
            }

            if (canDrop)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                e.Use();
            }
        }
        // 处理拖拽执行
        else if (e.type == EventType.DragPerform && trackRect.Contains(e.mousePosition))
        {
            bool dropped = false;
            float dropTime = (e.mousePosition.x - trackRect.x) / pps; // 将鼠标位置转换为时间
            dropTime = Mathf.Max(0f, dropTime); // 确保时间不小于0

            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (trackType == TrackType.Animation && obj is AnimationClip animationClip)
                {
                    // 创建新的AttackSegmentData作为动画片段
                    Undo.RecordObject(_configAsset, "Add Animation Segment via Drag & Drop");

                    var newSegment = new AttackSegmentData
                    {
                        Id = _config.Segments.Count,
                        Name = animationClip.name,
                        AnimationClipTrans = new ClipTransition() { Speed = 1f, Clip = animationClip, FadeDuration = 0f },
                        StartTime = dropTime
                    };

                    _config.Segments.Add(newSegment);
                    _selectedSegmentIndex = _config.Segments.Count - 1;
                    _selectedAnimationIndex = _selectedSegmentIndex;

                    dropped = true;
                }
                else if (trackType == TrackType.SFX && obj is AudioClip audioClip)
                {
                    // 创建新的SoundEffectData
                    Undo.RecordObject(_configAsset, "Add Sound Effect via Drag & Drop");

                    var newSFX = new SoundEffectData
                    {
                        Name = audioClip.name,
                        Clip = audioClip,
                        TriggerTime = Mathf.Clamp01(dropTime / Mathf.Max(0.001f, segment.TotalDuration)), // 标准化到0-1范围
                        Volume = 1f
                    };

                    segment.SoundEffects.Add(newSFX);
                    dropped = true;
                }
                else if (trackType == TrackType.VFX && obj is GameObject vfxPrefab)
                {
                    // 创建新的VisualEffectData
                    Undo.RecordObject(_configAsset, "Add Visual Effect via Drag & Drop");

                    var newVFX = new VisualEffectData
                    {
                        Name = vfxPrefab.name,
                        Prefab = vfxPrefab,
                        TriggerTime = Mathf.Clamp01(dropTime / Mathf.Max(0.001f, segment.TotalDuration)), // 标准化到0-1范围
                        Offset = Vector3.zero,
                        FollowTarget = false,
                        Duration = 2f
                    };

                    segment.VisualEffects.Add(newVFX);
                    dropped = true;
                }
            }

            if (dropped)
            {
                DragAndDrop.AcceptDrag();
                EditorUtility.SetDirty(_configAsset);
                AssetDatabase.SaveAssets();
                SampleAnimation(_previewTime);
                Repaint();
                e.Use();
            }
        }
    }

    #endregion

    #region Selection Management

    private void SetSelectedAnimation(int animationIndex)
    {
        _selectedAnimationIndex = animationIndex;
        _selectedSegmentIndex = animationIndex;
        _selectedHitBoxIndex = -1;
        _selectedVFXIndex = -1;
        _selectedSFXIndex = -1;
    }

    private void SetSelectedHitBox(int hitBoxIndex)
    {
        _selectedAnimationIndex = -1;
        _selectedHitBoxIndex = hitBoxIndex;
        _selectedVFXIndex = -1;
        _selectedSFXIndex = -1;
    }

    private void SetSelectedVFX(int segmentIndex, int vfxIndex)
    {
        _selectedSegmentIndex = segmentIndex;
        _selectedAnimationIndex = segmentIndex;
        _selectedHitBoxIndex = -1;
        _selectedVFXIndex = vfxIndex;
        _selectedSFXIndex = -1;
    }

    private void SetSelectedSFX(int segmentIndex, int sfxIndex)
    {
        _selectedSegmentIndex = segmentIndex;
        _selectedAnimationIndex = segmentIndex;
        _selectedHitBoxIndex = -1;
        _selectedVFXIndex = -1;
        _selectedSFXIndex = sfxIndex;
    }

    private void ClearAllSelection()
    {
        _selectedAnimationIndex = -1;
        _selectedHitBoxIndex = -1;
        _selectedVFXIndex = -1;
        _selectedSFXIndex = -1;
    }

    #endregion

    #region Unified Track Interactions

    private void HandleAllTrackInteractions(Rect animRect, Rect hitRect, Rect vfxRect, Rect sfxRect,
        AttackSegmentData segment, float pps, float totalDuration)
    {
        Event e = Event.current;

        // 按从上到下的顺序检查轨道 - 确保只被一个处理器处理
        if (HandleTimelineInteraction(animRect, segment, pps)) return;
        if (HandleHitBoxTrackInteraction(hitRect, pps, e)) return;
        if (HandleVFXInteraction(vfxRect, pps)) return;
        if (HandleSFXInteraction(sfxRect, pps)) return;
    }

    #endregion

    #region VFX Track Interaction

    private bool HandleVFXInteraction(Rect trackRect, float pps)
    {
        Event e = Event.current;
        if (!trackRect.Contains(e.mousePosition)) return false;

        // MouseDown: check left/right handles, body for current segment's VFX
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            bool handled = false;
            var segment = _config.Segments[_selectedSegmentIndex];
            var vfxList = segment.VisualEffects;

            for (int i = 0; i < vfxList.Count && !handled; i++)
            {
                var vfx = vfxList[i];
                float startSec = vfx.TriggerTime * segment.TotalDuration;
                float duration = vfx.Duration;
                float px = startSec * pps;
                float pw = Mathf.Max(20f, duration * pps);
                Rect r = new Rect(trackRect.x + px, trackRect.y + 4, pw, 32);
                Rect leftHandle = new Rect(r.xMin, r.yMin, 6, r.height);
                Rect rightHandle = new Rect(r.xMax - 6, r.yMin, 6, r.height);

                if (leftHandle.Contains(e.mousePosition))
                {
                    BeginVFXInteraction(_selectedSegmentIndex, i, VFXDragMode.ResizeLeft, e, trackRect, pps);
                    handled = true;
                }
                else if (rightHandle.Contains(e.mousePosition))
                {
                    BeginVFXInteraction(_selectedSegmentIndex, i, VFXDragMode.ResizeRight, e, trackRect, pps);
                    handled = true;
                }
                else if (r.Contains(e.mousePosition))
                {
                    BeginVFXInteraction(_selectedSegmentIndex, i, VFXDragMode.Move, e, trackRect, pps);
                    handled = true;
                }
            }

            // 如果没有处理拖拽，但鼠标在轨道内，清空选中状态
            if (!handled && trackRect.Contains(e.mousePosition))
            {
                ClearAllSelection();
                Repaint();
            }

            if (handled)
            {
                e.Use();
                return true;
            }
        }

        // MouseDrag: use stored dragStartMouseTime
        if (e.type == EventType.MouseDrag && _draggingVFXIndex >= 0)
        {
            var draggingSegment = _config.Segments[_selectedSegmentIndex];
            if (_draggingVFXIndex < draggingSegment.VisualEffects.Count)
            {
                var vfx = draggingSegment.VisualEffects[_draggingVFXIndex];
                float mouseTimeNow = (e.mousePosition.x + _timelineScroll.x - trackRect.x) / pps;
                float deltaSeconds = mouseTimeNow - _dragStartMouseTime;

                if (_vfxDragMode == VFXDragMode.Move)
                {
                    // 允许VFX自由移动，不受segment时长限制
                    float newTriggerTime = Mathf.Max(0f, _vfxTriggerTimeCache + deltaSeconds);
                    vfx.TriggerTime = Mathf.Clamp01(newTriggerTime / Mathf.Max(0.001f, draggingSegment.TotalDuration));
                }
                else if (_vfxDragMode == VFXDragMode.ResizeLeft)
                {
                    float newStartTime = Mathf.Max(0f, _vfxTriggerTimeCache + deltaSeconds);
                    float newDuration = _vfxDurationCache + (_vfxTriggerTimeCache - newStartTime);
                    newDuration = Mathf.Max(0.1f, newDuration);
                    vfx.TriggerTime = Mathf.Clamp01(newStartTime / Mathf.Max(0.001f, draggingSegment.TotalDuration));
                    vfx.Duration = newDuration;
                }
                else if (_vfxDragMode == VFXDragMode.ResizeRight)
                {
                    float newDuration = Mathf.Max(0.1f, _vfxDurationCache + deltaSeconds);
                    vfx.Duration = newDuration;
                }

                EditorUtility.SetDirty(_configAsset);
                Repaint();
                e.Use();
                return true;
            }
        }

        // MouseUp finalize
        if (e.type == EventType.MouseUp)
        {
            if (_draggingVFXIndex != -1)
            {
                _draggingVFXIndex = -1;
                _vfxDragMode = VFXDragMode.None;
                _undoRecorded = false;
                EditorUtility.SetDirty(_configAsset);
                AssetDatabase.SaveAssets();
                e.Use();
                return true;
            }
        }

        return false;
    }

    private void BeginVFXInteraction(int segmentIndex, int vfxIndex, VFXDragMode mode, Event e, Rect trackRect, float pps)
    {
        if (!_undoRecorded)
        {
            Undo.RecordObject(_configAsset, "Modify VFX");
            _undoRecorded = true;
        }

        // 设置选中状态
        SetSelectedVFX(segmentIndex, vfxIndex);
        Repaint(); // 刷新界面显示选中状态

        var segment = _config.Segments[segmentIndex];
        _draggingVFXIndex = vfxIndex; // 直接使用vfx索引，因为现在只处理当前segment
        _vfxDragMode = mode;
        _dragStartMouseX = e.mousePosition.x;
        _dragStartMouseTime = (e.mousePosition.x + _timelineScroll.x - trackRect.x) / pps;

        var vfx = segment.VisualEffects[vfxIndex];
        _vfxTriggerTimeCache = vfx.TriggerTime * segment.TotalDuration;
        _vfxDurationCache = vfx.Duration;
    }

    #endregion

    #region SFX Track Interaction

    private bool HandleSFXInteraction(Rect trackRect, float pps)
    {
        Event e = Event.current;
        if (!trackRect.Contains(e.mousePosition)) return false;

        // MouseDown: check for move or selection in current segment
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            bool handled = false;
            var segment = _config.Segments[_selectedSegmentIndex];
            var sfxList = segment.SoundEffects;

            for (int i = 0; i < sfxList.Count && !handled; i++)
            {
                var sfx = sfxList[i];
                float startSec = sfx.TriggerTime * segment.TotalDuration;
                float duration = sfx.Clip != null ? sfx.Clip.length : 1f;
                float px = startSec * pps;
                float pw = Mathf.Max(20f, duration * pps);
                Rect r = new Rect(trackRect.x + px, trackRect.y + 4, pw, 32);

                if (r.Contains(e.mousePosition))
                {
                    BeginSFXInteraction(_selectedSegmentIndex, i, e, trackRect, pps);
                    handled = true;
                }
            }

            // 如果没有处理拖拽，但鼠标在轨道内，清空选中状态
            if (!handled && trackRect.Contains(e.mousePosition))
            {
                ClearAllSelection();
                Repaint();
            }

            if (handled)
            {
                e.Use();
                return true;
            }
        }

        // MouseDrag: move only
        if (e.type == EventType.MouseDrag && _draggingSFXIndex >= 0)
        {
            var draggingSegment = _config.Segments[_selectedSegmentIndex];
            if (_draggingSFXIndex < draggingSegment.SoundEffects.Count)
            {
                var sfx = draggingSegment.SoundEffects[_draggingSFXIndex];
                float mouseTimeNow = (e.mousePosition.x + _timelineScroll.x - trackRect.x) / pps;
                float deltaSeconds = mouseTimeNow - _dragStartMouseTime;

                float newTriggerTime = Mathf.Max(0f, _sfxTriggerTimeCache + deltaSeconds);
                sfx.TriggerTime = Mathf.Clamp01(newTriggerTime / Mathf.Max(0.001f, draggingSegment.TotalDuration));

                EditorUtility.SetDirty(_configAsset);
                Repaint();
                e.Use();
                return true;
            }
        }

        // MouseUp finalize
        if (e.type == EventType.MouseUp)
        {
            if (_draggingSFXIndex != -1)
            {
                _draggingSFXIndex = -1;
                _undoRecorded = false;
                EditorUtility.SetDirty(_configAsset);
                AssetDatabase.SaveAssets();
                e.Use();
                return true;
            }
        }

        return false;
    }

    private void BeginSFXInteraction(int segmentIndex, int sfxIndex, Event e, Rect trackRect, float pps)
    {
        if (!_undoRecorded)
        {
            Undo.RecordObject(_configAsset, "Move SFX");
            _undoRecorded = true;
        }

        // 设置选中状态
        SetSelectedSFX(segmentIndex, sfxIndex);
        Repaint(); // 刷新界面显示选中状态

        var segment = _config.Segments[segmentIndex];
        _draggingSFXIndex = sfxIndex; // 直接使用sfx索引，因为现在只处理当前segment
        _dragStartMouseX = e.mousePosition.x;
        _dragStartMouseTime = (e.mousePosition.x + _timelineScroll.x - trackRect.x) / pps;

        var sfx = segment.SoundEffects[sfxIndex];
        _sfxTriggerTimeCache = sfx.TriggerTime * segment.TotalDuration;
    }

    #endregion
}
#endif

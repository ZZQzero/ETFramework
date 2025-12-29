/*
// Unity_SkillEditor.cs
// Place this file under Assets/Editor/ (or any Editor folder) in your Unity project.
// Requires your existing ET namespace classes (AttackConfig, AttackSegmentData, HitBoxData, AttackMovementData, etc.)
// The editor provides: create/load AttackConfig, edit segments, timeline view for HitBoxes/Movement/Cancel/Buffer, animation preview (using AnimationMode sampling), and scene Gizmo preview for HitBoxes.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ET
{
    public class SkillEditorWindow : EditorWindow
    {
        private AttackConfig _config;
        private AttackConfigAsset  _configAsset;
        private SerializedObject _configSO;

        private int _selectedSegmentIndex = -1;
        private Vector2 _leftPaneScroll;
        private Vector2 _rightPaneScroll;

        // Preview
        private GameObject _previewPrefab;
        private GameObject _previewInstance;
        private float _previewTime = 0f; // seconds
        private bool _isPlaying = false;
        private float _previewSpeed = 1f;

        // Timeline playback helpers
        private float _segmentDuration = 1f; // seconds; derived from clip length when available

        // Dragging timeline
        private bool _isDraggingTimeline = false;

        // Editor colors
        private Color _hitboxColor = new Color(1f, 0.4f, 0.2f, 0.5f);
        private Color _movementColor = new Color(0.2f, 0.6f, 1f, 0.4f);
        private Color _cancelColor = new Color(0.4f, 1f, 0.4f, 0.2f);
        private Color _bufferColor = new Color(1f, 1f, 0.2f, 0.2f);

        [MenuItem("Tools/ET/Skill Editor")]
        public static void OpenWindow()
        {
            var w = GetWindow<SkillEditorWindow>("Skill Editor");
            w.minSize = new Vector2(1000, 600);
            w.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += EditorUpdate;
            EditorSceneManager.sceneClosing += OnSceneClosing;
        }

        private void OnDisable()
        {
            EditorApplication.update -= EditorUpdate;
            EditorSceneManager.sceneClosing -= OnSceneClosing;
            StopPreview();
            DestroyPreviewInstanceImmediate();
        }

        private void OnSceneClosing(UnityEngine.SceneManagement.Scene scene, bool removingScene)
        {
            // ensure preview instance not left in scene
            DestroyPreviewInstanceImmediate();
        }
        
        private void EditorUpdate()
        {
            if (_isPlaying && _selectedSegmentIndex >= 0 && _config != null)
            {
                var seg = _config.GetSegmentByIndex(_selectedSegmentIndex);
                if (seg != null && TryGetAnimationClip(seg, out var clip))
                {
                    _segmentDuration = Mathf.Max(0.0001f, clip.length);
                    _previewTime += (float)(EditorApplication.timeSinceStartup - _lastFrameTime) * _previewSpeed;
                    if (_previewTime > _segmentDuration) _previewTime %= _segmentDuration;
                    SamplePreview(_previewTime);
                    Repaint();
                }
            }
            _lastFrameTime = (float)EditorApplication.timeSinceStartup;
        }

        private float _lastFrameTime;

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            DrawLeftPane();
            DrawRightPane();
            EditorGUILayout.EndHorizontal();
        }

        #region Left Pane - Config & Segment List
        private void DrawLeftPane()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.28f));

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("New AttackConfig", GUILayout.Height(28)))
            {
                CreateNewConfig();
            }
            if (GUILayout.Button("Load AttackConfig", GUILayout.Height(28)))
            {
                LoadConfig();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (_config == null)
            {
                EditorGUILayout.HelpBox("No AttackConfig loaded. Create or load one.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // Show basic config fields
            EditorGUILayout.LabelField("Config: " + _config.ConfigName, EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _leftPaneScroll = EditorGUILayout.BeginScrollView(_leftPaneScroll);

            EditorGUILayout.LabelField("Segments", EditorStyles.boldLabel);

            for (int i = 0; i < _config.Segments.Count; i++)
            {
                var seg = _config.Segments[i];
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Toggle(_selectedSegmentIndex == i, seg.Name, "Button"))
                {
                    if (_selectedSegmentIndex != i)
                    {
                        _selectedSegmentIndex = i;
                        EnsurePreviewInstanceForSelectedSegment();
                    }
                }
                if (GUILayout.Button("X", GUILayout.Width(24)))
                {
                    if (EditorUtility.DisplayDialog("Remove Segment", "Remove segment '" + seg.Name + "'?", "Yes", "No"))
                    {
                        _config.Segments.RemoveAt(i);
                        MarkConfigDirty();
                        if (_selectedSegmentIndex == i) _selectedSegmentIndex = -1;
                        break;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Segment"))
            {
                var seg = new AttackSegmentData();
                seg.Id = GenerateSegmentId();
                seg.Name = "New Segment " + seg.Id;
                _config.Segments.Add(seg);
                _selectedSegmentIndex = _config.Segments.Count - 1;
                MarkConfigDirty();
                EnsurePreviewInstanceForSelectedSegment();
            }

            if (GUILayout.Button("Duplicate Segment") && _selectedSegmentIndex >= 0)
            {
                /*var src = _config.GetSegmentByIndex(_selectedSegmentIndex);
                var copy = UnityEngine.Object.Instantiate(src);
                // deep copy basic lists
                copy.HitBoxes = new List<HitBoxData>(src.HitBoxes);
                copy.CancelableSkillIds = new List<int>(src.CancelableSkillIds);
                copy.ComboBranches = new Dictionary<ComboInputType, int>(src.ComboBranches);
                copy.Id = GenerateSegmentId();
                copy.Name = src.Name + " (Copy)";
                _config.Segments.Add(copy);
                MarkConfigDirty();#1#
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (GUILayout.Button("Save Config to Asset"))
            {
                SaveConfigAsset();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private int GenerateSegmentId()
        {
            int id = 1;
            foreach (var s in _config.Segments)
            {
                if (s.Id >= id) id = s.Id + 1;
            }
            return id;
        }
        #endregion

        #region Right Pane - Preview + Timeline + Inspector
        private void DrawRightPane()
        {
            EditorGUILayout.BeginVertical();

            if (_config == null)
            {
                EditorGUILayout.HelpBox("Load or create AttackConfig to edit.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            DrawPreviewArea();
            DrawInspectorArea();
            EditorGUILayout.EndHorizontal();

            DrawTimelineArea();

            EditorGUILayout.EndVertical();
        }

        private void DrawPreviewArea()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.45f));

            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            _previewPrefab = (GameObject)EditorGUILayout.ObjectField("Preview Prefab", _previewPrefab, typeof(GameObject), false);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(_isPlaying ? "Pause" : "Play", GUILayout.Width(80)))
            {
                _isPlaying = !_isPlaying;
                if (_isPlaying) StartPreview(); else StopPreview();
            }
            if (GUILayout.Button("Stop", GUILayout.Width(80)))
            {
                _isPlaying = false;
                _previewTime = 0f;
                StopPreview();
                SamplePreview(0f);
            }
            GUILayout.Label("Speed");
            _previewSpeed = EditorGUILayout.FloatField(_previewSpeed, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);

            if (_selectedSegmentIndex < 0 || _selectedSegmentIndex >= _config.Segments.Count)
            {
                EditorGUILayout.HelpBox("Select a segment to preview its animation and timeline.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            var seg = _config.GetSegmentByIndex(_selectedSegmentIndex);
            if (seg == null)
            {
                EditorGUILayout.HelpBox("Segment missing.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            // Animation clip field
            var prevClip = TryGetAnimationClip(seg, out var clip) ? clip : null;
            var newClip = (AnimationClip)EditorGUILayout.ObjectField("AnimationClip", prevClip, typeof(AnimationClip), false);
            if (newClip != prevClip)
            {
                // update path: since AttackSegmentData stores path string, but editor can set clip directly by assigning path or leave to user to set later.
                // We'll try to set animation via AssetDatabase path
                if (newClip == null)
                {
                    seg.AnimationPath = string.Empty;
                }
                else
                {
                    seg.AnimationPath = AssetDatabase.GetAssetPath(newClip);
                    MarkConfigDirty();
                }
                EnsurePreviewInstanceForSelectedSegment();
            }

            // segment duration derived
            if (TryGetAnimationClip(seg, out clip))
            {
                _segmentDuration = Mathf.Max(0.0001f, clip.length);
                GUILayout.Label("Clip length: " + clip.length.ToString("F3") + "s");
            }
            else
            {
                GUILayout.Label("Clip not assigned or not found at path.");
            }

            // timeline scrub
            EditorGUILayout.BeginHorizontal();
            float normalized = _segmentDuration > 0f ? Mathf.Clamp01(_segmentDuration == 0 ? 0 : _previewTime / _segmentDuration) : 0f;
            EditorGUI.BeginChangeCheck();
            normalized = EditorGUILayout.Slider(normalized, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                _previewTime = normalized * _segmentDuration;
                SamplePreview(_previewTime);
            }
            EditorGUILayout.LabelField(normalized.ToString("P1"), GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            // Scene draw toggle
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Focus Preview Instance"))
            {
                if (_previewInstance != null)
                {
                    Selection.activeGameObject = _previewInstance;
                }
            }
            if (GUILayout.Button("Recreate Preview Instance"))
            {
                EnsurePreviewInstanceForSelectedSegment(true);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawInspectorArea()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.27f));
            EditorGUILayout.LabelField("Segment Inspector", EditorStyles.boldLabel);

            if (_selectedSegmentIndex >= 0 && _selectedSegmentIndex < _config.Segments.Count)
            {
                _rightPaneScroll = EditorGUILayout.BeginScrollView(_rightPaneScroll);
                var seg = _config.GetSegmentByIndex(_selectedSegmentIndex);
                EditorGUI.BeginChangeCheck();

                seg.Name = EditorGUILayout.TextField("Name", seg.Name);
                seg.FadeDuration = EditorGUILayout.FloatField("Fade Duration", seg.FadeDuration);
                seg.AnimationSpeed = EditorGUILayout.FloatField("Animation Speed", seg.AnimationSpeed);
                seg.DamageMultiplier = EditorGUILayout.FloatField("Damage Multiplier", seg.DamageMultiplier);
                seg.HitReaction = (HitReactionType)EditorGUILayout.EnumPopup("HitReaction", seg.HitReaction);
                seg.KnockbackForce = EditorGUILayout.FloatField("KnockbackForce", seg.KnockbackForce);
                seg.KnockupForce = EditorGUILayout.FloatField("KnockupForce", seg.KnockupForce);
                seg.HitStunMs = EditorGUILayout.IntField("HitStunMs", seg.HitStunMs);
                seg.InputBufferStartTime = EditorGUILayout.Slider("InputBufferStart", seg.InputBufferStartTime, 0f, 1f);
                seg.CancelableTime = EditorGUILayout.Slider("CancelableTime", seg.CancelableTime, 0f, 1f);
                seg.EndTime = EditorGUILayout.Slider("EndTime", seg.EndTime, 0f, 1f);
                seg.TargetState = (TargetStateType)EditorGUILayout.EnumPopup("TargetState", seg.TargetState);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Combo Branches", EditorStyles.boldLabel);
                var keys = new List<ComboInputType>(seg.ComboBranches.Keys);
                foreach (var k in keys)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(k.ToString(), GUILayout.Width(80));
                    int nextId = seg.ComboBranches[k];
                    // show dropdown of segments
                    int selIdx = IndexOfSegmentId(nextId);
                    int newSel = EditorGUILayout.Popup(selIdx, GetSegmentNameArray());
                    if (newSel != selIdx)
                    {
                        int newid = newSel >= 0 ? _config.Segments[newSel].Id : -1;
                        seg.ComboBranches[k] = newid;
                        MarkConfigDirty();
                    }
                    if (GUILayout.Button("-", GUILayout.Width(24)))
                    {
                        seg.ComboBranches.Remove(k);
                        MarkConfigDirty();
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                if (GUILayout.Button("Add Combo Branch"))
                {
                    seg.ComboBranches[ComboInputType.Normal] = -1;
                    MarkConfigDirty();
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("HitBoxes", EditorStyles.boldLabel);
                for (int i = 0; i < seg.HitBoxes.Count; i++)
                {
                    var hb = seg.HitBoxes[i];
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.BeginHorizontal();
                    hb.ShapeType = (HitShapeType)EditorGUILayout.EnumPopup(hb.ShapeType, GUILayout.Width(100));
                    hb.StartTime = EditorGUILayout.Slider("Start", hb.StartTime, 0f, 1f);
                    hb.EndTime = EditorGUILayout.Slider("End", hb.EndTime, 0f, 1f);
                    if (GUILayout.Button("X", GUILayout.Width(24)))
                    {
                        seg.HitBoxes.RemoveAt(i);
                        MarkConfigDirty();
                        break;
                    }
                    EditorGUILayout.EndHorizontal();

                    hb.Offset = EditorGUILayout.Vector3Field("Offset", hb.Offset);
                    hb.Size = EditorGUILayout.Vector3Field("Size", hb.Size);
                    EditorGUILayout.EndVertical();
                }
                if (GUILayout.Button("Add HitBox"))
                {
                    seg.HitBoxes.Add(new HitBoxData());
                    MarkConfigDirty();
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Movement", EditorStyles.boldLabel);
                seg.Movement.EnableMovement = EditorGUILayout.Toggle("EnableMovement", seg.Movement.EnableMovement);
                if (seg.Movement.EnableMovement)
                {
                    seg.Movement.StartTime = EditorGUILayout.Slider("StartTime", seg.Movement.StartTime, 0f, 1f);
                    seg.Movement.EndTime = EditorGUILayout.Slider("EndTime", seg.Movement.EndTime, 0f, 1f);
                    seg.Movement.Distance = EditorGUILayout.FloatField("Distance", seg.Movement.Distance);
                    seg.Movement.TrackTarget = EditorGUILayout.Toggle("TrackTarget", seg.Movement.TrackTarget);
                    seg.Movement.TrackRange = EditorGUILayout.FloatField("TrackRange", seg.Movement.TrackRange);
                    // show curve editor
                    seg.Movement.MoveCurve = EditorGUILayout.CurveField("MoveCurve", seg.Movement.MoveCurve);
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Effect", EditorStyles.boldLabel);
                seg.Effect.AttackEffectPath = EditorGUILayout.TextField("AttackEffectPath", seg.Effect.AttackEffectPath);
                seg.Effect.HitEffectPath = EditorGUILayout.TextField("HitEffectPath", seg.Effect.HitEffectPath);
                seg.Effect.AttackSoundPath = EditorGUILayout.TextField("AttackSoundPath", seg.Effect.AttackSoundPath);
                seg.Effect.HitSoundPath = EditorGUILayout.TextField("HitSoundPath", seg.Effect.HitSoundPath);
                seg.Effect.ScreenShakeIntensity = EditorGUILayout.FloatField("ScreenShakeIntensity", seg.Effect.ScreenShakeIntensity);
                seg.Effect.ScreenShakeDuration = EditorGUILayout.FloatField("ScreenShakeDuration", seg.Effect.ScreenShakeDuration);
                seg.Effect.HitStopMs = EditorGUILayout.IntField("HitStopMs", seg.Effect.HitStopMs);
                seg.Effect.TimeScale = EditorGUILayout.FloatField("TimeScale", seg.Effect.TimeScale);
                seg.Effect.TimeScaleDurationMs = EditorGUILayout.IntField("TimeScaleDurationMs", seg.Effect.TimeScaleDurationMs);

                if (EditorGUI.EndChangeCheck())
                {
                    MarkConfigDirty();
                }

                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("Select a segment from left panel.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }
        #endregion

        #region Timeline Area
        private void DrawTimelineArea()
        {
            GUILayout.Space(8);
            EditorGUILayout.LabelField("Timeline", EditorStyles.boldLabel);

            if (_selectedSegmentIndex < 0 || _config == null)
            {
                EditorGUILayout.HelpBox("Select a segment to edit timeline.", MessageType.Info);
                return;
            }

            var seg = _config.GetSegmentByIndex(_selectedSegmentIndex);
            Rect timelineRect = GUILayoutUtility.GetRect(position.width * 0.95f, 120);
            EditorGUI.DrawRect(timelineRect, new Color(0.12f, 0.12f, 0.12f));

            // draw time ruler
            int ticks = 10;
            for (int i = 0; i <= ticks; i++)
            {
                float x = Mathf.Lerp(timelineRect.xMin, timelineRect.xMax, (float)i / ticks);
                Handles.color = Color.gray;
                Handles.DrawLine(new Vector3(x, timelineRect.yMin + 16, 0), new Vector3(x, timelineRect.yMax, 0));
                GUI.Label(new Rect(x + 2, timelineRect.yMin + 2, 40, 16), ((float)i / ticks).ToString("F1"));
            }

            // hitboxes
            foreach (var hb in seg.HitBoxes)
            {
                DrawTimelineEventRect(timelineRect, hb.StartTime, hb.EndTime, hb.ShapeType.ToString(), _hitboxColor);
            }

            // movement
            if (seg.Movement.EnableMovement)
            {
                DrawTimelineEventRect(timelineRect, seg.Movement.StartTime, seg.Movement.EndTime, "Move", _movementColor);
            }

            // cancel window -> show a small region from CancelableTime to EndTime
            DrawTimelineEventRect(timelineRect, seg.CancelableTime, seg.EndTime, "Cancelable", _cancelColor);

            // input buffer -> from InputBufferStartTime to EndTime
            DrawTimelineEventRect(timelineRect, seg.InputBufferStartTime, seg.EndTime, "InputBuffer", _bufferColor);

            // draw playhead
            float headX = Mathf.Lerp(timelineRect.xMin, timelineRect.xMax, _segmentDuration > 0 ? _previewTime / _segmentDuration : 0f);
            Handles.color = Color.white;
            Handles.DrawLine(new Vector3(headX, timelineRect.yMin, 0), new Vector3(headX, timelineRect.yMax, 0));

            // timeline interaction
            Event e = Event.current;
            if (e.type == EventType.MouseDown && timelineRect.Contains(e.mousePosition))
            {
                _isDraggingTimeline = true;
                SetPreviewTimeFromMouse(timelineRect, e.mousePosition.x);
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && _isDraggingTimeline)
            {
                SetPreviewTimeFromMouse(timelineRect, e.mousePosition.x);
                e.Use();
            }
            else if (e.type == EventType.MouseUp && _isDraggingTimeline)
            {
                _isDraggingTimeline = false;
                e.Use();
            }

            // context menu: when right click on timeline, show options to add events based on mouse pos
            if (Event.current.type == EventType.ContextClick && timelineRect.Contains(Event.current.mousePosition))
            {
                var menu = new GenericMenu();
                float norm = Mathf.InverseLerp(timelineRect.xMin, timelineRect.xMax, Event.current.mousePosition.x);
                menu.AddItem(new GUIContent("Add HitBox Here"), false, () => {
                    var hb = new HitBoxData { StartTime = Mathf.Clamp01(norm - 0.05f), EndTime = Mathf.Clamp01(norm + 0.05f) };
                    seg.HitBoxes.Add(hb);
                    MarkConfigDirty();
                });
                menu.AddItem(new GUIContent("Add Move Window"), false, () => {
                    seg.Movement.EnableMovement = true;
                    seg.Movement.StartTime = Mathf.Clamp01(norm - 0.05f);
                    seg.Movement.EndTime = Mathf.Clamp01(norm + 0.15f);
                    MarkConfigDirty();
                });
                menu.ShowAsContext();
                Event.current.Use();
            }
        }

        private void DrawTimelineEventRect(Rect timelineRect, float start, float end, string label, Color color)
        {
            float x1 = Mathf.Lerp(timelineRect.xMin, timelineRect.xMax, start);
            float x2 = Mathf.Lerp(timelineRect.xMin, timelineRect.xMax, end);
            Rect r = new Rect(x1, timelineRect.yMin + 24, Mathf.Max(8, x2 - x1), 28);
            EditorGUI.DrawRect(r, color);
            GUI.Label(new Rect(r.x + 4, r.y + 4, r.width - 8, r.height - 8), label);
        }

        private void SetPreviewTimeFromMouse(Rect timelineRect, float mouseX)
        {
            float norm = Mathf.InverseLerp(timelineRect.xMin, timelineRect.xMax, mouseX);
            _previewTime = norm * _segmentDuration;
            SamplePreview(_previewTime);
            Repaint();
        }
        #endregion

        #region Preview Instance & Sampling
        private void EnsurePreviewInstanceForSelectedSegment(bool recreate = false)
        {
            if (_selectedSegmentIndex < 0 || _config == null) return;
            var seg = _config.GetSegmentByIndex(_selectedSegmentIndex);
            if (seg == null) return;

            if (_previewPrefab == null)
            {
                // nothing to instantiate
                DestroyPreviewInstanceImmediate();
                return;
            }

            if (_previewInstance == null || recreate)
            {
                DestroyPreviewInstanceImmediate();
                _previewInstance = (GameObject)PrefabUtility.InstantiatePrefab(_previewPrefab);
                // hide from hierarchy but still editable: set hideFlags
                if (_previewInstance != null)
                {
                    _previewInstance.hideFlags = HideFlags.HideAndDontSave;
                    // reset transform
                    _previewInstance.transform.position = Vector3.zero;
                    _previewInstance.transform.rotation = Quaternion.identity;
                }
            }

            // When clip exists, sample at 0
            _previewTime = 0f;
            SamplePreview(0f);
        }

        private void DestroyPreviewInstanceImmediate()
        {
            if (_previewInstance != null)
            {
                // remove from scene
                try { DestroyImmediate(_previewInstance); } catch { }
                _previewInstance = null;
            }
        }

        private void StartPreview()
        {
            _isPlaying = true;
            _lastFrameTime = (float)EditorApplication.timeSinceStartup;
        }

        private void StopPreview()
        {
            _isPlaying = false;
        }

        private void SamplePreview(float timeSeconds)
        {
            if (_previewInstance == null) return;
            if (_selectedSegmentIndex < 0 || _config == null) return;
            var seg = _config.GetSegmentByIndex(_selectedSegmentIndex);
            if (seg == null) return;
            if (!TryGetAnimationClip(seg, out var clip) || clip == null) return;

            // Use AnimationMode to sample the animation on the preview instance
            try
            {
                if (!AnimationMode.InAnimationMode())
                    AnimationMode.StartAnimationMode();

                AnimationMode.SampleAnimationClip(_previewInstance, clip, timeSeconds);
            }
            catch (Exception)
            {
                // sampling may fail for some clips; ignore
            }

            // Repaint scene view so Gizmos update
            SceneView.RepaintAll();
        }

        private bool TryGetAnimationClip(AttackSegmentData seg, out AnimationClip clip)
        {
            clip = null;
            if (seg == null) return false;
            if (!string.IsNullOrEmpty(seg.AnimationPath))
            {
                var asset = AssetDatabase.LoadAssetAtPath<AnimationClip>(seg.AnimationPath);
                if (asset != null) { clip = asset; return true; }
            }
            return false;
        }
        #endregion

        #region Asset Handling
        private void CreateNewConfig()
        {
            var path = EditorUtility.SaveFilePanelInProject("Create AttackConfig", "AttackConfig", "asset", "Create AttackConfig asset");
            if (string.IsNullOrEmpty(path)) return;
            var ac = ScriptableObject.CreateInstance<AttackConfigAsset>();
            ac.Config.ConfigName = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(ac, path);
            AssetDatabase.SaveAssets();
            LoadConfigAtPath(path);
        }

        private void LoadConfig()
        {
            string path = EditorUtility.OpenFilePanel("Select AttackConfig asset", Application.dataPath, "asset");
            if (string.IsNullOrEmpty(path)) return;
            // convert to project-relative path
            if (path.StartsWith(Application.dataPath))
            {
                string rel = "Assets" + path.Substring(Application.dataPath.Length);
                LoadConfigAtPath(rel);
            }
            else
            {
                EditorUtility.DisplayDialog("Invalid Path", "Please choose an asset inside this project.", "OK");
            }
        }

        private void LoadConfigAtPath(string assetPath)
        {
            var ac = AssetDatabase.LoadAssetAtPath<AttackConfigAsset>(assetPath);
            if (ac == null)
            {
                EditorUtility.DisplayDialog("Error", "Selected asset is not an AttackConfig.", "OK");
                return;
            }

            _configAsset = ac;
            _config = _configAsset.Config;
            _configSO = new SerializedObject(ac);
            _selectedSegmentIndex = _config.Segments.Count > 0 ? 0 : -1;
            EnsurePreviewInstanceForSelectedSegment();
        }

        private void SaveConfigAsset()
        {
            if (_config == null) return;
            EditorUtility.SetDirty(_configAsset);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Saved", "AttackConfig saved.", "OK");
        }

        private void MarkConfigDirty()
        {
            if (_config == null) return;
            EditorUtility.SetDirty(_configAsset);
        }
        #endregion

        #region Utilities
        private string[] GetSegmentNameArray()
        {
            if (_config == null) return new string[0];
            var arr = new string[_config.Segments.Count];
            for (int i = 0; i < _config.Segments.Count; i++) arr[i] = _config.Segments[i].Name;
            return arr;
        }

        private int IndexOfSegmentId(int id)
        {
            if (_config == null) return -1;
            for (int i = 0; i < _config.Segments.Count; i++) if (_config.Segments[i].Id == id) return i;
            return -1;
        }
        #endregion

        #region Scene Gizmos for Preview Instance
        // Draw handles for the preview instance using SceneView.onSceneGUIDelegate
        [InitializeOnLoadMethod]
        private static void InitSceneDraw()
        {
            SceneView.duringSceneGui -= OnSceneGUIStatic;
            SceneView.duringSceneGui += OnSceneGUIStatic;
        }

        private static void OnSceneGUIStatic(SceneView sv)
        {
            var window = GetWindow<SkillEditorWindow>(false);
            if (window != null && window._previewInstance != null && window._selectedSegmentIndex >= 0 && window._config != null)
            {
                window.OnSceneGUI(sv);
            }
        }

        private void OnSceneGUI(SceneView sv)
        {
            if (_previewInstance == null) return;
            var seg = _config.GetSegmentByIndex(_selectedSegmentIndex);
            if (seg == null) return;

            // Draw hitboxes when active
            float normalized = _segmentDuration > 0 ? Mathf.Clamp01(_previewTime / _segmentDuration) : 0f;
            foreach (var hb in seg.HitBoxes)
            {
                if (normalized >= hb.StartTime && normalized <= hb.EndTime)
                {
                    DrawHitBoxGizmo(_previewInstance.transform, hb);
                }
            }

            // Draw movement preview when active
            if (seg.Movement.EnableMovement && normalized >= seg.Movement.StartTime && normalized <= seg.Movement.EndTime)
            {
                DrawMovementGizmo(_previewInstance.transform, seg.Movement);
            }
        }

        private void DrawHitBoxGizmo(Transform root, HitBoxData hb)
        {
            Vector3 center = root.TransformPoint(hb.Offset);
            switch (hb.ShapeType)
            {
                case HitShapeType.Box:
                    Vector3 size = hb.Size;
                    Matrix4x4 tm = Matrix4x4.TRS(center, root.rotation, Vector3.one);
                    Handles.matrix = tm;
                    Handles.color = _hitboxColor;
                    Handles.DrawWireCube(Vector3.zero, size);
                    Handles.matrix = Matrix4x4.identity;
                    break;
                case HitShapeType.Sphere:
                    Handles.color = _hitboxColor;
                    Handles.DrawWireDisc(center, Vector3.up, hb.Size.x);
                    break;
                case HitShapeType.Fan:
                    Handles.color = _hitboxColor;
                    // draw simple arc in forward direction
                    Vector3 dir = root.forward;
                    float radius = hb.Size.x;
                    float angle = hb.Size.y;
                    Handles.DrawWireArc(center, Vector3.up, Quaternion.Euler(0, -angle/2, 0) * dir, angle, radius);
                    break;
                case HitShapeType.Capsule:
                    Handles.color = _hitboxColor;
                    // approximate as two spheres + cylinder
                    Vector3 a = center + Vector3.up * (hb.Size.y * 0.5f);
                    Vector3 b = center - Vector3.up * (hb.Size.y * 0.5f);
                    Handles.DrawWireDisc(a, Vector3.up, hb.Size.x);
                    Handles.DrawWireDisc(b, Vector3.up, hb.Size.x);
                    break;
            }
        }

        private void DrawMovementGizmo(Transform root, AttackMovementData mv)
        {
            Vector3 start = root.position;
            Vector3 forward = root.forward;
            Vector3 target = start + forward * mv.Distance;
            Handles.color = _movementColor;
            Handles.DrawLine(start, target);
            Handles.SphereHandleCap(0, target, Quaternion.identity, 0.08f, EventType.Repaint);
        }
        #endregion
    }
}
*/

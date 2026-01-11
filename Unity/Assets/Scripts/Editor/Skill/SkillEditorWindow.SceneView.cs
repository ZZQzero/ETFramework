using System;
using ET;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public partial class SkillEditorWindow : EditorWindow
{
    private readonly BoxBoundsHandle hitBoxBoundsHandle = new BoxBoundsHandle();
    private static readonly Color HitBoxActiveColor = new Color(0.2f, 1f, 0.2f, 0.9f);
    private static readonly Color HitBoxInactiveColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        EditorApplication.update -= OnEditorUpdate;

        // 窗口关闭/失活时：停止预览，避免残留姿态/RootMotion 状态
        StopPreviewPlayback(resetTime: false, sampleAfterStop: false);
        this.DestroyPreviewObject();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (config == null || animancer == null)
        {
            return;
        }

        // animancer.transform 就是 player 的 Transform
        Transform player = animancer.transform;
        if (player == null)
        {
            return;
        }

        // 绘制位移轨迹（在所有其他绘制之前，作为背景显示）
        // 编辑模式：只显示当前选中段的位移轨迹；播放模式：显示当前播放段的位移轨迹
        DrawMovementTrajectoryForCurrentContext(player);

        // 播放模式：绘制当前时间点“激活窗口内”的所有 HitBox（只显示，不允许编辑）
        if (isPlaying)
        {
            DrawPreviewHitBoxesAtCurrentTime();
            DrawPreviewMovementProgress(player);
            return;
        }

        // 拖拽 playhead（scrub）时：仍然绘制 HitBox 预览；同时允许对“当前选中的特效实例”做 Offset/Rotation 调整并写回配置。
        if (isDraggingPlayhead)
        {
            DrawPreviewHitBoxesAtCurrentTime();
            DrawPreviewMovementProgress(player);
        }

        // 预览特效编辑：在场景里选中预览特效实例并用 Unity 默认 gizmo 改 Transform 时，
        // 检测变化并写回到 VisualEffectData（Offset/RotationEuler），同时回显右侧面板。
        TrySyncSelectedPreviewVfxTransformFromSceneSelection(sceneView);

        // 编辑模式：仅在选中了 HitBoxClip 时显示/编辑
        if (selectedClip is not HitBoxClipItem hitBoxClip || hitBoxClip.HitBoxData == null)
        {
            return;
        }

        // HitBox 的编辑仍然只在“非 scrub”时开启（避免拖动时间轴时误操作）
        if (isDraggingPlayhead)
        {
            return;
        }

        var hitBox = hitBoxClip.HitBoxData;

        // 使用统一的角色 Transform（已在方法开头获取）

        // world pose
        Vector3 worldCenter = player.TransformPoint(hitBox.Offset);
        Quaternion worldRot = player.rotation * Quaternion.Euler(hitBox.RotationEuler);
        Vector3 size = hitBox.Size;

        Handles.color = HitBoxActiveColor;

        EditorGUI.BeginChangeCheck();

        // 跟随 Unity 工具（W/E/R）：避免同时显示位移/旋转/缩放导致误操作
        Tool tool = Tools.current;
        bool showMove = tool == Tool.Move || tool == Tool.Transform || tool == Tool.Rect;
        bool showRotate = tool == Tool.Rotate || tool == Tool.Transform;
        bool showScale = tool == Tool.Scale || tool == Tool.Transform;

        Vector3 newWorldCenter = worldCenter;
        Quaternion newWorldRot = worldRot;
        Vector3 newSize = size;

        if (showMove)
        {
            newWorldCenter = Handles.PositionHandle(newWorldCenter, newWorldRot);
        }

        if (showRotate)
        {
            newWorldRot = Handles.RotationHandle(newWorldRot, newWorldCenter);
        }

        // 形状线框 + Scale 手柄
        float handleSize = HandleUtility.GetHandleSize(newWorldCenter);

        switch (hitBox.ShapeType)
        {
            case HitShapeType.Box:
            {
                // 预览线框：Move/Rotate 模式下提供上下文；Scale 模式下 BoxBoundsHandle 本身会有可视化
                // 注意：必须使用 hitBox.Size（而不是 Vector3.one），否则切换工具时会看到“盒子大小变化”的错觉。
                if (!showScale)
                {
                    Matrix4x4 prev = Handles.matrix;
                    Handles.matrix = Matrix4x4.TRS(newWorldCenter, newWorldRot, size);
                    Handles.DrawWireCube(Vector3.zero, Vector3.one);
                    Handles.matrix = prev;
                }
                else
                {
                    Matrix4x4 prevMatrix = Handles.matrix;
                    Handles.matrix = Matrix4x4.TRS(newWorldCenter, newWorldRot, Vector3.one);
                    hitBoxBoundsHandle.center = Vector3.zero;
                    hitBoxBoundsHandle.size = newSize;
                    hitBoxBoundsHandle.DrawHandle();
                    newSize = hitBoxBoundsHandle.size;
                    Handles.matrix = prevMatrix;
                }
                break;
            }
            case HitShapeType.Sphere:
            {
                float radius = Mathf.Max(0.001f, newSize.x);

                // wire sphere（用三组圆盘近似）
                Matrix4x4 prev = Handles.matrix;
                Handles.matrix = Matrix4x4.TRS(newWorldCenter, newWorldRot, Vector3.one);
                Handles.DrawWireDisc(Vector3.zero, Vector3.up, radius);
                Handles.DrawWireDisc(Vector3.zero, Vector3.right, radius);
                Handles.DrawWireDisc(Vector3.zero, Vector3.forward, radius);
                Handles.matrix = prev;

                if (showScale)
                {
                    radius = Handles.RadiusHandle(newWorldRot, newWorldCenter, radius);
                }

                newSize.x = Mathf.Max(0.001f, radius);
                break;
            }
            case HitShapeType.Fan:
            {
                float radius = Mathf.Max(0.001f, newSize.x);
                float angle = Mathf.Max(0.001f, newSize.y);
                float height = Mathf.Max(0f, newSize.z);

                // wire fan：Arc + 边线
                Vector3 forward = newWorldRot * Vector3.forward;
                Vector3 up = newWorldRot * Vector3.up;
                float half = angle * 0.5f;
                Vector3 startDir = Quaternion.AngleAxis(-half, up) * forward;
                // 直接填充扇形区域（更直观）
                Color prevColor = Handles.color;
                Handles.color = new Color(prevColor.r, prevColor.g, prevColor.b, 0.12f);
                if (height > 0f)
                {
                    float hh = height * 0.5f;
                    Vector3 top = newWorldCenter + up.normalized * hh;
                    Vector3 bottom = newWorldCenter - up.normalized * hh;
                    Handles.DrawSolidArc(top, up, startDir, angle, radius);
                    Handles.DrawSolidArc(bottom, up, startDir, angle, radius);
                }
                else
                {
                    Handles.DrawSolidArc(newWorldCenter, up, startDir, angle, radius);
                }
                Handles.color = prevColor;

                if (height > 0f)
                {
                    float hh = height * 0.5f;
                    Vector3 top = newWorldCenter + up.normalized * hh;
                    Vector3 bottom = newWorldCenter - up.normalized * hh;
                    Handles.DrawWireArc(top, up, startDir, angle, radius);
                    Handles.DrawWireArc(bottom, up, startDir, angle, radius);
                    Handles.DrawLine(bottom, top);
                    Handles.DrawLine(bottom + startDir.normalized * radius, top + startDir.normalized * radius);
                    Vector3 endDir = Quaternion.AngleAxis(half, up) * forward;
                    Handles.DrawLine(bottom + endDir.normalized * radius, top + endDir.normalized * radius);
                }
                else
                {
                    Handles.DrawWireArc(newWorldCenter, up, startDir, angle, radius);
                    Handles.DrawLine(newWorldCenter, newWorldCenter + startDir.normalized * radius);
                    Vector3 endDir = Quaternion.AngleAxis(half, up) * forward;
                    Handles.DrawLine(newWorldCenter, newWorldCenter + endDir.normalized * radius);
                }

                if (showScale)
                {
                    // 半径：可直接拖半径
                    radius = Handles.RadiusHandle(newWorldRot, newWorldCenter, radius);

                    // 角度：拖扇形边界点（更直观）
                    // 以 forward 为中线，保持左右对称：拖动一侧边界点即可调整半角，从而得到总角度 = 2 * halfAngle。
                    Vector3 planeNormal = up.normalized;
                    Vector3 forwardOnPlane = Vector3.ProjectOnPlane(forward, planeNormal).normalized;
                    if (forwardOnPlane.sqrMagnitude < 1e-6f)
                    {
                        forwardOnPlane = Vector3.forward;
                    }

                    Vector3 boundaryDir = Quaternion.AngleAxis(half, planeNormal) * forwardOnPlane;
                    Vector3 boundaryPoint = newWorldCenter + boundaryDir * radius;

                    float boundaryHandleSize = HandleUtility.GetHandleSize(boundaryPoint) * 0.06f;
                    Vector3 newBoundaryPoint = Handles.FreeMoveHandle(
                        boundaryPoint,
                        boundaryHandleSize,
                        Vector3.zero,
                        Handles.SphereHandleCap);

                    // 由拖拽后的点反推半径与角度（投影到扇形平面）
                    Vector3 v = Vector3.ProjectOnPlane(newBoundaryPoint - newWorldCenter, planeNormal);
                    float newRadius = v.magnitude;
                    if (newRadius > 1e-4f)
                    {
                        radius = newRadius;
                        float newHalf = Mathf.Abs(Vector3.SignedAngle(forwardOnPlane, v.normalized, planeNormal));
                        angle = Mathf.Clamp(newHalf * 2f, 0.001f, 360f);
                    }

                    // 辅助标签（便于读数）
                    // 高度：沿 up 的滑动手柄（ScaleValueHandle 在 value=0 时无法“从 0 拉起来”，这里改为 Slider）
                    float halfHeightForHandle = Mathf.Max(0.01f, height * 0.5f);
                    Vector3 heightHandlePos = newWorldCenter + planeNormal * halfHeightForHandle;
                    float heightHandleSize = HandleUtility.GetHandleSize(heightHandlePos) * 0.08f;
                    Vector3 newHeightHandlePos = Handles.Slider(heightHandlePos, planeNormal, heightHandleSize, Handles.ConeHandleCap, 0f);
                    float newHalfHeight = Mathf.Abs(Vector3.Dot(newHeightHandlePos - newWorldCenter, planeNormal));
                    height = Mathf.Max(0f, newHalfHeight * 2f);

                    Handles.Label(newWorldCenter + planeNormal * (handleSize * 0.25f), $"R={radius:F2}, A={angle:F1}°, H={height:F2}");
                }

                newSize.x = Mathf.Max(0.001f, radius);
                newSize.y = Mathf.Clamp(angle, 0.001f, 360f);
                newSize.z = Mathf.Max(0f, height);
                break;
            }
            case HitShapeType.Capsule:
            {
                float radius = Mathf.Max(0.001f, newSize.x);
                float height = Mathf.Max(0.001f, newSize.y);

                // 画胶囊线框（与播放预览一致的近似绘制）
                float halfHeight = Mathf.Max(0f, (height - radius * 2f) * 0.5f);
                Vector3 up = newWorldRot * Vector3.up;
                Vector3 p0 = newWorldCenter - up * halfHeight;
                Vector3 p1 = newWorldCenter + up * halfHeight;

                Handles.DrawWireDisc(p0, up, radius);
                Handles.DrawWireDisc(p1, up, radius);
                Vector3 right = newWorldRot * Vector3.right;
                Vector3 fwd = newWorldRot * Vector3.forward;
                Handles.DrawLine(p0 + right * radius, p1 + right * radius);
                Handles.DrawLine(p0 - right * radius, p1 - right * radius);
                Handles.DrawLine(p0 + fwd * radius, p1 + fwd * radius);
                Handles.DrawLine(p0 - fwd * radius, p1 - fwd * radius);
                Handles.DrawWireArc(p0, right, fwd, 180f, radius);
                Handles.DrawWireArc(p0, fwd, right, 180f, radius);
                Handles.DrawWireArc(p1, right, -fwd, 180f, radius);
                Handles.DrawWireArc(p1, fwd, -right, 180f, radius);

                if (showScale)
                {
                    radius = Handles.RadiusHandle(newWorldRot, newWorldCenter, radius);

                    Vector3 heightHandlePos = newWorldCenter + up.normalized * (handleSize * 0.8f);
                    height = Handles.ScaleValueHandle(height, heightHandlePos, newWorldRot, handleSize * 0.6f, Handles.ConeHandleCap, 0.2f);
                }

                radius = Mathf.Max(0.001f, radius);
                // 与运行时 OverlapCapsule 的 halfHeight 计算一致：height 至少为 2*radius
                height = Mathf.Max(height, radius * 2f + 0.001f);
                newSize.x = radius;
                newSize.y = height;
                break;
            }
        }

        if (EditorGUI.EndChangeCheck())
        {
            // Undo + 写回数据（Offset / RotationEuler / Size）
            if (selectConfigAsset != null && selectConfigAsset.value != null)
            {
                Undo.RecordObject(selectConfigAsset.value, "Edit HitBox");
            }

            hitBox.Offset = player.InverseTransformPoint(newWorldCenter);

            Quaternion relRot = Quaternion.Inverse(player.rotation) * newWorldRot;
            Vector3 euler = relRot.eulerAngles;
            euler.x = NormalizeAngle180(euler.x);
            euler.y = NormalizeAngle180(euler.y);
            euler.z = NormalizeAngle180(euler.z);
            hitBox.RotationEuler = euler;

            hitBox.Size = new Vector3(
                Mathf.Max(0.001f, newSize.x),
                Mathf.Max(0.001f, newSize.y),
                Mathf.Max(0.001f, newSize.z)
            );

            // 同步右侧面板的“触发时间（秒）”等显示（不改变归一化时间）
            UpdateClipProperties(hitBoxClip);

            // 标记资源脏 + 让 SceneView 及时刷新
            MarkAssetDirty();
            sceneView.Repaint();
        }
    }

    private void TrySyncSelectedPreviewVfxTransformFromSceneSelection(SceneView sceneView)
    {
        if (previewVfxInstances == null || previewVfxInstances.Count == 0)
        {
            return;
        }

        // 以场景选择为准：允许用户直接点击 Hierarchy/Scene 里的预览实例（或其子节点）
        var selectedTf = Selection.activeTransform;
        if (selectedTf == null)
        {
            return;
        }

        PreviewVfxInstance inst = null;
        Transform t = selectedTf;
        while (t != null && inst == null)
        {
            for (int i = 0; i < previewVfxInstances.Count; ++i)
            {
                var it = previewVfxInstances[i];
                if (it?.GameObject != null && ReferenceEquals(it.GameObject.transform, t))
                {
                    inst = it;
                    break;
                }
            }

            t = t.parent;
        }

        var go = inst?.GameObject;
        if (go == null || inst.Data == null || animancer == null)
        {
            return;
        }

        Transform owner = animancer.transform;
        if (owner == null)
        {
            return;
        }

        // FollowTarget 语义（与运行时/预览一致）：
        // - FollowTarget=true：实例在角色/挂点下，用户改的是 local pose，直接写回 Offset/RotationEuler。
        // - FollowTarget=false：实例定格在 world，用户改的是 world pose，需要反算回“相对角色”的 Offset/RotationEuler。
        Vector3 currentPos;
        Quaternion currentRot;
        Vector3 offset;
        Quaternion relRot;

        if (inst.Data.FollowTarget)
        {
            currentPos = go.transform.localPosition;
            currentRot = go.transform.localRotation;
            offset = currentPos;
            relRot = currentRot;
        }
        else
        {
            currentPos = go.transform.position;
            currentRot = go.transform.rotation;
            offset = owner.InverseTransformPoint(currentPos);
            relRot = Quaternion.Inverse(owner.rotation) * currentRot;
        }

        // 检测是否真的发生变化（支持 Unity 默认 gizmo 直接改 Transform）
        // 记录值的空间与预览创建时一致：FollowTarget=true 比较 local；FollowTarget=false 比较 world。
        bool hasPrev = inst.HasSyncedPose;
        bool moved = !hasPrev || (currentPos - inst.LastSyncedPosition).sqrMagnitude > 0.0000001f;
        bool rotated = !hasPrev || Quaternion.Angle(currentRot, inst.LastSyncedRotation) > 0.01f;
        if (!moved && !rotated)
        {
            return;
        }

        // Undo + 写回数据（Offset / RotationEuler）
        if (selectConfigAsset != null && selectConfigAsset.value != null)
        {
            Undo.RecordObject(selectConfigAsset.value, "Edit VFX Transform");
        }

        Vector3 euler = relRot.eulerAngles;
        euler.x = NormalizeAngle180(euler.x);
        euler.y = NormalizeAngle180(euler.y);
        euler.z = NormalizeAngle180(euler.z);

        inst.Data.Offset = offset;
        inst.Data.RotationEuler = euler;

        // 把实例姿态也“归一化”到 [-180,180] 的欧拉（防止 0-360 来回跳导致 UI 读数不稳定）
        if (inst.Data.FollowTarget)
        {
            go.transform.localPosition = offset;
            go.transform.localRotation = Quaternion.Euler(euler);
        }
        else
        {
            go.transform.position = owner.TransformPoint(offset);
            go.transform.rotation = owner.rotation * Quaternion.Euler(euler);
        }
        go.transform.localScale = Vector3.one;

        inst.HasSyncedPose = true;
        inst.LastSyncedPosition = inst.Data.FollowTarget ? go.transform.localPosition : go.transform.position;
        inst.LastSyncedRotation = inst.Data.FollowTarget ? go.transform.localRotation : go.transform.rotation;

        // 若当前右侧面板正在显示这一条 EffectClip，则回显数值（避免触发回调导致重建实例）
        if (selectedClip is EffectClipItem effectClipItem && ReferenceEquals(effectClipItem.EffectData, inst.Data))
        {
            if (effectOffsetField != null)
            {
                effectOffsetField.SetValueWithoutNotify(offset);
            }
            if (effectRotationField != null)
            {
                effectRotationField.SetValueWithoutNotify(euler);
            }
        }

        MarkAssetDirty();
        sceneView.Repaint();
        Repaint();
    }

    private static float NormalizeAngle180(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        if (angle < -180f) angle += 360f;
        return angle;
    }

    private void DrawPreviewHitBoxesAtCurrentTime()
    {
        // 当前时间对应的段
        var seg = FindSegmentAtTime(currentPlaybackTime);
        if (seg == null || seg.HitBoxes == null || seg.HitBoxes.Count == 0)
        {
            return;
        }

        // 段时长：与预览采样一致（Duration 优先，其次 clip.length / speed）
        float duration = Mathf.Max(0f, seg.Duration);
        if (duration <= 0f && seg.AnimationClipTrans != null && seg.AnimationClipTrans.Clip != null)
        {
            float s = Mathf.Max(0.01f, seg.AnimationClipTrans.Speed);
            duration = seg.AnimationClipTrans.Clip.length / s;
        }
        if (duration <= 0f)
        {
            return;
        }

        float normalizedTime = Mathf.Clamp01((currentPlaybackTime - seg.StartTime) / duration);

        Transform player = animancer.transform;
        if (player == null)
        {
            return;
        }

        // 在 AnimationEnd 之后运行时认为段已结束：预览同样不再绘制
        float endNorm = seg.TimeWindow != null ? seg.TimeWindow.AnimationEnd : 1f;
        if (endNorm <= 0f) endNorm = 1f;
        endNorm = Mathf.Clamp01(endNorm);
        if (normalizedTime > endNorm)
        {
            return;
        }

        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

        for (int i = 0; i < seg.HitBoxes.Count; ++i)
        {
            var hb = seg.HitBoxes[i];
            if (hb == null)
            {
                continue;
            }

            float start = Mathf.Clamp01(hb.NormalizedStart);
            float end = Mathf.Clamp01(hb.NormalizedEnd);
            bool isActive = normalizedTime >= start && normalizedTime <= end;
            if (!isActive)
            {
                continue; // 播放时仅显示激活窗口内的 HitBox
            }

            DrawHitBoxWire(player, hb, isActive);
        }
    }

    private static void DrawHitBoxWire(Transform player, HitBoxData hitBox, bool active)
    {
        if (player == null || hitBox == null)
        {
            return;
        }

        Vector3 worldCenter = player.TransformPoint(hitBox.Offset);
        Quaternion worldRot = player.rotation * Quaternion.Euler(hitBox.RotationEuler);

        Handles.color = active ? HitBoxActiveColor : HitBoxInactiveColor;

        switch (hitBox.ShapeType)
        {
            case HitShapeType.Box:
            {
                // Physics: OverlapBox(center, halfExtents, orientation)
                Vector3 size = hitBox.Size;
                Matrix4x4 prev = Handles.matrix;
                Handles.matrix = Matrix4x4.TRS(worldCenter, worldRot, size);
                Handles.DrawWireCube(Vector3.zero, Vector3.one);
                Handles.matrix = prev;
                break;
            }
            case HitShapeType.Sphere:
            {
                float radius = Mathf.Max(0f, hitBox.Size.x);
                if (radius <= 0f) break;

                // 画三个正交圆盘，便于观察空间范围
                Matrix4x4 prev = Handles.matrix;
                Handles.matrix = Matrix4x4.TRS(worldCenter, worldRot, Vector3.one);
                Handles.DrawWireDisc(Vector3.zero, Vector3.up, radius);
                Handles.DrawWireDisc(Vector3.zero, Vector3.right, radius);
                Handles.DrawWireDisc(Vector3.zero, Vector3.forward, radius);
                Handles.matrix = prev;
                break;
            }
            case HitShapeType.Fan:
            {
                float radius = Mathf.Max(0f, hitBox.Size.x);
                float angle = Mathf.Max(0f, hitBox.Size.y);
                float height = Mathf.Max(0f, hitBox.Size.z);
                if (radius <= 0f || angle <= 0f) break;

                // Physics: OverlapFan(center, forward, radius, angle)
                Vector3 forward = worldRot * Vector3.forward;
                Vector3 up = worldRot * Vector3.up;

                float half = angle * 0.5f;
                Vector3 startDir = Quaternion.AngleAxis(-half, up) * forward;
                // 填充扇形（更直观）。若 height > 0，则显示“有厚度的扇形体”。
                Color prevColor = Handles.color;
                Handles.color = new Color(prevColor.r, prevColor.g, prevColor.b, 0.12f);
                if (height > 0f)
                {
                    float hh = height * 0.5f;
                    Vector3 top = worldCenter + up.normalized * hh;
                    Vector3 bottom = worldCenter - up.normalized * hh;
                    Handles.DrawSolidArc(top, up, startDir, angle, radius);
                    Handles.DrawSolidArc(bottom, up, startDir, angle, radius);
                }
                else
                {
                    Handles.DrawSolidArc(worldCenter, up, startDir, angle, radius);
                }
                Handles.color = prevColor;

                // 线框
                if (height > 0f)
                {
                    float hh = height * 0.5f;
                    Vector3 top = worldCenter + up.normalized * hh;
                    Vector3 bottom = worldCenter - up.normalized * hh;
                    Handles.DrawWireArc(top, up, startDir, angle, radius);
                    Handles.DrawWireArc(bottom, up, startDir, angle, radius);
                    Handles.DrawLine(bottom, top); // 中心竖线
                    Handles.DrawLine(bottom + startDir.normalized * radius, top + startDir.normalized * radius);
                    Vector3 endDir = Quaternion.AngleAxis(half, up) * forward;
                    Handles.DrawLine(bottom + endDir.normalized * radius, top + endDir.normalized * radius);
                }
                else
                {
                    Handles.DrawWireArc(worldCenter, up, startDir, angle, radius);
                    Handles.DrawLine(worldCenter, worldCenter + startDir.normalized * radius);
                    Vector3 endDir = Quaternion.AngleAxis(half, up) * forward;
                    Handles.DrawLine(worldCenter, worldCenter + endDir.normalized * radius);
                }
                break;
            }
            case HitShapeType.Capsule:
            {
                // Physics: OverlapCapsule(center, radius, height, orientation) uses orientation*Vector3.up as axis.
                float radius = Mathf.Max(0f, hitBox.Size.x);
                float height = Mathf.Max(0f, hitBox.Size.y);
                if (radius <= 0f || height <= 0f) break;

                float halfHeight = Mathf.Max(0f, (height - radius * 2f) * 0.5f);
                Vector3 up = worldRot * Vector3.up;
                Vector3 p0 = worldCenter - up * halfHeight;
                Vector3 p1 = worldCenter + up * halfHeight;

                // 画两端球 + 连接线（近似胶囊线框）
                Matrix4x4 prev = Handles.matrix;
                Handles.matrix = Matrix4x4.TRS(Vector3.zero, worldRot, Vector3.one);

                // 在局部空间绘制更容易：把点转换到 world 下再绘制线框圆
                Handles.matrix = prev;
                Handles.DrawWireDisc(p0, up, radius);
                Handles.DrawWireDisc(p1, up, radius);

                // 画侧边线（取 right/forward 两个方向各一条）
                Vector3 right = worldRot * Vector3.right;
                Vector3 fwd = worldRot * Vector3.forward;
                Handles.DrawLine(p0 + right * radius, p1 + right * radius);
                Handles.DrawLine(p0 - right * radius, p1 - right * radius);
                Handles.DrawLine(p0 + fwd * radius, p1 + fwd * radius);
                Handles.DrawLine(p0 - fwd * radius, p1 - fwd * radius);

                // 端部半圆（可选：简单补两端的 wire arc，增强视觉）
                Handles.DrawWireArc(p0, right, fwd, 180f, radius);
                Handles.DrawWireArc(p0, fwd, right, 180f, radius);
                Handles.DrawWireArc(p1, right, -fwd, 180f, radius);
                Handles.DrawWireArc(p1, fwd, -right, 180f, radius);
                break;
            }
        }
    }

    #region 位移轨迹可视化

    /// <summary>
    /// 根据当前上下文绘制位移轨迹（编辑模式：选中段；播放模式：当前播放段）
    /// </summary>
    private void DrawMovementTrajectoryForCurrentContext(Transform player)
    {
        if (config == null || config.Segments == null || player == null)
        {
            return;
        }

        AttackSegmentData targetSegment = null;

        // 播放模式：显示当前播放段的位移轨迹
        if (isPlaying || isDraggingPlayhead)
        {
            targetSegment = FindSegmentAtTime(currentPlaybackTime);
        }
        // 编辑模式：显示当前选中段的位移轨迹
        else if (selectedClip is AnimationClipItem animClipItem && animClipItem.SegmentData != null)
        {
            targetSegment = animClipItem.SegmentData;
        }

        // 绘制目标段的位移轨迹
        if (targetSegment != null && targetSegment.Movement != null && targetSegment.Movement.EnableMovement)
        {
            DrawMovementTrajectory(targetSegment, player, isPreview: isPlaying || isDraggingPlayhead);
        }
    }

    /// <summary>
    /// 绘制预览位移进度（播放/拖拽时：显示当前位移状态）
    /// </summary>
    private void DrawPreviewMovementProgress(Transform player)
    {
        if (config == null || player == null)
        {
            return;
        }

        // 获取当前时间对应的段
        var segment = FindSegmentAtTime(currentPlaybackTime);
        if (segment?.Movement == null || !segment.Movement.EnableMovement)
        {
            return;
        }

        // 计算段的归一化时间
        float segmentDuration = segment.Duration;
        if (segmentDuration <= 0f && segment.AnimationClipTrans != null && segment.AnimationClipTrans.Clip != null)
        {
            float speed = Mathf.Max(0.01f, segment.AnimationClipTrans.Speed);
            segmentDuration = segment.AnimationClipTrans.Clip.length / speed;
        }
        if (segmentDuration <= 0f)
        {
            return;
        }

        float normalizedTime = Mathf.Clamp01((currentPlaybackTime - segment.StartTime) / segmentDuration);
        var movement = segment.Movement;

        // 检查是否在位移窗口内
        if (normalizedTime < movement.NormalizedStart || normalizedTime > movement.NormalizedEnd)
        {
            return;
        }

        // 计算当前位移进度
        float moveProgress = (normalizedTime - movement.NormalizedStart) / (movement.NormalizedEnd - movement.NormalizedStart);
        moveProgress = Mathf.Clamp01(moveProgress);
        float curveValue = movement.MoveCurve.Evaluate(moveProgress);

        Vector3 startPos = player.position;
        Vector3 direction = player.forward;
        Vector3 currentPos = startPos + direction * movement.Distance * curveValue;

        // 绘制当前位置标记
        Handles.color = Color.yellow;
        Handles.SphereHandleCap(0, currentPos, Quaternion.identity, 0.15f, EventType.Repaint);

        // 绘制从起点到当前位置的线
        Handles.color = Color.cyan;
        Handles.DrawLine(startPos, currentPos, 2f);

        // 绘制标签
        Handles.Label(currentPos + Vector3.up * 0.3f, $"位移进度: {moveProgress:P0}");
    }

    /// <summary>
    /// 绘制单个段的位移轨迹
    /// </summary>
    private void DrawMovementTrajectory(AttackSegmentData segment, Transform player, bool isPreview)
    {
        if (segment?.Movement == null || !segment.Movement.EnableMovement || player == null)
        {
            return;
        }

        var movement = segment.Movement;

        // 计算起始和结束位置
        Vector3 startPos = player.position;
        Vector3 direction = player.forward;
        Vector3 endPos = startPos + direction * movement.Distance;

        // 绘制位移轨迹主线（橙色，粗线）
        Handles.color = new Color(1f, 0.5f, 0f, 0.8f);
        Handles.DrawLine(startPos, endPos, 3f);

        // 绘制起点标记（绿色球）
        Handles.color = Color.green;
        float handleSize = HandleUtility.GetHandleSize(startPos);
        Handles.SphereHandleCap(0, startPos, Quaternion.identity, handleSize * 0.1f, EventType.Repaint);
        Handles.Label(startPos + Vector3.up * (handleSize * 0.2f), "起点");

        // 绘制终点标记（红色球）
        Handles.color = Color.red;
        Handles.SphereHandleCap(0, endPos, Quaternion.identity, handleSize * 0.1f, EventType.Repaint);
        Handles.Label(endPos + Vector3.up * (handleSize * 0.2f), $"终点 ({movement.Distance:F2}m)");

        // 绘制位移曲线预览（根据 MoveCurve 生成路径点）
        Vector3[] curvePoints = new Vector3[20];
        for (int i = 0; i <= 19; i++)
        {
            float t = i / 19f;
            float curveValue = movement.MoveCurve.Evaluate(t);
            curvePoints[i] = Vector3.Lerp(startPos, endPos, curveValue);
        }
        Handles.color = new Color(1f, 0.8f, 0f, 0.6f);
        Handles.DrawPolyLine(curvePoints);

        // 绘制位移窗口标签
        Handles.color = Color.white;
        float midProgress = (movement.NormalizedStart + movement.NormalizedEnd) * 0.5f;
        Vector3 midPos = Vector3.Lerp(startPos, endPos, midProgress);
        Handles.Label(midPos + Vector3.up * (handleSize * 0.3f), 
            $"位移窗口: {movement.NormalizedStart:F2} - {movement.NormalizedEnd:F2}");

        // 如果不在预览模式，可以添加可拖拽的终点 Handle（用于调整距离）
        if (!isPreview)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 newEndPos = Handles.PositionHandle(endPos, player.rotation);
            if (EditorGUI.EndChangeCheck())
            {
                // 计算新的距离并更新配置
                float newDistance = Vector3.Distance(startPos, newEndPos);
                // 检查方向是否与角色 forward 一致（允许小角度偏差）
                Vector3 newDirection = (newEndPos - startPos).normalized;
                float angle = Vector3.Angle(player.forward, newDirection);
                if (angle < 45f) // 允许45度内的偏差
                {
                    movement.Distance = newDistance;
                    MarkAssetDirty();
                    SceneView.RepaintAll();
                }
            }
        }
    }

    #endregion
}


using System;
using ET;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public partial class SkillEditorWindow : EditorWindow
{
    private readonly BoxBoundsHandle hitBoxBoundsHandle = new BoxBoundsHandle();

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        // 仅在窗口可用、且选中了 HitBoxClip 时显示/编辑
        if (config == null || animancer == null || selectedClip is not HitBoxClipItem hitBoxClip || hitBoxClip.HitBoxData == null)
        {
            return;
        }

        var hitBox = hitBoxClip.HitBoxData;

        // 当前只做 Box 的可视化编辑（其他形状后续可扩展对应 Handle）
        if (hitBox.ShapeType != HitShapeType.Box)
        {
            return;
        }

        Transform player = animancer.transform;

        // world pose
        Vector3 worldCenter = player.TransformPoint(hitBox.Offset);
        Quaternion worldRot = player.rotation * Quaternion.Euler(hitBox.RotationEuler);
        Vector3 size = hitBox.Size;

        Handles.color = new Color(0.2f, 1f, 0.2f, 0.9f);

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

        // 预览线框：Move/Rotate 模式下提供上下文；Scale 模式下 BoxBoundsHandle 本身会有可视化
        // 注意：必须使用 hitBox.Size（而不是 Vector3.one），否则切换工具时会看到“盒子大小变化”的错觉。
        if (!showScale)
        {
            Matrix4x4 prev = Handles.matrix;
            Handles.matrix = Matrix4x4.TRS(newWorldCenter, newWorldRot, size);
            Handles.DrawWireCube(Vector3.zero, Vector3.one);
            Handles.matrix = prev;
        }

        if (showScale)
        {
            Matrix4x4 prevMatrix = Handles.matrix;
            Handles.matrix = Matrix4x4.TRS(newWorldCenter, newWorldRot, Vector3.one);
            hitBoxBoundsHandle.center = Vector3.zero;
            hitBoxBoundsHandle.size = newSize;
            hitBoxBoundsHandle.DrawHandle();
            newSize = hitBoxBoundsHandle.size;
            Handles.matrix = prevMatrix;
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

    private static float NormalizeAngle180(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        if (angle < -180f) angle += 360f;
        return angle;
    }
}


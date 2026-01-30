using System.Collections.Generic;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// 物理检测辅助类
    /// - 使用 NonAlloc 系列 API，避免 GC
    /// - 内部使用静态缓冲区复用；仅限主线程/非并发调用
    /// </summary>
    public static class PhysicsHelper
    {
        // 复用缓冲区，避免 GC（注意：静态缓冲区不支持并发）
        private static readonly Collider[] ColliderBuffer = new Collider[64];
        private static readonly RaycastHit[] RaycastHitBuffer = new RaycastHit[64];
        private static readonly List<GameObject> UnitBuffer = new List<GameObject>(32);

        /// <summary>
        /// 尺寸/高度的“接近 0”判定容差（外部传 0，但经过序列化/计算后可能变成极小浮点数）。
        /// 约定：|v| <= epsilon 视为 0。
        /// </summary>
        private const float SizeEpsilon = 0.01f;

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器运行时调试绘制开关（Scene 视图需要开启 Gizmos 才能看到）。
        /// </summary>
        public static bool DebugDrawEnabled = true;

        /// <summary>调试线持续时间（秒）。0 = 仅 1 帧。</summary>
        public static float DebugDrawDurationSec = 0.25f;

        /// <summary>扇形检测调试颜色。</summary>
        public static Color DebugFanColor = new Color(0.1f, 0.9f, 0.3f, 1f);

        /// <summary>射线检测调试颜色。</summary>
        public static Color DebugRayColor = new Color(1f, 0.85f, 0.1f, 1f);

        /// <summary>命中包围盒调试颜色。</summary>
        public static Color DebugHitBoundsColor = new Color(1f, 0.3f, 0.3f, 1f);
#endif

        /// <summary>
        /// 盒体检测
        /// </summary>
        public static void OverlapBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, ListComponent<GameObject> list, int layerMask, int maxTargets = 0)
        {
            UnitBuffer.Clear();
            int count = Physics.OverlapBoxNonAlloc(center, halfExtents, ColliderBuffer, orientation, layerMask);
            
            for (int i = 0; i < count; i++)
            {
                var unit = GetUnitFromCollider(ColliderBuffer[i]);
                if (unit != null && !UnitBuffer.Contains(unit))
                {
                    UnitBuffer.Add(unit);
                    // 早期退出优化
                    if (maxTargets > 0 && UnitBuffer.Count >= maxTargets)
                    {
                        break;
                    }
                }
            }

            list.AddRange(UnitBuffer);
        }

        /// <summary>
        /// 球体检测（填充到 list）
        /// </summary>
        public static void OverlapSphere(Vector3 center, float radius, ListComponent<GameObject> list, int layerMask, int maxTargets = 0)
        {
            UnitBuffer.Clear();
            int count = Physics.OverlapSphereNonAlloc(center, radius, ColliderBuffer, layerMask);

            for (int i = 0; i < count; i++)
            {
                var unit = GetUnitFromCollider(ColliderBuffer[i]);
                if (unit != null && !UnitBuffer.Contains(unit))
                {
                    UnitBuffer.Add(unit);
                    // 早期退出优化
                    if (maxTargets > 0 && UnitBuffer.Count >= maxTargets)
                    {
                        break;
                    }
                }
            }

            list.AddRange(UnitBuffer);
        }
        
        /// <summary>
        /// 扇形检测（高性能版本：OverlapBoxNonAlloc 粗筛 + 扇形细筛）
        /// </summary>
        /// <param name="center">扇形中心点</param>
        /// <param name="forward">扇形方向（世界空间）</param>
        /// <param name="radius">扇形半径</param>
        /// <param name="angle">扇形总角度（度）</param>
        /// <param name="list">输出列表</param>
        /// <param name="layerMask">层级遮罩</param>
        /// <param name="height">扇形高度（0 表示无高度限制）</param>
        /// <param name="maxTargets">最大目标数（0 = 无限制，>0 = 找到足够目标后提前退出）</param>
        public static void OverlapFan(Vector3 center, Vector3 forward, float radius, float angle, ListComponent<GameObject> list, int layerMask, float height = 0f, int maxTargets = 0)
        {
            UnitBuffer.Clear();

#if UNITY_EDITOR
            // 统一归一化 height：避免 0 变成极小浮点数导致逻辑分支错误
            float h = Mathf.Abs(height) <= SizeEpsilon ? 0f : height;
            bool hasHeightLimit = h > SizeEpsilon;
#endif

#if UNITY_EDITOR
            if (DebugDrawEnabled)
            {
                DrawFanDebug(center, forward, radius, angle, h, DebugFanColor, DebugDrawDurationSec);
            }
#endif

            // === 第一步：快速粗检（AABB 盒体，最快） ===
            // 优势：比球体和胶囊体都快，且能精确控制范围
            Vector3 boxCenter = center;
            Vector3 boxSize;
            Vector3 boxForward = forward;
            boxForward.y = 0f;
            if (boxForward.sqrMagnitude < 1e-6f)
            {
                boxForward = Vector3.forward;
            }
            boxForward.Normalize();
            Quaternion boxRotation = Quaternion.LookRotation(boxForward);
            
            // 注意：不要用 0.01f 这种业务相关阈值判断“是否无限高度”，统一使用 SizeEpsilon
            float heightNormalized = Mathf.Abs(height) <= SizeEpsilon ? 0f : height;
            bool heightLimited = heightNormalized > SizeEpsilon;
            if (!heightLimited)
            {
                // 无限高度：使用一个很高的盒体
                boxSize = new Vector3(radius * 2f, 100f, radius);
            }
            else
            {
                // 有高度限制：使用精确盒体
                boxSize = new Vector3(radius * 2f, heightNormalized, radius);
            }

            // 关键：粗筛盒体需要覆盖“前方 0..radius”的扇形区域，而不是仅覆盖 [-radius/2, +radius/2]。
            // 将盒体沿 forward 前移 radius/2，使盒体局部 Z 轴覆盖 [0, radius]（更贴合扇形前向范围）。
            boxCenter += boxForward * (radius * 0.5f);
            
            int count = Physics.OverlapBoxNonAlloc(boxCenter, boxSize * 0.5f, ColliderBuffer, boxRotation, layerMask);

#if UNITY_EDITOR
            if (DebugDrawEnabled)
            {
                // 粗筛盒体（方便排查“检测范围到底画在哪”）
                DrawWireBox(boxCenter, boxRotation, boxSize, new Color(DebugFanColor.r, DebugFanColor.g, DebugFanColor.b, 0.6f), DebugDrawDurationSec);
            }
#endif

            // === 第二步：精确细检（扇形角度 + 高度） ===
            float halfAngle = angle * 0.5f;
            float halfHeight = heightLimited ? heightNormalized * 0.5f : 0f;
            
            // 预计算（避免循环内重复计算）
            Vector3 forwardFlat = forward;
            forwardFlat.y = 0;
            bool hasForward = forwardFlat.sqrMagnitude > 0.01f;
            if (hasForward) forwardFlat.Normalize();
            
            float radiusSqr = radius * radius;

            for (int i = 0; i < count; i++)
            {
                var collider = ColliderBuffer[i];
                if (collider == null) continue;

                // 使用 Bounds.Center（更稳定）
                Vector3 targetPos = collider.bounds.center;
                
                // === 优化 1：距离早期退出（避免后续计算） ===
                Vector3 toTarget = targetPos - center;
                if (toTarget.sqrMagnitude > radiusSqr) continue;

                // === 优化 2：高度过滤（如果有限制） ===
                if (heightLimited)
                {
                    float deltaY = Mathf.Abs(targetPos.y - center.y);
                    if (deltaY > halfHeight) continue;
                }

                // === 优化 3：角度过滤（快速版） ===
                if (hasForward)
                {
                    Vector3 dirToTarget = toTarget;
                    dirToTarget.y = 0;
                    float horizontalDistSqr = dirToTarget.sqrMagnitude;
                    
                    // 如果在中心点附近（0.1米内），直接通过
                    if (horizontalDistSqr > 0.01f)
                    {
                        dirToTarget.Normalize();
                        float angleToTarget = Vector3.Angle(forwardFlat, dirToTarget);
                        if (angleToTarget > halfAngle) continue;
                    }
                }

                // 添加到结果列表
                var unit = GetUnitFromCollider(collider);
                if (unit != null && !UnitBuffer.Contains(unit))
                {
                    UnitBuffer.Add(unit);
                    
                    // === 优化 4：早期退出（找到足够目标后立即停止） ===
                    if (maxTargets > 0 && UnitBuffer.Count >= maxTargets)
                    {
                        break;
                    }
                }
            }

            list.AddRange(UnitBuffer);
        }
        
        /// <summary>
        /// 胶囊体检测
        /// </summary>
        public static void OverlapCapsule(Vector3 center, float radius, float height, Quaternion orientation, ListComponent<GameObject> list, int layerMask, int maxTargets = 0)
        {
            // 约定：list 必须非空且由调用方保证“可写且当前为空”（推荐使用 ListComponent<T>.Create())。
            UnitBuffer.Clear();

            // 计算胶囊体两端点
            float halfHeight = Mathf.Max(0, (height - radius * 2) * 0.5f);
            Vector3 up = orientation * Vector3.up;
            Vector3 point0 = center - up * halfHeight;
            Vector3 point1 = center + up * halfHeight;

            int count = Physics.OverlapCapsuleNonAlloc(point0, point1, radius, ColliderBuffer, layerMask);

            for (int i = 0; i < count; i++)
            {
                var unit = GetUnitFromCollider(ColliderBuffer[i]);
                if (unit != null && !UnitBuffer.Contains(unit))
                {
                    UnitBuffer.Add(unit);
                    // 早期退出优化
                    if (maxTargets > 0 && UnitBuffer.Count >= maxTargets)
                    {
                        break;
                    }
                }
            }

            list.AddRange(UnitBuffer);
        }

        /// <summary>
        /// 从Collider获取GameObject
        /// </summary>
        private static GameObject GetUnitFromCollider(Collider collider)
        {
            if (collider == null)
                return null;

            return collider.gameObject;
        }

        /// <summary>
        /// 射线检测获取目标
        /// </summary>
        public static GameObject RaycastUnit(Vector3 origin, Vector3 direction, float maxDistance, int layerMask)
        {
            if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, layerMask))
            {
#if UNITY_EDITOR
                if (DebugDrawEnabled)
                {
                    DrawRayDebug(origin, direction, hit.distance, DebugRayColor, DebugDrawDurationSec);
                    DrawHitBoundsDebug(hit.collider, DebugHitBoundsColor, DebugDrawDurationSec);
                    DrawPoint(hit.point, 0.08f, DebugHitBoundsColor, DebugDrawDurationSec);
                }
#endif
                return GetUnitFromCollider(hit.collider);
            }

#if UNITY_EDITOR
            if (DebugDrawEnabled)
            {
                DrawRayDebug(origin, direction, maxDistance, DebugRayColor, DebugDrawDurationSec);
            }
#endif
            return null;
        }

        /// <summary>
        /// 射线检测获取所有目标（NonAlloc，零 GC）
        /// </summary>
        public static void RaycastAllUnits(Vector3 origin, Vector3 direction, float maxDistance, ListComponent<GameObject> list, int layerMask, int maxTargets = 0)
        {
            UnitBuffer.Clear();

            int hitCount = Physics.RaycastNonAlloc(origin, direction, RaycastHitBuffer, maxDistance, layerMask);

#if UNITY_EDITOR
            if (DebugDrawEnabled)
            {
                DrawRayDebug(origin, direction, maxDistance, DebugRayColor, DebugDrawDurationSec);
            }
#endif

            for (int i = 0; i < hitCount; i++)
            {
                var hit = RaycastHitBuffer[i];
#if UNITY_EDITOR
                if (DebugDrawEnabled)
                {
                    DrawHitBoundsDebug(hit.collider, DebugHitBoundsColor, DebugDrawDurationSec);
                    DrawPoint(hit.point, 0.06f, DebugHitBoundsColor, DebugDrawDurationSec);
                }
#endif
                var unit = GetUnitFromCollider(hit.collider);
                if (unit != null && !UnitBuffer.Contains(unit))
                {
                    UnitBuffer.Add(unit);
                    // 早期退出优化
                    if (maxTargets > 0 && UnitBuffer.Count >= maxTargets)
                    {
                        break;
                    }
                }
            }

            list.AddRange(UnitBuffer);
        }

#if UNITY_EDITOR
        private static void DrawRayDebug(Vector3 origin, Vector3 direction, float length, Color color, float durationSec)
        {
            if (!Application.isPlaying) return;
            if (direction.sqrMagnitude < 1e-6f) return;
            Vector3 dir = direction.normalized;
            DebugDrawLine(origin, origin + dir * Mathf.Max(0f, length), color, durationSec);
        }

        private static void DrawHitBoundsDebug(Collider collider, Color color, float durationSec)
        {
            if (!Application.isPlaying) return;
            if (collider == null) return;
            Bounds b = collider.bounds;
            DrawWireCube(b.center, b.size, color, durationSec);
        }

        private static void DrawPoint(Vector3 p, float size, Color color, float durationSec)
        {
            if (!Application.isPlaying) return;
            float s = Mathf.Max(0.001f, size);
            DebugDrawLine(p - Vector3.right * s, p + Vector3.right * s, color, durationSec);
            DebugDrawLine(p - Vector3.forward * s, p + Vector3.forward * s, color, durationSec);
            DebugDrawLine(p - Vector3.up * s, p + Vector3.up * s, color, durationSec);
        }

        /// <summary>
        /// 统一 Debug.DrawLine 包装：
        /// - 关闭 depthTest，避免 Unity 6.3 SceneView 下被几何体遮挡导致“线框看起来断裂/错位”。
        /// </summary>
        private static void DebugDrawLine(Vector3 a, Vector3 b, Color color, float durationSec)
        {
            // Unity 的 Debug.DrawLine 在不同版本/视图下 depthTest 表现可能不同，这里显式关闭。
            Debug.DrawLine(a, b, color, durationSec, depthTest: false);
        }

        /// <summary>
        /// 绘制世界空间线框立方体（Axis-Aligned）。
        /// </summary>
        private static void DrawWireCube(Vector3 center, Vector3 size, Color color, float durationSec)
        {
            Vector3 e = size * 0.5f;
            Vector3 p0 = center + new Vector3(-e.x, -e.y, -e.z);
            Vector3 p1 = center + new Vector3(e.x, -e.y, -e.z);
            Vector3 p2 = center + new Vector3(e.x, -e.y, e.z);
            Vector3 p3 = center + new Vector3(-e.x, -e.y, e.z);

            Vector3 p4 = center + new Vector3(-e.x, e.y, -e.z);
            Vector3 p5 = center + new Vector3(e.x, e.y, -e.z);
            Vector3 p6 = center + new Vector3(e.x, e.y, e.z);
            Vector3 p7 = center + new Vector3(-e.x, e.y, e.z);

            DebugDrawLine(p0, p1, color, durationSec);
            DebugDrawLine(p1, p2, color, durationSec);
            DebugDrawLine(p2, p3, color, durationSec);
            DebugDrawLine(p3, p0, color, durationSec);

            DebugDrawLine(p4, p5, color, durationSec);
            DebugDrawLine(p5, p6, color, durationSec);
            DebugDrawLine(p6, p7, color, durationSec);
            DebugDrawLine(p7, p4, color, durationSec);

            DebugDrawLine(p0, p4, color, durationSec);
            DebugDrawLine(p1, p5, color, durationSec);
            DebugDrawLine(p2, p6, color, durationSec);
            DebugDrawLine(p3, p7, color, durationSec);
        }

        /// <summary>
        /// 绘制有朝向的线框盒（用于展示 OverlapBox 粗筛范围）。
        /// </summary>
        private static void DrawWireBox(Vector3 center, Quaternion rotation, Vector3 size, Color color, float durationSec)
        {
            Vector3 e = size * 0.5f;
            Vector3[] corners =
            {
                new Vector3(-e.x, -e.y, -e.z),
                new Vector3(e.x, -e.y, -e.z),
                new Vector3(e.x, -e.y, e.z),
                new Vector3(-e.x, -e.y, e.z),
                new Vector3(-e.x, e.y, -e.z),
                new Vector3(e.x, e.y, -e.z),
                new Vector3(e.x, e.y, e.z),
                new Vector3(-e.x, e.y, e.z),
            };

            for (int i = 0; i < corners.Length; i++)
            {
                corners[i] = center + rotation * corners[i];
            }

            // bottom
            DebugDrawLine(corners[0], corners[1], color, durationSec);
            DebugDrawLine(corners[1], corners[2], color, durationSec);
            DebugDrawLine(corners[2], corners[3], color, durationSec);
            DebugDrawLine(corners[3], corners[0], color, durationSec);
            // top
            DebugDrawLine(corners[4], corners[5], color, durationSec);
            DebugDrawLine(corners[5], corners[6], color, durationSec);
            DebugDrawLine(corners[6], corners[7], color, durationSec);
            DebugDrawLine(corners[7], corners[4], color, durationSec);
            // sides
            DebugDrawLine(corners[0], corners[4], color, durationSec);
            DebugDrawLine(corners[1], corners[5], color, durationSec);
            DebugDrawLine(corners[2], corners[6], color, durationSec);
            DebugDrawLine(corners[3], corners[7], color, durationSec);
        }

        /// <summary>
        /// 绘制扇形范围（仅展示 XZ 平面扇形；如果 height > 0 也会画上下两层轮廓线）。
        /// </summary>
        private static void DrawFanDebug(Vector3 center, Vector3 forward, float radius, float angleDeg, float height, Color color, float durationSec)
        {
            if (!Application.isPlaying) return;
            if (radius <= 0f) return;

            Vector3 f = forward;
            f.y = 0f;
            if (f.sqrMagnitude < 1e-6f) return;
            f.Normalize();

            float half = angleDeg * 0.5f;
            int segments = 20; // 足够平滑且开销低
            float step = angleDeg / segments;

            float y0 = center.y;
            float h = Mathf.Abs(height) <= SizeEpsilon ? 0f : height;
            bool hasHeightLimit = h > SizeEpsilon;
            float y1 = hasHeightLimit ? center.y + h : center.y;
            float y2 = hasHeightLimit ? center.y - h : center.y;

            // 中心射线
            DebugDrawLine(new Vector3(center.x, y0, center.z), new Vector3(center.x, y0, center.z) + f * radius, color, durationSec);

            Vector3 prevTop = Vector3.zero;
            Vector3 prevBottom = Vector3.zero;
            for (int i = 0; i <= segments; i++)
            {
                float a = -half + step * i;
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * f;

                Vector3 p = new Vector3(center.x, y0, center.z) + dir * radius;
                DebugDrawLine(new Vector3(center.x, y0, center.z), p, color, durationSec);

                // 画弧线
                if (i > 0)
                {
                    DebugDrawLine(prevTop, p, color, durationSec);
                }
                prevTop = p;

                // 有高度时，画上下两层轮廓（简单表达 3D 体积）
                if (hasHeightLimit)
                {
                    Vector3 pUp = new Vector3(p.x, y1, p.z);
                    Vector3 pDown = new Vector3(p.x, y2, p.z);

                    if (i > 0)
                    {
                        DebugDrawLine(prevBottom, pDown, color, durationSec);
                    }

                    // 连接上/下
                    DebugDrawLine(pUp, pDown, color, durationSec);
                    prevBottom = pDown;
                }
            }
        }
#endif
    }
}
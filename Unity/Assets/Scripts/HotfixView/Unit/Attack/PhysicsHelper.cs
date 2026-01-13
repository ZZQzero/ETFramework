using System.Collections.Generic;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// 物理检测辅助类
    /// </summary>
    public static class PhysicsHelper
    {
        // 复用碰撞器数组，避免GC
        private static readonly Collider[] ColliderBuffer = new Collider[64];
        private static readonly List<GameObject> UnitBuffer = new List<GameObject>(32);

        /// <summary>
        /// 盒体检测
        /// </summary>
        public static void OverlapBox(Vector3 center, Vector3 halfExtents, Quaternion orientation,ListComponent<GameObject> list,int layerMask)
        {
            UnitBuffer.Clear();

            int count = Physics.OverlapBoxNonAlloc(center, halfExtents, ColliderBuffer, orientation, layerMask);
            
            for (int i = 0; i < count; i++)
            {
                var unit = GetUnitFromCollider(ColliderBuffer[i]);
                if (unit != null && !UnitBuffer.Contains(unit))
                {
                    UnitBuffer.Add(unit);
                }
            }

            list.AddRange(UnitBuffer);
        }

        /// <summary>
        /// 球体检测（填充到 list）
        /// </summary>
        public static void OverlapSphere(Vector3 center, float radius, ListComponent<GameObject> list, int layerMask)
        {
            UnitBuffer.Clear();

            int count = Physics.OverlapSphereNonAlloc(center, radius, ColliderBuffer, layerMask);

            for (int i = 0; i < count; i++)
            {
                var unit = GetUnitFromCollider(ColliderBuffer[i]);
                if (unit != null && !UnitBuffer.Contains(unit))
                {
                    UnitBuffer.Add(unit);
                }
            }

            list.AddRange(UnitBuffer);
        }
        
        /// <summary>
        /// 扇形检测
        /// </summary>
        /// <param name="center">扇形中心点</param>
        /// <param name="forward">扇形方向（世界空间）</param>
        /// <param name="radius">扇形半径</param>
        /// <param name="angle">扇形总角度（度）</param>
        /// <param name="list">输出列表</param>
        /// <param name="layerMask">层级遮罩</param>
        /// <param name="height">扇形高度（0 表示无高度限制）</param>
        public static void OverlapFan(Vector3 center, Vector3 forward, float radius, float angle, ListComponent<GameObject> list, int layerMask, float height = 0f)
        {
            UnitBuffer.Clear();

            // 先用球体检测获取范围内的目标
            int count = Physics.OverlapSphereNonAlloc(center, radius, ColliderBuffer, layerMask);

            float halfAngle = angle * 0.5f;
            float halfHeight = height > 0f ? height * 0.5f : 0f;

            for (int i = 0; i < count; i++)
            {
                var collider = ColliderBuffer[i];
                if (collider == null)
                    continue;

                // 高度过滤：检查目标的 Collider 边界是否与扇形高度范围有重叠
                if (halfHeight > 0f && !CheckHeightOverlap(center.y, halfHeight, collider))
                {
                    continue;
                }

                // 角度过滤：检查目标是否在扇形角度范围内（水平扇形）
                if (!CheckAngleInFan(center, forward, collider.transform.position, halfAngle))
                {
                    continue;
                }

                // 添加到结果列表
                var unit = GetUnitFromCollider(collider);
                if (unit != null && !UnitBuffer.Contains(unit))
                {
                    UnitBuffer.Add(unit);
                }
            }

            list.AddRange(UnitBuffer);
        }
        
        /// <summary>
        /// 胶囊体检测
        /// </summary>
        public static void OverlapCapsule(Vector3 center, float radius, float height, Quaternion orientation, ListComponent<GameObject> list, int layerMask)
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
                }
            }

            list.AddRange(UnitBuffer);
        }
        
        /// <summary>
        /// 检查目标的 Collider 边界是否与扇形高度范围有重叠
        /// </summary>
        /// <param name="centerY">扇形中心点的 Y 坐标</param>
        /// <param name="halfHeight">扇形半高</param>
        /// <param name="collider">目标的 Collider</param>
        /// <returns>如果有重叠返回 true，否则返回 false</returns>
        private static bool CheckHeightOverlap(float centerY, float halfHeight, Collider collider)
        {
            float fanBottom = centerY - halfHeight;
            float fanTop = centerY + halfHeight;
            
            Bounds colliderBounds = collider.bounds;
            float targetBottom = colliderBounds.min.y;
            float targetTop = colliderBounds.max.y;
            
            // 检查是否有重叠：目标的底部在扇形顶部之上，或目标的顶部在扇形底部之下，则无重叠
            return !(targetBottom > fanTop || targetTop < fanBottom);
        }

        /// <summary>
        /// 检查目标是否在扇形角度范围内（水平扇形）
        /// </summary>
        /// <param name="center">扇形中心点</param>
        /// <param name="forward">扇形方向</param>
        /// <param name="targetPos">目标位置</param>
        /// <param name="halfAngle">扇形半角（度）</param>
        /// <returns>如果在角度范围内返回 true，否则返回 false</returns>
        private static bool CheckAngleInFan(Vector3 center, Vector3 forward, Vector3 targetPos, float halfAngle)
        {
            // 计算水平方向向量：先计算方向，y 置 0，然后归一化
            Vector3 directionToTarget = targetPos - center;
            directionToTarget.y = 0;
            
            // 如果水平距离为 0（目标在正上方或正下方），跳过
            if (directionToTarget.sqrMagnitude < 1e-6f)
            {
                return false;
            }
            directionToTarget.Normalize();
            
            // forward 的水平方向向量
            Vector3 forwardFlat = forward;
            forwardFlat.y = 0;
            
            // 如果 forward 的水平分量为 0（forward 垂直向上或向下），跳过
            if (forwardFlat.sqrMagnitude < 1e-6f)
            {
                return false;
            }
            forwardFlat.Normalize();

            // 计算角度并判断
            float angleToTarget = Vector3.Angle(forwardFlat, directionToTarget);
            return angleToTarget <= halfAngle;
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
                return GetUnitFromCollider(hit.collider);
            }
            return null;
        }

        /// <summary>
        /// 射线检测获取所有目标
        /// </summary>
        public static List<GameObject> RaycastAllUnits(Vector3 origin, Vector3 direction, float maxDistance, int layerMask)
        {
            UnitBuffer.Clear();

            var hits = Physics.RaycastAll(origin, direction, maxDistance, layerMask);
            
            foreach (var hit in hits)
            {
                var unit = GetUnitFromCollider(hit.collider);
                if (unit != null && !UnitBuffer.Contains(unit))
                {
                    UnitBuffer.Add(unit);
                }
            }

            return new List<GameObject>(UnitBuffer);
        }
    }
}
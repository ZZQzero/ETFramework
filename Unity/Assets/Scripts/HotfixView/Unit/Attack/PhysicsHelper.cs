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
        /// 球体检测
        /// </summary>
        public static List<GameObject> OverlapSphere(Vector3 center, float radius, int layerMask)
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
            
            return new List<GameObject>(UnitBuffer);
        }

        /// <summary>
        /// 扇形检测
        /// </summary>
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

                // 可选高度限制（沿 world up）
                if (halfHeight > 0f)
                {
                    float dy = Mathf.Abs(collider.transform.position.y - center.y);
                    if (dy > halfHeight)
                    {
                        continue;
                    }
                }

                // 检查是否在扇形角度内（水平扇形）
                Vector3 directionToTarget = (collider.transform.position - center).normalized;
                directionToTarget.y = 0;
                forward.y = 0;

                float angleToTarget = Vector3.Angle(forward.normalized, directionToTarget);
                if (angleToTarget <= halfAngle)
                {
                    var unit = GetUnitFromCollider(collider);
                    if (unit != null && !UnitBuffer.Contains(unit))
                    {
                        UnitBuffer.Add(unit);
                    }
                }
            }

            list.AddRange(UnitBuffer);
        }

        public static List<GameObject> OverlapFan(Vector3 center, Vector3 forward, float radius, float angle, int layerMask, float height = 0f)
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

                // 可选高度限制（沿 world up）：
                // - 俯视角游戏但存在跳跃/浮空：需要限制扇形对空/对地的有效高度范围。
                // - 兼容旧数据：height <= 0 表示不启用高度限制（旧行为）。
                // 注意：OverlapFan 的角度判断是“水平扇形”（会把 y 归零），因此高度也按 world y 计算。
                if (halfHeight > 0f)
                {
                    float dy = Mathf.Abs(collider.transform.position.y - center.y);
                    if (dy > halfHeight)
                    {
                        continue;
                    }
                }

                // 检查是否在扇形角度内
                Vector3 directionToTarget = (collider.transform.position - center).normalized;
                directionToTarget.y = 0;
                forward.y = 0;
                
                float angleToTarget = Vector3.Angle(forward.normalized, directionToTarget);
                if (angleToTarget <= halfAngle)
                {
                    var unit = GetUnitFromCollider(collider);
                    if (unit != null && !UnitBuffer.Contains(unit))
                    {
                        UnitBuffer.Add(unit);
                    }
                }
            }

            return new List<GameObject>(UnitBuffer);
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

        public static List<GameObject> OverlapCapsule(Vector3 center, float radius, float height, Quaternion orientation, int layerMask)
        {
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

            return new List<GameObject>(UnitBuffer);
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
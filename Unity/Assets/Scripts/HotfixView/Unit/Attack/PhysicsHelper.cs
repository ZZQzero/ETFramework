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
        public static List<GameObject> OverlapBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, int layerMask)
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

            return new List<GameObject>(UnitBuffer);
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
        public static List<GameObject> OverlapFan(Vector3 center, Vector3 forward, float radius, float angle, int layerMask)
        {
            UnitBuffer.Clear();

            // 先用球体检测获取范围内的目标
            int count = Physics.OverlapSphereNonAlloc(center, radius, ColliderBuffer, layerMask);
            
            float halfAngle = angle * 0.5f;
            
            for (int i = 0; i < count; i++)
            {
                var collider = ColliderBuffer[i];
                if (collider == null)
                    continue;

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
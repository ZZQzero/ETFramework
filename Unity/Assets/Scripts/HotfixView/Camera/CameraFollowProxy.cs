using UnityEngine;

namespace ET
{
    public class CameraFollowProxy : MonoBehaviour
    {
        public Transform target;
        public float fixedY = 0f;

        void LateUpdate()
        {
            if (target == null) return;

            Vector3 pos = target.position;
            pos.y = fixedY;   // 永远锁死在地面
            transform.position = pos;
        }
    }
}
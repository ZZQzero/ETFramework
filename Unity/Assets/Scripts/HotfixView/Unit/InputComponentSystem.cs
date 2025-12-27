using UnityEngine;

namespace ET
{
    public static partial class InputComponentSystem
    {
        [EntitySystem]
        private static void Awake(this InputComponent self)
        {
        }

        [EntitySystem]
        private static void Update(this InputComponent self)
        {
            if (!self.EnableInput)
            {
                return;
            }
            
            // 读取移动输入
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            self.MoveDirection = new Vector3(horizontal, 0f, vertical).normalized;
            // 读取跳跃输入
            self.JumpPressed = Input.GetKeyDown(self.JumpKey);
        }

        /// <summary>
        /// 获取当前移动方向
        /// </summary>
        public static Vector3 GetMoveDirection(this InputComponent self)
        {
            return self.MoveDirection;
        }

        /// <summary>
        /// 检查是否有跳跃请求
        /// </summary>
        public static bool HasJumpRequest(this InputComponent self)
        {
            return self.JumpPressed;
        }

        /// <summary>
        /// 检查是否有移动输入
        /// </summary>
        public static bool HasMoveInput(this InputComponent self)
        {
            return self.MoveDirection.magnitude > 0.01f;
        }
    }
}

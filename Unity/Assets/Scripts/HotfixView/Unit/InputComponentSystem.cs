using UnityEngine;

namespace ET
{
    public static partial class InputComponentSystem
    {
        [EntitySystem]
        private static void Awake(this InputComponent self,Transform player)
        {
            self.LastAimDirection = player.forward;
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
            self.UpdateAimDirection();
            // 读取跳跃输入
            self.JumpPressed = Input.GetKey(self.JumpKey);
            // 读取攻击输入（鼠标左键按下）
            self.AttackPressed = Input.GetMouseButtonDown(0);
        }

        /// <summary>
        /// 获取当前移动方向
        /// </summary>
        public static Vector3 GetMoveDirection(this InputComponent self)
        {
            return self.MoveDirection;
        }
        
        public static void UpdateAimDirection(this InputComponent self)
        {
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");

            Vector3 dir = new Vector3(x, 0f, z);

            if (dir.sqrMagnitude > 0.001f)
            {
                self.LastAimDirection = dir.normalized;
            }
        }

        public static Vector3 GetAimDirection(this InputComponent self, Transform owner)
        {
            if (self.LastAimDirection.sqrMagnitude < 0.001f)
            {
                return owner.forward;
            }

            return self.LastAimDirection;
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
        
        /// <summary>
        /// 检查是否有攻击请求
        /// </summary>
        public static bool HasAttackRequest(this InputComponent self)
        {
            return self.AttackPressed;
        }
    }
}

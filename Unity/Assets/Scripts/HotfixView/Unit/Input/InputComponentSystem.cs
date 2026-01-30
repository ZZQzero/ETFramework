using UnityEngine;

namespace ET
{
    [EntitySystemOf(typeof(InputComponent))]
    [FriendOf(typeof(InputComponent))]
    public static partial class InputComponentSystem
    {
        [EntitySystem]
        private static void Awake(this InputComponent self)
        {
            // 初始化最后移动方向为角色初始朝向（如果能拿到）或世界正前
            var gameObjectComponent = self.GetParent<Unit>().GetComponent<GameObjectComponent>();
            if (gameObjectComponent != null && gameObjectComponent.Transform != null)
            {
                self.LastMoveInput = gameObjectComponent.Transform.forward;
            }
            else
            {
                self.LastMoveInput = Vector3.forward;
            }
        }

        [EntitySystem]
        private static void Update(this InputComponent self)
        {
            if (!self.EnableInput)
            {
                self.MoveInput = Vector3.zero;
                self.JumpPending = false;
                self.AttackPending = false;
                return;
            }
            
            // 1. 统一轴采样，避免重复调用 Input.GetAxisRaw
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            self.MoveInput = new Vector3(horizontal, 0f, vertical);

            // 2. 更新最后有效移动方向（用于驱动层计算朝向意图）
            if (self.MoveInput.sqrMagnitude > 0.001f)
            {
                self.LastMoveInput = self.MoveInput.normalized;
            }

            // 3. 脉冲信号累加（Pending 模式）：
            // 只要本帧按下了，就标记为 Pending，直到驱动层调用 Consume
            if (Input.GetKey(self.JumpKey))
            {
                self.JumpPending = true;
            }

            if (Input.GetMouseButtonDown(self.AttackMouseButton))
            {
                self.AttackPending = true;
            }
        }

        #region 外部接口（由 Driver 层消费）

        /// <summary>
        /// 获取当前移动输入向量
        /// </summary>
        public static Vector3 GetMoveInput(this InputComponent self)
        {
            return self.MoveInput;
        }

        /// <summary>
        /// 消费跳跃请求
        /// </summary>
        public static bool ConsumeJumpRequest(this InputComponent self)
        {
            if (!self.JumpPending) return false;
            self.JumpPending = false;
            return true;
        }

        /// <summary>
        /// 消费攻击请求
        /// </summary>
        public static bool ConsumeAttackRequest(this InputComponent self)
        {
            if (!self.AttackPending) return false;
            self.AttackPending = false;
            return true;
        }

        /// <summary>
        /// 获取最后一次有效的移动方向（用于朝向/瞄准）
        /// </summary>
        public static Vector3 GetLastMoveInput(this InputComponent self)
        {
            return self.LastMoveInput;
        }

        #endregion

        #region 兼容性/状态查询

        public static bool HasMoveInput(this InputComponent self)
        {
            return self.MoveInput.sqrMagnitude > 0.01f;
        }

        #endregion
    }
}

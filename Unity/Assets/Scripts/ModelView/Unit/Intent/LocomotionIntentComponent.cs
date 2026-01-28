using UnityEngine;

namespace ET
{
    [System.Flags]
    public enum ActionCapabilities : byte
    {
        None = 0,
        Move = 1 << 0,
        Rotate = 1 << 1,
        Attack = 1 << 2,
        Jump = 1 << 3,
        All = Move | Rotate | Attack | Jump
    }

    /// <summary>
    /// 移动意图（商业级解耦层）
    /// - 由上层 Driver（玩家输入/AI/回放/网络）写入
    /// - 由底层 Motor（CharacterControllerComponentSystem）消费执行
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class LocomotionIntentComponent : Entity, IAwake
    {
        /// <summary>
        /// 当前实体的行为能力权限
        /// </summary>
        public ActionCapabilities Capabilities = ActionCapabilities.All;

        /// <summary>
        /// 期望移动方向（世界空间，XZ 平面，已归一化）
        /// </summary>
        public Vector3 MoveDirection;

        /// <summary>
        /// 期望朝向（世界空间，XZ 平面，已归一化）。
        /// 若为零向量，Motor 可回退用 MoveDirection 或保持当前朝向。
        /// </summary>
        public Vector3 FaceDirection;

        /// <summary>
        /// 外部冲量速度（由受击、爆炸等注入，由 Motor 每一帧消费合成）。
        /// 注意：这通常是一个瞬时速度，Motor 消费后不会自动清理，
        /// 它的生命周期由注入源（如 HitReactionComponent）维护。
        /// </summary>
        public Vector3 ExternalImpulse;

        /// <summary>
        /// 跳跃请求（“边沿触发”：被消费后会清除）
        /// </summary>
        public bool JumpRequested;

        public void ClearFrameInputs()
        {
            // 连续量不清理（由 Driver 每帧刷新）；边沿量消费后清理。
        }

        public bool ConsumeJumpRequest()
        {
            if (!this.JumpRequested)
            {
                return false;
            }

            this.JumpRequested = false;
            return true;
        }
    }
}
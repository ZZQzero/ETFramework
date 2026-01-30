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
        /// 基础行为能力权限（单位天生能做什么，由配置或初始化设定）
        /// </summary>
        public ActionCapabilities BaseCapabilities = ActionCapabilities.All;

        // --- 屏蔽计数器（引用计数，多源锁定核心） ---
        public int MoveInhibitors;
        public int RotateInhibitors;
        public int AttackInhibitors;
        public int JumpInhibitors;

        // --- 逻辑判断接口（执行层与驱动层应使用这些接口） ---
        public bool IsMoveAllowed => BaseCapabilities.HasFlag(ActionCapabilities.Move) && MoveInhibitors == 0;
        public bool IsRotateAllowed => BaseCapabilities.HasFlag(ActionCapabilities.Rotate) && RotateInhibitors == 0;
        public bool IsAttackAllowed => BaseCapabilities.HasFlag(ActionCapabilities.Attack) && AttackInhibitors == 0;
        public bool IsJumpAllowed => BaseCapabilities.HasFlag(ActionCapabilities.Jump) && JumpInhibitors == 0;

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
        /// 外部水平目标速度（由受击位移等注入，XZ 平面）。
        /// 在位移禁用状态下，Motor 会直接跟随此速度以保证位移精确度。
        /// </summary>
        public Vector2 ExternalTargetVelocity;

        /// <summary>
        /// 外部 3D 瞬时冲量（由爆炸、击飞、跳跃等注入）。
        /// 这是一个“增量”，Motor 消费一次后立即归零。
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
using UnityEngine;

namespace ET
{
    /// <summary>
    /// 输入组件
    /// 职责：负责原始输入数据的采集与缓存。
    /// 采用“Pending”模式处理脉冲信号（跳跃、攻击），确保驱动层能 100% 消费，不丢帧。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class InputComponent : Entity, IAwake, IUpdate
    {
        /// <summary>
        /// 是否启用输入读取
        /// </summary>
        public bool EnableInput = true;

        /// <summary>
        /// 原始移动输入向量（由 Unity Axis 采样得到）
        /// </summary>
        public Vector3 MoveInput;

        /// <summary>
        /// 最后一次有效的移动输入方向（归一化，用于辅助瞄准/朝向）
        /// </summary>
        public Vector3 LastMoveInput;

        /// <summary>
        /// 跳跃请求（Pending 模式：一旦按下则保持为 true，直到被驱动层消费）
        /// </summary>
        public bool JumpPending;

        /// <summary>
        /// 攻击请求（Pending 模式：一旦按下则保持为 true，直到被驱动层消费）
        /// </summary>
        public bool AttackPending;

        /// <summary>
        /// 跳跃键配置（硬编码暂存，后续可迁移至配置）
        /// </summary>
        public KeyCode JumpKey = KeyCode.Space;
        
        /// <summary>
        /// 攻击键配置（硬编码暂存，鼠标左键）
        /// </summary>
        public int AttackMouseButton = 0;
    }
}
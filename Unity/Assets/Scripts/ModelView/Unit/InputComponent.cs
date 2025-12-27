using UnityEngine;

namespace ET
{
    /// <summary>
    /// 输入组件（简洁版本）
    /// 只负责基本的输入读取和缓存
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class InputComponent : Entity, IAwake, IUpdate
    {
        /// <summary>
        /// 是否启用输入读取
        /// </summary>
        public bool EnableInput = true;
        /// <summary>
        /// 移动方向（归一化向量，XZ平面）
        /// </summary>
        public Vector3 MoveDirection;
        /// <summary>
        /// 是否按下跳跃键
        /// </summary>
        public bool JumpPressed;
        /// <summary>
        /// 跳跃键配置
        /// </summary>
        public KeyCode JumpKey = KeyCode.Space;
    }
}

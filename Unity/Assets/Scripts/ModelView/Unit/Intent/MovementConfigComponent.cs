using UnityEngine;

namespace ET
{
    /// <summary>
    /// 运动配置（最终执行参数）
    /// - 挂在 Unit 上，由上层组装（表/数值/默认值/BUFF）
    /// - Motor（CharacterControllerComponentSystem）只消费，不关心数据来源
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public sealed class MovementConfigComponent : Entity, IAwake<string>
    {
        /// <summary>水平移动速度（m/s，XZ）</summary>
        public float MoveSpeed = 5f;

        /// <summary>加速度（m/s²）：从当前速度逼近目标速度的快慢</summary>
        public float Acceleration = 20f;

        /// <summary>减速度（m/s²）：无输入或禁用移动时的减速快慢</summary>
        public float Deceleration = 25f;

        /// <summary>旋转速度（deg/s）：朝向目标方向的转向快慢</summary>
        public float RotationSpeed = 720f;

        /// <summary>重力加速度（m/s²）</summary>
        public float Gravity = 9.81f;

        /// <summary>跳跃初速度（m/s，向上）</summary>
        public float JumpForce = 10f;

        /// <summary>重力倍率：最终重力 = Gravity * GravityMultiplier</summary>
        public float GravityMultiplier = 1.5f;

        /// <summary>地面检测配置资产（可选，优先使用）。</summary>
        public GroundDetectorConfigAsset GroundConfigAsset;
    }
}
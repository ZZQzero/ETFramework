using UnityEngine;

namespace ET
{
    /// <summary>
    /// 用于接收Unity AnimationEvent的MonoBehaviour组件
    /// 动画剪辑中的事件会调用这个脚本中对应的方法
    /// </summary>
    public class AttackEventReceiver : MonoBehaviour
    {
        /// <summary>
        /// 接收MeleeAttackStart动画事件
        /// 这个方法会被动画剪辑中的AnimationEvent调用
        /// </summary>
        /// <param name="throwing">可选的参数，由动画事件定义</param>
        private void MeleeAttackStart(int throwing = 0)
        {
            // 这里可以添加攻击开始的逻辑
            // 如果需要与ET框架通信，可以通过EventSystem或其他方式
            Log.Info($"MeleeAttackStart事件触发，参数: {throwing}");
        }

        /// <summary>
        /// 接收MeleeAttackEnd动画事件
        /// </summary>
        private void MeleeAttackEnd()
        {
            // 这里可以添加攻击结束的逻辑
            Log.Info("MeleeAttackEnd事件触发");
        }
    }
}
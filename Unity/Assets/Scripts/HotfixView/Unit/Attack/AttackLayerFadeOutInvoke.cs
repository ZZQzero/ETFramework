using System;

namespace ET
{
    /// <summary>
    /// 攻击层淡出处理器（方案A：Layer1 Attack 覆盖，Recovery 后延迟淡出回到 Layer0 Move/Idle）
    /// </summary>
    [Invoke(TimerInvokeType.AttackLayerFadeOut)]
    public class AttackLayerFadeOutInvoke : ATimer<AttackComponent>
    {
        protected override void Run(AttackComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.OnAttackLayerFadeOutTimer();
        }
    }
}
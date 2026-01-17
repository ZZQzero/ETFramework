using System;

namespace ET
{
    /// <summary>
    /// 攻击层淡出处理器
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
using System;

namespace ET
{
    /// <summary>
    /// 攻击连击超时处理器
    /// </summary>
    [Invoke(TimerInvokeType.AttackComboTimeout)]
    public class AttackComboTimeoutInvoke: ATimer<AttackComponent>
    {
        protected override void Run(AttackComponent self)
        {
            if (self == null || self.IsDisposed)
                return;

            self.OnComboTimeout();
            
            //self.ExitAttackState();
            
        }
    }
}
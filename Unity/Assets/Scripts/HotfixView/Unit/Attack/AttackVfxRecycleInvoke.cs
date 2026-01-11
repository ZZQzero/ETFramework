using System;
using GameUI;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// 不跟随特效回收数据（传递给定时器回调）
    /// </summary>
    public class AttackVfxRecycleArgs
    {
        public AttackComponent Component;
        public long TimerId;
        public GameObject Instance;
    }

    /// <summary>
    /// 不跟随特效回收处理器。
    /// 当 FollowTarget=false 的特效播放完成后，自动回收到对象池。
    /// </summary>
    [Invoke(TimerInvokeType.AttackVfxRecycle)]
    public class AttackVfxRecycleInvoke : AInvokeHandler<TimerCallback>
    {
        public override void Handle(TimerCallback callback)
        {
            var args = callback.Args as AttackVfxRecycleArgs;
            if (args == null)
            {
                return;
            }

            var self = args.Component;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            // 从字典中移除定时器ID（如果还存在）
            if (self.NonFollowVfxTimers != null && self.NonFollowVfxTimers.ContainsKey(args.TimerId))
            {
                self.NonFollowVfxTimers.Remove(args.TimerId);
            }

            // 回收特效实例到对象池
            if (args.Instance != null)
            {
                GameObjectPool.Instance.ReleaseObject(args.Instance, PoolType.Effect);
            }
        }
    }
}
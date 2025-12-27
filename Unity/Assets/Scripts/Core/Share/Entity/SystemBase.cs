using System;
using System.Runtime.CompilerServices;

namespace ET
{
    /// <summary>
    /// System统一泛型基类
    /// 自动实现ISystemType
    /// </summary>
    /// <typeparam name="TEntity">Entity类型（如TimerComponent）</typeparam>
    /// <typeparam name="TSystem">System接口类型（如IAwakeSystem）</typeparam>
    public abstract class SystemBase<TEntity, TSystem> : SystemObject, ISystemType where TEntity : Entity where TSystem : ISystemType
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Type ISystemType.Type() => typeof(TEntity);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Type ISystemType.SystemType() => typeof(TSystem);
    }
}
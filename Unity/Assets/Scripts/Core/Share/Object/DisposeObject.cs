using System;
using System.ComponentModel;

namespace ET
{
    public abstract class DisposeObject: IDisposable
    {
        public virtual void Dispose()
        {
        }
        

    }

    public interface IPool: IDisposable
    {
        bool IsFromPool
        {
            get;
            set;
        }
        
        void Reset(); // 回池时清理状态
    }
}
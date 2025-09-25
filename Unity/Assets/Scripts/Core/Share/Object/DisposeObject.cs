using System;
using System.ComponentModel;

namespace ET
{
    public interface IPool : IDisposable
    {
        bool IsFromPool
        {
            get;
            set;
        }

        
        // 回池时清理状态
        new void Dispose()
        {
        }
    }
}
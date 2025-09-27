using System;
using System.ComponentModel;

namespace ET
{
    public interface IPool : IDisposable
    {
        bool IsFromPool { get; set; }
        new void Dispose();
    }
}
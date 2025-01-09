

using System;

namespace ET
{
    public interface ETInvoke
    {
        public void Execute();
        
        public Action RegisterAction { get; set; }
    }
}

using System;
using UnityEngine;

namespace ET
{
    
    public static class InvokeHandle
    {
        [StaticField]
        public static ETInvoke invokeHandle;
    }
    
    public class RegisterInvoke : ETInvoke
    {
        public void Execute()
        {
            RegisterAction?.Invoke();
            Debug.LogError("RegisterInvoke");
        }

        public Action RegisterAction { get; set; }
    }
}
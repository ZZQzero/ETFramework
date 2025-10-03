using System;
using System.Collections.Generic;

namespace ET
{
    public class ListComponent<T>: List<T>, IPool
    {
        public static ListComponent<T> Create()
        {
            return ObjectPool.Fetch<ListComponent<T>>();
        }

        public bool IsFromPool { get; set; }

        public void Dispose()
        {
            Clear();
        }
    }
}
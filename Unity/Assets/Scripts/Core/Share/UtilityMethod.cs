using System.Threading;

namespace ET
{
    // 全局静态计数器
    internal static class TypeIdCounter
    {
        internal static long NextId = 0;
    }

    // 泛型静态类，用于给每个类型分配唯一运行时 ID
    public static class TypeId<T>
    {
        public static readonly long Id = Interlocked.Increment(ref TypeIdCounter.NextId);
    }
    
}
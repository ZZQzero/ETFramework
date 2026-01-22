using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    [EntitySystemOf(typeof(ObjectWait))]
    public static partial class ObjectWaitSystem
    {
        [EntitySystem]
        private static void Awake(this ObjectWait self)
        {
            self.tcss.Clear();
        }
        
        [EntitySystem]
        private static void Destroy(this ObjectWait self)
        {
            foreach (object v in self.tcss.Values.ToArray())
            {
                ((IDestroyRun) v).SetResult();
            }
            self.tcss.Clear();
        }
        
        public static async ETTask<T> Wait<T>(this ObjectWait self) where T : struct, IWaitType
        {
            Type type = typeof(T);
            
            ResultCallback<T> tcs = new ResultCallback<T>();
            // 先捕获 Task 引用：避免在 Wait() 内部 await 期间被 Notify 提前 SetResult()，
            // ResultCallback 会把内部 tcs 置空，导致后续 await tcs.Task 触发 NullReferenceException。
            ETTask<T> task = tcs.Task;
            
            if (!self.tcss.TryAdd(type, tcs))
            {
                Log.Error($"ObjectWait 重复等待同一类型: {type.FullName}");
                return new T { Error = WaitTypeError.Cancel };
            }
            
            // 先注册，再 await，避免注册空窗导致 Notify 丢失

            void CancelAction()
            {
                self.Notify(new T() { Error = WaitTypeError.Cancel });
            }
            
            T ret;
            try
            {
                // 快路径：如果已经完成（Notify 抢跑/缓存命中导致），不要再去等待 Context 注入
                if (task.IsCompleted)
                {
                    ret = await task;
                    return ret;
                }

                // 仅在确实需要“可取消”时，才去等待拿到 Context Token。
                // 这里必须 await，否则会创建一个 ContextTask（来自对象池）但没人消费，造成泄漏/池占用。
                ETCancellationToken cancellationToken = await ETTaskHelper.GetContextAsync<ETCancellationToken>();
                if (cancellationToken != null && !cancellationToken.IsDispose())
                {
                    cancellationToken.Add(CancelAction);
                }
                
                try
                {
                    ret = await task;
                }
                finally
                {
                    if (cancellationToken != null && !cancellationToken.IsDispose())
                    {
                        cancellationToken.Remove(CancelAction);
                    }
                }
            }
            finally
            {
                // 如果等待异常/取消导致未被 Notify 消费，清理残留 waiter，避免泄漏与下次重复 Wait 崩溃
                if (self.tcss.TryGetValue(type, out object cur) && ReferenceEquals(cur, tcs))
                {
                    self.tcss.Remove(type);
                }
            }
            return ret;
        }

        public static void Notify<T>(this ObjectWait self, T obj) where T : struct, IWaitType
        {
            Type type = typeof (T);
            if (!self.tcss.TryGetValue(type, out object tcs))
            {
                Log.Error($"ObjectWait.Notify 未命中 waiter: {type.FullName}");
                return;
            }

            self.tcss.Remove(type);
            ((ResultCallback<T>) tcs).SetResult(obj);
        }
    }
}

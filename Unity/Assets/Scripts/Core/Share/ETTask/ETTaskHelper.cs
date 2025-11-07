using System;
using System.Collections.Generic;
using System.Threading;

namespace ET
{
    public static class ETTaskHelper
    {
        public static async ETTask<T> GetContextAsync<T>() where T: class
        {
            ETTask<object> tcs = ETTask<object>.Create(true);
            tcs.TaskType = TaskType.ContextTask;
            object ret = await tcs;
            if (ret == null)
            {
                return null;
            }
            return (T)ret;
        }
        
        public static bool IsCancel(this ETCancellationToken self)
        {
            if (self == null)
            {
                return false;
            }
            return self.IsDispose();
        }
        
        private class CoroutineBlocker
        {
            private int count;

            private ETTask tcs;

            public CoroutineBlocker(int count)
            {
                this.count = count;
            }
            
            public async ETTask RunSubCoroutineAsync(ETTask task)
            {
                try
                {
                    await task;
                }
                finally
                {
                    int newCount = Interlocked.Decrement(ref this.count);
                    if (newCount <= 0)
                    {
                        // 使用原子交换，只有一个线程能成功获得非 null 的 tcs
                        ETTask t = Interlocked.Exchange(ref this.tcs, null);
                        if (t != null)
                        {
                            t.SetResult();
                        }
                    }
                }
            }

            public async ETTask WaitAsync()
            {
                if (this.count <= 0)
                {
                    return;
                }
                this.tcs = ETTask.Create(true);
                await tcs;
            }
        }

        private class CoroutineBlocker<T>
        {
            private int count;
            private T[] results;
            private ETTask<T[]> tcs;

            public CoroutineBlocker(int count)
            {
                this.count = count;
                this.results = new T[count];
            }
            
            public async ETTask RunSubCoroutineAsync(ETTask<T> task, int index)
            {
                try
                {
                    T result = await task;
                    this.results[index] = result;
                }
                finally
                {
                    int newCount = Interlocked.Decrement(ref this.count);
                    if (newCount <= 0)
                    {
                        // 使用原子交换，只有一个线程能成功获得非 null 的 tcs
                        ETTask<T[]> t = Interlocked.Exchange(ref this.tcs, null);
                        if (t != null)
                        {
                            t.SetResult(this.results);
                        }
                    }
                }
            }

            public async ETTask<T[]> WaitAsync()
            {
                if (this.count <= 0)
                {
                    return this.results;
                }
                this.tcs = ETTask<T[]>.Create(true);
                return await this.tcs;
            }
        }

        public static async ETTask WaitAny(List<ETTask> tasks)
        {
            if (tasks.Count == 0)
            {
                return;
            }

            CoroutineBlocker coroutineBlocker = new CoroutineBlocker(1);

            foreach (ETTask task in tasks)
            {
                coroutineBlocker.RunSubCoroutineAsync(task).NoContext();
            }

            await coroutineBlocker.WaitAsync();
        }

        public static async ETTask WaitAny(ETTask[] tasks)
        {
            if (tasks.Length == 0)
            {
                return;
            }

            CoroutineBlocker coroutineBlocker = new CoroutineBlocker(1);

            foreach (ETTask task in tasks)
            {
                coroutineBlocker.RunSubCoroutineAsync(task).NoContext();
            }

            await coroutineBlocker.WaitAsync();
        }

        public static async ETTask WaitAll(ETTask[] tasks)
        {
            if (tasks.Length == 0)
            {
                return;
            }

            CoroutineBlocker coroutineBlocker = new CoroutineBlocker(tasks.Length);

            foreach (ETTask task in tasks)
            {
                coroutineBlocker.RunSubCoroutineAsync(task).NoContext();
            }

            await coroutineBlocker.WaitAsync();
        }

        public static async ETTask WaitAll(List<ETTask> tasks)
        {
            if (tasks.Count == 0)
            {
                return;
            }

            CoroutineBlocker coroutineBlocker = new CoroutineBlocker(tasks.Count);

            foreach (ETTask task in tasks)
            {
                coroutineBlocker.RunSubCoroutineAsync(task).NoContext();
            }

            await coroutineBlocker.WaitAsync();
        }

        /// <summary>
        /// 并行等待所有ETTask&lt;T&gt;任务完成，返回结果数组
        /// </summary>
        public static async ETTask<T[]> WaitAll<T>(ETTask<T>[] tasks)
        {
            if (tasks.Length == 0)
            {
                return Array.Empty<T>();
            }

            CoroutineBlocker<T> coroutineBlocker = new CoroutineBlocker<T>(tasks.Length);

            for (int i = 0; i < tasks.Length; i++)
            {
                int index = i; // 闭包捕获
                coroutineBlocker.RunSubCoroutineAsync(tasks[index], index).NoContext();
            }

            return await coroutineBlocker.WaitAsync();
        }

        /// <summary>
        /// 并行等待所有ETTask任务完成，返回结果数组
        /// </summary>
        public static async ETTask<T[]> WaitAll<T>(List<ETTask<T>> tasks)
        {
            if (tasks.Count == 0)
            {
                return Array.Empty<T>();
            }

            CoroutineBlocker<T> coroutineBlocker = new CoroutineBlocker<T>(tasks.Count);

            for (int i = 0; i < tasks.Count; i++)
            {
                int index = i; // 闭包捕获
                coroutineBlocker.RunSubCoroutineAsync(tasks[index], index).NoContext();
            }

            return await coroutineBlocker.WaitAsync();
        }
    }
}
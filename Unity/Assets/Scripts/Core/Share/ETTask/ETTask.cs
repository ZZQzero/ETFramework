using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace ET
{
    internal static class IETTaskExtension
    {
        internal static void SetContext(this IETTask task, object context)
        {
            while (true)
            {
                if (task.TaskType == TaskType.ContextTask)
                {
                    ((ETTask<object>)task).SetResult(context);
                    break;
                }

                // cancellationToken传下去
                task.TaskType = TaskType.WithContext;
                object child = task.Context;
                task.Context = context;
                task = child as IETTask;
                if (task == null)
                {
                    break;
                }
                //// 传递到WithContext为止，因为可能这一层设置了新的context
                if (task.TaskType == TaskType.WithContext)
                {
                    break;
                }
            }
        }
    }
    
    public enum TaskType: byte
    {
        Common,
        WithContext,
        ContextTask,
    }
    
    public interface IETTask
    {
        public TaskType TaskType { get; set; }
        public object Context { get; set; }
    }
    
    [AsyncMethodBuilder(typeof (ETAsyncTaskMethodBuilder))]
    public class ETTask: ICriticalNotifyCompletion, IETTask
    {
        [StaticField]
        public static Action<Exception> ExceptionHandler;

        [StaticField]
        private static ETTask completedTask;
        
        [StaticField]
        public static ETTask CompletedTask
        {
            get
            {
                return completedTask ??= new ETTask() { state = AwaiterStatus.Succeeded };
            }
        }

        /// <summary>
        /// ETTask对象池使用警告（重要！）
        /// 
        /// 对象池已优化为ThreadLocal隔离，保证Fiber隔离性和线程安全。
        /// 但对象池复用机制的固有危险依然存在，使用时必须注意：
        /// 
        ///     await之后绝对不能再操作ETTask
        ///    - await时GetResult()会自动Recycle，对象回收到池中
        ///    - 回收后对象可能被其他地方获取并使用
        ///    - 继续操作会修改到别人的Task，导致逻辑错乱或崩溃
        /// 
        ///     不要将对象池的ETTask存储到字段中
        ///    - await后对象已回收，字段持有的是已回收的对象引用
        ///    - 后续操作字段会导致Use After Recycle问题
        /// 
        ///     避免多次调用SetResult/SetException
        ///    - Task状态机只能转换一次（Pending → Succeeded/Faulted）
        ///    - 重复调用会抛出InvalidOperationException
        ///    - 需要防重复时使用Interlocked.Exchange模式
        /// 
        /// 安全使用模式：
        /// 1. 创建后立即await，不存储引用：await ETTask.Create(true);
        /// 2. 局部变量使用，await后不再访问该变量
        /// 3. 需要持有引用时，使用Interlocked.Exchange防止重复操作
        /// 4. await后立即清空引用：task = null
        /// </summary>
        [DebuggerHidden]
        public static ETTask Create(bool fromPool = false)
        {
            if (!fromPool)
            {
                return new ETTask();
            }
            return ETTaskPool.Rent();
        }

        [DebuggerHidden]
        private void Recycle()
        {
            if (!this.fromPool)
            {
                return;
            }
            
            ETTaskPool.Return(this);
        }

        /// <summary>
        /// 重置状态（内部方法，由对象池调用）
        /// </summary>
        [DebuggerHidden]
        internal void ResetState()
        {
            this.state = AwaiterStatus.Pending;
            this.callback = null;
            this.Context = null;
            this.TaskType = TaskType.Common;
        }

        private bool fromPool;
        private AwaiterStatus state;
        private object callback; // Action or ExceptionDispatchInfo
        
        /// <summary>
        /// 池中标记：防止重复归还
        /// </summary>
        internal bool IsPooled;

        [DebuggerHidden]
        private ETTask()
        {
            this.TaskType = TaskType.Common;
        }
        
        [DebuggerHidden]
        internal ETTask(bool fromPool)
        {
            this.fromPool = fromPool;
            this.TaskType = TaskType.Common;
        }
        
        [DebuggerHidden]
        private async ETVoid InnerCoroutine()
        {
            await this;
        }

        [DebuggerHidden]
        public void NoContext()
        {
            this.SetContext(null);
            InnerCoroutine().Coroutine();
        }
        
        [DebuggerHidden]
        public void WithContext(object context)
        {
            this.SetContext(context);
            InnerCoroutine().Coroutine();
        }
        
        /// <summary>
        /// 在await的同时可以换一个新的上下文
        /// </summary>
        [DebuggerHidden]
        public async ETTask NewContext(object context)
        {
            this.SetContext(context);
            await this;
        }

        [DebuggerHidden]
        public ETTask GetAwaiter()
        {
            return this;
        }

        
        public bool IsCompleted
        {
            [DebuggerHidden]
            get => this.state != AwaiterStatus.Pending;
        }

        [DebuggerHidden]
        public void UnsafeOnCompleted(Action action)
        {
            if (this.state != AwaiterStatus.Pending)
            {
                action?.Invoke();
                return;
            }

            this.callback = action;
        }

        [DebuggerHidden]
        public void OnCompleted(Action action)
        {
            this.UnsafeOnCompleted(action);
        }

        [DebuggerHidden]
        public void GetResult()
        {
            switch (this.state)
            {
                case AwaiterStatus.Succeeded:
                    this.Recycle();
                    break;
                case AwaiterStatus.Faulted:
                    ExceptionDispatchInfo c = this.callback as ExceptionDispatchInfo;
                    this.callback = null;
                    this.Recycle();
                    c?.Throw();
                    break;
                default:
                    throw new NotSupportedException("ETTask does not allow call GetResult directly when task not completed. Please use 'await'.");
            }
        }

        [DebuggerHidden]
        public void SetResult()
        {
            if (this.state != AwaiterStatus.Pending)
            {
                throw new InvalidOperationException("TaskT_TransitionToFinal_AlreadyCompleted");
            }

            this.state = AwaiterStatus.Succeeded;

            Action c = this.callback as Action;
            this.callback = null;
            c?.Invoke();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public void SetException(Exception e)
        {
            if (this.state != AwaiterStatus.Pending)
            {
                throw new InvalidOperationException("TaskT_TransitionToFinal_AlreadyCompleted");
            }

            this.state = AwaiterStatus.Faulted;

            Action c = this.callback as Action;
            this.callback = ExceptionDispatchInfo.Capture(e);
            c?.Invoke();
        }

        public TaskType TaskType { get; set; }
        public object Context { get; set; }
    }

    [AsyncMethodBuilder(typeof (ETAsyncTaskMethodBuilder<>))]
    public class ETTask<T>: ICriticalNotifyCompletion, IETTask
    {
        /// <summary>
        /// ETTask对象池使用警告（同ETTask，请务必遵守！）
        /// 
        /// await之后绝对不能再操作ETTask
        /// 不要将对象池的ETTask存储到字段中
        /// 避免多次调用SetResult/SetException
        /// </summary>
        [DebuggerHidden]
        public static ETTask<T> Create(bool fromPool = false)
        {
            if (!fromPool)
            {
                return new ETTask<T>();
            }
            
            return ETTaskPool<T>.Rent();
        }
        
        [DebuggerHidden]
        private void Recycle()
        {
            if (!this.fromPool)
            {
                return;
            }
            
            ETTaskPool<T>.Return(this);
        }

        /// <summary>
        /// 重置状态（内部方法，由对象池调用）
        /// </summary>
        [DebuggerHidden]
        internal void ResetState()
        {
            this.callback = null;
            this.value = default;
            this.state = AwaiterStatus.Pending;
            this.Context = null;
            this.TaskType = TaskType.Common;
        }

        private bool fromPool;
        private AwaiterStatus state;
        private T value;
        private object callback; // Action or ExceptionDispatchInfo
        
        /// <summary>
        /// 池中标记：防止重复归还（仅供ETTaskPool使用）
        /// </summary>
        internal bool IsPooled;

        [DebuggerHidden]
        private ETTask()
        {
            this.TaskType = TaskType.Common;
        }
        
        [DebuggerHidden]
        internal ETTask(bool fromPool)
        {
            this.fromPool = fromPool;
            this.TaskType = TaskType.Common;
        }

        [DebuggerHidden]
        private async ETVoid InnerCoroutine()
        {
            await this;
        }

        [DebuggerHidden]
        public void NoContext()
        {
            this.SetContext(null);
            InnerCoroutine().Coroutine();
        }
        
        [DebuggerHidden]
        public void WithContext(object context)
        {
            this.SetContext(context);
            InnerCoroutine().Coroutine();
        }
        
        /// <summary>
        /// 在await的同时可以换一个新的cancellationToken
        /// </summary>
        [DebuggerHidden]
        public async ETTask<T> NewContext(object context)
        {
            this.SetContext(context);
            return await this;
        }

        [DebuggerHidden]
        public ETTask<T> GetAwaiter()
        {
            return this;
        }

        [DebuggerHidden]
        public T GetResult()
        {
            switch (this.state)
            {
                case AwaiterStatus.Succeeded:
                    T v = this.value;
                    this.Recycle();
                    return v;
                case AwaiterStatus.Faulted:
                    ExceptionDispatchInfo c = this.callback as ExceptionDispatchInfo;
                    this.callback = null;
                    this.Recycle();
                    c?.Throw();
                    return default;
                default:
                    throw new NotSupportedException("ETask does not allow call GetResult directly when task not completed. Please use 'await'.");
            }
        }
        
        public bool IsCompleted
        {
            [DebuggerHidden]
            get
            {
                return state != AwaiterStatus.Pending;
            }
        } 

        [DebuggerHidden]
        public void UnsafeOnCompleted(Action action)
        {
            if (this.state != AwaiterStatus.Pending)
            {
                action?.Invoke();
                return;
            }

            this.callback = action;
        }

        [DebuggerHidden]
        public void OnCompleted(Action action)
        {
            this.UnsafeOnCompleted(action);
        }

        [DebuggerHidden]
        public void SetResult(T result)
        {
            if (this.state != AwaiterStatus.Pending)
            {
                throw new InvalidOperationException("TaskT_TransitionToFinal_AlreadyCompleted");
            }

            this.state = AwaiterStatus.Succeeded;

            this.value = result;

            Action c = this.callback as Action;
            this.callback = null;
            c?.Invoke();
        }
        
        [DebuggerHidden]
        public void SetException(Exception e)
        {
            if (this.state != AwaiterStatus.Pending)
            {
                throw new InvalidOperationException("TaskT_TransitionToFinal_AlreadyCompleted");
            }

            this.state = AwaiterStatus.Faulted;

            Action c = this.callback as Action;
            this.callback = ExceptionDispatchInfo.Capture(e);
            c?.Invoke();
        }

        public TaskType TaskType { get; set; }
        public object Context { get; set; }
    }
}
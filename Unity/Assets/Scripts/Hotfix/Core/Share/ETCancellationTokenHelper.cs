using System;

namespace ET
{
    public static class ETCancellationTokenHelper
    {
        private static async ETTask TimeoutAsync(this ETCancellationToken self,Scene scene, long afterTimeCancel)
        {
            if (afterTimeCancel <= 0)
            {
                return;
            }
            
            if (self.IsCancel())
            {
                return;
            }

            // 这里用了Fiber.Instance是因为我知道有什么后果, 你们千万不能乱用这个Fiber.Instance
            await scene.GetComponent<TimerComponent>().WaitAsync(afterTimeCancel);
            if (self.IsCancel())
            {
                return;
            }
            self.Cancel();
        }
        
        /// <summary>
        /// 增加一个canceltoken，可以用新增的canceltoken取消协程，当然也可以用老的取消
        /// </summary>
        public static async ETTask AddCancel(this ETTask task, ETCancellationToken addCancelToken)
        {
            if (addCancelToken == null)
            {
                throw new Exception("add cancel token is null");
            }
            ETCancellationToken cancelToken = await ETTaskHelper.GetContextAsync<ETCancellationToken>();
            
            cancelToken?.Add(addCancelToken.Cancel);
            
            await task.NewContext(addCancelToken);
        }
        
        /// <summary>
        /// 增加一个canceltoken，可以用新增的canceltoken取消协程，当然也可以用老的取消
        /// </summary>
        public static async ETTask<T> AddCancel<T>(this ETTask<T> task, ETCancellationToken addCancelToken)
        {
            if (addCancelToken == null)
            {
                throw new Exception("add cancel token is null");
            }
            
            ETCancellationToken cancelToken = await ETTaskHelper.GetContextAsync<ETCancellationToken>();
            
            cancelToken?.Add(addCancelToken.Cancel);
            
            return await task.NewContext(addCancelToken);
        }
        
        public static async ETTask TimeoutAsync(this ETTask task,Scene scene,ETCancellationToken cancellationToken, long afterTimeCancel)
        {
            cancellationToken.TimeoutAsync(scene,afterTimeCancel).NoContext();
            await AddCancel(task, cancellationToken);
        }
        
        public static async ETTask<T> TimeoutAsync<T>(this ETTask<T> task,Scene scene,ETCancellationToken cancellationToken,long afterTimeCancel)
        {
            cancellationToken.TimeoutAsync(scene,afterTimeCancel).NoContext();
            return await AddCancel(task, cancellationToken);
        }
        
        public static async ETTask TimeoutAsync(this ETTask task, Scene scene,long afterTimeCancel)
        {
            ETCancellationToken cancellationToken = new();
            cancellationToken.TimeoutAsync(scene,afterTimeCancel).NoContext();
            await AddCancel(task, cancellationToken);
        }
        
        public static async ETTask<T> TimeoutAsync<T>(this ETTask<T> task,Scene scene,long afterTimeCancel)
        {
            ETCancellationToken cancellationToken = new();
            cancellationToken.TimeoutAsync(scene,afterTimeCancel).NoContext();
            return await AddCancel(task, cancellationToken);
        }
    }
}
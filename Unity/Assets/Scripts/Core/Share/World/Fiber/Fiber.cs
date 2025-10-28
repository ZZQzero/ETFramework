using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ET
{
    public static class FiberHelper
    {
        public static ActorId GetActorId(this Entity self)
        {
            Fiber root = self.Fiber();
            return new ActorId(root.Process, root.Id, self.InstanceId);
        }
    }
    
    public class Fiber: IDisposable
    {
        public int Id { get; }
        public int Zone { get; }
        public SchedulerType SchedulerType { get; }
        public Scene Root { get; }
        public Address Address => new(this.Process, this.Id);
        public int Process => Options.Instance.Process;
        public EntitySystem EntitySystem { get; }
        public Mailboxes Mailboxes { get; private set; }
        public ThreadSynchronizationContext ThreadSynchronizationContext { get; }
        public bool IsDisposed;
        private readonly ConcurrentQueue<ETTask> frameFinishTasks = new();

        public bool HasWork => this.frameFinishTasks.Count > 0 || this.ThreadSynchronizationContext.QueueCount > 0;
        
        internal Fiber(int id, int zone, int sceneType, string name,SchedulerType schedulerType)
        {
            this.Id = id;
            this.Zone = zone;
            SchedulerType = schedulerType;
            this.EntitySystem = new EntitySystem();
            this.Mailboxes = new Mailboxes();
            this.ThreadSynchronizationContext = new ThreadSynchronizationContext();
            Log.Info($"Fiber -->id:{id},process:{Process},sceneName:{SceneTypeSingleton.Instance.GetSceneName(sceneType)},schedulerType:{schedulerType}  name:{name}");
            this.Root = new Scene(this, id, 1, sceneType, name);
        }

        internal void Update()
        {
            try
            {
                this.EntitySystem.Publish(new UpdateEvent());
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
        
        internal void LateUpdate()
        {
            try
            {
                this.EntitySystem.Publish(new LateUpdateEvent());
                FrameFinishUpdate();
                this.ThreadSynchronizationContext.Update();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        public async ETTask WaitFrameFinish()
        {
            ETTask task = ETTask.Create(true);
            this.frameFinishTasks.Enqueue(task);
            await task;
        }

        private void FrameFinishUpdate()
        {
            while (this.frameFinishTasks.Count > 0)
            {
                if (this.frameFinishTasks.TryDequeue(out var task))
                {
                    task.SetResult();
                }
            }
        }

        public void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }
            this.IsDisposed = true;
            
            this.Root.Dispose();
        }
    }
}
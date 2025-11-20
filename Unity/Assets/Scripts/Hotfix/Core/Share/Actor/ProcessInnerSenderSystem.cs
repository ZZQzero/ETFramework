using System;

namespace ET
{
    [EntitySystemOf(typeof(ProcessInnerSender))]
    public static partial class ProcessInnerSenderSystem
    {
        [EntitySystem]
        private static void Destroy(this ProcessInnerSender self)
        {
            Fiber fiber = self.Fiber();
            MessageQueue.Instance.RemoveQueue(fiber.Id);
        }

        [EntitySystem]
        private static void Awake(this ProcessInnerSender self)
        {
            Fiber fiber = self.Fiber();
            MessageQueue.Instance.AddQueue(fiber.Id);
        }

        [EntitySystem]
        private static void Update(this ProcessInnerSender self)
        {
            self.list.Clear();
            Fiber fiber = self.Fiber();
            MessageQueue.Instance.Fetch(fiber.Id, 1000, self.list);

            foreach (MessageInfo actorMessageInfo in self.list)
            {
                self.HandleMessage(fiber, actorMessageInfo);
            }
        }

        private static void HandleMessage(this ProcessInnerSender self, Fiber fiber, in MessageInfo messageInfo)
        {
            if (messageInfo.MessageObject is IResponse response)
            {
                self.HandleIActorResponse(response);
                return;
            }

            ActorId actorId = messageInfo.ActorId;
            MessageObject message = messageInfo.MessageObject;

            Entity entity = self.Fiber().Mailboxes.Get(actorId.InstanceId);
            MailBoxComponent mailBoxComponent = entity as MailBoxComponent;
            if (mailBoxComponent == null)
            {
                Log.Warning($"actor not found mailbox, from: {actorId} current: {fiber.Address} {message}");
                if (message is IRequest request)
                {
                    IResponse resp = MessageHelper.CreateResponse(request.GetType(), request.RpcId, ErrorCode.ERR_NotFoundActor);
                    self.Reply(actorId.Address, resp);
                }
                return;
            }
            mailBoxComponent.Add(actorId.Address, message);
        }

        private static void HandleIActorResponse(this ProcessInnerSender self, IResponse response)
        {
            try
            {
                if (!self.requestCallback.Remove(response.RpcId, out MessageSenderStruct actorMessageSender))
                {
                    return;
                }
                Run(actorMessageSender, response);
            }
            finally
            {
                // Response 处理完成后回收
                (response as MessageObject)?.Dispose();
            }
        }
        
        private static void Run(MessageSenderStruct self, IResponse response)
        {
            if (response.Error == ErrorCode.ERR_MessageTimeout)
            {
                self.SetException(new RpcException(response.Error, $"Rpc error: request, 注意Actor消息超时，请注意查看是否死锁或者没有reply: actorId: {self.ActorId} {self.RequestType.FullName}, response: {response}"));
                return;
            }

            if (self.NeedException && ErrorCode.IsRpcNeedThrowException(response.Error))
            {
                self.SetException(new RpcException(response.Error, $"Rpc error: actorId: {self.ActorId} request: {self.RequestType.FullName}, response: {response}"));
                return;
            }

            self.SetResult(response);
        }
        
        public static void Reply(this ProcessInnerSender self, Address fromAddress, IResponse message)
        {
            self.SendInner(new ActorId(fromAddress, 0), (MessageObject)message);
        }

        public static void Send(this ProcessInnerSender self, ActorId actorId, IMessage message)
        {
            if (!self.SendInner(actorId, (MessageObject)message))
            {
                Log.Warning($"ProcessInnerSender.Send failed, target fiber not found: {actorId} {message.GetType().FullName}");
            }
        }

        private static bool SendInner(this ProcessInnerSender self, ActorId actorId, MessageObject message)
        {
            Fiber fiber = self.Fiber();
            
            // 如果发向同一个进程，则扔到消息队列中
            if (actorId.Process != fiber.Process)
            {
                throw new Exception($"actor inner process diff: {actorId.Process} {fiber.Process}");
            }
            
            return MessageQueue.Instance.Send(fiber.Address, actorId, message);
        }

        private static int GetRpcId(this ProcessInnerSender self)
        {
            return ++self.RpcId;
        }

        public static async ETTask<IResponse> Call(
                this ProcessInnerSender self,
                ActorId actorId,
                IRequest request,
                bool needException = true
        )
        {
            int rpcId = self.GetRpcId();
            request.RpcId = rpcId;
            
            if (actorId == default)
            {
                throw new Exception($"actor id is 0: {request}");
            }
            
            Fiber fiber = self.Fiber();
            if (fiber.Process != actorId.Process)
            {
                throw new Exception($"actor inner process diff: {actorId.Process} {fiber.Process}");
            }

            Type requestType = request.GetType();
            
            IResponse response;
            if (!self.SendInner(actorId, (MessageObject)request))  // 纤程不存在
            {
                response = MessageHelper.CreateResponse(requestType, rpcId, ErrorCode.ERR_NotFoundActor);
                return response;
            }
            
            MessageSenderStruct messageSenderStruct = new(actorId, requestType, needException);
            self.requestCallback.Add(rpcId, messageSenderStruct);
            
            // 创建取消令牌，用于取消超时任务
            ETCancellationToken timeoutCancelToken = new();
            
            async ETTask Timeout()
            {
                // 使用 NewContext 传递取消令牌，这样可以被外部取消
                // 注意：Cancel 时会通过 SetResult 正常完成，不会抛异常
                await fiber.Root.GetComponent<TimerComponent>().WaitAsync(ProcessInnerSender.TIMEOUT_TIME).NewContext(timeoutCancelToken);

                // 尝试移除回调，如果返回 false 说明已经被正常返回处理了（被 Cancel 了）
                if (!self.requestCallback.Remove(rpcId, out MessageSenderStruct action))
                {
                    return;
                }
                
                // 执行到这里说明真的超时了
                if (needException)
                {
                    action.SetException(new Exception($"actor sender timeout: {requestType.FullName}"));
                }
                else
                {
                    IResponse response = MessageHelper.CreateResponse(requestType, rpcId, ErrorCode.ERR_Timeout);
                    action.SetResult(response);
                }
            }
            
            Timeout().NoContext();
            
            long beginTime = TimeInfo.Instance.ServerFrameTime();

            response = await messageSenderStruct.Wait();
            
            // 重要：正常返回时立即取消 Timeout 任务
            timeoutCancelToken.Cancel();
            
            long endTime = TimeInfo.Instance.ServerFrameTime();

            long costTime = endTime - beginTime;
            if (costTime > 200)
            {
                Log.Warning($"actor rpc time > 200: {costTime} {requestType.FullName}");
            }
            
            return response;
        }
    }
}
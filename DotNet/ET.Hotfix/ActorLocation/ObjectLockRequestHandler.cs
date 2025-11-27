using System;

namespace ET
{
    [MessageHandler(SceneType.Location)]
    public class ObjectLockRequestHandler: MessageHandler<Scene, ObjectLockRequest, ObjectLockResponse>
    {
        protected override async ETTask Run(Scene scene, ObjectLockRequest request, ObjectLockResponse response)
        {
            await scene.GetComponent<LocationManagerComponent>().Get(request.Type).Lock(request.Key, request.ActorId, request.Time);
        }
    }
}
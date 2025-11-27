using System;

namespace ET
{
    [MessageHandler(SceneType.Location)]
    public class ObjectAddRequestHandler: MessageHandler<Scene, ObjectAddRequest, ObjectAddResponse>
    {
        protected override async ETTask Run(Scene scene, ObjectAddRequest request, ObjectAddResponse response)
        {
            await scene.GetComponent<LocationManagerComponent>().Get(request.Type).Add(request.Key, request.ActorId);
        }
    }
}
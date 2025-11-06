using System.Diagnostics;

namespace ET
{
    [EnableMethod]
    [ChildOf]
    public class Scene: Entity, IScene
    {
        public Fiber Fiber { get; set; }
        public int SceneType { get; set; }
        public string Name { get; set; }

        public Scene(Fiber fiber, long id, long instanceId, int sceneType, string name)
        {
            this.Id = id;
            this.Name = name;
            this.InstanceId = instanceId;
            this.SceneType = sceneType;
            this.IsNew = true;
            this.Fiber = fiber;
            this.IScene = this;
            this.IsRegister = true;
            TypeId = TypeId<Scene>.Id;
        }

        public new void Dispose()
        {
            base.Dispose();
            
            Log.Info($"scene dispose: {this.SceneType} {this.Id} {this.InstanceId}");
        }
    }
}
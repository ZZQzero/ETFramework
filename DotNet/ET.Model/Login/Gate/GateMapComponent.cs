namespace ET
{
    [ComponentOf(typeof(UserEntity))]
    public class GateMapComponent: Entity, IAwake
    {
        private EntityRef<Scene> scene;

        public Scene Scene
        {
            get => this.scene;
            set => this.scene = value;
        }
    }
}
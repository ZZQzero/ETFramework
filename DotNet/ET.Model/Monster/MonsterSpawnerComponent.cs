namespace ET
{
    [ComponentOf(typeof(Scene))]
    public class MonsterSpawnerComponent: Entity, IAwake, IDestroy
    {
        public long Timer;
        public bool Spawned;
    }
}
namespace ET;

[EntitySystemOf(typeof(LockInfo))]
[FriendOf(typeof(LockInfo))]
public static partial class LockInfoSystem
{
    [EntitySystem]
    private static void Awake(this LockInfo self, ActorId lockActorId, CoroutineLock coroutineLock)
    {
        self.CoroutineLock = coroutineLock;
        self.LockActorId = lockActorId;
    }
        
    [EntitySystem]
    private static void Destroy(this LockInfo self)
    {
        self.CoroutineLock.Dispose();
        self.LockActorId = default;
    }
}
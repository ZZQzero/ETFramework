using Unity.Mathematics;

namespace ET
{
    [Event(SceneType.Main)]
    public class EntryEvent1_InitShare: AEvent<Scene, EntryEvent1>
    {
        protected override async ETTask Run(Scene root, EntryEvent1 args)
        {
            Log.Info("EntryEvent1_InitShare");
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ObjectWait>();
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<ProcessInnerSender>();
            await ETTask.CompletedTask;
        }
    }
}
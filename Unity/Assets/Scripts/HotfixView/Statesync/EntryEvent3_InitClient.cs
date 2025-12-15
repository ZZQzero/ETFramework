using System;
using System.Collections.Generic;
using System.IO;

namespace ET
{
    [Event(SceneType.Main)]
    public class EntryEvent3_InitClient: AEvent<Scene, EntryEvent3>
    {
        protected override async ETTask Run(Scene root, EntryEvent3 args)
        {
            root.AddComponent<ResourcesLoaderComponent>();
            root.AddComponent<UserComponent>();
            root.AddComponent<CurrentScenesComponent>();
            Log.Error("EntryEvent3_InitClient");
            await EventSystem.Instance.PublishAsync(root, new AppStartInitFinish());
        }
    }
}
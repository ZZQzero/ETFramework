namespace ET;

[EntitySystemOf(typeof(ModeContext))]
public static partial class ModeContextSystem
{
    [EntitySystem]
    private static void Awake(this ModeContext self)
    {
        self.Mode = "";
    }
        
    [EntitySystem]
    private static void Destroy(this ModeContext self)
    {
        self.Mode = "";
    }
}
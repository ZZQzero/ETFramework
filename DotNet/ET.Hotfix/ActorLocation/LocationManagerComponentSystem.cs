namespace ET;

[EntitySystemOf(typeof(LocationManagerComponent))]
[FriendOf(typeof(LocationManagerComponent))]
public static partial class LocationManagerComponentSystem
{
    [EntitySystem]
    private static void Awake(this LocationManagerComponent self)
    {
    }
        
    public static LocationOneType Get(this LocationManagerComponent self, int locationType)
    {
        LocationOneType locationOneType = self.GetChild<LocationOneType>(locationType);
        if (locationOneType != null)
        {
            return locationOneType;
        }
        locationOneType = self.AddChildWithId<LocationOneType>(locationType);
        return locationOneType;
    }

}
namespace ET;

public static partial class LoginInfoRecordComponentSystem
{
    [EntitySystem]
    private static void Awake(this LoginInfoRecordComponent self)
    {
    }

    [EntitySystem]
    private static void Destroy(this LoginInfoRecordComponent self)
    {
        self.LoginAccountInfo.Clear();
    }

    public static void Add(this LoginInfoRecordComponent self, long userId, int value)
    {
        self.LoginAccountInfo[userId] = value;
    }

    public static void Remove(this LoginInfoRecordComponent self, long userId)
    {
        self.LoginAccountInfo.Remove(userId);
    }

    public static int Get(this LoginInfoRecordComponent self, long userId)
    {
        return self.LoginAccountInfo.GetValueOrDefault(userId);
    }

    public static bool IsExist(this LoginInfoRecordComponent self, long userId)
    {
        return self.LoginAccountInfo.ContainsKey(userId);
    }
}
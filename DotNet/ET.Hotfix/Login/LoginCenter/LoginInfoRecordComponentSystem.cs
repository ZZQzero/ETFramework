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

    public static void Add(this LoginInfoRecordComponent self, long key, int value)
    {
        if (self.LoginAccountInfo.ContainsKey(key))
        {
            self.LoginAccountInfo[key] = value;
        }

        self.LoginAccountInfo.Add(key, value);
    }

    public static void Remove(this LoginInfoRecordComponent self, long key)
    {
        self.LoginAccountInfo.Remove(key);
    }

    public static int Get(this LoginInfoRecordComponent self, long key)
    {
        return self.LoginAccountInfo.GetValueOrDefault(key);
    }

    public static bool IsExist(this LoginInfoRecordComponent self, long key)
    {
        return self.LoginAccountInfo.ContainsKey(key);
    }
}
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using UnityEngine;

namespace ET
{
    public class GameEntry : MonoBehaviour
    {
        void Awake()
        {
            TestToJson test = ObjectPool.Fetch<TestToJson>(false);
            test.aa = 1.1f;
            for (int i = 0; i < 4; i++)
            {
                test.list.Add(i.ToString());
            }
            Debug.Log(test.ToJson());
            var bson = test.ToBson();
            Debug.Log(bson);
            var test1 = BsonSerializer.Deserialize(bson, typeof(TestToJson)) as TestToJson;
            Debug.LogError(test1.aa);

            var httpRes = HttpGetRouterResponse.Create();
            for (int i = 0; i < 4; i++)
            {
                httpRes.Realms.Add(i.ToString());
            }

            for (int i = 0; i < 4; i++)
            {
                httpRes.Routers.Add(i.ToString());
            }
            
            Debug.Log(httpRes.ToJson());
            GameRegister.RegisterSingleton();
            GameRegister.RegisterEvent();
            GameRegister.RegisterInvoke();
            Entry.Start();
        }
    }
}

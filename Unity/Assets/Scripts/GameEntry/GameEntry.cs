using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using UnityEngine;

namespace ET
{
    public class GameEntry : MonoBehaviour
    {
        void Awake()
        {
            GameRegister.RegisterSingleton();
            GameRegister.RegisterEvent();
            GameRegister.RegisterInvoke();
            GameRegister.RegisterMessage();
            GameRegister.RegisterMessageSession();
            Entry.Start();
        }
    }
}

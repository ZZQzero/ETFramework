using UnityEngine;

namespace ET
{
    [EnableClass]
    public class StartGame : MonoBehaviour
    {
        void Awake()
        {
            Entry.Start();
        }
    }
}

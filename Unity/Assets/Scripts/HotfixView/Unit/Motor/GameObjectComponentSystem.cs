using System;
using UnityEngine;

namespace ET
{
    [EntitySystemOf(typeof(GameObjectComponent))]
    public static partial class GameObjectComponentSystem
    {
        [EntitySystem]
        private static void Destroy(this GameObjectComponent self)
        {
            UnityEngine.Object.Destroy(self.GameObject);
        }
        
        [EntitySystem]
        private static void Awake(this GameObjectComponent self)
        {

        }
        
        public static void SetLayer(this GameObject go, int layer)
        {
            if (go == null)
            {
                return;
            }
            SetLayerRecursive(go.transform, layer);
        }

        private static void SetLayerRecursive(Transform transform, int layer)
        {
            transform.gameObject.layer = layer;

            int childCount = transform.childCount;
            for (int i = 0; i < childCount; ++i)
            {
                SetLayerRecursive(transform.GetChild(i), layer);
            }
        }
    }
}
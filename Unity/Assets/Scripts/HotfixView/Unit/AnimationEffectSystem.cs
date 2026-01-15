using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameUI;
using UnityEngine;

namespace ET
{
    public class AnimationEffectSystem : MonoBehaviour
    {
        private Animation _animation;

        private void Awake()
        {
            _animation = GetComponent<Animation>();
        }

        public async UniTask Play(VisualEffectData data)
        {
            gameObject.SetActive(true);
            if (data.IsAnimation)
            {
                if (_animation)
                {
                    _animation.Play();
                }
                await UniTask.Delay(TimeSpan.FromSeconds(_animation.clip.length));
                GameObjectPool.Instance.ReleaseObject(this.gameObject, PoolType.Effect);
            }
            else
            {
                await UniTask.Delay(TimeSpan.FromSeconds(data.Length));
                GameObjectPool.Instance.ReleaseObject(this.gameObject, PoolType.Effect);
            }
        }
    } 
}
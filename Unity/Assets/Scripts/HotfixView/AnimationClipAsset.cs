using System.Collections.Generic;
using Animancer;
using UnityEngine;
using UnityEngine.Serialization;

namespace ET
{

    [CreateAssetMenu(
        menuName = "Animancer/AnimationClipTransition",
        fileName = "AnimationClipAsset",
        order = 100)]
    public class AnimationClipAsset : ScriptableObject
    {
        [SerializeField]
        private List<ClipTransition> animationClips = new List<ClipTransition>();

        /// <summary>
        /// 动画片段列表，每个元素代表一个ClipTransition动画
        /// </summary>
        public List<ClipTransition> AnimationClips
        {
            get => animationClips;
            set => animationClips = value ?? new List<ClipTransition>();
        }

        /// <summary>
        /// 获取指定索引的动画Transition
        /// </summary>
        /// <param name="index">索引</param>
        /// <returns>对应的ITransition，如果索引无效则返回null</returns>
        public ITransition GetTransition(int index)
        {
            if (index < 0 || index >= animationClips.Count)
            {
                return null;
            }
            return animationClips[index];
        }

        /// <summary>
        /// 获取所有有效的动画Transition数组
        /// </summary>
        /// <returns>所有有效的ITransition数组</returns>
        public ITransition[] GetAllTransitions()
        {
            var transitions = new List<ITransition>();
            for (int i = 0; i < animationClips.Count; i++)
            {
                if (animationClips[i] != null && animationClips[i].IsValid())
                {
                    transitions.Add(animationClips[i]);
                }
            }
            return transitions.ToArray();
        }

        /// <summary>
        /// 获取有效的动画片段数量
        /// </summary>
        /// <returns>有效动画片段的数量</returns>
        public int GetValidClipCount()
        {
            int count = 0;
            for (int i = 0; i < animationClips.Count; i++)
            {
                if (animationClips[i] != null && animationClips[i].IsValid())
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 获取动画片段总数（包括无效的）
        /// </summary>
        /// <returns>动画片段总数</returns>
        public int GetTotalClipCount()
        {
            return animationClips != null ? animationClips.Count : 0;
        }

        /// <summary>
        /// 添加一个动画片段
        /// </summary>
        /// <param name="clip">要添加的ClipTransition</param>
        public void AddClip(ClipTransition clip)
        {
            if (clip != null)
            {
                if (animationClips == null)
                {
                    animationClips = new List<ClipTransition>();
                }
                animationClips.Add(clip);
            }
        }

        /// <summary>
        /// 移除指定索引的动画片段
        /// </summary>
        /// <param name="index">要移除的索引</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveClipAt(int index)
        {
            if (index >= 0 && index < animationClips.Count)
            {
                animationClips.RemoveAt(index);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 清空所有动画片段
        /// </summary>
        public void ClearClips()
        {
            if (animationClips != null)
            {
                animationClips.Clear();
            }
        }
    }
}


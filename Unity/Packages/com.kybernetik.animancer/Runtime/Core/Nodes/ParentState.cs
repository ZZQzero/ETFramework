// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2025 Kybernetik //

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace Animancer
{
    /// <summary>[Pro-Only]
    /// An <see cref="AnimancerState"/> which contains other states.
    /// </summary>
    /// https://kybernetik.com.au/animancer/api/Animancer/ParentState
    /// 
    public abstract partial class ParentState : AnimancerState,
        ICopyable<ParentState>
    {
        /************************************************************************************************************************/
        #region Properties
        /************************************************************************************************************************/

        /// <summary>The children contained within this state.</summary>
        /// <remarks>Only states up to the <see cref="ChildCount"/> should be assigned.</remarks>
        protected AnimancerState[] ChildStates { get; private set; }
            = Array.Empty<AnimancerState>();

        /************************************************************************************************************************/

        private int _ChildCount;

        /// <inheritdoc/>
        public sealed override int ChildCount
            => _ChildCount;

        /************************************************************************************************************************/

        /// <summary>The size of the internal array of <see cref="ChildStates"/>.</summary>
        /// <remarks>
        /// This value starts at 0 then expands to <see cref="ChildCapacity"/>
        /// when the first child is added.
        /// </remarks>
        public int ChildCapacity
        {
            get => ChildStates.Length;
            set
            {
                if (value == ChildStates.Length)
                    return;

#if UNITY_ASSERTIONS
                if (value <= 1 && OptionalWarning.MixerMinChildren.IsEnabled())
                    OptionalWarning.MixerMinChildren.Log(
                        $"The {nameof(ChildCapacity)} of '{this}' is being set to {value}." +
                        $" The purpose of a mixer is to mix multiple child states so this may be a mistake.",
                        Graph?.Component);
#endif

                var newChildStates = new AnimancerState[value];
                if (value > _ChildCount)// Increase size.
                {
                    Array.Copy(ChildStates, newChildStates, _ChildCount);
                }
                else// Decrease size.
                {
                    for (int i = value; i < _ChildCount; i++)
                        ChildStates[i].Destroy();

                    Array.Copy(ChildStates, newChildStates, value);
                    _ChildCount = value;
                }

                ChildStates = newChildStates;

                if (_Playable.IsValid())
                {
                    _Playable.SetInputCount(value);
                }
                else if (Graph != null)
                {
                    CreatePlayable();
                }

                OnChildCapacityChanged();
            }
        }

        /// <summary>Called when the <see cref="ChildCapacity"/> is changed.</summary>
        protected virtual void OnChildCapacityChanged() { }

        /// <summary><see cref="ChildCapacity"/> starts at 0 then expands to this value when the first child is added.</summary>
        /// <remarks>Default 8.</remarks>
        public static int DefaultChildCapacity { get; set; } = 8;

        /// <summary>
        /// Ensures that the remaining unused <see cref="ChildCapacity"/>
        /// is greater than or equal to the specified `minimumCapacity`.
        /// </summary>
        public void EnsureRemainingChildCapacity(int minimumCapacity)
        {
            minimumCapacity += _ChildCount;
            if (ChildCapacity < minimumCapacity)
            {
                var capacity = Math.Max(ChildCapacity, DefaultChildCapacity);
                while (capacity < minimumCapacity)
                    capacity *= 2;

                ChildCapacity = capacity;
            }
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public sealed override AnimancerState GetChild(int index)
            => ChildStates[index];

        /// <inheritdoc/>
        public sealed override FastEnumerator<AnimancerState> GetEnumerator()
            => new(ChildStates, _ChildCount);

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Initialization
        /************************************************************************************************************************/

        /// <summary>Creates and assigns the <see cref="AnimationMixerPlayable"/> managed by this state.</summary>
        protected override void CreatePlayable(out Playable playable)
        {
            playable = AnimationMixerPlayable.Create(Graph._PlayableGraph, ChildCapacity);
        }

        /************************************************************************************************************************/

        /// <summary>Connects the `child` to this mixer at its <see cref="AnimancerNode.Index"/>.</summary>
        protected internal override void OnAddChild(AnimancerState child)
        {
            Validate.AssertGraph(child, Graph);

            var capacity = ChildCapacity;
            if (_ChildCount >= capacity)
                ChildCapacity = Math.Max(DefaultChildCapacity, capacity * 2);

            child.Index = _ChildCount;
            ChildStates[_ChildCount] = child;
            _ChildCount++;

            child.IsPlaying = IsPlaying;

            if (Graph != null)
                ConnectChildUnsafe(child.Index, child);

#if UNITY_ASSERTIONS
            _CachedToString = null;
#endif
        }

        /************************************************************************************************************************/

        /// <summary>Disconnects the `child` from this mixer at its <see cref="AnimancerNode.Index"/>.</summary>
        protected internal override void OnRemoveChild(AnimancerState child)
        {
            Validate.AssertCanRemoveChild(child, ChildStates, _ChildCount);

            // Shuffle all subsequent children down one place.
            if (Graph == null || !Graph._PlayableGraph.IsValid())
            {
                Array.Copy(
                    ChildStates, child.Index + 1,
                    ChildStates, child.Index,
                    _ChildCount - child.Index - 1);

                for (int i = child.Index; i < _ChildCount - 1; i++)
                    ChildStates[i].Index = i;
            }
            else
            {
                Graph._PlayableGraph.Disconnect(_Playable, child.Index);

                for (int i = child.Index + 1; i < _ChildCount; i++)
                {
                    var otherChild = ChildStates[i];
                    Graph._PlayableGraph.Disconnect(_Playable, otherChild.Index);
                    otherChild.Index = i - 1;
                    ChildStates[i - 1] = otherChild;
                    ConnectChildUnsafe(i - 1, otherChild);
                }
            }

            _ChildCount--;
            ChildStates[_ChildCount] = null;

#if UNITY_ASSERTIONS
            _CachedToString = null;
#endif
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void Destroy()
        {
            DestroyChildren();
            base.Destroy();
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public sealed override void CopyFrom(AnimancerState copyFrom, CloneContext context)
            => this.CopyFromBase(copyFrom, context);

        /// <inheritdoc/>
        public virtual void CopyFrom(ParentState copyFrom, CloneContext context)
        {
            base.CopyFrom(copyFrom, context);

            DestroyChildren();

            var childCount = copyFrom.ChildCount;
            EnsureRemainingChildCapacity(childCount);

            for (int i = 0; i < childCount; i++)
            {
                var child = copyFrom.ChildStates[i];
                child = context.Clone(child);
                Add(child);
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Child Configuration
        /************************************************************************************************************************/

        /// <summary>Assigns the `state` as a child of this mixer.</summary>
        /// <remarks>This is the same as calling <see cref="AnimancerState.SetParent"/>.</remarks>
        public virtual void Add(AnimancerState child)
            => child.SetParent(this);

        /// <summary>Creates and returns a new <see cref="ClipState"/> to play the `clip` as a child of this mixer.</summary>
        public virtual ClipState Add(AnimationClip clip)
        {
            var child = new ClipState(clip);
            Add(child);
            return child;
        }

        /// <summary>Calls <see cref="AnimancerUtilities.CreateStateAndApply"/> then <see cref="Add(AnimancerState)"/>.</summary>
        public virtual AnimancerState Add(ITransition transition)
        {
            var child = transition.CreateStateAndApply(Graph);
            Add(child);
            return child;
        }

        /// <summary>Calls one of the other <see cref="Add(object)"/> overloads as appropriate for the `child`.</summary>
        public virtual AnimancerState Add(object child)
        {
            if (child is AnimationClip clip)
                return Add(clip);

            if (child is ITransition transition)
                return Add(transition);

            if (child is AnimancerState state)
            {
                Add(state);
                return state;
            }

            MarkAsUsed(this);
            throw new ArgumentException(
                $"Failed to {nameof(Add)} '{AnimancerUtilities.ToStringOrNull(child)}'" +
                $" as child of '{this}' because it isn't an" +
                $" {nameof(AnimationClip)}, {nameof(ITransition)}, or {nameof(AnimancerState)}.");
        }

        /************************************************************************************************************************/

        /// <summary>Calls <see cref="Add(AnimationClip)"/> for each of the `clips`.</summary>
        public void AddRange(IList<AnimationClip> clips)
        {
            var count = clips.Count;
            EnsureRemainingChildCapacity(count);

            for (int i = 0; i < count; i++)
                Add(clips[i]);
        }

        /// <summary>Calls <see cref="Add(AnimationClip)"/> for each of the `clips`.</summary>
        public void AddRange(params AnimationClip[] clips)
            => AddRange((IList<AnimationClip>)clips);

        /************************************************************************************************************************/

        /// <summary>Calls <see cref="Add(ITransition)"/> for each of the `transitions`.</summary>
        public void AddRange(IList<ITransition> transitions)
        {
            var count = transitions.Count;
            EnsureRemainingChildCapacity(count);

            for (int i = 0; i < count; i++)
                Add(transitions[i]);
        }

        /// <summary>Calls <see cref="Add(ITransition)"/> for each of the `transitions`.</summary>
        public void AddRange(params ITransition[] transitions)
            => AddRange((IList<ITransition>)transitions);

        /************************************************************************************************************************/

        /// <summary>Calls <see cref="Add(object)"/> for each of the `children`.</summary>
        public void AddRange(IList<object> children)
        {
            var count = children.Count;
            EnsureRemainingChildCapacity(count);

            for (int i = 0; i < count; i++)
                Add(children[i]);
        }

        /// <summary>Calls <see cref="Add(object)"/> for each of the `children`.</summary>
        public void AddRange(params object[] children)
            => AddRange((IList<object>)children);

        /************************************************************************************************************************/

        /// <summary>Removes the child at the specified `index`.</summary>
        public void Remove(int index, bool destroy)
            => Remove(ChildStates[index], destroy);

        /// <summary>Removes the specified `child`.</summary>
        public void Remove(AnimancerState child, bool destroy)
        {
#if UNITY_ASSERTIONS
            if (child.Parent != this)
                Debug.LogWarning($"Attempting to remove a state which is not a child of this {GetType().Name}." +
                    $" This will remove the child from its actual parent so you should directly call" +
                    $" child.{nameof(child.Destroy)} or child.{nameof(child.SetParent)}(null, -1) instead." +
                    $"\n• Child: {child}" +
                    $"\n• Removing From: {this}" +
                    $"\n• Actual Parent: {child.Parent}",
                    Graph?.Component as Object);
#endif

            if (destroy)
                child.Destroy();
            else
                child.SetParent(null);
        }

        /************************************************************************************************************************/

        /// <summary>Replaces the `child` at the specified `index`.</summary>
        public virtual void Set(int index, AnimancerState child, bool destroyPrevious)
        {
#if UNITY_ASSERTIONS
            if ((uint)index >= _ChildCount)
            {
                MarkAsUsed(this);
                MarkAsUsed(child);
                throw new IndexOutOfRangeException(
                    $"Invalid child index. Must be 0 <= index < {nameof(ChildCount)} ({ChildCount}).");
            }
#endif

            if (child.Parent != null)
                child.SetParent(null);

            var previousChild = ChildStates[index];
            previousChild.SetParentInternal(null);

            child.SetGraph(Graph);
            ChildStates[index] = child;
            child.SetParentInternal(this, index);
            child.IsPlaying = IsPlaying;

            if (Graph != null)
            {
                Graph._PlayableGraph.Disconnect(_Playable, index);
                ConnectChildUnsafe(index, child);
            }

            child.CopyIKFlags(this);

            if (destroyPrevious)
                previousChild.Destroy();

#if UNITY_ASSERTIONS
            _CachedToString = null;
#endif
        }

        /// <summary>Replaces the child at the specified `index` with a new <see cref="ClipState"/>.</summary>
        public ClipState Set(int index, AnimationClip clip, bool destroyPrevious)
        {
            var child = new ClipState(clip);
            Set(index, child, destroyPrevious);
            return child;
        }

        /// <summary>Replaces the child at the specified `index` with a <see cref="ITransition.CreateState"/>.</summary>
        public AnimancerState Set(int index, ITransition transition, bool destroyPrevious)
        {
            var child = transition.CreateStateAndApply(Graph);
            Set(index, child, destroyPrevious);
            return child;
        }

        /// <summary>Calls one of the other <see cref="Set(int, object, bool)"/> overloads as appropriate for the `child`.</summary>
        public AnimancerState Set(int index, object child, bool destroyPrevious)
        {
            if (child is AnimationClip clip)
                return Set(index, clip, destroyPrevious);

            if (child is ITransition transition)
                return Set(index, transition, destroyPrevious);

            if (child is AnimancerState state)
            {
                Set(index, state, destroyPrevious);
                return state;
            }

            MarkAsUsed(this);
            throw new ArgumentException(
                $"Failed to {nameof(Set)} '{AnimancerUtilities.ToStringOrNull(child)}'" +
                $" as child of '{this}' because it isn't an" +
                $" {nameof(AnimationClip)}, {nameof(ITransition)}, or {nameof(AnimancerState)}.");
        }

        /************************************************************************************************************************/

        /// <summary>Returns the index of the specified `child` state.</summary>
        public int IndexOf(AnimancerState child)
            => Array.IndexOf(ChildStates, child, 0, _ChildCount);

        /************************************************************************************************************************/

        /// <summary>
        /// Destroys all <see cref="ChildStates"/> connected to this mixer.
        /// This operation cannot be undone.
        /// </summary>
        public void DestroyChildren()
        {
            for (int i = _ChildCount - 1; i >= 0; i--)
                ChildStates[i].Destroy();

            Array.Clear(ChildStates, 0, _ChildCount);
            _ChildCount = 0;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Updates
        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected internal override void UpdateEvents()
            => UpdateEventsRecursive(this);

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Inverse Kinematics
        /************************************************************************************************************************/

        private bool _ApplyAnimatorIK;

        /// <inheritdoc/>
        public override bool ApplyAnimatorIK
        {
            get => _ApplyAnimatorIK;
            set => base.ApplyAnimatorIK = _ApplyAnimatorIK = value;
        }

        /************************************************************************************************************************/

        private bool _ApplyFootIK;

        /// <inheritdoc/>
        public override bool ApplyFootIK
        {
            get => _ApplyFootIK;
            set => base.ApplyFootIK = _ApplyFootIK = value;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Other Methods
        /************************************************************************************************************************/

#if UNITY_ASSERTIONS
        /// <summary>[Assert-Only] A string built by <see cref="ToString"/> to describe this mixer.</summary>
        private string _CachedToString;
#endif

        /// <summary>
        /// Returns a string describing the type of this mixer and the name of states connected to it.
        /// </summary>
        public override string ToString()
        {
#if UNITY_ASSERTIONS
            if (NameCache.TryToString(DebugName, out var name))
                return name;

            if (_CachedToString != null)
                return _CachedToString;
#endif

            // Gather child names.
            var childNames = ListPool.Acquire<string>();
            var allSimple = true;
            for (int i = 0; i < _ChildCount; i++)
            {
                var state = ChildStates[i];
                if (state == null)
                    continue;

                if (state.MainObject != null)
                {
                    childNames.Add(state.MainObject.name);
                }
                else
                {
                    childNames.Add(state.ToString());
                    allSimple = false;
                }
            }

            // If they all have a main object, check if they all have the same prefix so it doesn't need to be repeated.
            int prefixLength = 0;
            var count = childNames.Count;
            if (count <= 1 || !allSimple)
            {
                prefixLength = 0;
            }
            else
            {
                var prefix = childNames[0];
                var shortest = prefixLength = prefix.Length;

                for (int iName = 0; iName < count; iName++)
                {
                    var childName = childNames[iName];

                    if (shortest > childName.Length)
                    {
                        shortest = prefixLength = childName.Length;
                    }

                    for (int iCharacter = 0; iCharacter < prefixLength; iCharacter++)
                    {
                        if (childName[iCharacter] != prefix[iCharacter])
                        {
                            prefixLength = iCharacter;
                            break;
                        }
                    }
                }

                if (prefixLength < 3 ||// Less than 3 characters probably isn't an intentional prefix.
                    prefixLength >= shortest)
                    prefixLength = 0;
            }

            // Build the parent name.
            var parentName = StringBuilderPool.Instance.Acquire();

            var type = GetType().Name;
            if (type.EndsWith("State"))
                parentName.Append(type, 0, type.Length - 5);
            else
                parentName.Append(type);

            parentName.Append('(');

            if (count > 0)
            {
                if (prefixLength > 0)
                    parentName.Append(childNames[0], 0, prefixLength).Append('[');

                for (int i = 0; i < count; i++)
                {
                    if (i > 0)
                        parentName.Append(", ");

                    var childName = childNames[i];
                    parentName.Append(childName, prefixLength, childName.Length - prefixLength);
                }

                parentName.Append(']');
            }
            ListPool.Release(childNames);

            parentName.Append(')');

            var result = parentName.ReleaseToString();

#if UNITY_ASSERTIONS
            _CachedToString = result;
#endif

            return result;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void GatherAnimationClips(ICollection<AnimationClip> clips)
            => clips.GatherFromSource(ChildStates);

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}


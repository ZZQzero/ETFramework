// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2025 Kybernetik //

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Animancer
{
    /// <summary>[Pro-Only]
    /// An <see cref="AnimancerState"/> which plays a sequence of other states.
    /// </summary>
    /// https://kybernetik.com.au/animancer/api/Animancer/SequenceState
    /// 
    public partial class SequenceState : ParentState,
        ICopyable<SequenceState>,
        IUpdatable
    {
        /************************************************************************************************************************/
        #region Fields and Properties
        /************************************************************************************************************************/

        private double[] _TimeOffsets = Array.Empty<double>();
        private double[] _FadeEndTimes = Array.Empty<double>();
        private double[] _StateEndTimes = Array.Empty<double>();

        /// <summary>The index of the child state which is active at the current time.</summary>
        private int _ActiveChildIndex;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override float Length
            => ChildCount > 0
            ? (float)_StateEndTimes[ChildCount - 1]
            : 0;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override double RawTime
        {
            get => base.RawTime;
            set
            {
                base.RawTime = value;

                if (ChildCount == 0)
                    return;

                var activeChildIndex = GetActiveChildIndex(value);
                SetActiveChildIndex(activeChildIndex);

                for (int i = 0; i < ChildCount; i++)
                {
                    var child = ChildStates[i];

                    var childTime = value;
                    if (i > 0)
                        childTime -= _StateEndTimes[i - 1];

                    childTime *= child.Speed;

                    child.TimeD = childTime + _TimeOffsets[i];
                }
            }
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void MoveTime(double time, bool normalized)
        {
            base.MoveTime(time, normalized);

            for (int i = 0; i < ChildCount; i++)
            {
                var child = ChildStates[i];

                var childTime = time;
                if (i > 0)
                    childTime -= _StateEndTimes[i - 1];

                childTime *= child.Speed;

                child.MoveTime(childTime + _TimeOffsets[i], normalized);
            }
        }

        /************************************************************************************************************************/

        /// <summary>Sequences don't loop.</summary>
        /// <remarks>
        /// If the last state in the sequence is set to loop it will do so,
        /// but the rest of the sequence won't replay automatically.
        /// </remarks>
        public override bool IsLooping
            => false;

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Initialisation
        /************************************************************************************************************************/

        /// <summary>Replaces the child states with new ones created from the `transitions`.</summary>
        public void Set(params ITransition[] transitions)
            => Set((IList<ITransition>)transitions);

        /// <summary>Replaces the child states with new ones created from the `transitions`.</summary>
        public void Set(IList<ITransition> transitions)
        {
            var oldChildCount = ChildCount;
            var newChildCount = transitions.Count;

            ChildCapacity = newChildCount;

            for (int i = 0; i < newChildCount; i++)
            {
                var transition = transitions[i];
                var state = transition.CreateStateAndApply(Graph);
                state.IsPlaying = IsPlaying;

                if (i < oldChildCount)
                    Set(i, state, true);
                else
                    Add(state);

                _FadeEndTimes[i] += transition.FadeDuration;
            }

            while (oldChildCount > newChildCount)
                Remove(--oldChildCount, true);
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected internal override void OnAddChild(AnimancerState child)
        {
            base.OnAddChild(child);
            GatherChildDetails(child);
        }

        /************************************************************************************************************************/

        /// <summary>Gathers the timing details of a newly added `child` state.</summary>
        private void GatherChildDetails(AnimancerState child)
        {
            var index = child.Index;

            if (index == 0)
                child.Weight = 1;

            _TimeOffsets[index] = child.TimeD;

            var startTime = GetStartTime(index);

            _FadeEndTimes[index] = startTime;

            var length = child.RemainingDuration;
            if (length < 0)
                length = 0;

            startTime += length;

            _StateEndTimes[index] = startTime;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override AnimancerState Add(ITransition transition)
        {
            var child = base.Add(transition);

            _FadeEndTimes[child.Index] += transition.FadeDuration;

            return child;
        }

        /************************************************************************************************************************/

        /// <summary>Adds the `child` to the end of this sequence.</summary>
        public void Add(AnimancerState child, float fadeDuration)
        {
            Add(child);

            _FadeEndTimes[child.Index] += fadeDuration;
        }

        /// <summary>Adds the `clip` to the end of this sequence.</summary>
        public void Add(AnimationClip clip, float fadeDuration)
        {
            var child = Add(clip);

            _FadeEndTimes[child.Index] += fadeDuration;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Execution
        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void OnChildCapacityChanged()
        {
            base.OnChildCapacityChanged();

            var capacity = ChildCapacity;
            Array.Resize(ref _TimeOffsets, capacity);
            Array.Resize(ref _FadeEndTimes, capacity);
            Array.Resize(ref _StateEndTimes, capacity);
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        int IUpdatable.UpdatableIndex { get; set; } = IUpdatable.List.NotInList;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void OnSetIsPlaying()
        {
            base.OnSetIsPlaying();

            var isPlaying = IsPlaying;
            var childStates = ChildStates;
            for (int i = 0; i < ChildCount; i++)
                childStates[i].IsPlaying = isPlaying;

            if (IsPlaying)
                Graph.RequirePreUpdate(this);
            else
                Graph.CancelPreUpdate(this);
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void Destroy()
        {
            base.Destroy();
            Graph.CancelPreUpdate(this);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Called every frame while this state is playing to control its children.
        /// </summary>
        public virtual void Update()
        {
            var time = Time;

            var activeChildIndex = _ActiveChildIndex;
            if (Speed >= 0)
            {
                while (activeChildIndex < ChildCount - 1 && time > _StateEndTimes[activeChildIndex])
                    activeChildIndex++;
            }
            else
            {
                while (activeChildIndex > 0 && time > _StateEndTimes[activeChildIndex - 1])
                    activeChildIndex--;
            }

            SetActiveChildIndex(activeChildIndex);

            var startTime = GetStartTime(activeChildIndex);

            var endFadeTime = _FadeEndTimes[activeChildIndex];

            if (activeChildIndex == 0 || time > endFadeTime)
            {
                ChildStates[activeChildIndex].Weight = 1;

                if (activeChildIndex > 0)
                    ChildStates[activeChildIndex - 1].Weight = 0;
            }
            else
            {
                var weight = Mathf.InverseLerp((float)startTime, (float)endFadeTime, time);

                ChildStates[activeChildIndex].Weight = weight;
                ChildStates[activeChildIndex - 1].Weight = 1 - weight;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Calculates the indices of the `first` and `last` child states
        /// which should be active at the specified `time`.
        /// </summary>
        public int GetActiveChildIndex(double time)
        {
            var index = Array.BinarySearch(_StateEndTimes, time);

            if (index < 0)
                index = ~index;

            if (index >= ChildCount)
                index = ChildCount - 1;

            return index;
        }

        /************************************************************************************************************************/

        /// <summary>Clears the weights of any active chhildren and sets the newly active child.</summary>
        private void SetActiveChildIndex(int index)
        {
            if (_ActiveChildIndex == index)
                return;

            ChildStates[_ActiveChildIndex].Weight = 0;

            if (_ActiveChildIndex > 0)
                ChildStates[_ActiveChildIndex - 1].Weight = 0;

            _ActiveChildIndex = index;
        }

        /************************************************************************************************************************/

        /// <summary>Gets the time when the specified child starts relative to the start of this sequence.</summary>
        public double GetStartTime(int childIndex)
            => childIndex > 0
            ? _StateEndTimes[childIndex - 1]
            : 0;

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Copying
        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override AnimancerState Clone(CloneContext context)
        {
            var clone = new SequenceState();
            clone.CopyFrom(this, context);
            return clone;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public sealed override void CopyFrom(ParentState copyFrom, CloneContext context)
            => this.CopyFromBase(copyFrom, context);

        /// <inheritdoc/>
        public virtual void CopyFrom(SequenceState copyFrom, CloneContext context)
        {
            base.CopyFrom(copyFrom, context);

            var childCount = Math.Min(copyFrom.ChildCount, ChildCount);

            Array.Copy(copyFrom._TimeOffsets, 0, _TimeOffsets, 0, childCount);
            Array.Copy(copyFrom._FadeEndTimes, 0, _FadeEndTimes, 0, childCount);
            Array.Copy(copyFrom._StateEndTimes, 0, _StateEndTimes, 0, childCount);

            _ActiveChildIndex = copyFrom._ActiveChildIndex;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}


// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2025 Kybernetik //

using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;

#if UNITY_BURST
using Unity.Burst;
#endif

namespace Animancer
{
    /// <summary>
    /// An <see cref="IAnimationJob"/> which mixes its inputs based on individual <see cref="boneWeights"/>.
    /// </summary>
#if UNITY_BURST
    [BurstCompile(FloatPrecision.Low, FloatMode.Fast
#if UNITY_BURST_1_6_0
        , OptimizeFor = OptimizeFor.Performance
#endif
        )]
#endif
    public struct WeightedMaskMixerJob : IAnimationJob
    {
        /************************************************************************************************************************/

        /// <summary>The handles for each bone being mixed.</summary>
        /// <remarks>
        /// All animated bones must be included,
        /// even if their individual weight isn't modified.
        /// </remarks>
        public NativeArray<TransformStreamHandle> boneTransforms;

        /// <summary>The blend weight of each bone.</summary>
        /// <remarks>
        /// This array corresponds to the <see cref="boneTransforms"/>,
        /// repeated for each layer after the first and excluding the base layer.
        /// For example, if there are 3 layers and 10 bones, then this array will have 20 elements
        /// with the first 10 being for Layer 1 and the next 10 being for Layer 2.
        /// </remarks>
        public NativeArray<float> boneWeights;

        /// <summary>The number of layers being mixed.</summary>
        public int layerCount;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        readonly void IAnimationJob.ProcessRootMotion(AnimationStream output)
        {
            var input = output.GetInputStream(0);

            var velocity = input.velocity;
            var angularVelocity = input.angularVelocity;

            for (int i = 1; i < layerCount; i++)// Start at 1.
            {
                input = output.GetInputStream(i);
                if (!input.isValid)
                    continue;

                var layerWeight = output.GetInputWeight(i);
                velocity = Vector3.LerpUnclamped(
                    velocity,
                    input.velocity,
                    layerWeight);
                angularVelocity = Vector3.LerpUnclamped(
                    angularVelocity,
                    input.angularVelocity,
                    layerWeight);
            }

            output.velocity = velocity;
            output.angularVelocity = angularVelocity;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        readonly void IAnimationJob.ProcessAnimation(AnimationStream output)
        {
            if (layerCount == 2)
            {
                ProcessAnimation2Layers(output);
                return;
            }

            // Blending more than 2 layers is less efficient because we need to use these temporary arrays.

            var transformCount = boneTransforms.Length;
            var localPositions = new NativeArray<Vector3>(transformCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var localRotations = new NativeArray<Quaternion>(transformCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            var input = output.GetInputStream(0);

            for (var i = 0; i < transformCount; i++)
            {
                var transform = boneTransforms[i];
                localPositions[i] = transform.GetLocalPosition(input);
                localRotations[i] = transform.GetLocalRotation(input);
            }

            for (int iLayer = 1; iLayer < layerCount; iLayer++)// Start at 1.
            {
                input = output.GetInputStream(iLayer);
                if (!input.isValid)
                    break;

                var layerWeight = output.GetInputWeight(iLayer);
                if (layerWeight == 0)
                    continue;

                var weightOffset = (iLayer - 1) * transformCount;

                for (var iTransform = 0; iTransform < transformCount; iTransform++)
                {
                    var transform = boneTransforms[iTransform];
                    var weight = layerWeight * boneWeights[weightOffset + iTransform];
                    if (weight == 0)
                        continue;

                    localPositions[iTransform] = Vector3.LerpUnclamped(
                        localPositions[iTransform],
                        transform.GetLocalPosition(input),
                        weight);

                    localRotations[iTransform] = Quaternion.SlerpUnclamped(
                        localRotations[iTransform],
                        transform.GetLocalRotation(input),
                        weight);
                }
            }

            for (var i = 0; i < transformCount; i++)
            {
                var transform = boneTransforms[i];
                transform.SetLocalPosition(output, localPositions[i]);
                transform.SetLocalRotation(output, localRotations[i]);
            }

            localPositions.Dispose();
            localRotations.Dispose();
        }

        /************************************************************************************************************************/

        /// <summary>Blends the layers in an optimized way when there are only 2.</summary>
        private readonly void ProcessAnimation2Layers(AnimationStream output)
        {
            var input0 = output.GetInputStream(0);
            var input1 = output.GetInputStream(1);

            if (input1.isValid)
            {
                var layerWeight = output.GetInputWeight(1);
                var transformCount = boneTransforms.Length;
                for (var i = 0; i < transformCount; i++)
                {
                    var transform = boneTransforms[i];
                    var weight = layerWeight * boneWeights[i];

                    var position0 = transform.GetLocalPosition(input0);
                    var position1 = transform.GetLocalPosition(input1);
                    transform.SetLocalPosition(output, Vector3.LerpUnclamped(position0, position1, weight));

                    var rotation0 = transform.GetLocalRotation(input0);
                    var rotation1 = transform.GetLocalRotation(input1);
                    transform.SetLocalRotation(output, Quaternion.SlerpUnclamped(rotation0, rotation1, weight));
                }
            }
            else
            {
                var transformCount = boneTransforms.Length;
                for (var i = 0; i < transformCount; i++)
                {
                    var transform = boneTransforms[i];
                    transform.SetLocalPosition(output, transform.GetLocalPosition(input0));
                    transform.SetLocalRotation(output, transform.GetLocalRotation(input0));
                }
            }
        }

        /************************************************************************************************************************/
    }
}


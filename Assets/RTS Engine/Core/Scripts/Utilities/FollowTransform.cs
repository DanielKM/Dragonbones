using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace RTSEngine.Utilities
{
    /// <summary>
    /// Allows a 'source' transform to follow the position and/or rotation of a 'target' Transform.
    /// </summary>
    public class FollowTransform
    {
        private readonly Transform source;
        private readonly bool canFollowPosition;
        private readonly bool canFollowRotation;

        // Callback called when the target is invalid to alter the source class implemeting this.
        private bool enableCallback;
        private readonly Action targetInvalidCallback;

        // Target that the source transform will be following
        private Transform target = null;
        private Vector3 offset = Vector3.zero;

        public bool HasTarget => target != null;

        public FollowTransform(Transform source, bool followPosition, bool followRotation, Action targetInvalidCallback)
        {
            this.source = source;
            Assert.IsNotNull(this.source,
                $"[{GetType()}] A valid 'source' Transform must be provided!");

            this.canFollowPosition = followPosition;
            this.canFollowRotation = followRotation;

            this.targetInvalidCallback = targetInvalidCallback;
        }

        public void ResetTarget()
            => SetTarget(null, offset: Vector3.zero, enableCallback: false);

        public void SetTarget(Transform target, bool enableCallback)
            => SetTarget(target, Vector3.zero, enableCallback);

        public void SetTarget(Transform target, Vector3 offset, bool enableCallback)
        {
            this.target = target;
            this.offset = offset;

            this.enableCallback = enableCallback;
        }

        public void Update()
        {
            if (!target.IsValid())
            {
                if(enableCallback && targetInvalidCallback.IsValid())
                    targetInvalidCallback();

                return;
            }

            if(canFollowPosition)
                source.position = target.position + offset;

            if (canFollowRotation)
                source.rotation = RTSHelper.GetLookRotation(source, target.position + offset);
        }
    }
}

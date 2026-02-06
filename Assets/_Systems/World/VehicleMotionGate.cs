using UnityEngine;

namespace Systems.World
{
    public class VehicleMotionGate : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Minimum distance in meters to consider significant movement.")]
        public float movementThreshold = 0.1f;

        private Transform _target;
        private Vector3 _lastPosition;

        public void SetTarget(Transform target)
        {
            _target = target;
            if (_target != null)
            {
                _lastPosition = _target.position;
            }
        }

        public void ClearTarget()
        {
            _target = null;
        }

        /// <summary>
        /// Returns the movement delta since last check if it exceeds the threshold.
        /// </summary>
        public float GetFilteredDelta()
        {
            if (_target == null) return 0f;

            float delta = Vector3.Distance(_target.position, _lastPosition);

            if (delta >= movementThreshold)
            {
                _lastPosition = _target.position;
                return delta;
            }

            return 0f;
        }

        /// <summary>
        /// Resets the base position to the target's current position without returning a delta.
        /// </summary>
        public void ResetBasePosition()
        {
            if (_target != null)
            {
                _lastPosition = _target.position;
            }
        }
    }
}

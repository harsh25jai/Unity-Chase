using UnityEngine;
using System;

namespace Data.Session.Runtime
{
    [Serializable]
    public struct HandoffTransitionData
    {
        public Vector3 spawnPosition;
        public Quaternion spawnRotation;
        public Vector3 cameraTargetPosition;
        public bool isValid;

        public static HandoffTransitionData Default => new HandoffTransitionData
        {
            spawnPosition = Vector3.zero,
            spawnRotation = Quaternion.identity,
            cameraTargetPosition = Vector3.zero,
            isValid = false
        };
    }
}

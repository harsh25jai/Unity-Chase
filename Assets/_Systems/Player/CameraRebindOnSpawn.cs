using System;
using UnityEngine;
using Unity.Cinemachine;

namespace Systems.Player
{
    public class CameraRebindOnSpawn : MonoBehaviour
    {
        [Header("Cameras")]
        public CinemachineCamera fpsVirtualCamera;
        public CinemachineCamera tpsVirtualCamera;

        [Header("Settings")]
        public string viewTargetRole = "Player_ViewTarget";

        private bool _warnedAboutMissingFPS = false;
        private bool _warnedAboutMissingTPS = false;

        /// <summary>
        /// Binds the configured cameras to the provided view target.
        /// </summary>
        /// <param name="viewTarget">The target transform to follow/look at.</param>
        /// <returns>True if at least one camera was successfully bound.</returns>
        public bool BindToTarget(Transform viewTarget)
        {
            if (viewTarget == null)
            {
                Debug.LogError("[CameraRebindOnSpawn] Cannot bind cameras: Player_ViewTarget is null.");
                return false;
            }

            bool boundAny = false;
            
            if (BindCamera(fpsVirtualCamera, viewTarget, ref _warnedAboutMissingFPS)) boundAny = true;
            if (BindCamera(tpsVirtualCamera, viewTarget, ref _warnedAboutMissingTPS)) boundAny = true;

            if (!boundAny)
            {
                Debug.LogWarning("[CameraRebindOnSpawn] No cameras were bound to the target.");
            }

            return boundAny;
        }

        private bool BindCamera(CinemachineCamera cam, Transform target, ref bool warned)
        {
            if (cam == null)
            {
                if (!warned)
                {
                    Debug.LogWarning($"[CameraRebindOnSpawn] { (cam == fpsVirtualCamera ? "FPS" : "TPS") } Virtual Camera is missing in scene references.");
                    warned = true;
                }
                return false;
            }

            cam.Follow = target;
            cam.LookAt = target;
            
            Debug.Log($"[CameraRebindOnSpawn] Successfully bound {cam.name} to {target.name}");
            return true;
        }

        public Transform FindViewTargetInInstance(GameObject playerInstance)
        {
            if (playerInstance == null) return null;

            // Search by name/role in children
            foreach (Transform child in playerInstance.GetComponentsInChildren<Transform>())
            {
                if (child.name == viewTargetRole) return child;
            }

            return null;
        }
    }
}

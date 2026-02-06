using System;
using UnityEngine;
using Unity.Cinemachine; // Updated for Unity 6 / Cinemachine 3.x
namespace Systems.Player
{
    public class CameraBindingController : MonoBehaviour
    {
        [Header("Cameras")]
        public CinemachineCamera fpsCamera;
        public CinemachineCamera tpsCamera;

        [Header("Targeting")]
        [Tooltip("Name of the child object or tag to look for on the player prefab.")]
        public string viewTargetRole = "Player_ViewTarget";

        [Header("References")]
        public PlayerSpawnController spawnController;

        // Events
        public event Action<string> OnCamerasBound;

        private void Start()
        {
            if (spawnController == null)
                spawnController = FindFirstObjectByType<PlayerSpawnController>();

            if (spawnController != null)
            {
                spawnController.OnPlayerSpawned += BindCameras;
            }
        }

        private void OnDestroy()
        {
            if (spawnController != null)
            {
                spawnController.OnPlayerSpawned -= BindCameras;
            }
        }

        private void BindCameras(string playerId, GameObject playerInstance)
        {
            Transform target = GetViewTarget(playerInstance);

            if (target == null)
            {
                Debug.LogWarning($"[CameraBindingController] Could not find Child/Role '{viewTargetRole}' on Player. Defaulting to Player Root.");
                target = playerInstance.transform;
            }

            Debug.Log($"[CameraBindingController] Binding Cameras to target: {target.name}");

            BindCamera(fpsCamera, target);
            BindCamera(tpsCamera, target);

            OnCamerasBound?.Invoke(playerId);
        }

        private Transform GetViewTarget(GameObject root)
        {
            // Recursive search or direct child check
            var t = root.transform.Find(viewTargetRole);
            if (t != null) return t;

            // Fallback: Check all children
            foreach (Transform child in root.transform)
            {
                if (child.name == viewTargetRole) return child;
                // Could recurse here if needed
            }
            return null;
        }

        private void BindCamera(CinemachineCamera cam, Transform target)
        {
            if (cam != null)
            {
                cam.Follow = target;
                cam.LookAt = target; // Oftentimes FPS doesn't need LookAt if it's strictly following head, but safe default.
                Debug.Log($"[CameraBindingController] Bound {cam.name}.");
            }
        }
    }
}

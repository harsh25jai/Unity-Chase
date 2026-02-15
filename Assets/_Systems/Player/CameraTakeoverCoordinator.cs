using UnityEngine;
using System;
using System.Collections;
using Unity.Cinemachine;
using Core.Bootstrap;

namespace Systems.Player
{
    /// <summary>
    /// Manages the smooth camera transition from the cutscene's final view to the gameplay camera.
    /// Handles Cinemachine blending, fade-in triggering, and completion signaling.
    /// </summary>
    public class CameraTakeoverCoordinator : MonoBehaviour
    {
        public static CameraTakeoverCoordinator Instance { get; private set; }

        [Header("Configuration")]
        public float blendDuration = 1.0f;
        public float fadeDuration = 0.3f;
        public float blendTimeout = 3.0f;
        public Vector3 cameraOffset = new Vector3(0, 2, -4);

        [Header("References")]
        public CinemachineCamera gameplayCamera;
        public TransitionUIController uiController;

        public event Action OnCameraBlendComplete;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (uiController == null) uiController = TransitionUIController.Instance;
        }

        public void InitializeCameraHandoff(Vector3 cameraTarget)
        {
            Debug.Log($"[CameraTakeover] Initializing handoff to target: {cameraTarget}");
            
            if (gameplayCamera == null)
            {
                Debug.LogWarning("[CameraTakeover] Gameplay camera not assigned. Signaling completion immediately.");
                OnCameraBlendComplete?.Invoke();
                return;
            }

            StartCoroutine(PerformTakeoverSequence(cameraTarget));
        }

        private IEnumerator PerformTakeoverSequence(Vector3 cameraTarget)
        {
            // 1. Initial State: Aim at handoff target
            // We'll use a temporary look target object to ensure smooth transition
            GameObject tempTarget = new GameObject("CameraHandoff_TempTarget");
            tempTarget.transform.position = cameraTarget;
            
            gameplayCamera.LookAt = tempTarget.transform;
            
            // 2. Start Blend
            // Cinemachine handles the transition if we switch targets or activate the camera.
            // Assuming the gameplay camera is already active but we are overriding its LookAt.
            
            // 3. Trigger Fade-In
            if (uiController != null)
            {
                uiController.FadeIn(fadeDuration);
            }

            // 4. Smoothly blend LookAt from handoff target to player
            float elapsed = 0f;
            Transform playerViewTarget = gameplayCamera.Follow;

            if (playerViewTarget == null)
            {
                Debug.LogWarning("[CameraTakeover] Gameplay camera has no Follow target. Waiting 0.5s for spawn.");
                yield return new WaitForSeconds(0.5f);
                playerViewTarget = gameplayCamera.Follow;
            }

            while (elapsed < blendDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / blendDuration;
                
                if (playerViewTarget != null)
                {
                    // Interpolate LookAt position if desired, or just let Cinemachine damping handle it.
                    // For now, we'll keep the LookAt on the temp target and then snap to player at end,
                    // or better: animate temp target toward player.
                    tempTarget.transform.position = Vector3.Lerp(cameraTarget, playerViewTarget.position, t);
                }
                
                yield return null;
            }

            // 5. Finalize setup
            if (playerViewTarget != null)
            {
                gameplayCamera.LookAt = playerViewTarget;
                Debug.Log("[CameraTakeover] Camera bound to Player View Target.");
            }
            
            Destroy(tempTarget);

            Debug.Log("[CameraTakeover] Camera handoff complete.");
            OnCameraBlendComplete?.Invoke();
        }
    }
}

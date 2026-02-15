using UnityEngine;
using Data.Session.Runtime;
using Core.GameState;
using System.Collections;
using System;

namespace Systems.Player
{
    /// <summary>
    /// Overrides default player spawn logic when transitioning from a cutscene.
    /// Applies handoff marker data and coordinates with the camera system.
    /// </summary>
    public class SpawnAlignmentResolver : MonoBehaviour
    {
        [Header("Configuration")]
        public string defaultSpawnMarkerName = "PlayerSpawn_Default";
        public float cameraBlendTimeout = 3.0f;

        [Header("Dependencies")]
        public PlayerStateController playerStateController;
        public CameraTakeoverCoordinator cameraCoordinator;

        private bool _isResolved = false;

        public void ResolveSpawn(HandoffTransitionData data)
        {
            if (_isResolved) return;
            _isResolved = true;
            StartCoroutine(ResolveSpawnSequence(data));
        }

        private void Start()
        {
            // For editor testing: If no loader triggers us, try to resolve from session
            StartCoroutine(AutoResolveFallback());
        }

        private IEnumerator AutoResolveFallback()
        {
            yield return new WaitForSeconds(0.1f);
            if (!_isResolved)
            {
                SessionRuntimeData sessionData = FindFirstObjectByType<SessionRuntimeData>();
                if (sessionData != null)
                {
                    ResolveSpawn(sessionData.activeHandoffData);
                }
            }
        }

        private IEnumerator ResolveSpawnSequence(HandoffTransitionData data)
        {
            SessionRuntimeData sessionData = FindFirstObjectByType<SessionRuntimeData>();
            if (sessionData == null)
            {
                Debug.LogError("[SpawnAlignmentResolver] SessionRuntimeData not found!");
                yield break;
            }

            Debug.Log($"[SpawnAlignmentResolver] Resolving spawn. Valid Handoff: {data.isValid}");

            if (data.isValid)
            {
                yield return ApplyHandoffSpawn(data);
            }
            else
            {
                ApplyDefaultSpawn();
            }

            // Clear handoff data after use to prevent reuse on restart etc.
            sessionData.activeHandoffData = HandoffTransitionData.Default;
            Debug.Log("[SpawnAlignmentResolver] Handoff data cleared.");
        }

        private IEnumerator ApplyHandoffSpawn(HandoffTransitionData data)
        {
            Debug.Log($"[SpawnAlignmentResolver] Applying handoff spawn at {data.spawnPosition}");

            if (playerStateController == null) playerStateController = FindFirstObjectByType<PlayerStateController>();

            if (playerStateController != null)
            {
                // 1. Position Player and set TransitionHold state
                playerStateController.SpawnAtPosition(data.spawnPosition, data.spawnRotation, "TransitionHold");

                // 2. Initialize Camera
                if (cameraCoordinator == null) cameraCoordinator = CameraTakeoverCoordinator.Instance;
                
                if (cameraCoordinator != null)
                {
                    bool cameraReady = false;
                    Action onReady = () => cameraReady = true;
                    cameraCoordinator.OnCameraBlendComplete += onReady;

                    cameraCoordinator.InitializeCameraHandoff(data.cameraTargetPosition);

                    // 3. Wait for Camera or Timeout
                    float startTime = Time.time;
                    while (!cameraReady && Time.time - startTime < cameraBlendTimeout)
                    {
                        yield return null;
                    }

                    if (!cameraReady)
                    {
                        Debug.LogWarning("[SpawnAlignmentResolver] Camera blend timeout, enabling control early.");
                    }

                    cameraCoordinator.OnCameraBlendComplete -= onReady;
                }
                else
                {
                    Debug.LogWarning("[SpawnAlignmentResolver] CameraTakeoverCoordinator not found. Skipping camera wait.");
                }

                // 4. Finalize: Switch to Grounded/Idle and enable input
                playerStateController.SetControlLock(false);
                Debug.Log("[SpawnAlignmentResolver] Player control enabled.");
            }
            else
            {
                Debug.LogError("[SpawnAlignmentResolver] PlayerStateController not found. Cannot align spawn.");
            }
        }

        private void ApplyDefaultSpawn()
        {
            Debug.LogWarning("[SpawnAlignmentResolver] No valid handoff data found, using default spawn.");
            
            GameObject defaultMarker = GameObject.Find(defaultSpawnMarkerName);
            if (defaultMarker != null)
            {
                if (playerStateController == null) playerStateController = FindFirstObjectByType<PlayerStateController>();
                
                if (playerStateController != null)
                {
                    playerStateController.SpawnAtPosition(defaultMarker.transform.position, defaultMarker.transform.rotation, "Grounded");
                    playerStateController.SetControlLock(false);
                }
            }
            else
            {
                Debug.LogError($"[SpawnAlignmentResolver] Default spawn marker '{defaultSpawnMarkerName}' not found!");
            }
        }
    }
}

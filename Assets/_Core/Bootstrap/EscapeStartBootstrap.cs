using UnityEngine;
using System.Collections;
using Data.Session.Runtime;

namespace Core.Bootstrap
{
    public class EscapeStartBootstrap : MonoBehaviour
    {
        [Header("Components")]
        public GameModeInitializer modeInitializer;
        public EscapeStartSpawnRequestor spawnRequestor;
        public SessionRuntimeData sessionData;

        [Header("Scene References")]
        [Tooltip("A marker object to validate we are in the correct EscapeStart context")]
        public Transform escapeStartSceneRoot;

        // Events for other systems to hook into
        public UnityEngine.Events.UnityEvent OnBootstrapCompleted;

        private void Start()
        {
            StartCoroutine(BootstrapSequence());
        }

        private IEnumerator BootstrapSequence()
        {
            Debug.Log("[EscapeStartBootstrap] Starting Sequence...");

            // 1. Validate Context
            if (escapeStartSceneRoot == null)
            {
                Debug.LogWarning("[EscapeStartBootstrap] Scene Root not assigned, attempting autodetection...");
                // Heuristic check if needed, or just proceed
            }

            // 2. Resolve Dependencies
            if (sessionData == null) sessionData = FindFirstObjectByType<SessionRuntimeData>();
            if (modeInitializer == null) modeInitializer = GetComponent<GameModeInitializer>();
            if (spawnRequestor == null) spawnRequestor = GetComponent<EscapeStartSpawnRequestor>();

            // 3. Initialize Game Mode
            if (modeInitializer != null)
            {
                modeInitializer.InitializeGameMode();
            }
            else
            {
                Debug.LogError("[EscapeStartBootstrap] Missing GameModeInitializer!");
            }

            yield return null; // Wait a frame for any immediate updates

            // 4. Request Spawn
            if (spawnRequestor != null && sessionData != null)
            {
                Debug.Log("[EscapeStartBootstrap] Requesting Player Spawn...");
                spawnRequestor.OnPlayerSpawnRequested += HandleSpawnLocationSelected;
                spawnRequestor.RequestSpawn(sessionData);
            }
            else
            {
                Debug.LogError("[EscapeStartBootstrap] Components missing for Spawn Request.");
            }
        }

        private void HandleSpawnLocationSelected(Transform spawnPoint)
        {
            // Here we would interface with the actual Player Spawner system.
            // For now, we log it and mark session as active.
            
            Debug.Log($"[EscapeStartBootstrap] Spawn Confirmed at {spawnPoint.position}.");
            
            if (sessionData != null)
            {
                sessionData.isSessionActive = true;
                sessionData.currentSessionState = SessionState.Active;
            }

            OnBootstrapCompleted?.Invoke();
            
            // Clean up event
            if (spawnRequestor != null)
            {
                spawnRequestor.OnPlayerSpawnRequested -= HandleSpawnLocationSelected;
            }
        }
    }
}

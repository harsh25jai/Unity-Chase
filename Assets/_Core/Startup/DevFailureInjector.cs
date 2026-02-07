using System;
using UnityEngine;
using Core.GameState;
using Data.Session.Runtime;

namespace Core.Startup
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>
    /// Debug-only utility to force death or busted outcomes for testing.
    /// </summary>
    public class DevFailureInjector : MonoBehaviour
    {
        [Header("Settings")]
        public string targetPlayerId = "Player_0";
        public KeyCode forceDeathKey = KeyCode.F7;
        public KeyCode forceBustedKey = KeyCode.F8;

        [Header("Dependencies")]
        public GameStateManager gameStateManager;
        public SessionRuntimeData sessionData;

        // Events
        public static event Action OnDevForceDeathRequested;
        public static event Action OnDevForceBustedRequested;

        private void Awake()
        {
            if (gameStateManager == null) gameStateManager = GameStateManager.Instance;
            if (sessionData == null) sessionData = FindFirstObjectByType<SessionRuntimeData>();
        }

        private void Update()
        {
            // Block triggers if game is not active or session is not active
            if (sessionData == null || sessionData.currentSessionState != SessionState.Active) return;

            if (Input.GetKeyDown(forceDeathKey))
            {
                RequestForceDeath();
            }

            if (Input.GetKeyDown(forceBustedKey))
            {
                RequestForceBusted();
            }
        }

        public void RequestForceDeath()
        {
            Debug.Log($"[DevFailure] Force DEATH requested for {targetPlayerId}.");
            
            // Only authority can trigger
            if (targetPlayerId != "Player_0")
            {
                Debug.LogWarning("[DevFailure] Authority check failed: Only Player_0 can trigger dev failure.");
                return;
            }

            OnDevForceDeathRequested?.Invoke();
            RestartResolver.RequestDeath(targetPlayerId);
        }

        public void RequestForceBusted()
        {
            Debug.Log($"[DevFailure] Force BUSTED requested for {targetPlayerId}.");

            // Only authority can trigger
            if (targetPlayerId != "Player_0")
            {
                Debug.LogWarning("[DevFailure] Authority check failed: Only Player_0 can trigger dev failure.");
                return;
            }

            OnDevForceBustedRequested?.Invoke();
            RestartResolver.RequestBusted(targetPlayerId);
        }
    }
#endif
}

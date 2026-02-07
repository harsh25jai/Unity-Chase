using UnityEngine;
using Core.GameState;
using Data.Session.Runtime;

namespace Systems.Police
{
    public class PursuitStateBridge : MonoBehaviour
    {
        [Header("Dependencies")]
        public GameStateManager gameStateManager;
        public SessionRuntimeData sessionData;
        public PolicePresenceSpawner presenceSpawner;

        private void Start()
        {
            if (gameStateManager == null) gameStateManager = GameStateManager.Instance;
            if (sessionData == null) sessionData = FindFirstObjectByType<SessionRuntimeData>();
            if (presenceSpawner == null) presenceSpawner = GetComponent<PolicePresenceSpawner>();

            if (gameStateManager != null)
            {
                gameStateManager.OnGameStateChanged += HandleGameStateChanged;
                gameStateManager.OnGameOver += HandleGameOver;
                gameStateManager.OnPlayerBusted += HandlePlayerBusted;
            }
        }

        private void OnDestroy()
        {
            if (gameStateManager != null)
            {
                gameStateManager.OnGameStateChanged -= HandleGameStateChanged;
                gameStateManager.OnGameOver -= HandleGameOver;
                gameStateManager.OnPlayerBusted -= HandlePlayerBusted;
            }
        }

        private void HandleGameStateChanged(SessionState state)
        {
            if (state == SessionState.Restarting || state == SessionState.Ended)
            {
                ShutdownPursuit();
            }
        }

        private void HandleGameOver() => ShutdownPursuit();
        private void HandlePlayerBusted() => ShutdownPursuit();

        public void ShutdownPursuit()
        {
            Debug.Log("[PursuitStateBridge] Shutting down pursuit systems.");

            if (sessionData != null)
            {
                sessionData.isPursuitActive = false;
                sessionData.currentWantedLevel = WantedLevel.None;
                sessionData.SetThreatLevel(0f);
            }

            if (presenceSpawner != null)
            {
                presenceSpawner.ClearPresence();
            }
        }
    }
}

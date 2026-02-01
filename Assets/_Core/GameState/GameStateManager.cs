using System;
using UnityEngine;
using Data.Session.Runtime;
using Data.Save.Manager;
using Systems.Player; // To listen for death events if needed directly, or via UnityEvent

namespace Core.GameState
{
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        [Header("Dependencies")]
        public SaveManager saveManager;
        public SessionRuntimeData sessionData;

        // Events
        public event Action<SessionState> OnGameStateChanged;
        public event Action OnSessionEnded;
        public event Action OnSessionRestarting;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (saveManager == null) saveManager = SaveManager.Instance;
            if (sessionData == null) sessionData = FindFirstObjectByType<SessionRuntimeData>();

            // Find Player Controller to listen for death? 
            // Or rely on Player calling a method here?
            // For decoupling, let's look for the PlayerStateController.
            var playerState = FindFirstObjectByType<Systems.Player.PlayerStateController>();
            if (playerState != null)
            {
                playerState.OnPlayerDeathRequested += () => EndSession("Death");
                playerState.OnPlayerBustedRequested += () => EndSession("Busted");
            }

            // Start logic
            SetState(SessionState.Active);
        }

        public void EndSession(string reason)
        {
            if (sessionData.currentSessionState == SessionState.Ended) return;

            Debug.Log($"[GameState] Session Ended. Reason: {reason}");
            SetState(SessionState.Ended);
            OnSessionEnded?.Invoke();

            // Here we could show UI, wait for input, then Restart
        }

        public void RestartSession()
        {
            if (sessionData.currentSessionState == SessionState.Restarting) return;

            Debug.Log("[GameState] Restarting Session...");
            SetState(SessionState.Restarting);
            OnSessionRestarting?.Invoke();

            // Coordinate Reset
            if (saveManager != null)
            {
                saveManager.OnPlayerDeath(); // Orchestrate the reset of runtime data
            }

            // Return to Active
            SetState(SessionState.Active);
            Debug.Log("[GameState] Session Active (Restarted).");
        }

        public void RestartFromCheckpoint()
        {
            Debug.Log("[GameState] Restarting from Checkpoint...");
            SetState(SessionState.Restarting);
            
            if (saveManager != null)
            {
                saveManager.LoadGame(); // Reloads persistent state (including last checkpoint data if structured that way)
            }
            
            SetState(SessionState.Active);
        }

        private void SetState(SessionState newState)
        {
            if (sessionData != null)
            {
                sessionData.currentSessionState = newState;
                OnGameStateChanged?.Invoke(newState);
            }
        }
    }
}

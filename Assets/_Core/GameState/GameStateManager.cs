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
        public event Action OnSessionEnded; // Generic
        public event Action OnGameOver; // Death
        public event Action OnPlayerBusted; // Arrested
        
        public event Action OnSessionRestarting;
        public event Action OnRestartFromCheckpointRequested;
        public event Action OnRestartFromBeginningRequested;

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
            Debug.Log($"[GameState] Session Ended. Reason: {reason}");
            SetState(SessionState.Ended);
            OnSessionEnded?.Invoke();

            if (reason == "Death") OnGameOver?.Invoke();
            if (reason == "Busted") OnPlayerBusted?.Invoke();

            // Here we could show UI, wait for input, then Restart
        }

        /// <summary>
        /// intelligently decides how to restart based on checkpoints.
        /// </summary>
        public void RequestSmartRestart()
        {
            // Check if we have a valid checkpoint in SaveData?
            // Since SaveData isn't cached publicly in SaveManager, we might verify if a file exists or check SessionData's current run.
            // Requirement: "Given SaveData indicates a valid last checkpoint exists... RestartFromCheckpoint".
            // Implementation: We'll assume SaveManager has persistence. If file exists, we *could* treat it as checkpointed if Game Design allows.
            // Or better: Check if SessionData.completedCheckpoints has any? 
            // Issue: SessionData resets on death (except checkpoints should persist).
            // Let's rely on SessionData having > 0 checkpoints OR check SaveManager logic.
            // For this implementation, I'll check SessionData.completedCheckpoints via SaveManager or direct.
            
            // Simplified check:
            bool hasCheckpoint = false;
            
            // If SessionData is still in memory and valid:
            if (sessionData != null && sessionData.completedCheckpoints.Count > 0)
            {
                hasCheckpoint = true;
            }
            // Or if SaveManager has a file (Assuming file implies progress):
            else if (saveManager != null && System.IO.File.Exists(System.IO.Path.Combine(Application.persistentDataPath, saveManager.saveFileName)))
            {
                // To be precise we should read it, but for now let's assume File Existence = Checkpoint/Progress
                hasCheckpoint = true; 
            }

            if (hasCheckpoint)
            {
                OnRestartFromCheckpointRequested?.Invoke();
                RestartFromCheckpoint();
            }
            else
            {
                OnRestartFromBeginningRequested?.Invoke();
                RestartSession();
            }
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

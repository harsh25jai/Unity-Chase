using System;
using UnityEngine;
using Data.Save.Manager;
using Data.Session.Runtime;

namespace Systems.Checkpoints
{
    public class CheckpointTriggerSystem : MonoBehaviour
    {
        // Events
        public event Action<string> OnCheckpointReached;
        public event Action OnCheckpointSaveRequested;

        [Header("Dependencies")]
        public SaveManager saveManager;
        
        // Internal
        private SessionRuntimeData _sessionData;

        private void Start()
        {
            if (saveManager == null)
            {
                saveManager = SaveManager.Instance;
            }
            
            _sessionData = FindFirstObjectByType<SessionRuntimeData>();
        }

        /// <summary>
        /// Call this when the player enters a checkpoint zone.
        /// </summary>
        public void TriggerCheckpoint(string checkpointID)
        {
            Debug.Log($"[CheckpointSystem] Triggered: {checkpointID}");

            if (CanSave())
            {
                // 1. Mark as completed in Session Data
                if (_sessionData != null)
                {
                    _sessionData.CompleteCheckpoint(checkpointID);
                }

                // 2. Notify events
                OnCheckpointReached?.Invoke(checkpointID);
                OnCheckpointSaveRequested?.Invoke();

                // 3. Request Save
                if (saveManager != null)
                {
                    saveManager.SaveGame();
                }
            }
            else
            {
                Debug.LogWarning("[CheckpointSystem] Checkpoint triggered but preconditions met (e.g. Player Dead?)");
            }
        }

        private bool CanSave()
        {
            if (_sessionData == null) return false;
            
            // 1. Session must be Active
            if (_sessionData.currentSessionState != SessionState.Active)
            {
                Debug.LogWarning("[CheckpointSystem] Cannot save: Session not Active.");
                return false;
            }

            // 2. Player must be Alive (Need PlayerData reference)
            // Ideally we'd cache this or look it up. For safety in this test turn:
            var playerData = FindFirstObjectByType<Data.Player.Runtime.PlayerRuntimeData>();
            if (playerData != null)
            {
                if (playerData.currentSurvivalState != Data.Player.Runtime.SurvivalState.Alive)
                {
                    Debug.LogWarning("[CheckpointSystem] Cannot save: Player not Alive.");
                    return false;
                }
            }
            // If no player data found? Assume safe or unsafe? Safe for now to allow basic tests without player instantiation if needed.
            // But real game should have player. 

            return true;
        }
    }
}

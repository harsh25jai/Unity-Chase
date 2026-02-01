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
            // Add conditions: Is Player Alive? Is Game Active?
            // For now, assume yes if the trigger was hit.
            return true;
        }
    }
}

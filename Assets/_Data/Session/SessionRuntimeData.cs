using System;
using System.Collections.Generic;
using UnityEngine;

namespace Data.Session.Runtime
{
    [Serializable]
    public enum WantedLevel
    {
        None,
        LowAlert,
        ActivePursuit,
        HighIntensity
    }

    [Serializable]
    public enum SessionState
    {
        Active,
        Ended,
        Restarting
    }

    [Serializable]
    public class SessionSaveData
    {
        public List<string> completedCheckpoints;
        // Cumulative stats could go here if they persist across sessions
    }

    public class SessionRuntimeData : MonoBehaviour
    {
        [Header("Time & Progress")]
        public float sessionTimer;
        
        [Header("Gameplay Metrics")]
        [Tooltip("Distance traveled per player in this session")]
        public Dictionary<string, float> playerDistances = new Dictionary<string, float>();
        
        [Tooltip("Global threat/intensity level (0-100)")]
        [Range(0f, 100f)]
        public float globalThreatLevel;

        [Tooltip("Current Wanted Level")]
        public WantedLevel currentWantedLevel;

        [Header("State")]
        public SessionState currentSessionState;

        [Header("Persistence")]
        public List<string> completedCheckpoints = new List<string>();

        // Events
        public event Action<string> OnCheckpointCompleted;
        public event Action<float> OnThreatLevelChanged;

        private void Update()
        {
            // Track session duration
            sessionTimer += Time.deltaTime;
        }

        /// <summary>
        /// Updates the distance traveled for a specific player.
        /// </summary>
        public void AddDistance(string playerID, float distance)
        {
            if (playerDistances.ContainsKey(playerID))
            {
                playerDistances[playerID] += distance;
            }
            else
            {
                playerDistances[playerID] = distance;
            }
        }

        /// <summary>
        /// Sets the current global threat level (e.g., police pursuit or enemy intensity).
        /// </summary>
        public void SetThreatLevel(float level)
        {
            globalThreatLevel = Mathf.Clamp(level, 0f, 100f);
            OnThreatLevelChanged?.Invoke(globalThreatLevel);
        }

        /// <summary>
        /// Marks a checkpoint as completed (Persistent).
        /// </summary>
        public void CompleteCheckpoint(string checkpointID)
        {
            if (!completedCheckpoints.Contains(checkpointID))
            {
                completedCheckpoints.Add(checkpointID);
                OnCheckpointCompleted?.Invoke(checkpointID);
                Debug.Log($"[Session] Checkpoint completed: {checkpointID}");
            }
        }

        /// <summary>
        /// Resets temporary session data (timer, active threat) on death/failure.
        /// </summary>
        public void ResetOnDeath()
        {
            sessionTimer = 0f;
            globalThreatLevel = 0f;
            currentWantedLevel = WantedLevel.None;
            currentSessionState = SessionState.Active;
            
            // Note: We do usually keep completedCheckpoints on death if they act as respawn points.
            // If the game is "Roguelike" and wipes progress, clear them here. 
            // Assuming standard checkpoint system -> Keep them.
            
            Debug.Log("[Session] Session temporary state reset.");
        }

        /// <summary>
        /// Returns data to be persisted by SaveManager.
        /// </summary>
        public SessionSaveData GetPersistentData()
        {
            SessionSaveData data = new SessionSaveData();
            data.completedCheckpoints = new List<string>(completedCheckpoints);
            return data;
        }

        /// <summary>
        /// Restores state from save.
        /// </summary>
        public void RestoreFromSave(SessionSaveData data)
        {
            if (data == null) return;

            completedCheckpoints.Clear();
            if (data.completedCheckpoints != null)
            {
                completedCheckpoints.AddRange(data.completedCheckpoints);
            }
        }
    }
}

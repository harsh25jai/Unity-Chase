using System;
using UnityEngine;
using Data.Session.Runtime;
using Data.Save.Repository;
using Data.Save.Checkpoint;

namespace Core.GameState
{
    public enum ResolverState
    {
        Monitoring,
        FailureDetected,
        ResolveDestination,
        RestartSignaled
    }

    public class RestartResolver : MonoBehaviour
    {
        [Header("References")]
        public SessionRuntimeData sessionData;
        
        [Header("Runtime State")]
        public ResolverState currentState = ResolverState.Monitoring;

        private SaveSlot0Repository _repository;

        // Events
        public static event Action<int, string> OnRestartFromCheckpointRequested;
        public static event Action OnRestartFromBeginningRequested;

        // Static hooks for survival systems to trigger (simulating the requirements)
        public static event Action<string> OnPlayerDeathRequested;
        public static event Action<string> OnPlayerBustedRequested;

        private void Awake()
        {
            _repository = new SaveSlot0Repository();
            if (sessionData == null) sessionData = FindFirstObjectByType<SessionRuntimeData>();
        }

        private void OnEnable()
        {
            OnPlayerDeathRequested += HandleFailure;
            OnPlayerBustedRequested += HandleFailure;
        }

        private void OnDisable()
        {
            OnPlayerDeathRequested -= HandleFailure;
            OnPlayerBustedRequested -= HandleFailure;
        }

        private void HandleFailure(string playerId)
        {
            if (currentState != ResolverState.Monitoring) return;
            
            // If already in a non-active session state (Restarting/Ended), ignore
            if (sessionData != null && sessionData.currentSessionState != SessionState.Active) return;

            Debug.Log($"[RestartResolver] Failure detected for {playerId}. Resolving destination...");
            currentState = ResolverState.FailureDetected;
            
            ResolveDestination(playerId);
        }

        private void ResolveDestination(string playerId)
        {
            currentState = ResolverState.ResolveDestination;

            // 1. Check for slot0 existence
            if (!_repository.DoesSlot0Exist(playerId))
            {
                Debug.Log("[RestartResolver] No Checkpoint found in Slot 0. Restarting from Beginning.");
                SignalRestartFromBeginning();
                return;
            }

            // 2. Read and Validate
            if (_repository.TryReadSlot0(out string json, playerId))
            {
                try
                {
                    CheckpointRecord record = JsonUtility.FromJson<CheckpointRecord>(json);
                    if (record != null && record.IsValid())
                    {
                        // Checkpoint valid (generic Checkpoint_01 for vertical slice)
                        Debug.Log($"[RestartResolver] Found valid checkpoint: {record.checkpointId}. Signal Checkpoint Reload.");
                        SignalRestartFromCheckpoint(0, record.checkpointId);
                        return;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[RestartResolver] Corrupt save found. Falling back. Error: {e.Message}");
                }
            }

            Debug.Log("[RestartResolver] Fallback: Restarting from Beginning.");
            SignalRestartFromBeginning();
        }

        private void SignalRestartFromBeginning()
        {
            currentState = ResolverState.RestartSignaled;
            OnRestartFromBeginningRequested?.Invoke();
            
            // Note: In a real flow, GameState would eventually set SessionState back to Active,
            // at which point we would return to Monitoring. 
            // For this slice, we'll reset state when the session state changes back.
            Invoke(nameof(ReturnToMonitoring), 0.5f); // Simple fallback for test/slice
        }

        private void SignalRestartFromCheckpoint(int slot, string checkpointId)
        {
            currentState = ResolverState.RestartSignaled;
            OnRestartFromCheckpointRequested?.Invoke(slot, checkpointId);
            
            Invoke(nameof(ReturnToMonitoring), 0.5f);
        }

        private void ReturnToMonitoring()
        {
            currentState = ResolverState.Monitoring;
        }

        // Static trigger methods for the tests/systems
        public static void RequestDeath(string playerId) => OnPlayerDeathRequested?.Invoke(playerId);
        public static void RequestBusted(string playerId) => OnPlayerBustedRequested?.Invoke(playerId);
    }
}

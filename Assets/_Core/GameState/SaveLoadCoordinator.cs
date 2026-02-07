using System;
using System.Collections;
using UnityEngine;
using Data.Session.Runtime;
using Data.Save.Orchestration;
using Systems.Checkpoints;
using Data.World.Checkpoint;

namespace Core.GameState
{
    public enum CoordinatorState
    {
        Idle,
        Saving,
        Loading
    }

    public class SaveLoadCoordinator : MonoBehaviour
    {
        [Header("References")]
        public CheckpointSaveOrchestrator saveOrchestrator;
        public CheckpointLoadOrchestrator loadOrchestrator;
        public SessionRuntimeData sessionData;
        public ChaseSaveBlockGate saveBlockGate;

        [Header("Runtime State")]
        public CoordinatorState currentState = CoordinatorState.Idle;

        // Events
        public event Action<int, string> OnSaveStarted;
        public event Action<int, string> OnSaveCompleted;
        public event Action<int, string, string> OnSaveFailed;
        
        public event Action<int> OnLoadStarted;
        public event Action<int, string> OnLoadCompleted;
        public event Action<int, string> OnLoadFailed;
        public event Action<string> OnPostLoadSpawnApplied;

        private void Awake()
        {
            // Auto-resolve
            if (saveOrchestrator == null) saveOrchestrator = FindFirstObjectByType<CheckpointSaveOrchestrator>();
            if (loadOrchestrator == null) loadOrchestrator = FindFirstObjectByType<CheckpointLoadOrchestrator>();
            if (sessionData == null) sessionData = FindFirstObjectByType<SessionRuntimeData>();
            if (saveBlockGate == null) saveBlockGate = FindFirstObjectByType<ChaseSaveBlockGate>();
        }

        private void OnEnable()
        {
            CheckpointTriggerBinder.OnCheckpointSaveRequested += HandleSaveRequest;
        }

        private void OnDisable()
        {
            CheckpointTriggerBinder.OnCheckpointSaveRequested -= HandleSaveRequest;
        }

        public void HandleSaveRequest(int slot, string checkpointId)
        {
            if (currentState != CoordinatorState.Idle) 
            {
                Debug.LogWarning("[SaveLoadCoordinator] Already busy. Ignoring save request.");
                return;
            }

            // Policy Gating
            if (saveBlockGate != null && !saveBlockGate.IsSaveAllowed())
            {
                Debug.Log("[SaveLoadCoordinator] Save blocked by ChaseSaveBlockGate policy.");
                OnSaveFailed?.Invoke(slot, checkpointId, "BlockedByPolicy");
                return;
            }
            else if (saveBlockGate == null && sessionData != null && sessionData.isPursuitActive)
            {
                // Fallback if gate is missing but session data exists
                Debug.Log("[SaveLoadCoordinator] Save blocked by legacy session policy: Active Pursuit.");
                OnSaveFailed?.Invoke(slot, checkpointId, "BlockedByPolicy");
                return;
            }

            StartCoroutine(PerformSaveSequence(slot, checkpointId));
        }

        private IEnumerator PerformSaveSequence(int slot, string checkpointId)
        {
            currentState = CoordinatorState.Saving;
            OnSaveStarted?.Invoke(slot, checkpointId);

            if (saveOrchestrator != null)
            {
                var triggerEvent = new CheckpointTriggerEvent(checkpointId);
                // The orchestrator handles the actual collection and repo call
                saveOrchestrator.PerformSave(triggerEvent);
            }

            yield return null; // Wait for write (mocking for now, repo is sync)

            OnSaveCompleted?.Invoke(slot, checkpointId);
            currentState = CoordinatorState.Idle;
        }

        public void HandleLoadRequest(int slot = 0)
        {
            if (currentState != CoordinatorState.Idle) return;
            StartCoroutine(PerformLoadSequence(slot));
        }

        private IEnumerator PerformLoadSequence(int slot)
        {
            currentState = CoordinatorState.Loading;
            OnLoadStarted?.Invoke(slot);

            bool success = false;
            if (loadOrchestrator != null)
            {
                success = loadOrchestrator.TryLoadCheckpoint(); // Assuming default player for now
            }

            if (success)
            {
                // Find spawn point and relocate
                // Based on requirements, we look for Checkpoint_01_Spawn as a baseline
                // but usually the record has the ID. Let's assume CP_01 for the slice.
                string spawnName = "Checkpoint_01_Spawn"; 
                GameObject spawn = GameObject.Find(spawnName);
                
                if (spawn != null)
                {
                    RelocatePlayer(spawn.transform);
                    OnLoadCompleted?.Invoke(slot, "Checkpoint_01");
                    OnPostLoadSpawnApplied?.Invoke("Checkpoint_01");
                }
                else
                {
                    Debug.LogWarning($"[SaveLoadCoordinator] Spawn marker {spawnName} not found after successful load.");
                    OnLoadFailed?.Invoke(slot, "InvalidSpawn");
                }
            }
            else
            {
                OnLoadFailed?.Invoke(slot, "CorruptOrIncomplete");
            }

            currentState = CoordinatorState.Idle;
            yield return null;
        }

        private void RelocatePlayer(Transform target)
        {
            // Find player - we look for Data components and then the parent transform
            var pData = FindFirstObjectByType<Data.Player.Runtime.PlayerRuntimeData>();
            if (pData != null)
            {
                var playerObj = pData.gameObject;
                // If it's a character controller, disable/enable to snap position
                var cc = playerObj.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                
                playerObj.transform.position = target.position;
                playerObj.transform.rotation = target.rotation;
                
                if (cc != null) cc.enabled = true;
                
                Debug.Log($"[SaveLoadCoordinator] Player relocated to {target.name}");
            }
        }
    }
}

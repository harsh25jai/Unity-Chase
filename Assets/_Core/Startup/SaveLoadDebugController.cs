using System;
using UnityEngine;
using Data.Save.Repository;
using Data.Save.Checkpoint;
using Data.Session.Runtime;
using Core.GameState;

namespace Core.Startup
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>
    /// Developer-only utility to validate the save/load loop and force game outcomes.
    /// No UI; operates purely via Debug Logs.
    /// </summary>
    public class SaveLoadDebugController : MonoBehaviour
    {
        [Header("Settings")]
        public string targetPlayerId = "Player_0";

        // Events
        public static event Action OnDevForceDeathRequested;
        public static event Action OnDevForceBustedRequested;
        public static event Action OnDevClearSlot0Requested;
        public static event Action OnDevPrintSaveStateRequested;

        private SaveSlot0Repository _repository;

        private void Awake()
        {
            _repository = new SaveSlot0Repository();
        }

        private void Update()
        {
            // F5: Print Status
            if (Input.GetKeyDown(KeyCode.F5))
            {
                PrintSaveState();
                OnDevPrintSaveStateRequested?.Invoke();
            }

            // F6: Clear Slot 0
            if (Input.GetKeyDown(KeyCode.F6))
            {
                ClearSlot0();
                OnDevClearSlot0Requested?.Invoke();
            }

            // F7: Force Death
            if (Input.GetKeyDown(KeyCode.F7))
            {
                ForceDeath();
                OnDevForceDeathRequested?.Invoke();
            }

            // F8: Force Busted
            if (Input.GetKeyDown(KeyCode.F8))
            {
                ForceBusted();
                OnDevForceBustedRequested?.Invoke();
            }
        }

        public void PrintSaveState()
        {
            Debug.Log($"[DEV] Checking Slot 0 for: {targetPlayerId}");
            
            if (_repository.TryReadSlot0(out string json, targetPlayerId))
            {
                try
                {
                    CheckpointRecord record = JsonUtility.FromJson<CheckpointRecord>(json);
                    if (record != null)
                    {
                        Debug.Log($"[DEV] Slot 0: [VALID] ID: {record.checkpointId} | Scene: {record.sceneName} | Timestamp: {record.createdAtUtc}");
                    }
                    else
                    {
                        Debug.LogWarning("[DEV] Slot 0 contains data but record is null.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[DEV] Slot 0 CORRUPT: {e.Message}");
                }
            }
            else
            {
                Debug.Log("[DEV] Slot 0 is EMPTY.");
            }
        }

        public void ClearSlot0()
        {
            Debug.Log($"[DEV] Clearing Slot 0 for: {targetPlayerId}");
            _repository.DeleteSlot0(targetPlayerId);
            
            if (!_repository.DoesSlot0Exist(targetPlayerId))
            {
                Debug.Log("[DEV] Slot 0 cleared successfully.");
            }
            else
            {
                Debug.LogError("[DEV] Failed to clear Slot 0!");
            }
        }

        public void ForceDeath()
        {
            Debug.Log($"[DEV] Forcing DEATH for: {targetPlayerId}");
            RestartResolver.RequestDeath(targetPlayerId);
        }

        public void ForceBusted()
        {
            Debug.Log($"[DEV] Forcing BUSTED for: {targetPlayerId}");
            RestartResolver.RequestBusted(targetPlayerId);
        }
    }
#endif
}

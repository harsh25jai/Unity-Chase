using System;
using UnityEngine;
using Data.Save.Repository;
using Data.Save.Checkpoint;

namespace Core.Startup
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>
    /// Debug-only utility to print the current Slot 0 save state for validation.
    /// </summary>
    public class DevSaveStatePrinter : MonoBehaviour
    {
        [Header("Settings")]
        public string targetPlayerId = "Player_0";
        public KeyCode printStatusKey = KeyCode.F5;

        // Events
        public static event Action OnDevPrintSaveStateRequested;

        private SaveSlot0Repository _repository;

        private void Awake()
        {
            _repository = new SaveSlot0Repository();
        }

        private void Update()
        {
            if (Input.GetKeyDown(printStatusKey))
            {
                PrintStatus();
            }
        }

        [ContextMenu("Print Save Status")]
        public void PrintStatus()
        {
            OnDevPrintSaveStateRequested?.Invoke();

            if (_repository == null) _repository = new SaveSlot0Repository();

            Debug.Log($"[DevSavePrinter] Checking Slot 0 for: {targetPlayerId}");
            
            if (_repository.TryReadSlot0(out string json, targetPlayerId))
            {
                try
                {
                    CheckpointRecord record = JsonUtility.FromJson<CheckpointRecord>(json);
                    if (record != null)
                    {
                        Debug.Log($"[DevSavePrinter] Slot 0: [VALID] ID: {record.checkpointId} | Scene: {record.sceneName} | Created: {record.createdAtUtc}");
                    }
                    else
                    {
                        Debug.LogWarning("[DevSavePrinter] Slot 0 contains data but CheckpointRecord is null.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[DevSavePrinter] Slot 0 CORRUPT or Invalid: {e.Message}");
                }
            }
            else
            {
                Debug.Log("[DevSavePrinter] Slot 0 is EMPTY.");
            }
        }
    }
#endif
}

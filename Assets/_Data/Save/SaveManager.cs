using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Data.Player.Runtime;
using Data.Vehicle.Runtime;
using Data.World.Runtime;
using Data.Session.Runtime;
using Data.Save;

namespace Data.Save.Manager
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        [Header("Settings")]
        public string saveFileName = "gamesave.json";
        public bool debugSaveOnExit = false;

        [Header("Runtime References")]
        [Tooltip("Reference to the player data component")]
        public PlayerRuntimeData playerRuntimeData;
        
        [Tooltip("Reference to the world state component")]
        public WorldState worldState;
        
        [Tooltip("Reference to the session state component")]
        public SessionRuntimeData sessionRuntimeData;

        // Vehicle list might change dynamically, so we might need to find them or register them
        [Tooltip("All active vehicles to be saved")]
        public List<VehicleRuntimeData> activeVehicles = new List<VehicleRuntimeData>();

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

        private void OnApplicationQuit()
        {
            if (debugSaveOnExit)
            {
                SaveGame();
            }
        }

        /// <summary>
        /// Registers a vehicle to be tracked for saving.
        /// </summary>
        public void RegisterVehicle(VehicleRuntimeData vehicle)
        {
            if (!activeVehicles.Contains(vehicle))
            {
                activeVehicles.Add(vehicle);
            }
        }

        /// <summary>
        /// Orchestrates the Reset process on player death (keep session persistent changes but reset temporary state).
        /// </summary>
        public void OnPlayerDeath()
        {
            Debug.Log("[SaveManager] Orchestrating ResetOnDeath...");

            if (playerRuntimeData != null) playerRuntimeData.ResetOnDeath();
            if (worldState != null) worldState.ResetOnDeath();
            if (sessionRuntimeData != null) sessionRuntimeData.ResetOnDeath();
            
            // Vehicles might reset or despawn depending on game design. 
            // Here we assume vehicles reset their temp state if they persist.
            foreach (var vehicle in activeVehicles)
            {
                if (vehicle != null) vehicle.ResetOnDestruction();
            }
        }

        /// <summary>
        /// Collects all data, serializes it, and writes to disk.
        /// </summary>
        public void SaveGame()
        {
            Debug.Log("[SaveManager] Saving game...");

            SaveGameData saveData = new SaveGameData();

            // 1. Collect Player Data
            if (playerRuntimeData != null)
            {
                saveData.playerData = playerRuntimeData.GetPersistentData();
            }

            // 2. Collect World Data
            if (worldState != null)
            {
                saveData.worldData = worldState.GetPersistentData();
            }

            // 3. Collect Session Data
            if (sessionRuntimeData != null)
            {
                saveData.sessionData = sessionRuntimeData.GetPersistentData();
            }

            // 4. Collect Vehicle Data
            saveData.vehicleData = new List<VehicleSaveData>();
            foreach (var vehicle in activeVehicles)
            {
                if (vehicle != null)
                {
                    saveData.vehicleData.Add(vehicle.GetPersistentData());
                }
            }

            // 5. Serialize and Write
            string json = JsonUtility.ToJson(saveData, true);
            string path = Path.Combine(Application.persistentDataPath, saveFileName);
            
            try 
            {
                File.WriteAllText(path, json);
                Debug.Log($"[SaveManager] Game saved to: {path}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to write save file: {e.Message}");
            }
        }

        /// <summary>
        /// Reads from disk, deserializes, and restores state to all managers.
        /// </summary>
        public void LoadGame()
        {
            string path = Path.Combine(Application.persistentDataPath, saveFileName);
            
            if (!File.Exists(path))
            {
                Debug.LogWarning("[SaveManager] No save string found at: " + path);
                return;
            }

            Debug.Log("[SaveManager] Loading game...");

            try
            {
                string json = File.ReadAllText(path);
                SaveGameData saveData = JsonUtility.FromJson<SaveGameData>(json);

                if (saveData == null)
                {
                    Debug.LogError("[SaveManager] Failed to deserialize save data.");
                    return;
                }

                // 1. Restore Player
                if (playerRuntimeData != null)
                {
                    playerRuntimeData.RestoreFromSave(saveData.playerData);
                }

                // 2. Restore World
                if (worldState != null)
                {
                    worldState.RestoreFromSave(saveData.worldData);
                }

                // 3. Restore Session
                if (sessionRuntimeData != null)
                {
                    sessionRuntimeData.RestoreFromSave(saveData.sessionData);
                }

                // 4. Restore Vehicles
                // Matching saved data to active vehicles by ID
                if (saveData.vehicleData != null)
                {
                    foreach (var vehicleSave in saveData.vehicleData)
                    {
                        // Find matching runtime vehicle.
                        // Ideally, we'd spawn them if they don't exist, but for now we look for existing ones.
                        var runtimeVehicle = activeVehicles.Find(v => v.vehicleID == vehicleSave.vehicleID);
                        if (runtimeVehicle != null)
                        {
                            runtimeVehicle.RestoreFromSave(vehicleSave);
                        }
                    }
                }
                
                Debug.Log("[SaveManager] Game loaded successfully.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to load save file: {e.Message}");
            }
        }

        [ContextMenu("Delete Save File")]
        public void DeleteSaveFile()
        {
            string path = Path.Combine(Application.persistentDataPath, saveFileName);
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log("[SaveManager] Save file deleted.");
            }
        }
    }
}

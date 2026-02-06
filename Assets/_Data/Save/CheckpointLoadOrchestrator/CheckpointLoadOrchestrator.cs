using System.Collections.Generic;
using UnityEngine;
using Data.Save.Checkpoint;
using Data.Save.Repository;
using Data.Player.Runtime;
using Data.Vehicle.Runtime;
using Data.Session.Runtime;
using Data.World.Runtime;

namespace Data.Save.Orchestration
{
    public class CheckpointLoadOrchestrator : MonoBehaviour
    {
        [Header("References")]
        public SessionRuntimeData sessionData;
        public WorldState worldState;

        private SaveSlot0Repository _repository;

        private void Awake()
        {
            _repository = new SaveSlot0Repository();
            
            // Auto-resolve if not assigned
            if (sessionData == null) sessionData = FindFirstObjectByType<SessionRuntimeData>();
            if (worldState == null) worldState = FindFirstObjectByType<WorldState>();
        }

        /// <summary>
        /// Attempts to load Slot 0 and apply it to the game world.
        /// </summary>
        public bool TryLoadCheckpoint(string playerId = "Player_0")
        {
            Debug.Log($"[LoadOrchestrator] Attempting to load Slot 0 for {playerId}");

            if (!_repository.TryReadSlot0(out string json, playerId))
            {
                Debug.LogWarning("[LoadOrchestrator] No Slot 0 save found. Starting fresh.");
                return false;
            }

            try
            {
                CheckpointRecord record = JsonUtility.FromJson<CheckpointRecord>(json);
                if (record == null || !record.IsValid())
                {
                    Debug.LogError("[LoadOrchestrator] Corrupt or invalid checkpoint record.");
                    return false;
                }

                ApplyToRuntime(record, playerId);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LoadOrchestrator] Load failed due to exception: {e.Message}");
                return false;
            }
        }

        public void ApplyToRuntime(CheckpointRecord record, string playerId)
        {
            Debug.Log($"[LoadOrchestrator] Applying Checkpoint: {record.checkpointId}");

            // 1. Apply World State
            if (worldState != null)
            {
                worldState.SetBiome(record.biomeId);
                worldState.SetRegion(record.isUrban ? RegionType.Urban : RegionType.Remote);
            }

            // 2. Apply Session Data
            if (sessionData != null)
            {
                sessionData.AddDistance(playerId, record.distanceTravelled);
                sessionData.currentWantedLevel = record.wantedLevel;
            }

            // 3. Apply Player Data
            var player = FindPlayer(playerId);
            if (player != null)
            {
                player.currentHealth = record.playerHealth;
                player.survivalNeeds = record.survivalNeeds;
                player.money = record.money;
                
                // Position should be handled by a higher-level Teleport or Spawn system,
                // but for this slice we assume they load in the right spot or we snap them.
            }

            // 4. Apply Vehicle Data (if applicable)
            if (!string.IsNullOrEmpty(record.vehicleId))
            {
                var vehicle = FindVehicle(record.vehicleId);
                if (vehicle != null)
                {
                    vehicle.currentHealth = record.vehicleHealth;
                    vehicle.engineHealth = record.engineHealth;
                    vehicle.currentFuel = record.fuel;
                    vehicle.tiresFlags = new List<string>(record.tiresFlags);
                    
                    // Logic to ensure the player is "in" this vehicle would happen here
                    // if not already handled by a bootstrap.
                }
                else
                {
                    Debug.LogWarning($"[LoadOrchestrator] Saved vehicle {record.vehicleId} not found in scene.");
                }
            }
        }

        private PlayerRuntimeData FindPlayer(string playerId)
        {
            var players = FindObjectsByType<PlayerRuntimeData>(FindObjectsSortMode.None);
            foreach (var p in players)
            {
                if (p.playerID == playerId) return p;
            }
            return null;
        }

        private VehicleRuntimeData FindVehicle(string vehicleId)
        {
            var vehicles = FindObjectsByType<VehicleRuntimeData>(FindObjectsSortMode.None);
            foreach (var v in vehicles)
            {
                if (v.vehicleID == vehicleId) return v;
            }
            return null;
        }
    }
}

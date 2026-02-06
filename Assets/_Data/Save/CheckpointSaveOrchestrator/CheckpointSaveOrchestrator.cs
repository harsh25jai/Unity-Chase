using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Data.Save.Checkpoint;
using Data.Save.Repository;
using Data.World.Checkpoint;
using Data.Player.Runtime;
using Data.Vehicle.Runtime;
using Data.Session.Runtime;
using Data.World.Runtime;
using Systems.World;

namespace Data.Save.Orchestration
{
    public class CheckpointSaveOrchestrator : MonoBehaviour
    {
        [Header("Gating")]
        public bool blockIfActiveChase = true;
        
        [Header("References")]
        public SessionRuntimeData sessionData;
        public WorldState worldState;

        private SaveSlot0Repository _repository;
        private HashSet<string> _completedCheckpointIds = new HashSet<string>();

        private void Awake()
        {
            _repository = new SaveSlot0Repository();
            
            // Auto-resolve if not assigned
            if (sessionData == null) sessionData = FindFirstObjectByType<SessionRuntimeData>();
            if (worldState == null) worldState = FindFirstObjectByType<WorldState>();
        }

        private void OnEnable()
        {
            CheckpointWorldTrigger.OnCheckpointTriggered += HandleCheckpointTrigger;
        }

        private void OnDisable()
        {
            CheckpointWorldTrigger.OnCheckpointTriggered -= HandleCheckpointTrigger;
        }

        private void HandleCheckpointTrigger(CheckpointTriggerEvent triggerEvent)
        {
            if (!triggerEvent.IsValid())
            {
                Debug.LogWarning("[SaveOrchestrator] Received invalid checkpoint trigger event.");
                return;
            }

            // Gating Rule 1: Pursuit
            if (blockIfActiveChase && sessionData != null && sessionData.isPursuitActive)
            {
                Debug.Log("[SaveOrchestrator] Save blocked: Active Pursuit in progress.");
                return;
            }

            // Gating Rule 2: Deduplication (already handled by dumb trigger cooldown, but we can double check here)
            // Or we ignore if it's already "recently" completed in this session if we want stricter rules.

            PerformSave(triggerEvent);
        }

        public void PerformSave(CheckpointTriggerEvent triggerEvent)
        {
            Debug.Log($"[SaveOrchestrator] Orchestrating save for checkpoint: {triggerEvent.checkpointId}");

            // 1. Create Record
            CheckpointRecord record = new CheckpointRecord();
            record.checkpointId = triggerEvent.checkpointId;
            record.sceneName = SceneManager.GetActiveScene().name;
            record.biomeId = string.IsNullOrEmpty(triggerEvent.biomeId) ? (worldState != null ? worldState.currentBiome : "") : triggerEvent.biomeId;
            record.isUrban = triggerEvent.isUrban;
            record.playerId = triggerEvent.triggeringPlayerId;

            // 2. Gather Player Data
            // In MP, find by triggerEvent.triggeringPlayerId
            var player = FindPlayer(triggerEvent.triggeringPlayerId);
            if (player != null)
            {
                record.playerHealth = player.currentHealth;
                record.survivalNeeds = player.survivalNeeds;
                record.money = player.money;
                record.wantedLevel = sessionData != null ? sessionData.currentWantedLevel : WantedLevel.None;
            }

            // 3. Gather Vehicle Data
            var vehicle = FindOccupiedVehicle(triggerEvent.triggeringPlayerId);
            if (vehicle != null)
            {
                record.vehicleId = vehicle.vehicleID;
                record.vehicleHealth = vehicle.currentHealth;
                record.engineHealth = vehicle.engineHealth;
                record.fuel = vehicle.currentFuel;
                record.tiresFlags = new List<string>(vehicle.tiresFlags);
            }

            // 4. Gather Session Progress
            if (sessionData != null)
            {
                if (sessionData.playerDistances.TryGetValue(triggerEvent.triggeringPlayerId, out float dist))
                {
                    record.distanceTravelled = dist;
                }
                
                // Track completion for internal session tracking
                sessionData.CompleteCheckpoint(triggerEvent.checkpointId);
            }

            // 5. Commit to Repository
            string json = JsonUtility.ToJson(record, true);
            _repository.WriteSlot0(json, triggerEvent.triggeringPlayerId);

            if (_repository.LastWriteSuccess)
            {
                Debug.Log($"[SaveOrchestrator] Checkpoint {triggerEvent.checkpointId} committed to Slot 0.");
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

        private VehicleRuntimeData FindOccupiedVehicle(string playerId)
        {
            // Simple heuristic: if player is in vehicle, find the one they are driving
            var pData = FindPlayer(playerId);
            if (pData != null && pData.isInVehicle)
            {
                var vehicles = FindObjectsByType<VehicleRuntimeData>(FindObjectsSortMode.None);
                foreach (var v in vehicles)
                {
                    if (v.driverPlayerID == playerId) return v;
                }
            }
            return null;
        }
    }
}

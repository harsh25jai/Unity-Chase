using System;
using System.Collections.Generic;
using UnityEngine;
using Data.Player.Runtime;
using Data.Vehicle.Runtime;
using Data.Session.Runtime;
using Data.World.Runtime;

namespace Data.Save.Checkpoint
{
    [Serializable]
    public class CheckpointRecord
    {
        [Header("Metadata")]
        public string saveVersion = "1.0.0";
        public long createdAtUtc;
        public string checkpointId;
        public string sceneName;
        public string biomeId;
        public bool isUrban; // isUrbanRemote policy
        public string playerId = "Player_0";

        [Header("Player Snapshot")]
        public float playerHealth;
        public SurvivalNeeds survivalNeeds;
        public float money;
        public WantedLevel wantedLevel;

        [Header("Vehicle Snapshot")]
        public string vehicleId;
        public float vehicleHealth;
        public float engineHealth;
        public float fuel;
        public List<string> tiresFlags = new List<string>();

        [Header("Session Snapshot")]
        public float distanceTravelled;

        public CheckpointRecord()
        {
            createdAtUtc = DateTime.UtcNow.Ticks;
        }

        /// <summary>
        /// Validates that the record is complete enough to load.
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(checkpointId) && !string.IsNullOrEmpty(sceneName);
        }

        public override string ToString()
        {
            return $"Checkpoint: {checkpointId} @ {sceneName} (Ver: {saveVersion})";
        }
    }
}

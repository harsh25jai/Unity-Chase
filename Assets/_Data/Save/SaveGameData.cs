using System;
using System.Collections.Generic;
using Data.Player.Runtime;
using Data.Vehicle.Runtime;
using Data.World.Runtime;
using Data.Session.Runtime;

namespace Data.Save
{
    [Serializable]
    public class SaveGameData
    {
        public string saveID;
        public long timestamp;
        public string gameVersion;
        
        // Metadata
        public string lastCheckpointID;
        public float totalPlayTime;

        // Sub-system Data
        public PlayerSaveData playerData;
        public List<VehicleSaveData> vehicleData;
        public WorldSaveData worldData;
        public SessionSaveData sessionData;

        public SaveGameData()
        {
            saveID = Guid.NewGuid().ToString();
            timestamp = DateTime.UtcNow.Ticks;
            gameVersion = UnityEngine.Application.version;
            
            // Initialize containers
            vehicleData = new List<VehicleSaveData>();
        }

        public void UpdateTimestamp()
        {
            timestamp = DateTime.UtcNow.Ticks;
        }

        public override string ToString()
        {
            return $"SaveID: {saveID}, Time: {new DateTime(timestamp)}, Ver: {gameVersion}";
        }
    }
}

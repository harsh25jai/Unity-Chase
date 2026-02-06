using System;

namespace Data.World.Checkpoint
{
    [Serializable]
    public class CheckpointTriggerEvent
    {
        public string checkpointId;
        public string biomeId;
        public bool isUrban;
        public string triggeringPlayerId;
        public long timestamp;

        public CheckpointTriggerEvent(string checkpointId, string triggeringPlayerId = "Player_0")
        {
            this.checkpointId = checkpointId;
            this.triggeringPlayerId = triggeringPlayerId;
            this.timestamp = DateTime.UtcNow.Ticks;
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(checkpointId);
        }
    }
}

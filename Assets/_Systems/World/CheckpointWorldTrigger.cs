using System;
using UnityEngine;
using Data.World.Checkpoint;

namespace Systems.World
{
    [RequireComponent(typeof(BoxCollider))]
    public class CheckpointWorldTrigger : MonoBehaviour
    {
        [Header("Configuration")]
        public string checkpointId;
        public string biomeId;
        public bool isUrban = true;
        
        [Header("Limits")]
        [Tooltip("Minimum time between repeat triggers for the same player (seconds).")]
        public float cooldownSeconds = 5.0f;

        // Central Dispatcher (mocking a central event system for now)
        public static event Action<CheckpointTriggerEvent> OnCheckpointTriggered;

        private float _lastTriggerTime;

        private void Awake()
        {
            var col = GetComponent<BoxCollider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Policy: Only trigger for player
            // Heuristic check for player: component check or tag
            // Assuming tag for MVP speed, or component check for robustness.
            if (other.CompareTag("Player") || other.GetComponentInParent<Data.Player.Runtime.PlayerRuntimeData>() != null)
            {
                if (Time.time < _lastTriggerTime + cooldownSeconds) return;

                _lastTriggerTime = Time.time;
                
                var payload = new CheckpointTriggerEvent(checkpointId);
                payload.biomeId = biomeId;
                payload.isUrban = isUrban;
                
                Debug.Log($"[CheckpointTrigger] Fired: {checkpointId}");
                OnCheckpointTriggered?.Invoke(payload);
            }
        }
    }
}

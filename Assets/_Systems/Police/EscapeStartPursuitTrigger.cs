using UnityEngine;
using Systems.Vehicle;
using Data.Session.Runtime;
using Data.World.Runtime;

namespace Systems.Police
{
    public class EscapeStartPursuitTrigger : MonoBehaviour
    {
        [Header("References")]
        public VehicleEntryController entryController;
        public SessionRuntimeData sessionData;
        public WorldState worldState;
        public PursuitStartSignaler signaler;

        [Header("Scene Markers")]
        public Transform bankAreaAnchor;
        public Transform escapeStartPhaseMarker;

        private bool _hasTriggered;

        private void Start()
        {
            if (entryController == null) entryController = FindFirstObjectByType<VehicleEntryController>();
            if (sessionData == null) sessionData = FindFirstObjectByType<SessionRuntimeData>();
            if (worldState == null) worldState = FindFirstObjectByType<WorldState>();
            if (signaler == null) signaler = GetComponent<PursuitStartSignaler>();

            if (entryController != null)
            {
                entryController.OnVehicleEntered += HandleVehicleEntered;
            }
        }

        private void OnDestroy()
        {
            if (entryController != null)
            {
                entryController.OnVehicleEntered -= HandleVehicleEntered;
            }
        }

        private void HandleVehicleEntered(string playerId, string vehicleId)
        {
            // Only trigger on first entry (idempotent)
            if (_hasTriggered) return;

            // Only trigger for primary local player (Player_0)
            if (playerId != "Player_0") return;

            // Check if game is in a state where pursuit can trigger (not dead/busted)
            if (sessionData != null && sessionData.currentSessionState != SessionState.Active)
            {
                Debug.LogWarning("[PursuitTrigger] Entry received while session is not active/ready. Ignoring.");
                return;
            }

            TriggerPursuit();
        }

        public void TriggerPursuit()
        {
            _hasTriggered = true;
            Debug.Log("[EscapeStartPursuitTrigger] Vehicle Entry Detected! Starting Pursuit...");

            // Context Validation
            if (worldState != null && worldState.currentRegionType != RegionType.Urban)
            {
                Debug.LogWarning("[PursuitTrigger] Pursuit triggered in non-urban context (Remote/Transition). Verify world configuration.");
            }

            // Update State
            if (sessionData != null)
            {
                sessionData.isPursuitActive = true;
            }

            // Signal
            if (signaler != null)
            {
                signaler.TriggerPursuit("EscapeStartVehicleEntry");
            }
        }

        /// <summary>
        /// Resets the trigger for a new session.
        /// </summary>
        public void ResetTrigger()
        {
            _hasTriggered = false;
        }
    }
}

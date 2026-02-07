using UnityEngine;
using Systems.Vehicle;
using Systems.Police;
using Data.Session.Runtime;

namespace Systems.Police
{
    public class MinimalPursuitActivator : MonoBehaviour
    {
        [Header("Dependencies")]
        public VehicleEntryStabilityController vehicleEntryController;
        public PursuitStartSignaler pursuitSignaler;
        public SessionRuntimeData sessionData;
        public PolicePresenceSpawner presenceSpawner;

        [Header("Settings")]
        public bool startPursuitOnVehicleEntry = true;
        public float initialIntensity = 30f;


        private void Start()
        {
            if (vehicleEntryController == null)
                vehicleEntryController = FindFirstObjectByType<VehicleEntryStabilityController>();

            if (pursuitSignaler == null)
                pursuitSignaler = FindFirstObjectByType<PursuitStartSignaler>();

            if (sessionData == null)
                sessionData = FindFirstObjectByType<SessionRuntimeData>();

            if (presenceSpawner == null)
                presenceSpawner = GetComponent<PolicePresenceSpawner>();

            if (vehicleEntryController != null)
            {
                vehicleEntryController.OnVehicleEntered += HandleVehicleEntered;
            }

        }

        private void OnDestroy()
        {
            if (vehicleEntryController != null)
            {
                vehicleEntryController.OnVehicleEntered -= HandleVehicleEntered;
            }
        }

        private void HandleVehicleEntered(string playerId, string vehicleId)
        {
            // Only authority (player0) triggers the session-level pursuit
            if (playerId != "Player_0") return;

            if (!startPursuitOnVehicleEntry) return;

            // Prevent duplicate starts if already active in session
            if (sessionData != null && sessionData.isPursuitActive)
            {
                Debug.Log("[MinimalPursuitActivator] Pursuit already active in session. Skipping trigger.");
                return;
            }

            StartPursuit();
        }

        public void StartPursuit()
        {
            Debug.Log("[MinimalPursuitActivator] Triggering Police Pursuit via Vehicle Entry.");

            if (sessionData != null)
            {
                sessionData.isPursuitActive = true;
            }

            if (pursuitSignaler != null)
            {
                pursuitSignaler.TriggerPursuit("VehicleEntry");
            }

            if (presenceSpawner != null)
            {
                presenceSpawner.SpawnInitialPresence();
            }

            // Emit the custom event if this component has its own (per requirements)
            OnPursuitStarted?.Invoke("VehicleEntry");
        }

        // Event for external listeners
        public event System.Action<string> OnPursuitStarted;
    }
}

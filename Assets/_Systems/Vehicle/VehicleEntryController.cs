using System;
using UnityEngine;
using Data.Vehicle.Runtime;
using Data.Player.Runtime;

namespace Systems.Vehicle
{
    public class VehicleEntryController : MonoBehaviour
    {
        [Header("References")]
        public VehicleDiscoveryController discoveryController;
        public PlayerRuntimeData playerData;

        // Events
        public event Action<string> OnVehicleEnterRequested; // vehicleID
        public event Action<string, string> OnVehicleEntered; // playerID, vehicleID
        public event Action<string, string> OnVehicleExited; // playerID, vehicleID

        // Mock Input - should ideally come from InputSystem
        [Header("Debug Input")]
        public bool debugSimulateInteract;

        private void Start()
        {
            if (discoveryController == null) discoveryController = GetComponent<VehicleDiscoveryController>();
            if (playerData == null) playerData = GetComponentInParent<PlayerRuntimeData>();
        }

        private void Update()
        {
            if (debugSimulateInteract)
            {
                debugSimulateInteract = false;
                RequestInteract();
            }
        }

        /// <summary>
        /// Public method to be called by InputSystem or UI
        /// </summary>
        public void RequestInteract()
        {
            if (playerData == null) return;

            if (playerData.isInVehicle)
            {
                RequestExit();
            }
            else
            {
                RequestEntry();
            }
        }

        private void RequestEntry()
        {
            if (discoveryController == null || discoveryController.currentTargetVehicle == null)
            {
                Debug.Log("[VehicleEntry] No vehicle in range to enter.");
                return;
            }

            var target = discoveryController.currentTargetVehicle;

            // Integrity Check
            if (target.currentDegradationState == VehicleDegradationState.Disabled)
            {
                Debug.Log($"[VehicleEntry] Cannot enter {target.vehicleID}: Vehicle is Disabled.");
                return;
            }

            if (target.isOccupied)
            {
                Debug.Log($"[VehicleEntry] Cannot enter {target.vehicleID}: Already occupied by {target.driverPlayerID}.");
                return;
            }

            Debug.Log($"[VehicleEntry] Requesting entry to {target.vehicleID}...");
            OnVehicleEnterRequested?.Invoke(target.vehicleID);

            // Execute Entry (In a real networked game, this would be optimistic or callback-based)
            PerformEntry(target);
        }

        private void RequestExit()
        {
            if (!playerData.isInVehicle) return;
            
            Debug.Log($"[VehicleEntry] Requesting exit from {playerData.currentVehicleID}...");
            PerformExit();
        }

        private void PerformEntry(VehicleRuntimeData vehicle)
        {
            Debug.Log($"[VehicleEntry] Entering {vehicle.vehicleID}.");
            OnVehicleEntered?.Invoke(playerData.playerID, vehicle.vehicleID);
        }

        private void PerformExit()
        {
            string oldVehicle = playerData.currentVehicleID;
            Debug.Log($"[VehicleEntry] Exiting {oldVehicle}.");
            OnVehicleExited?.Invoke(playerData.playerID, oldVehicle);
        }
    }
}

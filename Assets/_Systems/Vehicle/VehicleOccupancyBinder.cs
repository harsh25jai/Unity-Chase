using System;
using UnityEngine;
using Data.Vehicle.Runtime;
using Data.Player.Runtime;

namespace Systems.Vehicle
{
    public class VehicleOccupancyBinder : MonoBehaviour
    {
        [Header("References")]
        public VehicleEntryController entryController;
        public PlayerRuntimeData playerData;

        private void Start()
        {
            if (entryController == null) entryController = GetComponent<VehicleEntryController>();
            if (playerData == null) playerData = GetComponentInParent<PlayerRuntimeData>();

            if (entryController != null)
            {
                entryController.OnVehicleEntered += HandleVehicleEntered;
                entryController.OnVehicleExited += HandleVehicleExited;
            }
        }

        private void OnDestroy()
        {
            if (entryController != null)
            {
                entryController.OnVehicleEntered -= HandleVehicleEntered;
                entryController.OnVehicleExited -= HandleVehicleExited;
            }
        }

        private void HandleVehicleEntered(string playerId, string vehicleId)
        {
            // 1. Update Player Data
            playerData.isInVehicle = true;
            playerData.currentVehicleID = vehicleId;

            // 2. Update Vehicle Data
            var vehicle = FindVehicle(vehicleId);
            if (vehicle != null)
            {
                vehicle.driverPlayerID = playerId;
                
                // 3. Visuals - Disable Player Model/Collider/Physics
                // For MVP, we just disable the player root mesh or something?
                // Actually, often we just treat this script as being ON the player, so we might disable the player controller.
                // But we don't want to disable *this* component.
                // Assuming PlayerRoot has a visual child.
                SetPlayerVisuals(false);
                
                // 4. Snap Player to Vehicle for consistency (optional)
                playerData.transform.SetParent(vehicle.transform);
                playerData.transform.localPosition = Vector3.zero; 
            }
        }

        private void HandleVehicleExited(string playerId, string vehicleId)
        {
             // 1. Update Player Data
            playerData.isInVehicle = false;
            playerData.currentVehicleID = "";

            // 2. Update Vehicle Data
            var vehicle = FindVehicle(vehicleId);
            if (vehicle != null)
            {
                if (vehicle.driverPlayerID == playerId)
                    vehicle.driverPlayerID = "";
                
                // 3. Visuals - Re-enable
                playerData.transform.SetParent(null); // Unparent
                // Should place player at exit point (e.g. left of vehicle), but for now current pos is fine or offset.
                playerData.transform.position = vehicle.transform.position - vehicle.transform.right * 2.0f; 
                SetPlayerVisuals(true);
            }
        }

        private VehicleRuntimeData FindVehicle(string vehicleId)
        {
            // Expensive lookup for prototype; in prod use a Manager registry
            var vehicles = FindObjectsByType<VehicleRuntimeData>(FindObjectsSortMode.None);
            foreach(var v in vehicles)
            {
                if (v.vehicleID == vehicleId) return v;
            }
            return null;
        }

        private void SetPlayerVisuals(bool active)
        {
            // Simplistic toggle of renderers for now. 
            // In a real system, we'd switch State Machine state which handles this.
            var renderers = playerData.GetComponentsInChildren<Renderer>();
            foreach(var r in renderers) r.enabled = active;

            var colliders = playerData.GetComponentsInChildren<Collider>();
            foreach(var c in colliders) c.enabled = active;
        }
    }
}

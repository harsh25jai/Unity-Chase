using UnityEngine;
using Data.Vehicle.Runtime;
using Data.Player.Runtime;

namespace Systems.Vehicle
{
    public class VehicleOccupancyGuard : MonoBehaviour
    {
        /// <summary>
        /// Checks if a vehicle is safe to enter for a specific player.
        /// </summary>
        public bool IsVehicleAvailable(VehicleRuntimeData vehicle, string requesterPlayerId)
        {
            if (vehicle == null) return false;

            // 1. Check basic occupancy
            if (vehicle.isOccupied)
            {
                // If it's already occupied by the requester, technically it's "available" (they are already in it)
                if (vehicle.driverPlayerID == requesterPlayerId) return true;

                Debug.LogWarning($"[VehicleOccupancyGuard] Vehicle {vehicle.vehicleID} is already occupied by {vehicle.driverPlayerID}.");
                return false;
            }

            // 2. Check for ownership/authority (Simplified for vertical slice)
            // Multi-player safety: Only Player_0 can claim occupancy for now unless designated otherwise.
            if (requesterPlayerId != "Player_0")
            {
                Debug.LogWarning($"[VehicleOccupancyGuard] Player {requesterPlayerId} attempted to claim occupancy, but only authority (Player_0) is allowed in this slice.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Atomically assigns occupancy in the data.
        /// </summary>
        public void AssignOccupancy(VehicleRuntimeData vehicle, string playerId)
        {
            if (vehicle != null)
            {
                vehicle.driverPlayerID = playerId;
                Debug.Log($"[VehicleOccupancyGuard] Occupancy assigned: Player {playerId} -> Vehicle {vehicle.vehicleID}");
            }
        }

        /// <summary>
        /// Clears occupancy for a specific player.
        /// </summary>
        public void ClearOccupancy(VehicleRuntimeData vehicle, string playerId)
        {
            if (vehicle != null && vehicle.driverPlayerID == playerId)
            {
                vehicle.driverPlayerID = string.Empty;
                Debug.Log($"[VehicleOccupancyGuard] Occupancy cleared for Vehicle {vehicle.vehicleID}.");
            }
        }
    }
}

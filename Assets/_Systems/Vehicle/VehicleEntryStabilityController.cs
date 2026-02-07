using System;
using UnityEngine;
using Data.Player.Runtime;
using Data.Vehicle.Runtime;
using Data.Session.Runtime;
using Core.GameState;

namespace Systems.Vehicle
{
    public enum VehicleEntryState
    {
        OnFoot,
        Entering,
        InVehicle,
        Exiting
    }

    public class VehicleEntryStabilityController : MonoBehaviour
    {
        [Header("State")]
        public VehicleEntryState currentState = VehicleEntryState.OnFoot;
        public VehicleRuntimeData activeVehicle;

        [Header("Dependencies")]
        public PlayerRuntimeData playerData;
        public VehicleProximitySelector proximitySelector;
        public VehicleOccupancyGuard occupancyGuard;
        public GameStateManager gameStateManager;

        // Events
        public event Action<string, string> OnVehicleEntered; // playerId, vehicleId
        public event Action<string, string> OnVehicleExited; // playerId, vehicleId
        public event Action<string> OnVehicleEntryRejected; // reason

        private void Awake()
        {
            if (playerData == null) playerData = GetComponentInParent<PlayerRuntimeData>();
            if (proximitySelector == null) proximitySelector = GetComponent<VehicleProximitySelector>();
            if (occupancyGuard == null) occupancyGuard = GetComponent<VehicleOccupancyGuard>();
            if (gameStateManager == null) gameStateManager = GameStateManager.Instance;
        }

        private void OnEnable()
        {
            if (gameStateManager != null)
                gameStateManager.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            if (gameStateManager != null)
                gameStateManager.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void HandleGameStateChanged(SessionState state)
        {
            // Reset or disable interaction during loading/restarting
            if (state == SessionState.Restarting || state == SessionState.Booting)
            {
                activeVehicle = null;
                currentState = VehicleEntryState.OnFoot;
                Debug.Log("[VehicleEntryStability] Resetting state due to GameState change.");
            }
        }

        public void HandleInteractionIntent()
        {
            // Block interaction if game is not active
            if (gameStateManager != null && gameStateManager.sessionData != null)
            {
                if (gameStateManager.sessionData.currentSessionState != SessionState.Active)
                {
                    Debug.Log("[VehicleEntryStability] Interaction ignored: Game is not Active.");
                    return;
                }
            }

            switch (currentState)
            {
                case VehicleEntryState.OnFoot:
                    TryEnterVehicle();
                    break;
                case VehicleEntryState.InVehicle:
                    TryExitVehicle();
                    break;
                default:
                    Debug.Log($"[VehicleEntryStability] Interaction ignored: Busy in state {currentState}.");
                    break;
            }
        }

        private void TryEnterVehicle()
        {
            if (proximitySelector == null) return;

            VehicleRuntimeData target = proximitySelector.FindNearestVehicle();

            if (target == null)
            {
                OnVehicleEntryRejected?.Invoke("NoVehicleNearby");
                Debug.Log("[VehicleEntryStability] Entry rejected: No vehicle nearby.");
                return;
            }

            // Occupancy check
            if (!occupancyGuard.IsVehicleAvailable(target, playerData.playerID))
            {
                OnVehicleEntryRejected?.Invoke("VehicleUnavailable");
                return;
            }

            StartCoroutine(PerformEntrySequence(target));
        }

        private void TryExitVehicle()
        {
            if (activeVehicle == null)
            {
                currentState = VehicleEntryState.OnFoot;
                return;
            }

            StartCoroutine(PerformExitSequence(activeVehicle));
        }

        private System.Collections.IEnumerator PerformEntrySequence(VehicleRuntimeData vehicle)
        {
            currentState = VehicleEntryState.Entering;
            Debug.Log($"[VehicleEntryStability] Entering {vehicle.vehicleID}...");

            // In a real system, wait for animation duration here
            yield return new WaitForSeconds(0.1f);

            // Commit data changes
            occupancyGuard.AssignOccupancy(vehicle, playerData.playerID);
            playerData.isInVehicle = true;
            playerData.currentVehicleID = vehicle.vehicleID;
            activeVehicle = vehicle;

            currentState = VehicleEntryState.InVehicle;
            OnVehicleEntered?.Invoke(playerData.playerID, vehicle.vehicleID);
            Debug.Log($"[VehicleEntryStability] Successfully entered {vehicle.vehicleID}.");
        }

        private System.Collections.IEnumerator PerformExitSequence(VehicleRuntimeData vehicle)
        {
            currentState = VehicleEntryState.Exiting;
            Debug.Log($"[VehicleEntryStability] Exiting {vehicle.vehicleID}...");

            // In a real system, wait for animation duration here
            yield return new WaitForSeconds(0.1f);

            // Commit data changes
            occupancyGuard.ClearOccupancy(vehicle, playerData.playerID);
            playerData.isInVehicle = false;
            playerData.currentVehicleID = string.Empty;
            activeVehicle = null;

            currentState = VehicleEntryState.OnFoot;
            OnVehicleExited?.Invoke(playerData.playerID, vehicle.vehicleID);
            Debug.Log($"[VehicleEntryStability] Successfully exited vehicle.");
        }
    }
}

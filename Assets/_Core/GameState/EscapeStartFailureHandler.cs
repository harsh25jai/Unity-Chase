using System;
using UnityEngine;
using Systems.Player;
using Systems.Vehicle;
using Data.Session.Runtime;
using Data.Vehicle.Runtime;

namespace Core.GameState
{
    public enum FailureOutcome
    {
        Dead,
        Busted,
        VehicleLost
    }

    public class EscapeStartFailureHandler : MonoBehaviour
    {
        [Header("Settings")]
        public bool isVehicleRequired = true;

        [Header("State")]
        private bool _isOutcomePending;

        // Events
        public event Action<FailureOutcome> OnEscapeStartFailed;
        public event Action OnResetPhaseStarted;

        private void Start()
        {
            // We listen for terminal events from the primary systems.
            // GameStateManager already listens for PlayerStateController death/busted,
            // but we add this layer for phase-specific logic (e.g. VehicleLost) and idempotency.
            
            var playerStates = FindObjectsByType<PlayerStateController>(FindObjectsSortMode.None);
            foreach (var ps in playerStates)
            {
                ps.OnPlayerDeathRequested += () => HandleTerminalOutcome(FailureOutcome.Dead, "Player_0");
                ps.OnPlayerBustedRequested += () => HandleTerminalOutcome(FailureOutcome.Busted, "Player_0");
            }

            var vehicles = FindObjectsByType<VehicleStateController>(FindObjectsSortMode.None);
            foreach (var v in vehicles)
            {
                v.OnVehicleDisabled += () => HandleVehicleDisabled(v);
            }
        }

        private void HandleVehicleDisabled(VehicleStateController vehicle)
        {
            if (!isVehicleRequired) return;

            // Policy: Failure if the occupied vehicle is destroyed.
            if (vehicle.vehicleRuntimeData != null && vehicle.vehicleRuntimeData.isOccupied)
            {
                // In multiplayer, we might check if ANY player is in it.
                // For now, if it was occupied by the primary driver.
                if (vehicle.vehicleRuntimeData.driverPlayerID == "Player_0")
                {
                    HandleTerminalOutcome(FailureOutcome.VehicleLost, "Player_0");
                }
            }
        }

        private void HandleTerminalOutcome(FailureOutcome outcome, string playerId)
        {
            if (_isOutcomePending) return;

            // Validate phase context via SessionData
            var sessionData = FindFirstObjectByType<SessionRuntimeData>();
            if (sessionData == null || sessionData.currentSessionState != SessionState.Active)
            {
                return;
            }

            // Multi-player Policy: If session is session-wide failure (standard), trigger outcome.
            // If it were per-player, we'd check if playerId == LocalPlayer.
            // Assuming default session-wide failure for EscapeStart.

            _isOutcomePending = true;
            Debug.Log($"[EscapeStartFailureHandler] Phase Failure Detected: {outcome} for {playerId}");

            OnResetPhaseStarted?.Invoke();
            OnEscapeStartFailed?.Invoke(outcome);

            // Signal GameStateManager
            if (GameStateManager.Instance != null)
            {
                // GameStateManager handles the actual state transition and restart routing
                GameStateManager.Instance.EndSession(outcome.ToString());
                
                // Route smart restart (Beginning vs Checkpoint)
                GameStateManager.Instance.RequestSmartRestart();
            }
        }

        /// <summary>
        /// Resets the handler for a new attempt.
        /// </summary>
        public void ResetHandler()
        {
            _isOutcomePending = false;
        }
    }
}

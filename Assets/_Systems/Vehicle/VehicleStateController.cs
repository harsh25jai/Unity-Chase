using System;
using UnityEngine;
using Data.Vehicle.Runtime;

namespace Systems.Vehicle
{
    public class VehicleStateController : MonoBehaviour
    {
        [Header("Backend Data")]
        [Tooltip("Direct reference to the Runtime Data container")]
        public VehicleRuntimeData vehicleRuntimeData;

        // Events
        public event Action<float, float> OnHealthChanged; // current, max
        public event Action<VehicleDegradationState> OnDegradationStateChanged;
        public event Action OnVehicleDisabled;
        public event Action OnVehicleZoneDamaged;

        // Internal
        private bool _isOccupied;

        private void Start()
        {
            if (vehicleRuntimeData == null)
            {
                vehicleRuntimeData = GetComponent<VehicleRuntimeData>();
                if (vehicleRuntimeData == null)
                {
                    Debug.LogError("[VehicleStateController] No VehicleRuntimeData found!");
                    enabled = false;
                    return;
                }
            }
            
            EvaluateState();
        }

        /// <summary>
        /// Applies damage to the vehicle.
        /// </summary>
        public void ApplyDamage(float amount)
        {
            if (vehicleRuntimeData.currentDegradationState == VehicleDegradationState.Disabled) return;

            vehicleRuntimeData.currentHealth = Mathf.Max(0, vehicleRuntimeData.currentHealth - amount);
            
            Debug.Log($"[VehicleState] Took {amount} damage. Current Health: {vehicleRuntimeData.currentHealth}");

            // Notify
            OnHealthChanged?.Invoke(vehicleRuntimeData.currentHealth, vehicleRuntimeData.maxHealth);
            OnVehicleZoneDamaged?.Invoke(); // Placeholder for zone logic

            EvaluateState();
        }

        public void SetOccupied(bool occupied)
        {
            _isOccupied = occupied;
            // logic for ignition or activation could go here
        }

        private void EvaluateState()
        {
            float healthPercentage = vehicleRuntimeData.currentHealth / vehicleRuntimeData.maxHealth;
            VehicleDegradationState newState = vehicleRuntimeData.currentDegradationState;

            if (healthPercentage <= 0f)
            {
                newState = VehicleDegradationState.Disabled;
            }
            else if (healthPercentage < 0.4f)
            {
                newState = VehicleDegradationState.Critical;
            }
            else if (healthPercentage < 0.75f)
            {
                newState = VehicleDegradationState.Degraded;
            }
            else
            {
                newState = VehicleDegradationState.Operational;
            }

            // Detect Transition
            if (vehicleRuntimeData.currentDegradationState != newState)
            {
                vehicleRuntimeData.currentDegradationState = newState;
                OnDegradationStateChanged?.Invoke(newState);
                Debug.Log($"[VehicleState] State changed to: {newState}");

                if (newState == VehicleDegradationState.Disabled)
                {
                    OnVehicleDisabled?.Invoke();
                }
            }
        }
    }
}

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

        public void ApplyZoneDamage(string zoneID, float amount)
        {
            if (vehicleRuntimeData.currentDegradationState == VehicleDegradationState.Disabled) return;
            
            // 1. Update Zone Health
            if (vehicleRuntimeData.zoneHealths.ContainsKey(zoneID))
            {
                vehicleRuntimeData.zoneHealths[zoneID] -= amount;
            }
            else
            {
                vehicleRuntimeData.zoneHealths[zoneID] = -amount; // Start negative if tracking damage, or assume max? 
                // Let's assume zones start at 0 damage taken or 100 health. 
                // Simplified: Start at 100 on first hit? Or just accumulate damage?
                // Request implies "body zone value updates", let's assume it tracks REMAINING health.
                // But we didn't init it. Let's assume we init on first hit to "Max - amount".
                // Better: Just track damage taken if we don't have max per zone defined.
                // Re-reading: "body zone value updates". 
                // Let's set a default max if not present? Or just subtract.
                // For simplicity: We track *Health*. If missing, init to 100.
                vehicleRuntimeData.zoneHealths[zoneID] = 100f - amount;
            }

            Debug.Log($"[VehicleState] Zone {zoneID} hit. Value: {vehicleRuntimeData.zoneHealths[zoneID]}");

            // 2. Apply Overall Damage
            ApplyDamage(amount);

            // 3. Notify
            OnVehicleZoneDamaged?.Invoke();
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

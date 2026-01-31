using System;
using System.Collections.Generic;
using UnityEngine;

namespace Data.Vehicle.Runtime
{
    [Serializable]
    public class VehicleUpgrade
    {
        public string upgradeID;
        public int level;
        // Add other upgrade stats here
    }

    [Serializable]
    public class VehicleBoost
    {
        public string boostID;
        public float duration;
        public float multiplier; // Example effect
    }

    [Serializable]
    public class VehicleSaveData
    {
        public string vehicleID;
        public List<VehicleUpgrade> upgrades;
        public bool isUnlocked;
    }

    public class VehicleRuntimeData : MonoBehaviour
    {
        [Header("Identity")]
        public string vehicleID;
        public string ownerPlayerID;

        [Header("Status")]
        public float currentHealth;
        public float maxHealth = 1000f;
        public float currentFuel;
        public float maxFuel = 100f;

        [Header("Physics State")]
        public Vector3 currentPosition;
        public Vector3 currentVelocity;
        
        // Stored initial state for reset
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;

        [Header("Progression")]
        public bool isUnlocked = true;
        public List<VehicleUpgrade> upgrades = new List<VehicleUpgrade>();
        public List<VehicleBoost> activeBoosts = new List<VehicleBoost>();

        private void Start()
        {
            // Capture initial spawn point
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            
            // Initialize
            currentHealth = maxHealth;
            currentFuel = maxFuel;
        }

        private void Update()
        {
            // Continuously update physics state
            currentPosition = transform.position;
            // Assuming Rigidbody usage for velocity, but keeping it generic for now
            // If Rigidbody exists, we'd read velocity from it. 
            // currentVelocity = rb.velocity;
            
            // Update Boosts (reduce duration) - simplified logic
            for (int i = activeBoosts.Count - 1; i >= 0; i--)
            {
                activeBoosts[i].duration -= Time.deltaTime;
                if (activeBoosts[i].duration <= 0)
                {
                    activeBoosts.RemoveAt(i);
                }
            }
        }

        public void SetOwner(string playerID)
        {
            ownerPlayerID = playerID;
        }

        /// <summary>
        /// Resets on death/destruction: Position (or handling), temporary boosts, damage.
        /// </summary>
        public void ResetOnDestruction()
        {
            currentHealth = maxHealth;
            currentFuel = maxFuel;
            
            // Reset transforms
            transform.position = _initialPosition;
            transform.rotation = _initialRotation;
            currentPosition = _initialPosition;
            
            // Clear temporary boosts
            activeBoosts.Clear();
            
            Debug.Log($"[VehicleRuntimeData] Vehicle {vehicleID} reset on destruction.");
        }

        /// <summary>
        /// Returns data to be persisted by SaveManager.
        /// </summary>
        public VehicleSaveData GetPersistentData()
        {
            VehicleSaveData data = new VehicleSaveData();
            data.vehicleID = vehicleID;
            data.isUnlocked = isUnlocked;
            data.upgrades = new List<VehicleUpgrade>(upgrades); // Copy upgrades
            return data;
        }

        /// <summary>
        /// Restores state from save.
        /// </summary>
        public void RestoreFromSave(VehicleSaveData data)
        {
            if (data == null) return;
            
            // Verify ID match if necessary
            // vehicleID = data.vehicleID; 

            isUnlocked = data.isUnlocked;
            
            // Restore upgrades
            upgrades.Clear();
            if (data.upgrades != null)
            {
                upgrades.AddRange(data.upgrades);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Data.Player.Runtime
{
    // Defined here since global definitions were not found
    [Serializable]
    public class InventoryItem
    {
        public string itemID;
        public int quantity;
        public bool isSessionOnly; // To distinguish for reset logic
    }

    [Serializable]
    public class RuntimeBuff
    {
        public string buffID;
        public float duration;
        public bool isPersistent;
    }

    [Serializable]
    public enum SurvivalState
    {
        Alive,
        Incapacitated,
        Dead,
        Busted
    }

    [Serializable]
    public class PlayerSaveData
    {
        public string playerID;
        public SurvivalState survivalState;
        public int experiencePoints;
        public List<InventoryItem> persistentInventory;
        // Add other persistent fields as needed
    }

    public class PlayerRuntimeData : MonoBehaviour
    {
        [Header("Identity")]
        public string playerID;

        [Header("Survival State")]
        public SurvivalState currentSurvivalState;

        [Header("Health & Status")]
        [Tooltip("Current Health Points")]
        public float currentHealth;
        public float maxHealth = 100f;
        
        [Tooltip("Current Stamina Points")]
        public float currentStamina;
        public float maxStamina = 100f;
        
        [Tooltip("Current Shield Points")]
        public float currentShield;
        public float maxShield = 50f;

        [Header("Position & Rotation")]
        public Vector3 currentPosition;
        public Quaternion currentRotation;

        [Header("Inventory & Progression")]
        public int experiencePoints;
        public List<InventoryItem> inventory = new List<InventoryItem>();
        public List<RuntimeBuff> activeBuffs = new List<RuntimeBuff>();

        // Internal state
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;

        private void Awake()
        {
            // Initialize basic state
            currentHealth = maxHealth;
            currentStamina = maxStamina;
            currentShield = maxShield;
        }

        private void Start()
        {
            // Capture initial spawn point
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
        }

        private void Update()
        {
            // Continuously update in-memory state from Transform
            currentPosition = transform.position;
            currentRotation = transform.rotation;
        }

        /// <summary>
        /// Resets session-specific data: Position, temporary buffs, session-only inventory.
        /// </summary>
        public void ResetOnDeath()
        {
            // Reset Survival State
            currentSurvivalState = SurvivalState.Alive;

            // Reset Vitals
            currentHealth = maxHealth;
            currentStamina = maxStamina;
            currentShield = maxShield;

            // Reset Position
            transform.position = _initialPosition;
            transform.rotation = _initialRotation;
            currentPosition = _initialPosition;
            currentRotation = _initialRotation;

            // Clear temporary buffs
            activeBuffs.RemoveAll(buff => !buff.isPersistent);

            // clear session-only inventory
            inventory.RemoveAll(item => item.isSessionOnly);
            
            Debug.Log($"[PlayerRuntimeData] Reset on death for Player {playerID}");
        }

        /// <summary>
        /// Populates runtime data from a save file.
        /// </summary>
        public void RestoreFromSave(PlayerSaveData data)
        {
            if (data == null) return;

            playerID = data.playerID;
            currentSurvivalState = data.survivalState; // Restore state (though usually loaded as Alive unless persistent death)
            experiencePoints = data.experiencePoints;
            
            // Merge or replace inventory
            if (data.persistentInventory != null)
            {
                foreach (var savedItem in data.persistentInventory)
                {
                    // Logic to add or update existing inventory
                    var existing = inventory.Find(i => i.itemID == savedItem.itemID);
                    if (existing != null)
                    {
                        existing.quantity = savedItem.quantity;
                    }
                    else
                    {
                        inventory.Add(savedItem);
                    }
                }
            }
        }

        /// <summary>
        /// Returns data to be persisted by SaveManager.
        /// </summary>
        public PlayerSaveData GetPersistentData()
        {
            var data = new PlayerSaveData();
            data.playerID = playerID;
            data.survivalState = currentSurvivalState;
            data.experiencePoints = experiencePoints;
            
            // Filter persistent inventory
            data.persistentInventory = new List<InventoryItem>();
            foreach (var item in inventory)
            {
                if (!item.isSessionOnly)
                {
                    data.persistentInventory.Add(item);
                }
            }

            return data;
        }
    }
}

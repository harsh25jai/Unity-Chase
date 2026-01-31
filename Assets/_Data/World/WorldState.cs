using System;
using System.Collections.Generic;
using UnityEngine;

namespace Data.World.Runtime
{
    [Serializable]
    public class WorldSaveData
    {
        public List<string> destroyedObjectIDs;
        // Add other persistent world state here (e.g. unlocked doors)
    }

    public class WorldState : MonoBehaviour
    {
        [Header("Environment")]
        public string currentBiome;
        
        [Header("Persistence")]
        [Tooltip("List of unique IDs for objects destroyed by the player (Persistent)")]
        public List<string> destroyedObjectIDs = new List<string>();

        [Header("Session State")]
        [Tooltip("List of currently active hazards (Reset on Death)")]
        public List<string> activeHazards = new List<string>();

        // Events
        public event Action<string> OnBiomeChanged;

        /// <summary>
        /// Updates the current biome and triggers event.
        /// </summary>
        public void SetBiome(string biomeName)
        {
            if (currentBiome != biomeName)
            {
                currentBiome = biomeName;
                OnBiomeChanged?.Invoke(currentBiome);
                Debug.Log($"[WorldState] Biome changed to: {currentBiome}");
            }
        }

        /// <summary>
        /// Records an object as destroyed so it doesn't spawn again.
        /// </summary>
        public void RecordDestruction(string objectID)
        {
            if (!destroyedObjectIDs.Contains(objectID))
            {
                destroyedObjectIDs.Add(objectID);
            }
        }

        /// <summary>
        /// Checks if an object has been destroyed previously.
        /// </summary>
        public bool IsObjectDestroyed(string objectID)
        {
            return destroyedObjectIDs.Contains(objectID);
        }

        /// <summary>
        /// Registers an active hazard (e.g., fire, toxic cloud) created during the session.
        /// </summary>
        public void RegisterHazard(string hazardID)
        {
            if (!activeHazards.Contains(hazardID))
            {
                activeHazards.Add(hazardID);
            }
        }

        /// <summary>
        /// Resets temporary session state on player death.
        /// </summary>
        public void ResetOnDeath()
        {
            // Clear dynamic hazards or temporary environmental effects
            activeHazards.Clear();
            
            // Note: We do NOT clear destroyedObjectIDs because world changes (like opening a shortcut or destroying a wall) usually persist.
            // If specific destructibles should reset, they should be tracked in a separate "SessionDestroyed" list.
            
            Debug.Log("[WorldState] World temporary state reset on death.");
        }

        /// <summary>
        /// Returns data to be persisted by SaveManager.
        /// </summary>
        public WorldSaveData GetPersistentData()
        {
            WorldSaveData data = new WorldSaveData();
            data.destroyedObjectIDs = new List<string>(destroyedObjectIDs);
            return data;
        }

        /// <summary>
        /// Restores state from save.
        /// </summary>
        public void RestoreFromSave(WorldSaveData data)
        {
            if (data == null) return;
            
            destroyedObjectIDs.Clear();
            if (data.destroyedObjectIDs != null)
            {
                destroyedObjectIDs.AddRange(data.destroyedObjectIDs);
            }
        }
    }
}

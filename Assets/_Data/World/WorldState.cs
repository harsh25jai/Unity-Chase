using System;
using System.Collections.Generic;
using UnityEngine;

namespace Data.World.Runtime
{
    [Serializable]
    public class WorldSaveData
    {
        public List<string> destroyedObjectIDs;
        public RegionType regionType;
        public float totalDistanceTraveled;
        // Add other persistent world state here (e.g. unlocked doors)
    }

    [Serializable]
    public enum RegionType
    {
        Urban,
        Remote,
        Transition
    }

    public class WorldState : MonoBehaviour
    {
        [Header("Environment")]
        public string currentBiome;
        public RegionType currentRegionType;
        
        [Header("Progression")]
        public float worldDistanceTraveled;
        
        [Header("Persistence")]
        [Tooltip("List of unique IDs for objects destroyed by the player (Persistent)")]
        public List<string> destroyedObjectIDs = new List<string>();

        [Header("Session State")]
        [Tooltip("List of currently active hazards (Reset on Death)")]
        public List<string> activeHazards = new List<string>();

        // Events
        public event Action<string> OnBiomeChanged;
        public event Action<RegionType> OnRegionChanged;
        public event Action<float> OnDistanceUpdated;

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

        public void SetRegion(RegionType regionType)
        {
            if (currentRegionType != regionType)
            {
                currentRegionType = regionType;
                OnRegionChanged?.Invoke(currentRegionType);
                Debug.Log($"[WorldState] Region changed to: {currentRegionType}");
            }
        }

        public void AddDistance(float amount)
        {
            if (amount > 0)
            {
                worldDistanceTraveled += amount;
                OnDistanceUpdated?.Invoke(worldDistanceTraveled);
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
            data.regionType = currentRegionType;
            data.totalDistanceTraveled = worldDistanceTraveled;
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
            
            currentRegionType = data.regionType;
            worldDistanceTraveled = data.totalDistanceTraveled;
        }
    }
}

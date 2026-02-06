using System;
using UnityEngine;
using Data.Player.Runtime;
using Core.Bootstrap;

namespace Systems.Player
{
    public class PlayerSpawnController : MonoBehaviour
    {
        [Header("Configuration")]
        public GameObject playerPrefab;

        [Header("References")]
        public PlayerRuntimeData playerRuntimeData;
        public EscapeStartSpawnRequestor spawnRequestor;

        // Events
        public event Action<string, GameObject> OnPlayerSpawned;

        private void Start()
        {
            if (spawnRequestor == null) 
                spawnRequestor = FindFirstObjectByType<EscapeStartSpawnRequestor>();

            if (spawnRequestor != null)
            {
                spawnRequestor.OnPlayerSpawnRequested += SpawnPlayer;
            }
            else
            {
                Debug.LogWarning("[PlayerSpawnController] No SpawnRequestor found. Player will not spawn via bootstrap.");
            }
        }

        private void OnDestroy()
        {
            if (spawnRequestor != null)
            {
                spawnRequestor.OnPlayerSpawnRequested -= SpawnPlayer;
            }
        }

        public void SpawnPlayer(Transform spawnTransform)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("[PlayerSpawnController] Player Prefab is missing! Cannot spawn.");
                return;
            }

            Debug.Log($"[PlayerSpawnController] Spawning player at {spawnTransform.position}");

            GameObject playerInstance = Instantiate(playerPrefab, spawnTransform.position, spawnTransform.rotation);
            playerInstance.name = "Player_0"; // Ensure consistent naming for now

            // Update Runtime Data
            if (playerRuntimeData != null)
            {
                playerRuntimeData.currentSurvivalState = SurvivalState.Alive;
                playerRuntimeData.currentPosition = spawnTransform.position;
                playerRuntimeData.currentHealth = playerRuntimeData.maxHealth;
            }
            
            // Notify systems
            // Assuming simplified ID "Player_0" for single player
            OnPlayerSpawned?.Invoke("Player_0", playerInstance);
        }
    }
}

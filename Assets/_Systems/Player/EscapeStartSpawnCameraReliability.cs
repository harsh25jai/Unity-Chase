using System;
using UnityEngine;
using Core.GameState;
using Data.Session.Runtime;
using Data.Player.Runtime;
using Systems.Player;

namespace Systems.Player
{
    public class EscapeStartSpawnCameraReliability : MonoBehaviour
    {
        [Header("Dependencies")]
        public GameStateManager gameStateManager;
        public SaveLoadCoordinator saveLoadCoordinator;
        public PlayerSpawnController playerSpawnController;
        public CameraRebindOnSpawn cameraRebinder;
        public SessionRuntimeData sessionData;

        [Header("Spawn Markers")]
        public string checkpointSpawnPrefix = "Checkpoint_";
        public string checkpointSpawnSuffix = "_Spawn";
        public string defaultSpawnMarkerName = "Player_Spawn_01"; // Generic fallback

        // Events
        public event Action<string> OnCamerasBound;
        public event Action<string> OnSpawnPlacementConfirmed; // source: "Fresh" or "Checkpoint"
        public event Action<string> OnSpawnPlacementFailed;

        private void Awake()
        {
            // Auto-resolve references if missing
            if (gameStateManager == null) gameStateManager = GameStateManager.Instance;
            if (saveLoadCoordinator == null) saveLoadCoordinator = FindFirstObjectByType<SaveLoadCoordinator>();
            if (playerSpawnController == null) playerSpawnController = FindFirstObjectByType<PlayerSpawnController>();
            if (cameraRebinder == null) cameraRebinder = GetComponent<CameraRebindOnSpawn>();
            if (sessionData == null) sessionData = FindFirstObjectByType<SessionRuntimeData>();
        }

        private void OnEnable()
        {
            if (playerSpawnController != null)
                playerSpawnController.OnPlayerSpawned += HandlePlayerSpawned;

            if (saveLoadCoordinator != null)
                saveLoadCoordinator.OnLoadCompleted += HandleCheckpointLoadCompleted;
        }

        private void OnDisable()
        {
            if (playerSpawnController != null)
                playerSpawnController.OnPlayerSpawned -= HandlePlayerSpawned;

            if (saveLoadCoordinator != null)
                saveLoadCoordinator.OnLoadCompleted -= HandleCheckpointLoadCompleted;
        }

        private void HandlePlayerSpawned(string playerId, GameObject playerInstance)
        {
            // Multiplayer Safety: Only authority (player0)
            if (playerId != "Player_0") return;

            Debug.Log($"[EscapeStartSpawnCameraReliability] Fresh spawn detected for {playerId}.");
            
            ConfirmPlacementAndBind(playerInstance, "Fresh");
        }

        private void HandleCheckpointLoadCompleted(int slot, string checkpointId)
        {
            Debug.Log($"[EscapeStartSpawnCameraReliability] Checkpoint load completed: {checkpointId}.");

            // Find the player instance
            var pData = FindFirstObjectByType<PlayerRuntimeData>();
            if (pData == null)
            {
                string reason = "Player instance not found after checkpoint load.";
                Debug.LogError($"[EscapeStartSpawnCameraReliability] {reason}");
                OnSpawnPlacementFailed?.Invoke(reason);
                return;
            }

            GameObject playerInstance = pData.gameObject;
            
            // Relocation logic (if SaveLoadCoordinator didn't do it perfectly or we need secondary guard)
            // SaveLoadCoordinator already tries to find "Checkpoint_01_Spawn", but we'll make it generic.
            string spawnName = $"{checkpointId}{checkpointSpawnSuffix}";
            GameObject spawnMarker = GameObject.Find(spawnName);

            if (spawnMarker == null)
            {
                Debug.LogWarning($"[EscapeStartSpawnCameraReliability] Specific checkpoint spawn {spawnName} not found. Falling back to default.");
                spawnMarker = GameObject.Find(defaultSpawnMarkerName);
            }

            if (spawnMarker != null)
            {
                RelocatePlayer(playerInstance, spawnMarker.transform);
                ConfirmPlacementAndBind(playerInstance, "Checkpoint");
            }
            else
            {
                string reason = "No valid spawn markers found for checkpoint reload.";
                Debug.LogError($"[EscapeStartSpawnCameraReliability] {reason}");
                OnSpawnPlacementFailed?.Invoke(reason);
                // We still try to bind cameras to current position as a last ditch playable state
                ConfirmPlacementAndBind(playerInstance, "Checkpoint_Degraded");
            }
        }

        private void RelocatePlayer(GameObject player, Transform target)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = target.position;
            player.transform.rotation = target.rotation;

            if (cc != null) cc.enabled = true;
            
            Debug.Log($"[EscapeStartSpawnCameraReliability] Relocated player to {target.name}");
        }

        private void ConfirmPlacementAndBind(GameObject playerInstance, string source)
        {
            if (cameraRebinder == null)
            {
                Debug.LogError("[EscapeStartSpawnCameraReliability] CameraRebinder reference is missing!");
                return;
            }

            Transform viewTarget = cameraRebinder.FindViewTargetInInstance(playerInstance);
            
            if (viewTarget == null)
            {
                string reason = "Player_ViewTarget role not found on player instance.";
                Debug.LogError($"[EscapeStartSpawnCameraReliability] {reason}");
                OnSpawnPlacementFailed?.Invoke(reason);
                return;
            }

            bool success = cameraRebinder.BindToTarget(viewTarget);
            
            if (success)
            {
                OnSpawnPlacementConfirmed?.Invoke(source);
                OnCamerasBound?.Invoke("Player_0");
                Debug.Log($"[EscapeStartSpawnCameraReliability] Spawn ({source}) and Camera Rebiding confirmed.");
            }
            else
            {
                OnSpawnPlacementFailed?.Invoke("Camera binding failed.");
            }
        }
    }
}

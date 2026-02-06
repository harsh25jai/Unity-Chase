using System;
using UnityEngine;
using Systems.Vehicle;
using Core.GameState;
using Data.Session.Runtime;
using Data.World.Runtime;
using Data.Vehicle.Runtime;

namespace Systems.World
{
    public enum TrackingState
    {
        Idle,
        WaitingForVehicle,
        Armed,
        Tracking,
        Paused
    }

    public class SessionDistanceTracker : MonoBehaviour
    {
        [Header("References")]
        public VehicleEntryController entryController;
        public GameStateManager gameStateManager;
        public SessionRuntimeData sessionData;
        public WorldState worldState;
        public VehicleMotionGate motionGate;

        [Header("Settings")]
        public float updateRate = 0.5f; // Rate limited updates to data backend

        [Header("State Output")]
        public TrackingState currentState = TrackingState.Idle;
        public float sessionTotalDistance;
        public string currentVehicleID;

        // Events
        public event Action OnDistanceTrackingStarted;
        public event Action<float> OnDistanceUpdated;
        public event Action OnDistanceTrackingStopped;

        private Transform _authorityTransform;
        private float _updateTimer;
        private float _accumulatedDelta;

        private void Start()
        {
            if (entryController == null) entryController = FindFirstObjectByType<VehicleEntryController>();
            if (gameStateManager == null) gameStateManager = GameStateManager.Instance;
            if (sessionData == null) sessionData = FindFirstObjectByType<SessionRuntimeData>();
            if (worldState == null) worldState = FindFirstObjectByType<WorldState>();
            if (motionGate == null) motionGate = GetComponent<VehicleMotionGate>();

            if (entryController != null)
            {
                entryController.OnVehicleEntered += HandleVehicleEntered;
                entryController.OnVehicleExited += HandleVehicleExited;
            }

            if (gameStateManager != null)
            {
                gameStateManager.OnSessionEnded += HandleSessionEnded;
            }

            currentState = TrackingState.WaitingForVehicle;
        }

        private void OnDestroy()
        {
            if (entryController != null)
            {
                entryController.OnVehicleEntered -= HandleVehicleEntered;
                entryController.OnVehicleExited -= HandleVehicleExited;
            }

            if (gameStateManager != null)
            {
                gameStateManager.OnSessionEnded -= HandleSessionEnded;
            }
        }

        private void Update()
        {
            if (currentState != TrackingState.Tracking) return;

            float delta = motionGate.GetFilteredDelta();
            if (delta > 0)
            {
                _accumulatedDelta += delta;
                sessionTotalDistance += delta;
            }

            _updateTimer += Time.deltaTime;
            if (_updateTimer >= updateRate)
            {
                _updateTimer = 0f;
                SyncToData();
            }
        }

        public void HandleVehicleEntered(string playerId, string vehicleId)
        {
            // Policy: Track only Player_0 (local authority)
            if (playerId != "Player_0") return;

            var vehicle = FindVehicle(vehicleId);
            if (vehicle != null)
            {
                _authorityTransform = vehicle.transform;
                currentVehicleID = vehicleId;
                motionGate.SetTarget(_authorityTransform);
                motionGate.ResetBasePosition();
                
                currentState = TrackingState.Tracking;
                Debug.Log($"[DistanceTracker] Tracking started for vehicle: {vehicleId}");
                OnDistanceTrackingStarted?.Invoke();
            }
        }

        public void HandleVehicleExited(string playerId, string vehicleId)
        {
            if (playerId != "Player_0") return;

            StopTracking();
        }

        private void HandleSessionEnded()
        {
            currentState = TrackingState.Paused;
            SyncToData();
            Debug.Log("[DistanceTracker] Session ended. Tracking paused.");
        }

        private void StopTracking()
        {
            SyncToData();
            currentState = TrackingState.WaitingForVehicle;
            _authorityTransform = null;
            currentVehicleID = "";
            motionGate.ClearTarget();
            
            Debug.Log("[DistanceTracker] Tracking stopped.");
            OnDistanceTrackingStopped?.Invoke();
        }

        private void SyncToData()
        {
            if (_accumulatedDelta <= 0) return;

            if (sessionData != null)
            {
                sessionData.AddDistance("Player_0", _accumulatedDelta);
            }

            if (worldState != null)
            {
                worldState.AddDistance(_accumulatedDelta);
            }

            OnDistanceUpdated?.Invoke(sessionTotalDistance);
            _accumulatedDelta = 0;
        }

        private VehicleRuntimeData FindVehicle(string vehicleId)
        {
            // Similar lookup to Binder; ideally use a registry in prod
            var vehicles = FindObjectsByType<VehicleRuntimeData>(FindObjectsSortMode.None);
            foreach(var v in vehicles)
            {
                if (v.vehicleID == vehicleId) return v;
            }
            return null;
        }
    }
}

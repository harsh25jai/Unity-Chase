using System;
using UnityEngine;
using Data.World.Runtime;

namespace Systems.World
{
    public class WorldProgressionController : MonoBehaviour
    {
        [Header("Backend Data")]
        public WorldState worldState;

        [Header("Settings")]
        [Tooltip("Simulated speed if no physical player reference is tracking distance")]
        public float defaultSpeed = 10f;
        public bool autoIncrementDistance = true;

        [Header("Progression Rules")]
        public float distanceForTransition = 1000f;
        public float distanceForRemote = 5000f;

        // Internal
        private Transform _trackedObject;

        private void Start()
        {
            if (worldState == null)
            {
                worldState = FindFirstObjectByType<WorldState>();
            }

            // Find something to track (Player or Vehicle)
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _trackedObject = player.transform;
        }

        private void Update()
        {
            if (worldState == null) return;

            // Update Distance
            if (autoIncrementDistance)
            {
                // In a real infinite runner, this might be constant
                // In an open world, we check delta movement
                // For this implementation, we simulate "driving forward" along the infinite road
                float distance = defaultSpeed * Time.deltaTime;
                worldState.AddDistance(distance);
            }
            
            // Region Logic
            EvaluateRegion(worldState.worldDistanceTraveled);
        }

        public void SetRegion(RegionType type)
        {
            if (worldState != null)
            {
                worldState.SetRegion(type);
            }
        }

        private void EvaluateRegion(float distance)
        {
            RegionType targetRegion = RegionType.Urban;

            if (distance > distanceForRemote)
            {
                targetRegion = RegionType.Remote;
            }
            else if (distance > distanceForTransition)
            {
                targetRegion = RegionType.Transition;
            }

            if (worldState.currentRegionType != targetRegion)
            {
                SetRegion(targetRegion);
            }
        }
    }
}

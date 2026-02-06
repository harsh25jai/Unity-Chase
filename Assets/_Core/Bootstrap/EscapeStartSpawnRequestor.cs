using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Data.Session.Runtime;

namespace Core.Bootstrap
{
    public class EscapeStartSpawnRequestor : MonoBehaviour
    {
        [Header("Configuration")]
        public bool deterministicSpawn = true;
        [Tooltip("If true, assumes the first marker found is the desired one if no specific seed is present.")]
        public bool useFirstMarkerFallback = true;

        // Events
        public event Action<Transform> OnPlayerSpawnRequested;

        public void RequestSpawn(SessionRuntimeData sessionData)
        {
            var markers = FindObjectsByType<PlayerSpawnMarker>(FindObjectsSortMode.None).ToList();

            if (markers.Count == 0)
            {
                Debug.LogError("[EscapeStartSpawnRequestor] No PlayerSpawnMarker found in scene! Cannot spawn player.");
                return;
            }

            PlayerSpawnMarker selectedMarker = null;

            if (deterministicSpawn)
            {
                // Simple deterministic logic: Sort by name, pick by index (e.g. 0 or based on seed)
                // For now, we default to the first one alphabetically to be stable across runs.
                markers.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
                selectedMarker = markers[0];
            }
            else
            {
                // Random
                selectedMarker = markers[UnityEngine.Random.Range(0, markers.Count)];
            }

            Debug.Log($"[EscapeStartSpawnRequestor] Selected Spawn Marker: {selectedMarker.name} (Role: {selectedMarker.spawnRole})");
            OnPlayerSpawnRequested?.Invoke(selectedMarker.transform);
        }
    }
}

using UnityEngine;
using System.Collections.Generic;

namespace Systems.Police
{
    public class PolicePresenceSpawner : MonoBehaviour
    {
        [Header("Placeholder Settings")]
        public GameObject policePlaceholderPrefab;
        public int initialSpawnCount = 1;
        public float spawnRadius = 20f;
        public float minSpawnDistance = 10f;

        private List<GameObject> _activePlaceholders = new List<GameObject>();

        public void SpawnInitialPresence()
        {
            if (policePlaceholderPrefab == null)
            {
                Debug.LogWarning("[PolicePresenceSpawner] No Police Placeholder Prefab assigned. Testing escalation logic without presence.");
                return;
            }

            Debug.Log($"[PolicePresenceSpawner] Spawning {initialSpawnCount} police placeholders.");

            for (int i = 0; i < initialSpawnCount; i++)
            {
                Vector3 spawnPos = CalculateSafeSpawnPosition();
                GameObject policeUnit = Instantiate(policePlaceholderPrefab, spawnPos, Quaternion.identity);
                policeUnit.name = $"Police_Placeholder_{i}";
                _activePlaceholders.Add(policeUnit);
            }

            OnPolicePresenceActive?.Invoke(_activePlaceholders.Count);
        }

        private Vector3 CalculateSafeSpawnPosition()
        {
            // Simple random point in a ring around the spawner (likely attached to player or world origin)
            // In a real system, this would use NavMesh and check line of sight.
            Vector2 randomRing = Random.insideUnitCircle.normalized * Random.Range(minSpawnDistance, spawnRadius);
            Vector3 offset = new Vector3(randomRing.x, 0, randomRing.y);
            
            // Try to find ground
            Vector3 targetPos = transform.position + offset;
            if (Physics.Raycast(targetPos + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f))
            {
                return hit.point;
            }

            return targetPos;
        }

        public void ClearPresence()
        {
            foreach (var unit in _activePlaceholders)
            {
                if (unit != null) Destroy(unit);
            }
            _activePlaceholders.Clear();
        }

        public event System.Action<int> OnPolicePresenceActive;
    }
}

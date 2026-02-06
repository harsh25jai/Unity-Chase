using System;
using System.Linq;
using UnityEngine;
using Data.Vehicle.Runtime;

namespace Systems.Vehicle
{
    public class VehicleDiscoveryController : MonoBehaviour
    {
        [Header("Settings")]
        public float discoveryRadius = 5.0f;
        public LayerMask vehicleLayer = -1; // Default to all

        [Header("State")]
        public VehicleRuntimeData currentTargetVehicle;
        public float currentTargetDistance;

        // Events
        public event Action<string, float> OnVehicleDiscovered; // vehicleId, distance
        public event Action OnVehicleLost;

        private Collider[] _hitBuffer = new Collider[10];

        private void Update()
        {
            ScanForVehicles();
        }

        private void ScanForVehicles()
        {
            int hits = Physics.OverlapSphereNonAlloc(transform.position, discoveryRadius, _hitBuffer, vehicleLayer);
            
            VehicleRuntimeData bestCandidate = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < hits; i++)
            {
                var vehicleData = _hitBuffer[i].GetComponentInParent<VehicleRuntimeData>();
                if (vehicleData != null && vehicleData.currentDegradationState != VehicleDegradationState.Disabled)
                {
                    float dist = Vector3.Distance(transform.position, vehicleData.transform.position);
                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        bestCandidate = vehicleData;
                    }
                }
            }

            // State Change Logic
            if (bestCandidate != currentTargetVehicle)
            {
                if (bestCandidate != null)
                {
                    currentTargetVehicle = bestCandidate;
                    currentTargetDistance = bestDistance;
                    Debug.Log($"[VehicleDiscovery] Discovered: {bestCandidate.vehicleID} at distance {bestDistance:F1}");
                    OnVehicleDiscovered?.Invoke(bestCandidate.vehicleID, bestDistance);
                }
                else
                {
                    if (currentTargetVehicle != null)
                    {
                        Debug.Log("[VehicleDiscovery] Lost vehicle target.");
                        OnVehicleLost?.Invoke();
                    }
                    currentTargetVehicle = null;
                    currentTargetDistance = 0f;
                }
            }
            else if (currentTargetVehicle != null)
            {
                // Update distance for active target
                currentTargetDistance = bestDistance;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, discoveryRadius);
        }
    }
}

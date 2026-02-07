using UnityEngine;
using Data.Vehicle.Runtime;

namespace Systems.Vehicle
{
    public class VehicleProximitySelector : MonoBehaviour
    {
        [Header("Settings")]
        public float discoveryRadius = 5.0f;
        public LayerMask vehicleLayer = -1;

        private Collider[] _hitBuffer = new Collider[10];

        /// <summary>
        /// Finds the nearest valid vehicle within the discovery radius.
        /// </summary>
        /// <returns>The nearest valid vehicle's runtime data, or null if none found.</returns>
        public VehicleRuntimeData FindNearestVehicle()
        {
            int hits = Physics.OverlapSphereNonAlloc(transform.position, discoveryRadius, _hitBuffer, vehicleLayer);
            
            VehicleRuntimeData bestCandidate = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < hits; i++)
            {
                // Look for VehicleRuntimeData in the hit object or its parents
                var vehicleData = _hitBuffer[i].GetComponentInParent<VehicleRuntimeData>();
                
                if (vehicleData != null)
                {
                    // Validation: Only discover vehicles that are NOT disabled
                    if (vehicleData.currentDegradationState != VehicleDegradationState.Disabled)
                    {
                        float dist = Vector3.Distance(transform.position, vehicleData.transform.position);
                        if (dist < bestDistance)
                        {
                            bestDistance = dist;
                            bestCandidate = vehicleData;
                        }
                    }
                }
            }

            return bestCandidate;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, discoveryRadius);
        }
    }
}

using UnityEngine;

namespace Core.Bootstrap
{
    public class PlayerSpawnMarker : MonoBehaviour
    {
        [Tooltip("Optional role or ID for specific spawn logic (e.g., 'Player1', 'Host', 'CheckpointAlpha')")]
        public string spawnRole = "Default";

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward);
        }
    }
}

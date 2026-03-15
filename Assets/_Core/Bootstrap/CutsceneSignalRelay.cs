using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Bootstrap
{
    /// <summary>
    /// Relays Timeline signals from the cutscene scene (BankPrologue_Timeline) 
    /// to the master RC1FlowOrchestrator in the Bootstrap scene.
    /// Ensures signals are relayed only once and correctly handled across boundaries.
    /// </summary>
    public class CutsceneSignalRelay : MonoBehaviour
    {
        private bool _hasFired = false;

        /// <summary>
        /// Public method meant to be called by a Timeline Signal Receiver.
        /// EXACT METHOD NAME: OnRobberyIntroEndSignal
        /// </summary>
        public void OnRobberyIntroEndSignal()
        {
            // 1. Idempotency Check
            if (_hasFired)
            {
                Debug.LogWarning("[SignalRelay] Duplicate signal received; ignoring second fire.");
                return;
            }

            // 2. Sanity Check: Ensure we are in the correct scene context
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.name.Contains("BankPrologue_Timeline"))
            {
                Debug.LogWarning($"[SignalRelay] Signal fired while in unexpected scene '{activeScene.name}'. Ignoring.");
                return;
            }

            // 3. Find Orchestrator
            RC1FlowOrchestrator orchestrator = FindFirstObjectByType<RC1FlowOrchestrator>();
            
            if (orchestrator != null)
            {
                Debug.Log("[SignalRelay] Relaying cutscene end signal to RC1FlowOrchestrator.");
                _hasFired = true;
                orchestrator.LoadEscapeStart();
            }
            else
            {
                Debug.LogError("[SignalRelay] CRITICAL: RC1FlowOrchestrator not found in active scenes! Cannot transition.");
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            // Static reset not needed for instance-level _hasFired flag,
            // but included for pattern consistency.
        }
    }
}

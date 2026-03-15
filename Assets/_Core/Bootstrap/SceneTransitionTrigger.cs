using UnityEngine;
using UnityEngine.SceneManagement;

namespace EscapeGame.Cutscenes
{
    /// <summary>
    /// Minimal MonoBehaviour wrapper that triggers a scene transition from cutscene to gameplay.
    /// Designed for RC1 simplicity with strict duplicate prevention and Editor-only validation.
    /// </summary>
    [AddComponentMenu("EscapeGame/Cutscenes/Scene Transition Trigger")]
    public class SceneTransitionTrigger : MonoBehaviour
    {
        private static bool isTransitioning = false;

        /// <summary>
        /// Resets the transition flag when Play mode is restarted.
        /// Mandatory for projects with Domain Reload disabled.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            isTransitioning = false;
        }

        /// <summary>
        /// Public method callable from Unity's Timeline Signal Receiver UnityEvent.
        /// Triggers transition to "EscapeStart" scene.
        /// </summary>
        [Tooltip("Called by Timeline Signal Receiver to transition from cutscene to gameplay")]
        public void LoadEscapeStart()
        {
            if (isTransitioning)
            {
                Debug.LogWarning("[SceneTransitionTrigger] Scene transition already in progress, ignoring duplicate call.");
                return;
            }

#if UNITY_EDITOR
            // Validation: Ensure the target scene exists in Build Settings
            // RC1: Hardcoded path validation for reliable slice testing
            if (SceneUtility.GetBuildIndexByScenePath("Assets/_World/Scenes/EscapeStart.unity") == -1)
            {
                Debug.LogError("[SceneTransitionTrigger] EscapeStart scene not in Build Settings. Add via File -> Build Settings.");
                return;
            }
#endif

            isTransitioning = true;
            Debug.Log("[SceneTransitionTrigger] Scene transition to EscapeStart initiated.");
            
            // RC1: Synchronous load for immediate handoff (Scene index 1)
            SceneManager.LoadScene("EscapeStart");
        }
    }
}

using UnityEngine;

namespace Data.Session.Runtime
{
    public enum RestartMode
    {
        RestartFromBeginning,
        LoadLastCheckpoint
    }

    [CreateAssetMenu(fileName = "RestartModePolicy", menuName = "Data/Session/RestartModePolicy")]
    public class RestartModePolicy : ScriptableObject
    {
        [Header("Configuration")]
        public RestartMode currentMode = RestartMode.LoadLastCheckpoint;
        public RestartMode defaultMode = RestartMode.RestartFromBeginning;

        [Header("Runtime State")]
        [Tooltip("If true, the policy will yield to any externally forced mode (e.g. from a network host).")]
        public bool allowExternalOverrides = true;

        /// <summary>
        /// Logic to decide if a checkpoint load should be attempted.
        /// </summary>
        public bool ShouldAttemptLoad()
        {
            return currentMode == RestartMode.LoadLastCheckpoint;
        }

        /// <summary>
        /// Resets the policy to the default mode.
        /// </summary>
        public void ResetToDefault()
        {
            currentMode = defaultMode;
        }

        /// <summary>
        /// Forcefully overrides the current mode.
        /// </summary>
        public void ForceMode(RestartMode mode)
        {
            if (allowExternalOverrides)
            {
                currentMode = mode;
                Debug.Log($"[RestartModePolicy] Mode forced to: {mode}");
            }
        }
    }
}

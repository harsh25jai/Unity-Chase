using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace Core.Bootstrap
{
    /// <summary>
    /// Guarantees that only the primary Bootstrap Camera and AudioListener are active.
    /// Prevents cutscene scenes or gameplay levels from introducing competing rendering cameras or listeners.
    /// </summary>
    [DefaultExecutionOrder(-100)] // Run before most other logic
    public class CameraExclusivityEnforcer : MonoBehaviour
    {
        public static CameraExclusivityEnforcer Instance { get; private set; }

        [Header("Authority References")]
        public Camera authorityCamera;
        public AudioListener authorityListener;

        private HashSet<int> _loggedObjectIds = new HashSet<int>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Auto-resolve authority if not assigned
            if (authorityCamera == null) authorityCamera = GetComponent<Camera>();
            if (authorityListener == null) authorityListener = GetComponent<AudioListener>();

            if (authorityCamera == null || authorityListener == null)
            {
                Debug.LogError("[CameraEnforcer] CRITICAL: Authority Camera or Listener missing on Enforcer object!");
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            // Immediate scan in case we're added to a scene already containing conflicts
            ScanForExclusivityConflicts();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[CameraEnforcer] Scanning Scene: {scene.name} for hardware exclusivity conflicts.");
            ScanForExclusivityConflicts();
        }

        /// <summary>
        /// Performas a non-destructive scan of all cameras and listeners in the active scenes.
        /// Disables any components that conflict with the bootstrap authority.
        /// </summary>
        public void ScanForExclusivityConflicts()
        {
            // 1. Enforce AudioListener Exclusivity
            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var listener in listeners)
            {
                if (listener != authorityListener && listener.enabled)
                {
                    listener.enabled = false;
                    LogErrorOnce(listener.gameObject, "Competing AudioListener detected and disabled.");
                }
            }

            // 2. Enforce Camera Rendering Exclusivity
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var cam in cameras)
            {
                if (cam != authorityCamera && cam.enabled)
                {
                    // Case A: CM_CutsceneCamera conflict (vCam driver with a camera component)
                    if (cam.gameObject.name.Contains("CM_CutsceneCamera"))
                    {
                        cam.enabled = false;
                        LogErrorOnce(cam.gameObject, "Cutscene camera conflict: CM_CutsceneCamera has an enabled Camera component. Disabling it to allow vCam behavior.");
                    }
                    // Case B: General competing rendering camera
                    else
                    {
                        cam.enabled = false;
                        LogErrorOnce(cam.gameObject, "Competing rendering Camera detected and disabled. Only Bootstrap MainCamera should render.");
                    }
                }
            }
        }

        private void LogErrorOnce(GameObject obj, string message)
        {
            int id = obj.GetInstanceID();
            if (!_loggedObjectIds.Contains(id))
            {
                Debug.LogError($"[CameraEnforcer] Conflict on '{obj.name}': {message}", obj);
                _loggedObjectIds.Add(id);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            // Reset if Instance was static across domain reloads, but Instance is instance-based.
        }
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using static Core.Bootstrap.RC1FlowOrchestrator;

namespace Core.Bootstrap
{
    /// <summary>
    /// Guardrail that ensures presentation exclusivity (Camera/Audio) based on the session flow state.
    /// Disables Bootstrap camera during cutscenes and re-enables it for gameplay takeover.
    /// Also prevents hardware conflicts (multiple AudioListeners or MainCamera tags).
    /// </summary>
    [DefaultExecutionOrder(-110)] // Run before Orchestrator and Enforcer
    public class CameraExclusiveStateGate : MonoBehaviour
    {
        public static CameraExclusiveStateGate Instance { get; private set; }

        [Header("References")]
        public GameObject bootstrapMainCamera;
        public RC1FlowOrchestrator orchestrator;

        private HashSet<int> _loggedConflictIds = new HashSet<int>();

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

            if (orchestrator == null) orchestrator = FindFirstObjectByType<RC1FlowOrchestrator>();
            if (bootstrapMainCamera == null && orchestrator != null) bootstrapMainCamera = orchestrator.bootstrapMainCamera;
        }

        private void OnEnable()
        {
            if (orchestrator != null)
            {
                orchestrator.OnStateChanged += HandleStateChanged;
            }
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            if (orchestrator != null)
            {
                orchestrator.OnStateChanged -= HandleStateChanged;
            }
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnforceHardwareExclusivity();
        }

        private void HandleStateChanged(FlowState newState)
        {
            Debug.Log($"[CameraGate] Reacting to state: {newState}");

            switch (newState)
            {
                case FlowState.CutsceneActive:
                case FlowState.TransitioningToGameplay:
                    SetBootstrapCameraActive(false);
                    break;

                case FlowState.GameplayCameraTakeover:
                case FlowState.GameplayActive:
                    SetBootstrapCameraActive(true);
                    break;
            }

            EnforceHardwareExclusivity();
        }

        private void SetBootstrapCameraActive(bool active)
        {
            if (bootstrapMainCamera != null)
            {
                if (bootstrapMainCamera.activeSelf != active)
                {
                    Debug.Log($"[CameraGate] Setting Bootstrap MainCamera active: {active}");
                    bootstrapMainCamera.SetActive(active);
                }
            }
            else
            {
                Debug.LogError("[CameraGate] CRITICAL: Bootstrap MainCamera reference is missing!");
            }
        }

        /// <summary>
        /// Scans for and disables competing AudioListeners and warns about MainCamera tag conflicts.
        /// </summary>
        public void EnforceHardwareExclusivity()
        {
            // 1. AudioListener Exclusivity
            AudioListener bootstrapListener = null;
            if (bootstrapMainCamera != null) bootstrapListener = bootstrapMainCamera.GetComponentInChildren<AudioListener>();

            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var listener in listeners)
            {
                if (listener != bootstrapListener && listener.enabled)
                {
                    listener.enabled = false;
                    LogConflictOnce(listener.gameObject, "Competing AudioListener disabled to prevent rendering/audio glitches.");
                }
            }

            // 2. MainCamera Tag Check
            Camera[] mainCameras = GameObject.FindGameObjectsWithTag("MainCamera").GetComponents<Camera>();
            if (mainCameras.Length > 1)
            {
                foreach (var cam in mainCameras)
                {
                    if (cam.gameObject != bootstrapMainCamera)
                    {
                        LogConflictOnce(cam.gameObject, "CRITICAL WARNING: Multiple objects tagged 'MainCamera' detected. This can break camera brain binding.");
                    }
                }
            }
        }

        private void LogConflictOnce(GameObject obj, string message)
        {
            int id = obj.GetInstanceID();
            if (!_loggedConflictIds.Contains(id))
            {
                Debug.LogError($"[CameraGate] Conflict on '{obj.name}': {message}", obj);
                _loggedConflictIds.Add(id);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetGate()
        {
            // Reset logic for domain reload disabled support
        }
    }

    public static class CameraExtensions
    {
        public static Camera[] GetComponents<T>(this GameObject[] objects) where T : Component
        {
            List<Camera> cameras = new List<Camera>();
            foreach (var obj in objects)
            {
                Camera cam = obj.GetComponent<Camera>();
                if (cam != null) cameras.Add(cam);
            }
            return cameras.ToArray();
        }
    }
}

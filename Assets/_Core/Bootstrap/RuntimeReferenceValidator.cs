using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Video;
using Unity.Cinemachine;
using System.Collections.Generic;
using System.Linq;
using Data.Session.Runtime;

namespace Core.Bootstrap
{
    /// <summary>
    /// Runtime validator that checks for missing references on scene load and provides graceful degradation.
    /// </summary>
    public class RuntimeReferenceValidator : MonoBehaviour
    {
        public static RuntimeReferenceValidator Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                ValidateSingletons();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void ValidateSingletons()
        {
            CheckSingleton<GameState.GameStateManager>();
            CheckSingleton<GameplaySceneLoader>();
            CheckSingleton<TransitionUIController>();
        }

        private void CheckSingleton<T>() where T : MonoBehaviour
        {
            if (FindAnyObjectByType<T>() == null)
            {
                Debug.LogError($"[RuntimeValidator] CRITICAL: Singleton {typeof(T).Name} is missing! Creating emergency instance.");
                GameObject go = new GameObject($"Emergency_{typeof(T).Name}");
                go.AddComponent<T>();
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[RuntimeValidator] Validating Scene: {scene.name}");

            if (scene.name == "BankPrologue_Timeline")
            {
                ValidateBankPrologue();
            }
            else if (scene.name == "EscapeStart")
            {
                ValidateEscapeStart();
            }
        }

        private void ValidateBankPrologue()
        {
            GameObject videoRef = GameObject.Find("Video_Reference");
            if (videoRef == null)
            {
                LogCritical("BankPrologue", "Video_Reference missing. Skipping cutscene to avoid black screen hang.");
                SkipToEscapeStart();
                return;
            }

            if (GameObject.Find("CM_CutsceneCamera") == null)
                LogMedium("BankPrologue", "CM_CutsceneCamera missing. Cutscene may look incorrect.");

            GameObject markers = GameObject.Find("Cutscene_Root/Markers");
            if (markers == null || markers.transform.childCount == 0)
                LogMedium("BankPrologue", "Handoff markers missing. Default spawn will be used in next scene.");
        }

        private void ValidateEscapeStart()
        {
            bool hasTriggers = GameObject.Find("Checkpoint_01_Trigger") != null;
            bool hasSpawn = GameObject.Find("Checkpoint_01_Spawn") != null;

            if (!hasTriggers || !hasSpawn)
            {
                LogMedium("EscapeStart", "Checkpoint 01 markers missing. Progress/Saves may be disabled.");
                // Graceful degradation: Already handled by GameplaySceneLoader/SpawnAlignmentResolver logic
                // which usually falls back to PlayerSpawn_Default if handoff data is incomplete.
            }

            if (GameObject.Find("PlayerSpawn_Default") == null)
                LogCritical("EscapeStart", "PlayerSpawn_Default missing! Player may spawn at (0,0,0).");

            var allGo = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            bool vehicleFound = allGo.Any(go => go.name.ToLower().Contains("vehicle"));
            if (!vehicleFound)
                LogCritical("EscapeStart", "No vehicle found in scene. Gameplay cannot proceed.");
        }

        private void SkipToEscapeStart()
        {
            if (GameplaySceneLoader.Instance != null)
            {
                Debug.Log("[RuntimeValidator] Action: Force-transitioning to EscapeStart due to critical missing prologue reference.");
                GameplaySceneLoader.Instance.BeginTransition(default);
            }
            else
            {
                SceneManager.LoadScene("EscapeStart");
            }
        }

        private void LogMedium(string context, string message)
        {
            Debug.LogWarning($"[RuntimeValidator] <color=yellow>MEDIUM</color> ({context}): {message}");
            // Show non-intrusive warning if a UI system exists
        }

        private void LogCritical(string context, string message)
        {
            Debug.LogError($"[RuntimeValidator] <color=red>CRITICAL</color> ({context}): {message}");
            ShowCriticalErrorUI($"{context} CRITICAL ERROR:\n{message}");
        }

        private void ShowCriticalErrorUI(string message)
        {
            // Simple OnGUI fallback if no formal UI is found
            CriticalErrorOverlay.Show(message);
        }
    }

    /// <summary>
    /// Minimalist UI overlay for critical runtime errors.
    /// </summary>
    public class CriticalErrorOverlay : MonoBehaviour
    {
        private string errorMessage;
        private static CriticalErrorOverlay instance;

        public static void Show(string message)
        {
            if (instance == null)
            {
                GameObject go = new GameObject("CriticalErrorOverlay");
                instance = go.AddComponent<CriticalErrorOverlay>();
                DontDestroyOnLoad(go);
            }
            instance.errorMessage = message;
        }

        private void OnGUI()
        {
            Rect rect = new Rect(Screen.width / 2 - 200, Screen.height / 2 - 100, 400, 200);
            GUI.Box(rect, "CRITICAL SYSTEM ERROR");
            GUI.Label(new Rect(rect.x + 20, rect.y + 40, rect.width - 40, 100), errorMessage);

            if (GUI.Button(new Rect(rect.x + 100, rect.y + 150, 200, 30), "Return to Main Menu"))
            {
                SceneManager.LoadScene(0); // Assuming 0 is bootstrap/menu
                Destroy(gameObject);
            }
        }
    }
}

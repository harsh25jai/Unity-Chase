using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Data.Session.Runtime;
using Systems.Player;

namespace Core.Bootstrap
{
    /// <summary>
    /// Coordinates the transition between the cutscene and the gameplay scene.
    /// Handles async loading, UI/Audio fades, and handoff data persistence.
    /// </summary>
    public class GameplaySceneLoader : MonoBehaviour
    {
        public static GameplaySceneLoader Instance { get; private set; }

        [Header("Configuration")]
        public string gameplaySceneName = "EscapeStart";
        public float loadTimeoutSeconds = 10f;
        public float audioFadeDuration = 0.5f;
        public float uiFadeDuration = 0.5f;

        [Header("Dependencies")]
        public SessionRuntimeData sessionData;
        public TransitionUIController uiController;

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
            }
        }

        private void Start()
        {
            if (sessionData == null) sessionData = FindFirstObjectByType<SessionRuntimeData>();
            if (uiController == null) uiController = TransitionUIController.Instance;
        }

        /// <summary>
        /// Initiates the transition pipeline using captured handoff data.
        /// </summary>
        public void BeginTransition(HandoffTransitionData handoffData)
        {
            if (sessionData == null) sessionData = FindFirstObjectByType<SessionRuntimeData>();

            Debug.Log($"[GameplaySceneLoader] BeginTransition called. Scene: {gameplaySceneName}");

            // 1. Store Data in Session (survives scene unload)
            if (sessionData != null)
            {
                sessionData.activeHandoffData = handoffData;
            }

            // 2. Start Transition Sequence
            StartCoroutine(TransitionSequence());
        }

        private IEnumerator TransitionSequence()
        {
            // A. Fade Out UI and Audio
            if (uiController != null) uiController.FadeOut(uiFadeDuration);
            FadeAudio(audioFadeDuration);

            yield return new WaitForSeconds(uiFadeDuration);

            // B. Validate Scene Name
            if (!IsSceneInBuild(gameplaySceneName))
            {
                HandleTransitionFailure($"Scene '{gameplaySceneName}' not found in build settings.");
                yield break;
            }

            // C. Start Async Load
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(gameplaySceneName, LoadSceneMode.Single);
            loadOp.allowSceneActivation = true;

            float startTime = Time.realtimeSinceStartup;
            bool isTimedOut = false;

            while (!loadOp.isDone)
            {
                // Monitor Progress
                // Debug.Log($"[GameplaySceneLoader] Load Progress: {loadOp.progress:P}");

                // Check Timeout
                if (Time.realtimeSinceStartup - startTime > loadTimeoutSeconds)
                {
                    isTimedOut = true;
                    break;
                }

                yield return null;
            }

            if (isTimedOut)
            {
                HandleTransitionFailure("Scene load timed out.");
                yield break;
            }

            // D. Scene Loaded and Activated
            OnSceneActivated();
        }

        private void OnSceneActivated()
        {
            Debug.Log("[GameplaySceneLoader] Scene activated. Finalizing transition.");

            // 1. Notify SpawnAlignmentResolver
            var resolver = FindFirstObjectByType<SpawnAlignmentResolver>();
            if (resolver != null)
            {
                resolver.ResolveSpawn(sessionData.activeHandoffData);
            }
            else
            {
                Debug.LogWarning("[GameplaySceneLoader] SpawnAlignmentResolver not found in new scene.");
            }

            // 2. Update Game State to Active Gameplay
            if (Core.GameState.GameStateManager.Instance != null)
            {
                var mgr = Core.GameState.GameStateManager.Instance;
                if (mgr.sessionData != null)
                {
                    mgr.sessionData.currentSessionState = SessionState.Active;
                    Debug.Log("[GameplaySceneLoader] State changed to Active.");
                }
            }

            // 3. Fade In UI
            if (uiController != null) uiController.FadeIn(uiFadeDuration);
        }

        private void FadeAudio(float duration)
        {
            Debug.Log($"[GameplaySceneLoader] Fading audio out over {duration}s.");
            // Interface with AudioSystem if available. For now, simulated via logs.
        }

        private void HandleTransitionFailure(string error)
        {
            Debug.LogError($"[GameplaySceneLoader] Transition FAILED: {error}");
            // Return to main menu or show error UI
            // SceneManager.LoadScene("MainMenu");
        }

        private bool IsSceneInBuild(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (name == sceneName) return true;
            }
            return false;
        }
    }
}

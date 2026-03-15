using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Data.Session.Runtime;

namespace Core.Bootstrap
{
    /// <summary>
    /// Master orchestrator for RC1 scene flow and camera exclusivity.
    /// Manages the full lifecycle from Bootstrap to GameplayActive.
    /// </summary>
    public class RC1FlowOrchestrator : MonoBehaviour
    {
        public enum FlowState
        {
            Boot,
            CutsceneLoadRequested,
            CutsceneActive,
            TransitionSignalReceived,
            TransitioningToGameplay,
            CutsceneUnloadRequested,
            GameplayCameraTakeover,
            GameplayActive
        }

        [Header("Configuration")]
        public string prologueScene = "BankPrologue_Timeline";
        public string gameplayScene = "EscapeStart";
        public float signalTimeoutLimit = 35f;

        [Header("References")]
        public GameObject bootstrapMainCamera;
        public SessionRuntimeData sessionData;

        [Header("Runtime State")]
        public FlowState currentState = FlowState.Boot;
        private bool _transitionInProgress = false;
        private Coroutine _timeoutCoroutine;

        private void Awake()
        {
            // Auto-resolve session data if missing
            if (sessionData == null) sessionData = FindFirstObjectByType<SessionRuntimeData>();
            
            // Validate references
            if (bootstrapMainCamera == null)
            {
                Debug.LogError("[RC1Orchestrator] CRITICAL: Bootstrap MainCamera reference is missing!");
            }
        }

        private void Start()
        {
            StartCoroutine(FlowSequence());
        }

        private IEnumerator FlowSequence()
        {
            UpdateFlowState(FlowState.Boot);

            // 1. Request Cutscene Load
            UpdateFlowState(FlowState.CutsceneLoadRequested);
            AsyncOperation prologueLoad = SceneManager.LoadSceneAsync(prologueScene, LoadSceneMode.Additive);
            
            if (prologueLoad == null)
            {
                LogErrorOnce("SceneLoad", $"Failed to initiate load for {prologueScene}. Falling back to gameplay.");
                StartCoroutine(FallbackToGameplayReady());
                yield break;
            }

            yield return prologueLoad;

            // 2. Cutscene Active - Disable Bootstrap Camera
            UpdateFlowState(FlowState.CutsceneActive);
            if (bootstrapMainCamera != null) bootstrapMainCamera.SetActive(false);
            
            _timeoutCoroutine = StartCoroutine(SignalTimeoutGate());
        }

        /// <summary>
        /// Public method callable by Timeline Signal or Relay.
        /// Handles the transition from cutscene to gameplay.
        /// </summary>
        public void LoadEscapeStart()
        {
            if (_transitionInProgress)
            {
                Debug.LogWarning("[RC1Orchestrator] Transition already in-flight. Ignoring duplicate signal.");
                return;
            }

            _transitionInProgress = true;
            UpdateFlowState(FlowState.TransitionSignalReceived);

            if (_timeoutCoroutine != null)
            {
                StopCoroutine(_timeoutCoroutine);
                _timeoutCoroutine = null;
            }

            StartCoroutine(TransitionToGameplaySequence());
        }

        private IEnumerator TransitionToGameplaySequence()
        {
            UpdateFlowState(FlowState.TransitioningToGameplay);

            // 1. Load Gameplay Scene Additively
            AsyncOperation gameplayLoad = SceneManager.LoadSceneAsync(gameplayScene, LoadSceneMode.Additive);
            if (gameplayLoad == null)
            {
                LogErrorOnce("GameplayLoad", $"Failed to initiate load for {gameplayScene}. Halting in safe state.");
                _transitionInProgress = false;
                yield break;
            }

            yield return gameplayLoad;

            // 2. Unload Cutscene Scene
            UpdateFlowState(FlowState.CutsceneUnloadRequested);
            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(prologueScene);
            if (unloadOp != null) yield return unloadOp;

            // 3. Camera Takeover - Re-enable Bootstrap Camera
            UpdateFlowState(FlowState.GameplayCameraTakeover);
            if (bootstrapMainCamera != null) bootstrapMainCamera.SetActive(true);

            // 4. Gameplay Active
            UpdateFlowState(FlowState.GameplayActive);
            _transitionInProgress = false;
            
            Debug.Log("[RC1Orchestrator] Flow completed successfully. Gameplay is now active.");
        }

        private IEnumerator FallbackToGameplayReady()
        {
            // Immediate jump to gameplay if prologue fails
            UpdateFlowState(FlowState.TransitioningToGameplay);
            AsyncOperation gameplayLoad = SceneManager.LoadSceneAsync(gameplayScene, LoadSceneMode.Single);
            if (gameplayLoad != null)
            {
                yield return gameplayLoad;
                if (bootstrapMainCamera != null) bootstrapMainCamera.SetActive(true);
                UpdateFlowState(FlowState.GameplayActive);
            }
        }

        private IEnumerator SignalTimeoutGate()
        {
            yield return new WaitForSeconds(signalTimeoutLimit);
            
            if (currentState == FlowState.CutsceneActive)
            {
                LogErrorOnce("Timeout", $"Cutscene signal timeout ({signalTimeoutLimit}s). Forcing transition.");
                LoadEscapeStart();
            }
        }

        public event System.Action<FlowState> OnStateChanged;

        private void UpdateFlowState(FlowState newState)
        {
            currentState = newState;
            Debug.Log($"[RC1Orchestrator] State -> {newState}");

            OnStateChanged?.Invoke(newState);

            // Sync with SessionState if possible
            if (sessionData != null)
            {
                switch (newState)
                {
                    case FlowState.Boot: sessionData.currentSessionState = SessionState.Booting; break;
                    case FlowState.CutsceneActive: sessionData.currentSessionState = SessionState.CutsceneMode; break;
                    case FlowState.TransitioningToGameplay: sessionData.currentSessionState = SessionState.TransitionMode; break;
                    case FlowState.GameplayActive: sessionData.currentSessionState = SessionState.Active; break;
                }
            }
        }

        private void LogErrorOnce(string context, string message)
        {
            Debug.LogError($"[RC1Orchestrator] ({context}) {message}");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOrchestrator()
        {
            // Domain reload disabled support
        }
    }
}

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Core.GameState;
using Data.Session.Runtime;
using Systems.Player;
using System.Collections;

namespace Core.Bootstrap
{
    /// <summary>
    /// Listens for specific Timeline signals to trigger scene transitions and capture handoff data.
    /// </summary>
    public class CutsceneSignalReceiver : MonoBehaviour, INotificationReceiver
    {
        [Header("Configuration")]
        [Tooltip("The expected signal name from Timeline")]
        public string targetSignal = "SIG_Cutscene_RobberyIntro_End";
        
        [Tooltip("Expected timing of the signal in seconds")]
        public float expectedSignalTime = 25.8f;
        
        [Tooltip("Allowed drift for timing validation")]
        public float timingTolerance = 0.2f;

        [Header("Marker Paths")]
        public string anchorPath = "Cutscene_Root/Markers/Handoff_Anchor";
        public string playerFacingPath = "Cutscene_Root/Markers/Handoff_PlayerFacing";
        public string lookTargetPath = "Cutscene_Root/Markers/Handoff_LookTarget";

        [Header("Dependencies")]
        public GameStateManager gameStateManager;
        public GameplaySceneLoader sceneLoader;
        public PlayerStateController playerStateController;

        private bool _signalProcessed = false;

        private void OnEnable()
        {
            // Note: Registration often happens automatically if attached to a Signal Receiver 
            // component with this script as the listener.
        }

        /// <summary>
        /// Implementation of INotificationReceiver.
        /// </summary>
        public void OnNotify(Playable origin, INotification notification, object context)
        {
            if (_signalProcessed) return;

            if (notification is SignalEmitter signalEmitter)
            {
                if (signalEmitter.asset != null && signalEmitter.asset.name == targetSignal)
                {
                    HandleSignalReceived(origin);
                }
                // Fallback if the asset name isn't set but we want to check something else? 
                // Usually SignalEmitter asset name is the key.
            }
        }

        private void HandleSignalReceived(Playable origin)
        {
            if (_signalProcessed)
            {
                Debug.LogWarning("[CutsceneSignalReceiver] Duplicate transition signal ignored, transition already in progress.");
                return;
            }

            _signalProcessed = true;
            Debug.Log($"[CutsceneSignalReceiver] Signal '{targetSignal}' received.");

            // 1. Validate Timing
            ValidateTiming(origin);

            // 2. Disable Player Input
            LockInput();

            // 3. Capture Handoff Data
            HandoffTransitionData handoffData = CaptureHandoffData();

            // 4. Update Game State
            UpdateGameState();

            // 5. Initiate Transition
            InitiateTransition(handoffData);
        }

        private void ValidateTiming(Playable origin)
        {
            double currentTime = origin.GetTime();
            float drift = Mathf.Abs((float)currentTime - expectedSignalTime);

            if (drift > timingTolerance)
            {
                Debug.LogWarning($"[CutsceneSignalReceiver] Signal timing drift: expected {expectedSignalTime}s, received at {currentTime:F2}s (Drift: {drift:F2}s)");
            }
            else
            {
                Debug.Log($"[CutsceneSignalReceiver] Signal received on time ({currentTime:F2}s).");
            }
        }

        private void LockInput()
        {
            if (playerStateController == null) playerStateController = FindFirstObjectByType<PlayerStateController>();
            
            if (playerStateController != null)
            {
                playerStateController.SetControlLock(true);
                Debug.Log("[CutsceneSignalReceiver] Player input disabled.");
            }
            else
            {
                Debug.LogWarning("[CutsceneSignalReceiver] PlayerStateController not found. Could not lock input.");
            }
        }

        private HandoffTransitionData CaptureHandoffData()
        {
            HandoffTransitionData data = HandoffTransitionData.Default;
            bool allMarkersFound = true;

            try
            {
                GameObject anchorObj = GameObject.Find(anchorPath);
                GameObject facingObj = GameObject.Find(playerFacingPath);
                GameObject lookTargetObj = GameObject.Find(lookTargetPath);

                if (anchorObj != null)
                {
                    data.spawnPosition = anchorObj.transform.position;
                }
                else
                {
                    Debug.LogError($"[CutsceneSignalReceiver] Handoff marker 'Handoff_Anchor' not found at path: {anchorPath}");
                    allMarkersFound = false;
                }

                if (facingObj != null)
                {
                    data.spawnRotation = facingObj.transform.rotation;
                }
                else
                {
                    Debug.LogError($"[CutsceneSignalReceiver] Handoff marker 'Handoff_PlayerFacing' not found at path: {playerFacingPath}");
                    allMarkersFound = false;
                }

                if (lookTargetObj != null)
                {
                    data.cameraTargetPosition = lookTargetObj.transform.position;
                }
                else
                {
                    Debug.LogError($"[CutsceneSignalReceiver] Handoff marker 'Handoff_LookTarget' not found at path: {lookTargetPath}");
                    allMarkersFound = false;
                }

                data.isValid = allMarkersFound;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CutsceneSignalReceiver] Marker lookup failed, scene unloaded prematurely or other error: {ex.Message}");
                data.isValid = false;
            }

            return data;
        }

        private void UpdateGameState()
        {
            if (gameStateManager == null) gameStateManager = GameStateManager.Instance;

            if (gameStateManager != null)
            {
                // Note: We need to ensure GameStateManager handles TransitionMode.
                // Based on our update to SessionState, we should set it via GameStateManager if possible.
                // Currently GameStateManager.SetState is private. We might need a public way or just set it on SessionData.
                
                // For now, let's assume we can trigger a state change or set it directly on sessionData.
                if (gameStateManager.sessionData != null)
                {
                    gameStateManager.sessionData.currentSessionState = SessionState.TransitionMode;
                    Debug.Log("[CutsceneSignalReceiver] GameStateManager transitioned to TransitionMode.");
                }
            }
        }

        private void InitiateTransition(HandoffTransitionData data)
        {
            if (sceneLoader == null) sceneLoader = GameplaySceneLoader.Instance;

            if (sceneLoader != null)
            {
                sceneLoader.BeginTransition(data);
            }
            else
            {
                Debug.LogError("[CutsceneSignalReceiver] GameplaySceneLoader not found. Transition aborted.");
            }
        }
    }
}

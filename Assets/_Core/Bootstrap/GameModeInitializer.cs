using UnityEngine;
using Data.Session.Runtime;

namespace Core.Bootstrap
{
    public class GameModeInitializer : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("The default mode if none is specified in SessionData")]
        public GameMode defaultMode = GameMode.SinglePlayer;
        public int defaultPlayerCount = 1;

        [Header("References")]
        public SessionRuntimeData sessionData;

        private void Awake()
        {
            if (sessionData == null)
            {
                sessionData = FindFirstObjectByType<SessionRuntimeData>();
                if (sessionData == null)
                {
                    Debug.LogError("[GameModeInitializer] Missing SessionRuntimeData! Cannot initialize mode.");
                    return;
                }
            }
        }

        public void InitializeGameMode()
        {
            // Logic to check if session data is already populated (e.g. from main menu)
            // For EscapeStart, we assume if it's "Booting" or uninitialized, we force defaults.
            // If we add Multiplayer later, we might read a "NetworkSession" config here.

            if (sessionData.currentSessionState == SessionState.Booting || !sessionData.isSessionActive)
            {
                Debug.Log($"[GameModeInitializer] Initializing Session as {defaultMode}");
                
                // Set Data
                sessionData.gameMode = defaultMode;
                sessionData.playerCount = defaultPlayerCount;
                
                // If the design requires clamping for SP:
                if (sessionData.gameMode == GameMode.SinglePlayer)
                {
                    sessionData.playerCount = 1;
                }

                // We don't set "Active" here yet, Bootstrap will do it after spawn.
            }
            else
            {
                Debug.Log($"[GameModeInitializer] Session already active: {sessionData.gameMode}");
            }
        }
    }
}

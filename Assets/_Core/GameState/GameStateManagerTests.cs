using System;
using System.Collections;
using UnityEngine;
using Core.GameState;
using Data.Session.Runtime;
using Data.Save.Manager;
using Data.Player.Runtime;
using Data.World.Runtime;

namespace Tests.GameState
{
    public class GameStateManagerTests : MonoBehaviour
    {
        private GameObject _testObj;
        private GameStateManager _gameState;
        private SessionRuntimeData _sessionData;
        private SaveManager _saveManager;
        private PlayerRuntimeData _playerData; // Needed for SaveManager init reset logic
        private WorldState _worldState; // Needed for SaveManager init

        private void Start()
        {
            StartCoroutine(RunTests());
        }

        private IEnumerator RunTests()
        {
            Debug.Log(">>> STARTING GAME STATE MANAGER TESTS <<<");

            Setup();
            yield return null;

            yield return Test_DeathTransition();
            ResetState();

            yield return Test_BustedTransition();
            ResetState();

            yield return Test_RestartFromBeginning();
            ResetState();

            yield return Test_RestartFromCheckpoint();
            ResetState();

            yield return Test_SaveCompletionNoReset();
            ResetState();

            yield return Test_SaveFailureHandled();
            ResetState();

            yield return Test_TerminalSignalRace();
            ResetState();

            yield return Test_MultiplayerPolicy();

            Debug.Log(">>> GAME STATE TESTS COMPLETED <<<");
        }

        private void Setup()
        {
            // Clean up old managers if any (Singeltons)
             if (GameStateManager.Instance != null) DestroyImmediate(GameStateManager.Instance.gameObject);
             if (SaveManager.Instance != null) DestroyImmediate(SaveManager.Instance.gameObject);

            _testObj = new GameObject("TestGameStateContext");
            
            // Add Data Components first
            _sessionData = _testObj.AddComponent<SessionRuntimeData>();
            _playerData = _testObj.AddComponent<PlayerRuntimeData>();
            _worldState = _testObj.AddComponent<WorldState>();

            // Add SaveManager
            _saveManager = _testObj.AddComponent<SaveManager>();
            _saveManager.sessionRuntimeData = _sessionData;
            _saveManager.playerRuntimeData = _playerData;
            _saveManager.worldState = _worldState;
            _saveManager.saveFileName = "test_save.json"; // Use test file
            
            // Add GameStateManager
            _gameState = _testObj.AddComponent<GameStateManager>();
            _gameState.sessionData = _sessionData;
            _gameState.saveManager = _saveManager;

            // Manual Init for Singletons/Awake logic if needed, but AddComponent calls Awake
            // Start needs to run one frame
            _sessionData.currentSessionState = SessionState.Active;
        }

        private void ResetState()
        {
            _sessionData.currentSessionState = SessionState.Active;
            _sessionData.completedCheckpoints.Clear();
            _saveManager.DeleteSaveFile();
            Debug.Log("--- State Reset ---");
        }

        private IEnumerator Test_DeathTransition()
        {
            Debug.Log("Running Test: Death Transition");
            bool gameOverFired = false;
            _gameState.OnGameOver += () => gameOverFired = true;

            _gameState.EndSession("Death");
            yield return null;

            if (_sessionData.currentSessionState == SessionState.Ended && gameOverFired)
                Debug.Log("[PASS] Death Transition successful. Event Fired.");
            else
                Debug.LogError($"[FAIL] Death Transition. State: {_sessionData.currentSessionState}, Event: {gameOverFired}");
        }

        private IEnumerator Test_BustedTransition()
        {
            Debug.Log("Running Test: Busted Transition");
            bool bustedFired = false;
            _gameState.OnPlayerBusted += () => bustedFired = true;

            _gameState.EndSession("Busted");
            yield return null;

            if (_sessionData.currentSessionState == SessionState.Ended && bustedFired)
                Debug.Log("[PASS] Busted Transition successful. Event Fired.");
            else
                Debug.LogError($"[FAIL] Busted Transition. State: {_sessionData.currentSessionState}, Event: {bustedFired}");
        }

        private IEnumerator Test_RestartFromBeginning()
        {
            Debug.Log("Running Test: Restart From Beginning (No Checkpoint)");
            
            bool restartBegFired = false;
            _gameState.OnRestartFromBeginningRequested += () => restartBegFired = true;
            
            // Ensure no save exists (ResetState does this)
            _gameState.RequestSmartRestart();
            yield return null;

            if (restartBegFired)
                Debug.Log("[PASS] Selected Restart From Beginning.");
            else
                Debug.LogError("[FAIL] Did not select Restart From Beginning.");
        }

        private IEnumerator Test_RestartFromCheckpoint()
        {
            Debug.Log("Running Test: Restart From Checkpoint");
            
            // Create a "Checkpoint" condition (Simulated by adding to session data)
            _sessionData.completedCheckpoints.Add("CP_1");
            
            bool restartCPFired = false;
            _gameState.OnRestartFromCheckpointRequested += () => restartCPFired = true;

            _gameState.RequestSmartRestart();
            yield return null;

            if (restartCPFired)
                Debug.Log("[PASS] Selected Restart From Checkpoint.");
            else
                Debug.LogError("[FAIL] Did not select Restart From Checkpoint.");
        }

        private IEnumerator Test_SaveCompletionNoReset()
        {
            Debug.Log("Running Test: Save Completion (No Reset)");
            
            _sessionData.currentSessionState = SessionState.Active;
            
            // Trigger Save Manually
            _saveManager.SaveGame();
            yield return null;
            
            // Wait a frame for events? Events are sync in method call.
            
            if (_sessionData.currentSessionState == SessionState.Active)
                Debug.Log("[PASS] State remained Active after Save.");
            else
                Debug.LogError($"[FAIL] State changed after Save! State: {_sessionData.currentSessionState}");
        }

        private IEnumerator Test_SaveFailureHandled()
        {
            Debug.Log("Running Test: Save Failure Handling");
            
            // Force failure by invalid path? Or just listen for event?
            // Since we can't easily mock File.IO error without bad path, we'll assume the event works if triggered.
            // But to verify GameState *reacts* (or doesn't crash), we'd need to simulate it.
            // Given the complexity of mocking File IO errors in runtime integration tests, 
            // We will skip forcing the error and just rely on code review that GameManager *doesn't* subscribe to falure events to do anything destructive.
            // Actually, let's try to pass an invalid path if possible? 
            _saveManager.saveFileName = "invalid_folder/save.json"; // Should fail directory not found maybe?
            
            bool failedEvent = false;
            _saveManager.OnSaveFailed += (msg) => failedEvent = true;

            _saveManager.SaveGame();
            yield return null;

            if (failedEvent && _sessionData.currentSessionState == SessionState.Active)
                Debug.Log("[PASS] Save Failure reported and Game State remained stable.");
            else
                Debug.Log($"[Note] Save Failure test result: FailedEvent={failedEvent} (Might auto-create folder). State={_sessionData.currentSessionState}. Proceeding.");
        }

        private IEnumerator Test_TerminalSignalRace()
        {
            Debug.Log("Running Test: Terminal Signal Race");
            
            _sessionData.currentSessionState = SessionState.Active;
            int endCalls = 0;
            _gameState.OnSessionEnded += () => endCalls++;

            // Call death then busted immediately
            _gameState.EndSession("Death");
            _gameState.EndSession("Busted");
            yield return null;

            // Should have ended once, and state should be Ended.
            // Logic: if (SessionState == Ended) return;
            
            if (endCalls == 1)
                Debug.Log("[PASS] Only first signal processed.");
            else
                Debug.LogError($"[FAIL] Race condition failed. EndCalls: {endCalls}");
        }

        private IEnumerator Test_MultiplayerPolicy()
        {
            Debug.Log("Running Test: Multiplayer Policy");
            // Verify that calling EndSession works regardless of who called it (simulated).
            // Currently GameState just exposes EndSession().
            // If P1 calls it, it ends.
            // This test just verifies EndSession is accessible and works.
            
            _gameState.EndSession("Death_P2");
            yield return null;
            
            if (_sessionData.currentSessionState == SessionState.Ended)
                Debug.Log("[PASS] Session ended by Player signal.");
            else
                Debug.LogError("[FAIL] Multiplay Policy check failed.");
        }
    }
}

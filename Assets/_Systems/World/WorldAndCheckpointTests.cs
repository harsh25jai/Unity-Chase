using System;
using System.Collections;
using UnityEngine;
using Systems.World;
using Systems.Checkpoints;
using Data.Session.Runtime;
using Data.World.Runtime;
using Data.Player.Runtime;
using Data.Save.Manager;

namespace Tests.Integration
{
    public class WorldAndCheckpointTests : MonoBehaviour
    {
        private GameObject _testObj;
        private WorldProgressionController _worldController;
        private CheckpointTriggerSystem _checkpointSystem;
        private SessionRuntimeData _sessionData;
        private WorldState _worldState;
        private PlayerRuntimeData _playerData;
        
        // Mock Save Manager behavior by listening to events instead of full file IO for speed?
        // But dependencies require SaveManager singleton or reference. 
        // We'll trust the SaveManager exists or let it log errors, focusing on the EVENTS emitted.

        private void Start()
        {
            StartCoroutine(RunTests());
        }

        private IEnumerator RunTests()
        {
            Debug.Log(">>> STARTING WORLD & CHECKPOINT TESTS <<<");

            Setup();
            yield return null;

            yield return Test_DistanceMonotonic();
            ResetState();

            yield return Test_RegionTransition();
            ResetState();

            yield return Test_SessionEndPause();
            ResetState();

            yield return Test_CheckpointActive();
            ResetState();

            yield return Test_CheckpointSuppression_Dead();
            ResetState();

            yield return Test_CheckpointSuppression_Restarting();

            Debug.Log(">>> WORLD & CHECKPOINT TESTS COMPLETED <<<");
        }

        private void Setup()
        {
            _testObj = new GameObject("TestWorldContext");
            
            // Data
            _sessionData = _testObj.AddComponent<SessionRuntimeData>();
            _worldState = _testObj.AddComponent<WorldState>();
            _playerData = _testObj.AddComponent<PlayerRuntimeData>();
            
            // Systems
            _worldController = _testObj.AddComponent<WorldProgressionController>();
            _checkpointSystem = _testObj.AddComponent<CheckpointTriggerSystem>();

            // Link references manually
            _worldController.worldState = _worldState;
            _worldController.sessionData = _sessionData;
            _worldController.defaultSpeed = 100f; // High speed for faster testing

            // We need a dummy SaveManager reference if we want to avoid NullRef, 
            // though the Checkpoint system checks null. 
            // We'll let it be null and check EVENTS.
            
            // Init Data
            _sessionData.currentSessionState = SessionState.Active;
            _playerData.currentSurvivalState = SurvivalState.Alive;
        }

        private void ResetState()
        {
            _worldState.worldDistanceTraveled = 0f;
            _worldState.currentRegionType = RegionType.Urban;
            _sessionData.currentSessionState = SessionState.Active;
            _playerData.currentSurvivalState = SurvivalState.Alive;
            _sessionData.completedCheckpoints.Clear();
            Debug.Log("--- State Reset ---");
        }

        private IEnumerator Test_DistanceMonotonic()
        {
            Debug.Log("Running Test: Distance Monotonic Increase");
            float initial = _worldState.worldDistanceTraveled;
            
            yield return new WaitForSeconds(0.1f);
            
            if (_worldState.worldDistanceTraveled > initial)
                Debug.Log("[PASS] Distance increased over time.");
            else
                Debug.LogError($"[FAIL] Distance did not increase. Val: {_worldState.worldDistanceTraveled}");
        }

        private IEnumerator Test_RegionTransition()
        {
            Debug.Log("Running Test: Region Transition");
            
            // Set distance near transition (1000 is default threshold)
            _worldState.worldDistanceTraveled = 900f;
            
            // Allow update to push it over via speed (100/sec)
            yield return new WaitForSeconds(1.1f); // Should cover > 100 distance + buffer
            
            // Manually force update if speed is unreliable in test runner? No, WaitForSeconds should work in PlayMode.
            
            if (_worldState.currentRegionType == RegionType.Transition)
                Debug.Log("[PASS] Region transitioned to Transition type.");
            else
                Debug.LogError($"[FAIL] Region Transition. Current: {_worldState.currentRegionType}, Dist: {_worldState.worldDistanceTraveled}");
        }

        private IEnumerator Test_SessionEndPause()
        {
            Debug.Log("Running Test: Session End Pause");
            
            _sessionData.currentSessionState = SessionState.Ended;
            float initial = _worldState.worldDistanceTraveled;
            
            yield return null; // Wait 1 frame
            yield return null; 

            if (Mathf.Approximately(_worldState.worldDistanceTraveled, initial))
                Debug.Log("[PASS] Progression paused on Session End.");
            else
                Debug.LogError($"[FAIL] Pause Failed. Dist moved from {initial} to {_worldState.worldDistanceTraveled}");
        }

        private IEnumerator Test_CheckpointActive()
        {
            Debug.Log("Running Test: Checkpoint Active");
            
            bool eventFired = false;
            _checkpointSystem.OnCheckpointSaveRequested += () => eventFired = true;

            _checkpointSystem.TriggerCheckpoint("CP_Test_1");
            yield return null;

            bool isMarked = _sessionData.completedCheckpoints.Contains("CP_Test_1");

            if (eventFired && isMarked)
                Debug.Log("[PASS] Checkpoint triggered and handled.");
            else
                Debug.LogError($"[FAIL] Checkpoint Active. Event: {eventFired}, Marked: {isMarked}");
        }

        private IEnumerator Test_CheckpointSuppression_Dead()
        {
            Debug.Log("Running Test: Checkpoint Supression (Dead)");
            
            _playerData.currentSurvivalState = SurvivalState.Dead;
            bool eventFired = false;
            _checkpointSystem.OnCheckpointSaveRequested += () => eventFired = true;

            _checkpointSystem.TriggerCheckpoint("CP_Test_Fail");
            yield return null;

            if (!eventFired)
                Debug.Log("[PASS] Checkpoint suppressed when Player is Dead.");
            else
                Debug.LogError("[FAIL] Checkpoint triggered despite death!");
        }

        private IEnumerator Test_CheckpointSuppression_Restarting()
        {
            Debug.Log("Running Test: Checkpoint Supression (Restarting)");
            
            _playerData.currentSurvivalState = SurvivalState.Alive; // Reset player
            _sessionData.currentSessionState = SessionState.Restarting; // But session restarting
            
            bool eventFired = false;
            _checkpointSystem.OnCheckpointSaveRequested += () => eventFired = true;

            _checkpointSystem.TriggerCheckpoint("CP_Test_Fail_2");
            yield return null;

            if (!eventFired)
                Debug.Log("[PASS] Checkpoint suppressed when Session is Restarting.");
            else
                Debug.LogError("[FAIL] Checkpoint triggered during restart!");
        }
    }
}

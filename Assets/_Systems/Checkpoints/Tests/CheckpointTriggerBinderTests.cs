using System;
using System.Collections;
using UnityEngine;
using Systems.Checkpoints;
using Data.Session.Runtime;

namespace Systems.Checkpoints.Tests
{
    public class CheckpointTriggerBinderTests : MonoBehaviour
    {
        private SessionRuntimeData _sessionData;
        private GameObject _triggerGo;
        private GameObject _spawnGo;

        private void Start()
        {
            StartCoroutine(RunTests());
        }

        private IEnumerator RunTests()
        {
            Debug.Log(">>> STARTING CHECKPOINT BINDER TESTS <<<");

            yield return Test_Successful_Binding_And_Trigger();
            yield return Test_Gating_Session_State();
            yield return Test_Cooldown_Deduplication();
            yield return Test_Failure_Missing_Markers();

            Debug.Log(">>> CHECKPOINT BINDER TESTS COMPLETED <<<");
        }

        private void ClearScene()
        {
            if (_triggerGo != null) UnityEngine.Object.Destroy(_triggerGo);
            if (_spawnGo != null) UnityEngine.Object.Destroy(_spawnGo);
            if (_sessionData != null) UnityEngine.Object.Destroy(_sessionData.gameObject);
        }

        private IEnumerator Test_Successful_Binding_And_Trigger()
        {
            Debug.Log("Running Test: Successful Binding and Trigger");
            
            _triggerGo = new GameObject("Checkpoint_01_Trigger");
            var col = _triggerGo.AddComponent<BoxCollider>();
            col.isTrigger = true;

            _spawnGo = new GameObject("Checkpoint_01_Spawn");

            var sessGo = new GameObject("Session");
            _sessionData = sessGo.AddComponent<SessionRuntimeData>();
            _sessionData.currentSessionState = SessionState.Active;

            var binder = sessGo.AddComponent<CheckpointTriggerBinder>();
            binder.triggerName = "Checkpoint_01_Trigger";
            binder.spawnName = "Checkpoint_01_Spawn";

            yield return null; // Wait for Start()

            if (binder.currentState == BinderState.Armed)
                Debug.Log("[PASS] Binder correctly Armed after discovery.");
            else
                Debug.LogError($"[FAIL] Binder in state {binder.currentState} instead of Armed!");

            // Trigger simulation
            bool reachedCalled = false;
            CheckpointTriggerBinder.OnCheckpointReached += (id, obj) => { if(id == "Checkpoint_01") reachedCalled = true; };
            
            // Manual trigger call (mimicking physics)
            var player = new GameObject("PlayerMock");
            player.tag = "Player";
            binder.OnTriggerEnter(player.AddComponent<BoxCollider>());

            if (reachedCalled && binder.currentState == BinderState.Cooldown)
                Debug.Log("[PASS] Event fired and binder entered Cooldown.");
            else
                Debug.LogError("[FAIL] Event not fired or state not updated!");

            UnityEngine.Object.Destroy(player);
            ClearScene();
        }

        private IEnumerator Test_Gating_Session_State()
        {
            Debug.Log("Running Test: Gating Session State");
            
            _triggerGo = new GameObject("Checkpoint_01_Trigger");
            _triggerGo.AddComponent<BoxCollider>().isTrigger = true;
            _spawnGo = new GameObject("Checkpoint_01_Spawn");

            var sessGo = new GameObject("Session");
            _sessionData = sessGo.AddComponent<SessionRuntimeData>();
            _sessionData.currentSessionState = SessionState.Restarting; // BLOCKED STATE

            var binder = sessGo.AddComponent<CheckpointTriggerBinder>();
            
            yield return null;

            if (binder.currentState == BinderState.Bound)
                Debug.Log("[PASS] Binder remains Bound (not Armed) during Restarting state.");
            else
                Debug.LogError($"[FAIL] Binder Armed during Restarting state! Current: {binder.currentState}");

            ClearScene();
        }

        private IEnumerator Test_Cooldown_Deduplication()
        {
            Debug.Log("Running Test: Cooldown Deduplication");
            
            _triggerGo = new GameObject("Checkpoint_01_Trigger");
            _triggerGo.AddComponent<BoxCollider>().isTrigger = true;
            _spawnGo = new GameObject("Checkpoint_01_Spawn");

            var sessGo = new GameObject("Session");
            _sessionData = sessGo.AddComponent<SessionRuntimeData>();
            _sessionData.currentSessionState = SessionState.Active;

            var binder = sessGo.AddComponent<CheckpointTriggerBinder>();
            binder.cooldownDuration = 10f;

            yield return null;

            int triggerCount = 0;
            Action<int, string> handler = (slot, id) => { if(id == "Checkpoint_01") triggerCount++; };
            CheckpointTriggerBinder.OnCheckpointSaveRequested += handler;

            var player = new GameObject("PlayerMock");
            player.tag = "Player";
            var col = player.AddComponent<BoxCollider>();

            binder.OnTriggerEnter(col);
            binder.OnTriggerEnter(col); // Spam

            if (triggerCount == 1)
                Debug.Log("[PASS] Rapid triggers were deduplicated by binder cooldown.");
            else
                Debug.LogError($"[FAIL] Binder fired {triggerCount} times!");

            CheckpointTriggerBinder.OnCheckpointSaveRequested -= handler;
            UnityEngine.Object.Destroy(player);
            ClearScene();
        }

        private IEnumerator Test_Failure_Missing_Markers()
        {
            Debug.Log("Running Test: Failure Missing Markers");

            var sessGo = new GameObject("Session");
            _sessionData = sessGo.AddComponent<SessionRuntimeData>();

            // No markers created
            var binder = sessGo.AddComponent<CheckpointTriggerBinder>();
            
            yield return null;

            if (binder.currentState == BinderState.Unbound && !binder.enabled)
                Debug.Log("[PASS] Binder correctly disabled itself when markers missing.");
            else
                Debug.LogError("[FAIL] Binder should have disabled itself!");

            ClearScene();
        }
    }
}

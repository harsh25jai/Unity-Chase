using System.Collections;
using UnityEngine;
using Core.GameState;
using Data.Session.Runtime;
using Data.Save.Repository;
using Data.Save.Checkpoint;

namespace Core.GameState.Tests
{
    public class RestartResolverTests : MonoBehaviour
    {
        private GameObject _mgrGo;
        private RestartResolver _resolver;
        private SessionRuntimeData _sessionData;
        private SaveSlot0Repository _repo;

        private void Start()
        {
            StartCoroutine(RunTests());
        }

        private IEnumerator RunTests()
        {
            Debug.Log(">>> STARTING RESTART RESOLVER TESTS <<<");
            
            _repo = new SaveSlot0Repository();
            _repo.DeleteSlot0("ResolverTestPlayer");

            yield return Test_Failure_Results_In_Checkpoint_Reload();
            yield return Test_Failure_Results_In_Beginning_Reload_When_No_Save();
            yield return Test_Spam_Filtering_Idempotency();
            yield return Test_Corrupt_Save_Fallback();

            Debug.Log(">>> RESTART RESOLVER TESTS COMPLETED <<<");
        }

        private void Setup()
        {
            _mgrGo = new GameObject("Resolver_Test");
            _resolver = _mgrGo.AddComponent<RestartResolver>();
            _sessionData = _mgrGo.AddComponent<SessionRuntimeData>();
            _resolver.sessionData = _sessionData;
            _sessionData.currentSessionState = SessionState.Active;
        }

        private void Teardown()
        {
            if (_mgrGo != null) UnityEngine.Object.Destroy(_mgrGo);
            _repo.DeleteSlot0("ResolverTestPlayer");
        }

        private IEnumerator Test_Failure_Results_In_Checkpoint_Reload()
        {
            Debug.Log("Running Test: Restart from Checkpoint");
            Setup();

            // 1. Create a Save
            CheckpointRecord record = new CheckpointRecord();
            record.checkpointId = "Checkpoint_01";
            record.sceneName = "TestScene";
            _repo.WriteSlot0(JsonUtility.ToJson(record), "ResolverTestPlayer");

            bool checkpointRequested = false;
            RestartResolver.OnRestartFromCheckpointRequested += (s, id) => checkpointRequested = true;

            // 2. Trigger Death
            RestartResolver.RequestDeath("ResolverTestPlayer");

            yield return new WaitForSeconds(0.1f);

            if (checkpointRequested && _resolver.currentState == ResolverState.RestartSignaled)
                Debug.Log("[PASS] Resolver correctly signaled restart from checkpoint.");
            else
                Debug.LogError("[FAIL] Resolver failed to signal checkpoint reload!");

            Teardown();
        }

        private IEnumerator Test_Failure_Results_In_Beginning_Reload_When_No_Save()
        {
            Debug.Log("Running Test: Restart from Beginning");
            Setup();
            _repo.DeleteSlot0("ResolverTestPlayer");

            bool beginningRequested = false;
            RestartResolver.OnRestartFromBeginningRequested += () => beginningRequested = true;

            // Trigger Busted
            RestartResolver.RequestBusted("ResolverTestPlayer");

            yield return new WaitForSeconds(0.1f);

            if (beginningRequested)
                Debug.Log("[PASS] Resolver correctly signaled restart from beginning.");
            else
                Debug.LogError("[FAIL] Resolver failed to signal fresh start!");

            Teardown();
        }

        private IEnumerator Test_Spam_Filtering_Idempotency()
        {
            Debug.Log("Running Test: Spam Filtering");
            Setup();
            _repo.DeleteSlot0("ResolverTestPlayer");

            int signals = 0;
            RestartResolver.OnRestartFromBeginningRequested += () => signals++;

            // Multiple rapid triggers
            RestartResolver.RequestDeath("ResolverTestPlayer");
            RestartResolver.RequestBusted("ResolverTestPlayer");
            RestartResolver.RequestDeath("ResolverTestPlayer");

            yield return new WaitForSeconds(0.1f);

            if (signals == 1)
                Debug.Log("[PASS] Spam failure events were filtered correctly.");
            else
                Debug.LogError($"[FAIL] Resolver emitted {signals} restart signals!");

            Teardown();
        }

        private IEnumerator Test_Corrupt_Save_Fallback()
        {
            Debug.Log("Running Test: Corrupt Save Fallback");
            Setup();
            _repo.WriteSlot0("This is garbage data", "ResolverTestPlayer");

            bool beginningRequested = false;
            RestartResolver.OnRestartFromBeginningRequested += () => beginningRequested = true;

            RestartResolver.RequestDeath("ResolverTestPlayer");

            yield return new WaitForSeconds(0.1f);

            if (beginningRequested)
                Debug.Log("[PASS] Corrupt save correctly fell back to beginning.");
            else
                Debug.LogError("[FAIL] Corrupt save did not trigger fallback!");

            Teardown();
        }
    }
}

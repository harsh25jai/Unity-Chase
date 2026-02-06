using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Data.Save.Orchestration;
using Data.Save.Checkpoint;
using Data.Save.Repository;
using Data.Player.Runtime;
using Data.Vehicle.Runtime;
using Data.Session.Runtime;
using Data.World.Runtime;

namespace Data.Save.Orchestration.Tests
{
    public class CheckpointLoadOrchestratorTests : MonoBehaviour
    {
        private GameObject _mgrGo;
        private CheckpointLoadOrchestrator _loader;
        private SaveSlot0Repository _repo;

        private void Start()
        {
            StartCoroutine(RunTests());
        }

        private IEnumerator RunTests()
        {
            Debug.Log(">>> STARTING LOAD ORCHESTRATOR TESTS <<<");
            
            _repo = new SaveSlot0Repository();
            _repo.DeleteSlot0("LoadTestPlayer");

            yield return Test_Successful_Load_Applies_Data();
            yield return Test_Load_Fails_When_No_Save();
            yield return Test_Corrupt_Save_Rejection();
            yield return Test_Partial_Apply_When_Vehicle_Missing();

            Debug.Log(">>> LOAD ORCHESTRATOR TESTS COMPLETED <<<");
        }

        private void Setup()
        {
            _mgrGo = new GameObject("LoadOrchestrator_Test");
            _loader = _mgrGo.AddComponent<CheckpointLoadOrchestrator>();
            _loader.sessionData = _mgrGo.AddComponent<SessionRuntimeData>();
            _loader.worldState = _mgrGo.AddComponent<WorldState>();
        }

        private void Teardown()
        {
            if (_mgrGo != null) Object.Destroy(_mgrGo);
            _repo.DeleteSlot0("LoadTestPlayer");
        }

        private IEnumerator Test_Successful_Load_Applies_Data()
        {
            Debug.Log("Running Test: Successful Load Integrity");
            Setup();

            // 1. Prepare Save
            CheckpointRecord record = new CheckpointRecord();
            record.checkpointId = "LOAD_TEST_CP";
            record.sceneName = "TestScene";
            record.playerHealth = 42f;
            record.money = 999f;
            record.distanceTravelled = 500f;
            record.isUrban = true;
            
            string json = JsonUtility.ToJson(record);
            _repo.WriteSlot0(json, "LoadTestPlayer");

            // 2. Prepare Matcher
            var pGo = new GameObject("PlayerMatch");
            var pData = pGo.AddComponent<PlayerRuntimeData>();
            pData.playerID = "LoadTestPlayer";

            // 3. Load
            _loader.TryLoadCheckpoint("LoadTestPlayer");

            yield return null;

            if (pData.currentHealth == 42f && pData.money == 999f && _loader.worldState.currentRegionType == RegionType.Urban)
                Debug.Log("[PASS] Data successfully applied from record to runtime.");
            else
                Debug.LogError($"[FAIL] Data mismatch after load! Health: {pData.currentHealth}");

            Object.Destroy(pGo);
            Teardown();
        }

        private IEnumerator Test_Load_Fails_When_No_Save()
        {
            Debug.Log("Running Test: Load Fallback (No Save)");
            Setup();
            _repo.DeleteSlot0("LoadTestPlayer");

            bool success = _loader.TryLoadCheckpoint("LoadTestPlayer");

            if (!success)
                Debug.Log("[PASS] Load correctly failed when no save existed.");
            else
                Debug.LogError("[FAIL] Load reported success despite missing save!");

            Teardown();
            yield return null;
        }

        private IEnumerator Test_Corrupt_Save_Rejection()
        {
            Debug.Log("Running Test: Corrupt Save Rejection");
            Setup();
            _repo.WriteSlot0("This is not JSON", "LoadTestPlayer");

            bool success = _loader.TryLoadCheckpoint("LoadTestPlayer");

            if (!success)
                Debug.Log("[PASS] Corrupt JSON rejected.");
            else
                Debug.LogError("[FAIL] Corrupt JSON was not rejected!");

            Teardown();
            yield return null;
        }

        private IEnumerator Test_Partial_Apply_When_Vehicle_Missing()
        {
            Debug.Log("Running Test: Partial Apply (Missing Vehicle)");
            Setup();

            CheckpointRecord record = new CheckpointRecord();
            record.checkpointId = "PARTIAL_VEH";
            record.sceneName = "TestScene";
            record.vehicleId = "MISSING_CAR_99";
            record.playerHealth = 100f;

            _repo.WriteSlot0(JsonUtility.ToJson(record), "LoadTestPlayer");

            var pGo = new GameObject("PlayerMatch");
            var pData = pGo.AddComponent<PlayerRuntimeData>();
            pData.playerID = "LoadTestPlayer";

            bool success = _loader.TryLoadCheckpoint("LoadTestPlayer");

            yield return null;

            if (success && pData.currentHealth == 100f)
                Debug.Log("[PASS] Load finished successfully even though vehicle was missing.");
            else
                Debug.LogError("[FAIL] Partial apply failed.");

            Object.Destroy(pGo);
            Teardown();
        }
    }
}

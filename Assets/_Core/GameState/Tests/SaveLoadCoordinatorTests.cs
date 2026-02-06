using System;
using System.Collections;
using UnityEngine;
using Core.GameState;
using Data.Session.Runtime;
using Data.Save.Orchestration;
using Data.Player.Runtime;
using Data.Save.Repository;
using Data.Save.Checkpoint;

namespace Core.GameState.Tests
{
    public class SaveLoadCoordinatorTests : MonoBehaviour
    {
        private GameObject _mgrGo;
        private SaveLoadCoordinator _coordinator;
        private SessionRuntimeData _sessionData;
        private CheckpointSaveOrchestrator _saveOrch;
        private CheckpointLoadOrchestrator _loadOrch;
        private SaveSlot0Repository _repo;

        private void Start()
        {
            StartCoroutine(RunTests());
        }

        private IEnumerator RunTests()
        {
            Debug.Log(">>> STARTING COORDINATOR TESTS <<<");
            
            _repo = new SaveSlot0Repository();
            _repo.DeleteSlot0("Player_0");

            yield return Test_Successful_Save_Flow();
            yield return Test_Save_Blocked_By_Pursuit();
            yield return Test_Successful_Load_And_Teleport();
            yield return Test_Load_Fails_On_Missing_Spawn();

            Debug.Log(">>> COORDINATOR TESTS COMPLETED <<<");
        }

        private void Setup()
        {
            _mgrGo = new GameObject("Coordinator_Test");
            _coordinator = _mgrGo.AddComponent<SaveLoadCoordinator>();
            _sessionData = _mgrGo.AddComponent<SessionRuntimeData>();
            _saveOrch = _mgrGo.AddComponent<CheckpointSaveOrchestrator>();
            _loadOrch = _mgrGo.AddComponent<CheckpointLoadOrchestrator>();
            
            _coordinator.sessionData = _sessionData;
            _coordinator.saveOrchestrator = _saveOrch;
            _coordinator.loadOrchestrator = _loadOrch;
        }

        private void Teardown()
        {
            if (_mgrGo != null) UnityEngine.Object.Destroy(_mgrGo);
            _repo.DeleteSlot0("Player_0");
        }

        private IEnumerator Test_Successful_Save_Flow()
        {
            Debug.Log("Running Test: Successful Save Flow");
            Setup();

            // Setup player for orchestrator
            var pGo = new GameObject("Player_Data");
            var pData = pGo.AddComponent<PlayerRuntimeData>();
            pData.playerID = "Player_0";

            _sessionData.isPursuitActive = false;

            bool started = false;
            bool completed = false;
            _coordinator.OnSaveStarted += (s, id) => started = true;
            _coordinator.OnSaveCompleted += (s, id) => completed = true;

            _coordinator.HandleSaveRequest(0, "CP_COORD_TEST");

            yield return new WaitForSeconds(0.1f);

            if (started && completed && _repo.DoesSlot0Exist("Player_0"))
                Debug.Log("[PASS] Save sequence completed successfully.");
            else
                Debug.LogError("[FAIL] Save sequence failed!");

            UnityEngine.Object.Destroy(pGo);
            Teardown();
        }

        private IEnumerator Test_Save_Blocked_By_Pursuit()
        {
            Debug.Log("Running Test: Save Blocked By Pursuit");
            Setup();
            _sessionData.isPursuitActive = true;

            string failReason = "";
            _coordinator.OnSaveFailed += (s, id, r) => failReason = r;

            _coordinator.HandleSaveRequest(0, "CP_BLOCKED");

            yield return null;

            if (failReason == "BlockedByPolicy" && !_repo.DoesSlot0Exist("Player_0"))
                Debug.Log("[PASS] Save blocked and event emitted correctly.");
            else
                Debug.LogError($"[FAIL] Incorrect blocking behavior. Reason: {failReason}");

            Teardown();
        }

        private IEnumerator Test_Successful_Load_And_Teleport()
        {
            Debug.Log("Running Test: Successful Load & Teleport");
            Setup();

            // 1. Create a Save
            CheckpointRecord record = new CheckpointRecord();
            record.checkpointId = "Checkpoint_01";
            record.sceneName = "TestScene";
            _repo.WriteSlot0(JsonUtility.ToJson(record), "Player_0");

            // 2. Create Spawn Marker
            var spawnGo = new GameObject("Checkpoint_01_Spawn");
            spawnGo.transform.position = new Vector3(100f, 0, 100f);

            // 3. Create Player
            var playerGo = new GameObject("Player_Data");
            var pData = playerGo.AddComponent<PlayerRuntimeData>();
            pData.playerID = "Player_0";
            playerGo.transform.position = Vector3.zero;

            // 4. Load
            _coordinator.HandleLoadRequest(0);

            yield return new WaitForSeconds(0.1f);

            float dist = Vector3.Distance(playerGo.transform.position, spawnGo.transform.position);
            if (dist < 0.1f)
                Debug.Log("[PASS] Player teleported to spawn marker correctly.");
            else
                Debug.LogError($"[FAIL] Player not teleported! Pos: {playerGo.transform.position}");

            UnityEngine.Object.Destroy(playerGo);
            UnityEngine.Object.Destroy(spawnGo);
            Teardown();
        }

        private IEnumerator Test_Load_Fails_On_Missing_Spawn()
        {
            Debug.Log("Running Test: Load Failed on Missing Spawn");
            Setup();

            CheckpointRecord record = new CheckpointRecord();
            record.checkpointId = "Checkpoint_01";
            record.sceneName = "TestScene";
            _repo.WriteSlot0(JsonUtility.ToJson(record), "Player_0");

            // No spawn marker created

            string failReason = "";
            _coordinator.OnLoadFailed += (s, r) => failReason = r;

            _coordinator.HandleLoadRequest(0);

            yield return new WaitForSeconds(0.1f);

            if (failReason == "InvalidSpawn")
                Debug.Log("[PASS] Load failed gracefully when spawn were missing.");
            else
                Debug.LogError($"[FAIL] Incorrect failure reason: {failReason}");

            Teardown();
        }
    }
}

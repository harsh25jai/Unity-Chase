using System.Collections;
using UnityEngine;
using Core.GameState;
using Systems.Player;
using Systems.Vehicle;
using Data.Session.Runtime;
using Data.Player.Runtime;
using Data.Vehicle.Runtime;

namespace Core.GameState.Tests
{
    public class FailureHandlingTests : MonoBehaviour
    {
        private GameObject _testGo;
        private GameObject _playerGo;
        private GameObject _vehicleGo;
        
        private GameStateManager _gsm;
        private EscapeStartFailureHandler _handler;
        private PlayerStateController _playerController;
        private VehicleStateController _vehicleController;
        private SessionRuntimeData _sessionData;

        private void Start()
        {
            StartCoroutine(RunTests());
        }

        private IEnumerator RunTests()
        {
            Debug.Log(">>> STARTING FAILURE HANDLING TESTS <<<");

            yield return Test_Death_Triggers_Restart();
            yield return Test_Busted_Triggers_Restart();
            yield return Test_Vehicle_Loss_Triggers_Restart_If_Required();
            yield return Test_Idempotency_Only_One_Restart();
            yield return Test_Smart_Restart_Uses_Checkpoint_If_Exists();

            Debug.Log(">>> FAILURE HANDLING TESTS COMPLETED <<<");
        }

        private void Setup()
        {
            _testGo = new GameObject("FailureTest_Manager");
            _gsm = _testGo.AddComponent<GameStateManager>();
            _sessionData = _testGo.AddComponent<SessionRuntimeData>();
            _handler = _testGo.AddComponent<EscapeStartFailureHandler>();
            
            _gsm.sessionData = _sessionData;
            _sessionData.currentSessionState = SessionState.Active;

            _playerGo = new GameObject("TestPlayer");
            var pData = _playerGo.AddComponent<PlayerRuntimeData>();
            _playerController = _playerGo.AddComponent<PlayerStateController>();
            _playerController.playerRuntimeData = pData;
            pData.currentHealth = 100;

            _vehicleGo = new GameObject("TestVehicle");
            var vData = _vehicleGo.AddComponent<VehicleRuntimeData>();
            _vehicleController = _vehicleGo.AddComponent<VehicleStateController>();
            _vehicleController.vehicleRuntimeData = vData;
            vData.vehicleID = "FailTestCar";
            vData.driverPlayerID = "Player_0";
        }

        private void Teardown()
        {
            if (_testGo != null) Object.Destroy(_testGo);
            if (_playerGo != null) Object.Destroy(_playerGo);
            if (_vehicleGo != null) Object.Destroy(_vehicleGo);
            
            // Cleanup Singleton
            var existingGsm = FindFirstObjectByType<GameStateManager>();
            if (existingGsm != null) Object.Destroy(existingGsm.gameObject);
        }

        private IEnumerator Test_Death_Triggers_Restart()
        {
            Debug.Log("Running Test: Death Terminal Outcome");
            Setup();
            yield return null;

            bool restartCalled = false;
            _gsm.OnRestartFromBeginningRequested += () => restartCalled = true;

            _playerController.ApplyDamage(1000); // Trigger death

            yield return new WaitForSeconds(0.1f);

            if (restartCalled && _sessionData.currentSessionState == SessionState.Active) // GSM restarts back to Active
                Debug.Log("[PASS] Death triggered smart restart.");
            else
                Debug.LogError($"[FAIL] Death did not trigger restart. State: {_sessionData.currentSessionState}");

            Teardown();
        }

        private IEnumerator Test_Busted_Triggers_Restart()
        {
            Debug.Log("Running Test: Busted Terminal Outcome");
            Setup();
            yield return null;

            bool restartCalled = false;
            _gsm.OnRestartFromBeginningRequested += () => restartCalled = true;

            _playerController.ArrestPlayer();

            yield return new WaitForSeconds(0.1f);

            if (restartCalled)
                Debug.Log("[PASS] Busted triggered smart restart.");
            else
                Debug.LogError("[FAIL] Busted did not trigger restart.");

            Teardown();
        }

        private IEnumerator Test_Vehicle_Loss_Triggers_Restart_If_Required()
        {
            Debug.Log("Running Test: Vehicle Loss Outcome");
            Setup();
            _handler.isVehicleRequired = true;
            yield return null;

            bool failureEmitted = false;
            _handler.OnEscapeStartFailed += (outcome) => { if (outcome == FailureOutcome.VehicleLost) failureEmitted = true; };

            _vehicleController.ApplyDamage(1000); // Disable vehicle

            yield return new WaitForSeconds(0.1f);

            if (failureEmitted)
                Debug.Log("[PASS] Vehicle loss triggered failure.");
            else
                Debug.LogError("[FAIL] Vehicle loss ignored.");

            Teardown();
        }

        private IEnumerator Test_Idempotency_Only_One_Restart()
        {
            Debug.Log("Running Test: Idempotency");
            Setup();
            yield return null;

            int restartCount = 0;
            _gsm.OnRestartFromBeginningRequested += () => restartCount++;

            _playerController.ApplyDamage(1000);
            _playerController.ArrestPlayer(); // Double terminal

            yield return new WaitForSeconds(0.1f);

            if (restartCount == 1)
                Debug.Log("[PASS] Only one restart triggered.");
            else
                Debug.LogError($"[FAIL] Multiple restarts triggered: {restartCount}");

            Teardown();
        }

        private IEnumerator Test_Smart_Restart_Uses_Checkpoint_If_Exists()
        {
            Debug.Log("Running Test: Checkpoint Routing");
            Setup();
            // Simulate having a checkpoint
            _sessionData.completedCheckpoints.Add("Bank_Vault_Checkpoint");
            
            yield return null;

            bool checkpointRestartCalled = false;
            _gsm.OnRestartFromCheckpointRequested += () => checkpointRestartCalled = true;

            _playerController.ApplyDamage(1000);

            yield return new WaitForSeconds(0.1f);

            if (checkpointRestartCalled)
                Debug.Log("[PASS] Smart restart prioritized checkpoint.");
            else
                Debug.LogError("[FAIL] Smart restart ignored checkpoint!");

            Teardown();
        }
    }
}

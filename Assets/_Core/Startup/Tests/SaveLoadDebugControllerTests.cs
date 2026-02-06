using System.Collections;
using UnityEngine;
using Core.Startup;
using Data.Save.Repository;
using Core.GameState;

namespace Core.Startup.Tests
{
    public class SaveLoadDebugControllerTests : MonoBehaviour
    {
        private GameObject _mgrGo;
        private SaveLoadDebugController _debugController;
        private SaveSlot0Repository _repo;

        private void Start()
        {
            StartCoroutine(RunTests());
        }

        private IEnumerator RunTests()
        {
            Debug.Log(">>> STARTING DEBUG UTILITY TESTS <<<");
            
            _repo = new SaveSlot0Repository();
            _repo.DeleteSlot0("DevTestPlayer");

            yield return Test_PrintSaveState_Outputs_Correctly();
            yield return Test_ClearSlot0_Physically_Deletes_File();
            yield return Test_ForceFailure_Triggers_Resolver();

            Debug.Log(">>> DEBUG UTILITY TESTS COMPLETED <<<");
        }

        private void Setup()
        {
            _mgrGo = new GameObject("Debug_Test");
            _debugController = _mgrGo.AddComponent<SaveLoadDebugController>();
            _debugController.targetPlayerId = "DevTestPlayer";
        }

        private void Teardown()
        {
            if (_mgrGo != null) Object.Destroy(_mgrGo);
            _repo.DeleteSlot0("DevTestPlayer");
        }

        private IEnumerator Test_PrintSaveState_Outputs_Correctly()
        {
            Debug.Log("Running Test: Print Save State");
            Setup();

            // 1. Test Empty
            _debugController.PrintSaveState();
            // Verification is visual/log-based, but we ensure no crash.
            Debug.Log("[PASS] PrintSaveState handled empty slot without crash.");

            // 2. Test with Data
            _repo.WriteSlot0("{\"checkpointId\":\"CP_LOG_TEST\"}", "DevTestPlayer");
            _debugController.PrintSaveState();
            Debug.Log("[PASS] PrintSaveState handled populated slot without crash.");

            Teardown();
            yield return null;
        }

        private IEnumerator Test_ClearSlot0_Physically_Deletes_File()
        {
            Debug.Log("Running Test: Clear Slot 0");
            Setup();

            _repo.WriteSlot0("{}", "DevTestPlayer");
            if (!_repo.DoesSlot0Exist("DevTestPlayer"))
            {
                Debug.LogError("[FAIL] Failed to create test save file!");
                yield break;
            }

            _debugController.ClearSlot0();

            if (!_repo.DoesSlot0Exist("DevTestPlayer"))
                Debug.Log("[PASS] Debug controller physically deleted the save file.");
            else
                Debug.LogError("[FAIL] Save file still exists after clear command!");

            Teardown();
            yield return null;
        }

        private IEnumerator Test_ForceFailure_Triggers_Resolver()
        {
            Debug.Log("Running Test: Force Failure Triggers Resolver");
            Setup();

            // We need a resolver to catch the signal
            var resGo = new GameObject("Resolver_Mock");
            var resolver = resGo.AddComponent<RestartResolver>();
            
            bool deathRequested = false;
            RestartResolver.OnRestartFromBeginningRequested += () => deathRequested = true;

            _debugController.ForceDeath();

            yield return new WaitForSeconds(0.1f);

            if (deathRequested)
                Debug.Log("[PASS] ForceDeath signal correctly triggered the failure pipeline.");
            else
                Debug.LogError("[FAIL] Failure pipeline was not triggered by debug command!");

            Object.Destroy(resGo);
            Teardown();
        }
    }
}

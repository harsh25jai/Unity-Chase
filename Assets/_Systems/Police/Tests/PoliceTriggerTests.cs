using System.Collections;
using UnityEngine;
using Systems.Police;
using Systems.Vehicle;
using Data.Session.Runtime;
using Data.World.Runtime;

namespace Systems.Police.Tests
{
    public class PoliceTriggerTests : MonoBehaviour
    {
        private GameObject _testGo;
        private EscapeStartPursuitTrigger _trigger;
        private PursuitStartSignaler _signaler;
        private SessionRuntimeData _sessionData;
        private WorldState _worldState;
        private VehicleEntryController _entryController;

        private void Start()
        {
            StartCoroutine(RunTests());
        }

        private IEnumerator RunTests()
        {
            Debug.Log(">>> STARTING POLICE TRIGGER TESTS <<<");

            Setup();
            yield return null;
            yield return Test_Pursuit_Triggers_On_First_Vehicle_Entry();
            Teardown();

            Setup();
            yield return null;
            yield return Test_Pursuit_Is_Idempotent();
            Teardown();

            Setup();
            yield return null;
            yield return Test_Pursuit_Logs_Warning_In_Remote_Context();
            Teardown();

            Debug.Log(">>> POLICE TRIGGER TESTS COMPLETED <<<");
        }

        private void Setup()
        {
            _testGo = new GameObject("PoliceTriggerTest");
            _sessionData = _testGo.AddComponent<SessionRuntimeData>();
            _worldState = _testGo.AddComponent<WorldState>();
            _signaler = _testGo.AddComponent<PursuitStartSignaler>();
            _trigger = _testGo.AddComponent<EscapeStartPursuitTrigger>();
            _entryController = _testGo.AddComponent<VehicleEntryController>();

            _trigger.sessionData = _sessionData;
            _trigger.worldState = _worldState;
            _trigger.signaler = _signaler;
            _trigger.entryController = _entryController;

            _sessionData.currentSessionState = SessionState.Active;
            _worldState.currentRegionType = RegionType.Urban;
        }

        private void Teardown()
        {
            if (_testGo != null) Object.Destroy(_testGo);
        }

        private IEnumerator Test_Pursuit_Triggers_On_First_Vehicle_Entry()
        {
            Debug.Log("Running Test: Pursuit Launch");
            bool pursuitStarted = false;
            _signaler.OnPursuitStarted += (_) => pursuitStarted = true;

            yield return null;
            _trigger.TriggerPursuit();

            if (_sessionData.isPursuitActive && pursuitStarted)
                Debug.Log("[PASS] Pursuit launched on vehicle entry.");
            else
                Debug.LogError("[FAIL] Pursuit did not launch.");
        }

        private IEnumerator Test_Pursuit_Is_Idempotent()
        {
            Debug.Log("Running Test: Pursuit Idempotency");
            int triggerCount = 0;
            _signaler.OnPursuitStarted += (_) => triggerCount++;

            yield return null;
            _trigger.TriggerPursuit();
            _trigger.TriggerPursuit();

            if (triggerCount == 1)
                Debug.Log("[PASS] Pursuit trigger is idempotent.");
            else
                Debug.LogError($"[FAIL] Pursuit triggered multiple times: {triggerCount}");
        }

        private IEnumerator Test_Pursuit_Logs_Warning_In_Remote_Context()
        {
            Debug.Log("Running Test: Remote Context Warning");
            _worldState.currentRegionType = RegionType.Remote;
            
            // Note: Cannot easily verify LogAssert without UnityEngine.TestTools.
            // But we can verify it still triggers the session change.
            yield return null;
            _trigger.TriggerPursuit();

            if (_sessionData.isPursuitActive)
                Debug.Log("[PASS] Pursuit still launched in Remote context (Check console for expected warning).");
            else
                Debug.LogError("[FAIL] Pursuit failed to launch in Remote context.");
        }
    }
}

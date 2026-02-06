using System.Collections;
using UnityEngine;
using Systems.World;
using Systems.Vehicle;
using Data.Session.Runtime;
using Data.World.Runtime;
using Data.Vehicle.Runtime;
using Core.GameState;

namespace Systems.World.Tests
{
    public class DistanceTrackingTests : MonoBehaviour
    {
        private GameObject _testGo;
        private GameObject _vehicleGo;
        private SessionDistanceTracker _tracker;
        private VehicleMotionGate _gate;
        private SessionRuntimeData _sessionData;
        private WorldState _worldState;
        private VehicleEntryController _entryController;

        private void Start()
        {
            StartCoroutine(RunTests());
        }

        private IEnumerator RunTests()
        {
            Debug.Log(">>> STARTING DISTANCE TRACKING TESTS <<<");

            Setup();
            yield return null;
            yield return Test_No_Tracking_On_Foot();
            Teardown();

            Setup();
            yield return null;
            yield return Test_Tracking_Increases_Distance_In_Vehicle();
            Teardown();

            Setup();
            yield return null;
            yield return Test_Jitter_Is_Filtered();
            Teardown();

            Setup();
            yield return null;
            yield return Test_Cutoff_On_Session_End();
            Teardown();

            Debug.Log(">>> DISTANCE TRACKING TESTS COMPLETED <<<");
        }

        private void Setup()
        {
            _testGo = new GameObject("DistanceTest");
            _sessionData = _testGo.AddComponent<SessionRuntimeData>();
            _worldState = _testGo.AddComponent<WorldState>();
            _gate = _testGo.AddComponent<VehicleMotionGate>();
            _tracker = _testGo.AddComponent<SessionDistanceTracker>();
            _entryController = _testGo.AddComponent<VehicleEntryController>();

            _tracker.sessionData = _sessionData;
            _tracker.worldState = _worldState;
            _tracker.motionGate = _gate;
            _tracker.entryController = _entryController;
            _tracker.updateRate = 0.01f; // Fast updates for tests

            _vehicleGo = new GameObject("TestVehicle");
            var vData = _vehicleGo.AddComponent<VehicleRuntimeData>();
            vData.vehicleID = "Car_Distance_Test";

            _testGo.transform.position = Vector3.zero;
            _vehicleGo.transform.position = Vector3.zero;
        }

        private void Teardown()
        {
            if (_testGo != null) Object.Destroy(_testGo);
            if (_vehicleGo != null) Object.Destroy(_vehicleGo);
        }

        private IEnumerator Test_No_Tracking_On_Foot()
        {
            Debug.Log("Running Test: No Tracking On Foot");
            
            // Move "player" (testGo)
            _testGo.transform.position = new Vector3(10, 0, 0);

            yield return new WaitForSeconds(0.2f);

            if (_tracker.sessionTotalDistance == 0)
                Debug.Log("[PASS] Distance remained 0 while on foot.");
            else
                Debug.LogError($"[FAIL] Distance increased on foot: {_tracker.sessionTotalDistance}");
        }

        private IEnumerator Test_Tracking_Increases_Distance_In_Vehicle()
        {
            Debug.Log("Running Test: Tracking In Vehicle");
            
            // Enter Vehicle (Direct call or simulate event)
            // Trigger's internal lookup will find _vehicleGo
            _tracker.HandleVehicleEntered("Player_0", "Car_Distance_Test");

            yield return null;

            // Move Vehicle
            _vehicleGo.transform.position = new Vector3(5, 0, 0);

            yield return new WaitForSeconds(0.2f);

            if (_tracker.sessionTotalDistance >= 4.9f) // threshold check
                Debug.Log("[PASS] Distance tracked monotonically.");
            else
                Debug.LogError($"[FAIL] Distance not tracked. Value: {_tracker.sessionTotalDistance}");
        }

        private IEnumerator Test_Jitter_Is_Filtered()
        {
            Debug.Log("Running Test: Jitter Filtering");
            _tracker.HandleVehicleEntered("Player_0", "Car_Distance_Test");
            _gate.movementThreshold = 0.5f;

            yield return null;

            // Move slightly
            _vehicleGo.transform.position = new Vector3(0.1f, 0, 0);

            yield return new WaitForSeconds(0.2f);

            if (_tracker.sessionTotalDistance == 0)
                Debug.Log("[PASS] Jitter was filtered out.");
            else
                Debug.LogError($"[FAIL] Jitter was tracked incorrectly: {_tracker.sessionTotalDistance}");
        }

        private IEnumerator Test_Cutoff_On_Session_End()
        {
            Debug.Log("Running Test: Session Cutoff");
            _tracker.HandleVehicleEntered("Player_0", "Car_Distance_Test");
            
            yield return null;

            _tracker.SendMessage("HandleSessionEnded");
            
            // Move vehicle after end
            _vehicleGo.transform.position = new Vector3(100, 0, 0);

            yield return new WaitForSeconds(0.2f);

            if (_tracker.sessionTotalDistance == 0)
                Debug.Log("[PASS] Tracking paused on session end.");
            else
                Debug.LogError($"[FAIL] Tracking continued after session end!");
        }
    }
}

using System.Collections;
using UnityEngine;
using Systems.Vehicle;
using Data.Vehicle.Runtime;
using Data.Player.Runtime;

namespace Systems.Vehicle.Tests
{
    public class VehicleEntryTests : MonoBehaviour
    {
        private GameObject _playerGo;
        private GameObject _vehicleGo;
        private VehicleRuntimeData _vehicleData;
        private PlayerRuntimeData _playerData;
        private VehicleDiscoveryController _discovery;
        private VehicleEntryController _entry;
        private VehicleOccupancyBinder _binder;

        private void Start()
        {
            StartCoroutine(RunTests());
        }

        private IEnumerator RunTests()
        {
            Debug.Log(">>> STARTING VEHICLE ENTRY TESTS <<<");

            Setup();
            yield return null;
            yield return Test_Vehicle_Is_Discovered_When_In_Range();
            Teardown();

            Setup();
            yield return null;
            yield return Test_Entry_Request_Updates_Data_State();
            Teardown();

            Setup();
            yield return null;
            yield return Test_Disabled_Vehicle_Rejects_Entry();
            Teardown();

            Setup();
            yield return null;
            yield return Test_Exit_Request_Resets_Data_State();
            Teardown();

            Debug.Log(">>> VEHICLE ENTRY TESTS COMPLETED <<<");
        }

        private void Setup()
        {
            _playerGo = new GameObject("Player");
            _playerGo.AddComponent<CapsuleCollider>();
            _playerData = _playerGo.AddComponent<PlayerRuntimeData>();
            _playerData.playerID = "Player1";
            
            _discovery = _playerGo.AddComponent<VehicleDiscoveryController>();
            _entry = _playerGo.AddComponent<VehicleEntryController>();
            _binder = _playerGo.AddComponent<VehicleOccupancyBinder>();

            _entry.discoveryController = _discovery;
            _entry.playerData = _playerData;
            _binder.entryController = _entry;
            _binder.playerData = _playerData;

            _vehicleGo = new GameObject("Vehicle");
            var box = _vehicleGo.AddComponent<BoxCollider>();
            box.isTrigger = true;
            _vehicleData = _vehicleGo.AddComponent<VehicleRuntimeData>();
            _vehicleData.vehicleID = "Car_A";
            _vehicleData.currentDegradationState = VehicleDegradationState.Operational;

            _playerGo.transform.position = Vector3.zero;
            _vehicleGo.transform.position = new Vector3(2, 0, 0);
        }

        private void Teardown()
        {
            if (_playerGo != null) Object.Destroy(_playerGo);
            if (_vehicleGo != null) Object.Destroy(_vehicleGo);
        }

        private IEnumerator Test_Vehicle_Is_Discovered_When_In_Range()
        {
            Debug.Log("Running Test: Vehicle Discovery");
            yield return null;

            if (_discovery.currentTargetVehicle != null && _discovery.currentTargetVehicle.vehicleID == "Car_A")
                Debug.Log("[PASS] Vehicle discovered in range.");
            else
                Debug.LogError("[FAIL] Vehicle discovery failed.");
        }

        private IEnumerator Test_Entry_Request_Updates_Data_State()
        {
            Debug.Log("Running Test: Vehicle Entry State");
            yield return null;
            _entry.RequestInteract();
            yield return null;

            if (_playerData.isInVehicle && _playerData.currentVehicleID == "Car_A" && _vehicleData.driverPlayerID == "Player1")
                Debug.Log("[PASS] Entry state updated correctly.");
            else
                Debug.LogError("[FAIL] Entry state not updated.");
        }

        private IEnumerator Test_Disabled_Vehicle_Rejects_Entry()
        {
            Debug.Log("Running Test: Disabled Vehicle Rejection");
            _vehicleData.currentDegradationState = VehicleDegradationState.Disabled;
            yield return null;
            _entry.RequestInteract();
            yield return null;

            if (!_playerData.isInVehicle)
                Debug.Log("[PASS] Entry rejected for disabled vehicle.");
            else
                Debug.LogError("[FAIL] Player entered disabled vehicle!");
        }

        private IEnumerator Test_Exit_Request_Resets_Data_State()
        {
            Debug.Log("Running Test: Vehicle Exit State");
            yield return null;
            _entry.RequestInteract();
            yield return null;
            
            _entry.RequestInteract();
            yield return null;

            if (!_playerData.isInVehicle && string.IsNullOrEmpty(_playerData.currentVehicleID))
                Debug.Log("[PASS] Exit state reset correctly.");
            else
                Debug.LogError("[FAIL] Exit state failed to reset.");
        }
    }
}

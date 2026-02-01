using System;
using System.Collections;
using UnityEngine;
using Systems.Vehicle;
using Data.Vehicle.Runtime;

namespace Tests.Vehicle
{
    public class VehicleStateControllerTests : MonoBehaviour
    {
        private GameObject _vehicleObject;
        private VehicleStateController _stateController;
        private VehicleRuntimeData _runtimeData;

        private void Start()
        {
            StartCoroutine(RunTests());
        }

        private IEnumerator RunTests()
        {
            Debug.Log(">>> STARTING VEHICLE STATE CONTROLLER TESTS <<<");

            Setup();
            yield return null;

            yield return Test_ZoneDamage_Operational();
            ResetState();

            yield return Test_CriticalThreshold();
            ResetState();

            yield return Test_DisabledState();
            ResetState();

            yield return Test_RapidDamageStability();
            ResetState();

            yield return Test_MultiVehicleIsolation();

            Debug.Log(">>> TESTS COMPLETED <<<");
        }

        private void Setup()
        {
            _vehicleObject = new GameObject("TestVehicle");
            _runtimeData = _vehicleObject.AddComponent<VehicleRuntimeData>();
            _stateController = _vehicleObject.AddComponent<VehicleStateController>();

            // Init Data
            _runtimeData.maxHealth = 1000f;
            _runtimeData.currentHealth = 1000f;
            _runtimeData.currentDegradationState = VehicleDegradationState.Operational;
            _runtimeData.vehicleID = "Vehicle_1";
            _runtimeData.zoneHealths.Clear();
        }

        private void ResetState()
        {
            _runtimeData.currentHealth = 1000f;
            _runtimeData.currentDegradationState = VehicleDegradationState.Operational;
            _runtimeData.zoneHealths.Clear();
            Debug.Log("--- State Reset ---");
        }

        private IEnumerator Test_ZoneDamage_Operational()
        {
            Debug.Log("Running Test: Zone Damage & Operational State");
            float damage = 100f; // 10% damage
            
            _stateController.ApplyZoneDamage("Body", damage);
            yield return null;

            bool zoneUpdated = _runtimeData.zoneHealths.ContainsKey("Body") && _runtimeData.zoneHealths["Body"] < 100f;
            bool healthUpdated = _runtimeData.currentHealth == 900f;
            bool stateOperational = _runtimeData.currentDegradationState == VehicleDegradationState.Operational; // > 75%

            if (zoneUpdated && healthUpdated && stateOperational)
                Debug.Log("[PASS] Zone damage applied, integrity updated, state remains Operational.");
            else
                Debug.LogError($"[FAIL] Zone Test. ZoneUpdated: {zoneUpdated}, Health: {_runtimeData.currentHealth}, State: {_runtimeData.currentDegradationState}");
        }

        private IEnumerator Test_CriticalThreshold()
        {
            Debug.Log("Running Test: Critical Threshold");
            // Set near Critical (40% is threshold). 
            _runtimeData.currentHealth = 410f; // 41%
            _stateController.ApplyDamage(0f); // Force eval? No, ApplyDamage triggers eval.
            yield return null;
            
            // Should be Degraded currently (Wait, 41% is < 75% so Degraded)
            
            // Apply damage to cross 40% (Critical)
            bool eventFired = false;
            _stateController.OnDegradationStateChanged += (state) => { if (state == VehicleDegradationState.Critical) eventFired = true; };

            _stateController.ApplyZoneDamage("Engine", 20f); // Drops to 390 (39%)
            yield return null;

            if (_runtimeData.currentDegradationState == VehicleDegradationState.Critical && eventFired)
                Debug.Log("[PASS] Transitioned to Critical on threshold cross.");
            else
                Debug.LogError($"[FAIL] Critical Test. State: {_runtimeData.currentDegradationState}, Event: {eventFired}");
        }

        private IEnumerator Test_DisabledState()
        {
            Debug.Log("Running Test: Disabled State");
            _runtimeData.currentHealth = 50f;
            _runtimeData.currentDegradationState = VehicleDegradationState.Critical;
            bool eventFired = false;
            _stateController.OnVehicleDisabled += () => eventFired = true;

            // Final blow
            _stateController.ApplyDamage(50f); // 0 Health
            yield return null;

            bool isDisabled = _runtimeData.currentDegradationState == VehicleDegradationState.Disabled;
            
            // Try damage more
            _stateController.ApplyDamage(100f);
            yield return null;

            // Should ignore
            bool damageIgnored = _runtimeData.currentHealth == 0f;

            if (isDisabled && eventFired && damageIgnored)
                Debug.Log("[PASS] Vehicle Disabled and subsequent damage ignored.");
            else
                Debug.LogError($"[FAIL] Disabled Test. State: {_runtimeData.currentDegradationState}, Health: {_runtimeData.currentHealth}");
        }

        private IEnumerator Test_RapidDamageStability()
        {
            Debug.Log("Running Test: Rapid Damage Stability");
            float startHealth = _runtimeData.currentHealth;
            
            // Simulate 10 rapid hits
            for(int i=0; i<10; i++)
            {
                _stateController.ApplyDamage(10f);
            }
            yield return null;

            float expected = startHealth - 100f;
            if (Mathf.Approximately(_runtimeData.currentHealth, expected))
                Debug.Log("[PASS] Rapid damage accumulated correctly without oscillation.");
            else
                Debug.LogError($"[FAIL] Rapid Stability. Expected {expected}, Got {_runtimeData.currentHealth}");
        }

        private IEnumerator Test_MultiVehicleIsolation()
        {
            Debug.Log("Running Test: Multi-Vehicle Isolation");
            
            GameObject v2Obj = new GameObject("TestVehicle_2");
            var v2Data = v2Obj.AddComponent<VehicleRuntimeData>();
            var v2Ctrl = v2Obj.AddComponent<VehicleStateController>();
            v2Data.maxHealth = 1000f;
            v2Data.currentHealth = 1000f;
            
            yield return null;

            _stateController.ApplyDamage(500f);
            yield return null;

            if (_runtimeData.currentHealth == 500f && v2Data.currentHealth == 1000f)
                Debug.Log("[PASS] Vehicle 1 damage did not affect Vehicle 2.");
            else
                Debug.LogError($"[FAIL] Isolation. V1: {_runtimeData.currentHealth}, V2: {v2Data.currentHealth}");

            Destroy(v2Obj);
            Destroy(_vehicleObject);
        }
    }
}

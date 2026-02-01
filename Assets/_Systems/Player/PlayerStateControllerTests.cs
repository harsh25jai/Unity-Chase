using System;
using System.Collections;
using UnityEngine;
using Systems.Player;
using Data.Player.Runtime;

namespace Tests.Player
{
    public class PlayerStateControllerTests : MonoBehaviour
    {
        private GameObject _playerObject;
        private PlayerStateController _stateController;
        private PlayerRuntimeData _runtimeData;

        private void Start()
        {
            StartCoroutine(RunTests());
        }

        private IEnumerator RunTests()
        {
            Debug.Log(">>> STARTING PLAYER STATE CONTROLLER TESTS <<<");

            Setup();
            yield return null;

            yield return Test_NormalDamage();
            ResetState();

            yield return Test_LethalDamage();
            ResetState();

            yield return Test_BustedImmunity();
            ResetState();

            yield return Test_ControlLockDamage();
            ResetState();

            yield return Test_MultiplayerIsolation();

            Debug.Log(">>> TESTS COMPLETED <<<");
        }

        private void Setup()
        {
            _playerObject = new GameObject("TestPlayer");
            _runtimeData = _playerObject.AddComponent<PlayerRuntimeData>();
            _stateController = _playerObject.AddComponent<PlayerStateController>();

            // Setup Data
            _runtimeData.maxHealth = 100f;
            _runtimeData.currentHealth = 100f;
            _runtimeData.currentSurvivalState = SurvivalState.Alive;
            _runtimeData.playerID = "Player_1";
            
            // Re-initialize controller logic manually if Start() missed timing (though standard AddComponent calls Awake/Start)
        }

        private void ResetState()
        {
            _runtimeData.currentHealth = 100f;
            _runtimeData.currentSurvivalState = SurvivalState.Alive;
            _stateController.SetControlLock(false);
            Debug.Log("--- State Reset ---");
        }

        private IEnumerator Test_NormalDamage()
        {
            Debug.Log("Running Test: Normal Damage");
            float initialHealth = _runtimeData.currentHealth;
            float damage = 20f;
            bool eventFired = false;

            _stateController.OnHealthChanged += (current, max) => 
            {
                if (Mathf.Approximately(current, initialHealth - damage)) eventFired = true;
            };

            _stateController.ApplyDamage(damage);
            yield return null;

            if (_runtimeData.currentHealth == 80f && eventFired)
                Debug.Log("[PASS] Normal Damage applied correctly.");
            else
                Debug.LogError($"[FAIL] Normal Damage failed. Health: {_runtimeData.currentHealth}, Event: {eventFired}");
        }

        private IEnumerator Test_LethalDamage()
        {
            Debug.Log("Running Test: Lethal Damage");
            _runtimeData.currentHealth = 10f;
            bool deathEventFired = false;

            _stateController.OnPlayerDeathRequested += () => deathEventFired = true;

            _stateController.ApplyDamage(20f);
            yield return null;

            if (_runtimeData.currentHealth == 0f && 
                _runtimeData.currentSurvivalState == SurvivalState.Dead && 
                deathEventFired)
                Debug.Log("[PASS] Lethal Damage triggers Death state.");
            else
                Debug.LogError($"[FAIL] Lethal Damage failed. Health: {_runtimeData.currentHealth}, State: {_runtimeData.currentSurvivalState}");
        }

        private IEnumerator Test_BustedImmunity()
        {
            Debug.Log("Running Test: Busted Immunity");
            _stateController.ArrestPlayer(); // Set to Busted
            yield return null;

            float healthBefore = _runtimeData.currentHealth;
            bool deathEventFired = false;
            _stateController.OnPlayerDeathRequested += () => deathEventFired = true;

            _stateController.ApplyDamage(500f);
            yield return null;

            if (_runtimeData.currentHealth == healthBefore && 
                _runtimeData.currentSurvivalState == SurvivalState.Busted && 
                !deathEventFired)
                Debug.Log("[PASS] Busted state is immune to damage.");
            else
                Debug.LogError($"[FAIL] Busted Immunity failed. Health: {_runtimeData.currentHealth}");
        }

        private IEnumerator Test_ControlLockDamage()
        {
            Debug.Log("Running Test: Control Lock Damage");
            _stateController.SetControlLock(true);
            float initialHealth = _runtimeData.currentHealth;

            _stateController.ApplyDamage(10f);
            yield return null;

            if (_runtimeData.currentHealth < initialHealth) 
                Debug.Log("[PASS] Damage received during Control Lock.");
            else
                Debug.LogError("[FAIL] Damage ignored during Control Lock.");
        }

        private IEnumerator Test_MultiplayerIsolation()
        {
            Debug.Log("Running Test: Multiplayer Isolation");
            
            // Create second player
            GameObject p2Obj = new GameObject("TestPlayer_2");
            var p2Data = p2Obj.AddComponent<PlayerRuntimeData>();
            var p2Controller = p2Obj.AddComponent<PlayerStateController>();
            
            p2Data.playerID = "Player_2";
            p2Data.currentHealth = 100f;
            p2Data.currentSurvivalState = SurvivalState.Alive;

            yield return null; // let Start() run

            // Damage Player 1
            _stateController.ApplyDamage(50f);
            yield return null;

            if (_runtimeData.currentHealth == 50f && p2Data.currentHealth == 100f)
                Debug.Log("[PASS] Player 1 damage did not affect Player 2.");
            else
                Debug.LogError($"[FAIL] Isolation failed. P1: {_runtimeData.currentHealth}, P2: {p2Data.currentHealth}");

            // Cleanup
            Destroy(p2Obj);
            Destroy(_playerObject);
        }
    }
}

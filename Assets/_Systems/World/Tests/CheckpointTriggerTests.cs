using System;
using System.Collections;
using UnityEngine;
using Systems.World;
using Data.World.Checkpoint;
using Data.Player.Runtime;

namespace Systems.World.Tests
{
    public class CheckpointTriggerTests : MonoBehaviour
    {
        private void Start()
        {
            StartCoroutine(RunTests());
        }

        private IEnumerator RunTests()
        {
            Debug.Log(">>> STARTING CHECKPOINT TRIGGER TESTS <<<");

            yield return Test_Single_Fire_Emission();
            yield return Test_Deduplication_Cooldown();
            yield return Test_Invalid_Payload_Handling();

            Debug.Log(">>> CHECKPOINT TRIGGER TESTS COMPLETED <<<");
        }

        private IEnumerator Test_Single_Fire_Emission()
        {
            Debug.Log("Running Test: Single Fire Emission");
            
            var triggerGo = new GameObject("TestTrigger");
            var trigger = triggerGo.AddComponent<CheckpointWorldTrigger>();
            trigger.checkpointId = "CP_ALPHA";
            var col = triggerGo.GetComponent<BoxCollider>();
            col.size = Vector3.one * 10f;

            var playerGo = new GameObject("Player");
            playerGo.tag = "Player";
            playerGo.AddComponent<PlayerRuntimeData>();
            playerGo.AddComponent<BoxCollider>(); // Needed for trigger
            var rb = playerGo.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            int triggerCount = 0;
            CheckpointWorldTrigger.OnCheckpointTriggered += (ev) => { if(ev.checkpointId == "CP_ALPHA") triggerCount++; };

            // Simulate entrance
            playerGo.transform.position = triggerGo.transform.position;
            
            yield return new WaitForSeconds(0.2f);

            if (triggerCount == 1)
                Debug.Log("[PASS] Trigger emitted event exactly once.");
            else
                Debug.LogError($"[FAIL] Trigger emitted {triggerCount} times!");

            UnityEngine.Object.Destroy(triggerGo);
            UnityEngine.Object.Destroy(playerGo);
        }

        private IEnumerator Test_Deduplication_Cooldown()
        {
            Debug.Log("Running Test: Deduplication Cooldown");
            
            var triggerGo = new GameObject("TestTrigger");
            var trigger = triggerGo.AddComponent<CheckpointWorldTrigger>();
            trigger.checkpointId = "CP_BETA";
            trigger.cooldownSeconds = 5.0f;

            var playerGo = new GameObject("Player");
            playerGo.tag = "Player";
            playerGo.AddComponent<PlayerRuntimeData>();

            int triggerCount = 0;
            Action<CheckpointTriggerEvent> handler = (ev) => { if(ev.checkpointId == "CP_BETA") triggerCount++; };
            CheckpointWorldTrigger.OnCheckpointTriggered += handler;

            // Trigger 1
            trigger.SendMessage("OnTriggerEnter", playerGo.GetComponent<Collider>() ?? playerGo.AddComponent<BoxCollider>());
            // Trigger 2 (Rapid)
            trigger.SendMessage("OnTriggerEnter", playerGo.GetComponent<Collider>());

            yield return null;

            if (triggerCount == 1)
                Debug.Log("[PASS] Rapid triggers were deduplicated by cooldown.");
            else
                Debug.LogError($"[FAIL] Rapid triggers fired {triggerCount} times!");

            CheckpointWorldTrigger.OnCheckpointTriggered -= handler;
            UnityEngine.Object.Destroy(triggerGo);
            UnityEngine.Object.Destroy(playerGo);
        }

        private IEnumerator Test_Invalid_Payload_Handling()
        {
            Debug.Log("Running Test: Invalid Payload Validation");
            
            var payload = new CheckpointTriggerEvent(""); // Empty ID
            
            if (!payload.IsValid())
            {
                Debug.Log("[PASS] Payload correctly identified as invalid.");
            }
            else
            {
                Debug.LogError("[FAIL] Payload with empty ID marked as valid!");
            }
            yield return null;
        }
    }
}

using System.Collections;
using UnityEngine;
using Data.Save.Checkpoint;
using Data.Session.Runtime;

namespace Data.Save.Checkpoint.Tests
{
    public class CheckpointSchemaTests : MonoBehaviour
    {
        private void Start()
        {
            StartCoroutine(RunTests());
        }

        private IEnumerator RunTests()
        {
            Debug.Log(">>> STARTING CHECKPOINT SCHEMA TESTS <<<");

            Test_Serialization_Integrity();
            Test_Validation_Rejects_Incomplete_Record();
            Test_Id_Persistence();

            Debug.Log(">>> CHECKPOINT SCHEMA TESTS COMPLETED <<<");
            yield return null;
        }

        private void Test_Serialization_Integrity()
        {
            Debug.Log("Running Test: Serialization Integrity");
            
            CheckpointRecord record = new CheckpointRecord();
            record.checkpointId = "Test_CP_01";
            record.sceneName = "EscapeStart";
            record.money = 500.50f;
            record.survivalNeeds.hunger = 80f;
            record.tiresFlags.Add("FrontLeft_Flat");

            string json = JsonUtility.ToJson(record);
            CheckpointRecord restored = JsonUtility.FromJson<CheckpointRecord>(json);

            if (restored.checkpointId == record.checkpointId && 
                restored.money == record.money && 
                restored.survivalNeeds.hunger == record.survivalNeeds.hunger &&
                restored.tiresFlags.Count == 1)
            {
                Debug.Log("[PASS] Serialization matched exactly.");
            }
            else
            {
                Debug.LogError("[FAIL] Serialization mismatch!");
            }
        }

        private void Test_Validation_Rejects_Incomplete_Record()
        {
            Debug.Log("Running Test: Validation logic");
            
            CheckpointRecord record = new CheckpointRecord();
            // Missing checkpointId and sceneName

            if (!record.IsValid())
            {
                Debug.Log("[PASS] Invalid record rejected successfully.");
            }
            else
            {
                Debug.LogError("[FAIL] Incomplete record marked as valid!");
            }
        }

        private void Test_Id_Persistence()
        {
            Debug.Log("Running Test: Metadata Persistence");
            
            CheckpointRecord record = new CheckpointRecord();
            record.checkpointId = "BANK_VAULT_EXIT";
            record.sceneName = "BankPrologue";
            record.isUrban = true;
            record.playerId = "Player_0";

            string json = JsonUtility.ToJson(record);
            CheckpointRecord restored = JsonUtility.FromJson<CheckpointRecord>(json);

            if (restored.isUrban == true && restored.playerId == "Player_0")
            {
                Debug.Log("[PASS] Metadata flags persisted.");
            }
            else
            {
                Debug.LogError("[FAIL] Metadata flags lost!");
            }
        }
    }
}
